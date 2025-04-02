using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json;

/// <summary>
/// 百度语音识别组件
/// </summary>
public class BaiduSpeechRecognizer : MonoBehaviour
{
    private static BaiduSpeechRecognizer _instance;

    public static BaiduSpeechRecognizer Instance
    {
        get { return _instance; }
    }

    [Header("百度语音API参数")]
    [SerializeField] private string appID = ""; // 填写你的App ID
    [SerializeField] private string apiKey = ""; // 填写你的Api Key
    [SerializeField] private string secretKey = ""; // 填写你的Secret Key

    [Header("录音参数")]
    [SerializeField] private int recordingFrequency = 16000; // 录音频率，百度ASR要求16k采样
    [SerializeField] private int recordingLength = 10; // 最大录音时长（秒）

    private AudioClip recordingClip;
    private bool isRecording = false;
    private string accessToken;
    private DateTime tokenExpirationTime;
    private int startPosition = 0;
    private int endPosition = 0;

    // 语音识别事件
    public event Action<string> OnPartialResult; // 实时识别结果
    public event Action<string> OnFinalResult; // 最终识别结果

    // 录音状态改变事件
    public event Action<bool> OnRecordingStatusChanged;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化时获取访问令牌
        StartCoroutine(GetAccessToken());
    }

    /// <summary>
    /// 获取百度AI平台的访问令牌
    /// </summary>
    private IEnumerator GetAccessToken()
    {
        if (!string.IsNullOrEmpty(accessToken) && DateTime.Now < tokenExpirationTime)
        {
            yield break; // 如果令牌有效，直接返回
        }

        string tokenUrl = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
        
        using (UnityWebRequest www = UnityWebRequest.Get(tokenUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"获取访问令牌失败: {www.error}");
                yield break;
            }

            string response = www.downloadHandler.text;
            // 解析JSON响应获取访问令牌
            var jsonObj = JsonConvert.DeserializeObject<TokenResponse>(response);
            if (jsonObj != null && !string.IsNullOrEmpty(jsonObj.access_token))
            {
                accessToken = jsonObj.access_token;
                tokenExpirationTime = DateTime.Now.AddSeconds(jsonObj.expires_in - 60); // 提前60秒过期，避免边界问题
                Debug.Log("访问令牌获取成功，有效期: " + jsonObj.expires_in + " 秒");
            }
            else
            {
                Debug.LogError("无法解析访问令牌响应");
            }
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    public void StartRecording()
    {
        StartRecording(null);
    }
    
    /// <summary>
    /// 开始录音，并提供结果回调
    /// </summary>
    /// <param name="resultCallback">识别结果的回调函数</param>
    public void StartRecording(Action<string> resultCallback)
    {
        if (isRecording)
        {
            Debug.LogWarning("已经在录音中，请先停止当前录音");
            return;
        }

        // 确保有麦克风权限
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogError("没有可用的麦克风设备");
            return;
        }

        Debug.Log("开始录音...");
        isRecording = true;
        OnRecordingStatusChanged?.Invoke(true);

        // 如果提供了回调，订阅最终结果事件
        if (resultCallback != null)
        {
            // 移除之前的所有订阅者，避免多次回调
            OnFinalResult = null;
            OnFinalResult += resultCallback;
        }

        // 创建录音剪辑
        recordingClip = Microphone.Start(null, false, recordingLength, recordingFrequency);
        startPosition = 0;
        
        // 启动实时识别
        StartCoroutine(ProcessRecordingInRealTime());
    }

    /// <summary>
    /// 停止录音并进行最终识别
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("当前没有录音进行中");
            return;
        }

        // 记录结束位置
        endPosition = Microphone.GetPosition(null);
        
        // 停止麦克风
        Microphone.End(null);
        Debug.Log("录音已停止，总时长: " + (float)endPosition / recordingFrequency + " 秒");
        
        // 更新状态
        isRecording = false;
        OnRecordingStatusChanged?.Invoke(false);

        // 发送完整的录音进行识别
        StartCoroutine(RecognizeSpeech(recordingClip, 0, endPosition));
    }

    /// <summary>
    /// 实时处理录音并进行识别
    /// </summary>
    private IEnumerator ProcessRecordingInRealTime()
    {
        int lastPosition = 0;
        int currentPosition = 0;
        const int samplesThreshold = 16000; // 每秒钟的采样数
        
        while (isRecording)
        {
            yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次
            
            if (!Microphone.IsRecording(null))
            {
                Debug.LogWarning("麦克风已断开连接");
                isRecording = false;
                OnRecordingStatusChanged?.Invoke(false);
                break;
            }
            
            currentPosition = Microphone.GetPosition(null);
            
            // 计算有多少新的采样点
            int newSamples = 0;
            if (currentPosition < lastPosition) // 环绕情况
            {
                newSamples = (recordingClip.samples - lastPosition) + currentPosition;
            }
            else
            {
                newSamples = currentPosition - lastPosition;
            }
            
            // 如果新采样点足够多，发送进行识别
            if (newSamples >= samplesThreshold)
            {
                // 使用临时的AudioClip进行识别
                StartCoroutine(RecognizePartialSpeech(recordingClip, lastPosition, newSamples, true));
                lastPosition = currentPosition;
            }
        }
    }

    /// <summary>
    /// 识别部分语音（用于实时反馈）
    /// </summary>
    private IEnumerator RecognizePartialSpeech(AudioClip clip, int startSample, int sampleCount, bool isPartial)
    {
        // 确保我们有有效的访问令牌
        if (string.IsNullOrEmpty(accessToken) || DateTime.Now >= tokenExpirationTime)
        {
            yield return StartCoroutine(GetAccessToken());
            
            // 如果获取令牌失败，终止识别
            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogError("无法获取访问令牌，语音识别失败");
                yield break;
            }
        }

        // 提取音频数据
        float[] samples = new float[sampleCount];
        clip.GetData(samples, startSample);
        
        // 将float转换为16位PCM
        byte[] audioData = ConvertToWav(samples, clip.channels, clip.frequency);
        
        // 发送到百度ASR接口
        yield return StartCoroutine(SendAudioToASR(audioData, isPartial));
    }

    /// <summary>
    /// 识别完整语音
    /// </summary>
    private IEnumerator RecognizeSpeech(AudioClip clip, int startSample, int endSample)
    {
        // 提取完整音频数据
        int sampleCount = endSample - startSample;
        if (sampleCount <= 0) 
        {
            sampleCount += clip.samples; // 处理环绕情况
        }
        
        yield return StartCoroutine(RecognizePartialSpeech(clip, startSample, sampleCount, false));
    }

    /// <summary>
    /// 将音频数据发送到百度ASR进行识别
    /// </summary>
    private IEnumerator SendAudioToASR(byte[] audioData, bool isPartial)
    {
        string url = "https://vop.baidu.com/server_api";
        
        // 构建请求体
        var requestData = new ASRRequestData
        {
            format = "pcm",
            rate = recordingFrequency,
            channel = 1,
            cuid = SystemInfo.deviceUniqueIdentifier,
            token = accessToken,
            dev_pid = 1537, // 普通话识别模型
            speech = Convert.ToBase64String(audioData),
            len = audioData.Length
        };
        
        // 序列化为JSON
        string jsonBody = JsonConvert.SerializeObject(requestData);
        
        // 发送请求
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"语音识别请求失败: {www.error}");
                yield break;
            }
            
            // 解析响应
            string response = www.downloadHandler.text;
            Debug.Log($"收到ASR响应: {response}");
            
            // 解析JSON响应
            var asrResponse = JsonConvert.DeserializeObject<ASRResponse>(response);
            
            if (asrResponse != null && asrResponse.err_no == 0 && asrResponse.result != null && asrResponse.result.Length > 0)
            {
                string recognizedText = string.Join("", asrResponse.result);
                
                // 根据是否为部分识别结果触发不同事件
                if (isPartial)
                {
                    OnPartialResult?.Invoke(recognizedText);
                    Debug.Log($"部分识别结果: {recognizedText}");
                }
                else
                {
                    OnFinalResult?.Invoke(recognizedText);
                    Debug.Log($"最终识别结果: {recognizedText}");
                }
            }
            else
            {
                Debug.LogError($"语音识别返回错误: {(asrResponse != null ? asrResponse.err_msg : "未知错误")}");
            }
        }
    }

    /// <summary>
    /// 将float音频数据转换为16位PCM格式
    /// </summary>
    private byte[] ConvertToWav(float[] samples, int channels, int frequency)
    {
        // 创建内存流存储PCM数据
        using (var memoryStream = new System.IO.MemoryStream())
        {
            // 创建二进制写入器
            using (var writer = new System.IO.BinaryWriter(memoryStream))
            {
                // 转换音频样本为16位PCM
                short[] intData = new short[samples.Length];
                for (int i = 0; i < samples.Length; i++)
                {
                    // 将-1到1的float值转换为-32768到32767的short值
                    intData[i] = (short)(samples[i] * 32767);
                }
                
                // 写入PCM数据
                byte[] byteData = new byte[intData.Length * 2];
                System.Buffer.BlockCopy(intData, 0, byteData, 0, byteData.Length);
                
                // 返回PCM数据
                return byteData;
            }
        }
    }

    /// <summary>
    /// 用于解析访问令牌响应的类
    /// </summary>
    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public int expires_in;
        public string refresh_token;
        public string scope;
        public string session_key;
        public string session_secret;
    }

    /// <summary>
    /// 用于ASR请求数据的类
    /// </summary>
    [Serializable]
    private class ASRRequestData
    {
        public string format;
        public int rate;
        public int channel;
        public string cuid;
        public string token;
        public int dev_pid;
        public string speech;
        public int len;
    }

    /// <summary>
    /// 用于解析ASR响应的类
    /// </summary>
    [Serializable]
    private class ASRResponse
    {
        public int err_no;
        public string err_msg;
        public string corpus_no;
        public string sn;
        public string[] result;
    }
}
