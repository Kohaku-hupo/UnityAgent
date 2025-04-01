using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Security.Cryptography;

/// <summary>
/// 百度语音合成组件
/// </summary>
public class BaiduSpeechSynthesizer : MonoBehaviour
{
    private static BaiduSpeechSynthesizer _instance;

    public static BaiduSpeechSynthesizer Instance
    {
        get { return _instance; }
    }

    [Header("百度语音API参数")]
    [SerializeField] private string appID = ""; // 填写你的App ID
    [SerializeField] private string apiKey = ""; // 填写你的Api Key
    [SerializeField] private string secretKey = ""; // 填写你的Secret Key

    [Header("语音参数")]
    [SerializeField, Range(0, 15)] private int speed = 5; // 语速，取值0-15，默认为5中语速
    [SerializeField, Range(0, 15)] private int pitch = 5; // 音调，取值0-15，默认为5中音调
    [SerializeField, Range(0, 15)] private int volume = 15; // 音量，取值0-15，默认为5中音量
    [SerializeField] private int person = 1; // 发音人，默认为1，详见百度文档

    private AudioSource audioSource;
    private string accessToken;
    private DateTime tokenExpirationTime;
    private bool isPlayingAudio = false;
    private UnityAction onSpeechCompleted = null;
    private Coroutine checkAudioPlaybackCoroutine = null;

    // 语音播放完成事件
    public event Action OnSpeechFinished;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
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

    void Update()
    {
        // 检测音频是否播放完成
        if (isPlayingAudio && !audioSource.isPlaying)
        {
            isPlayingAudio = false;
            Debug.Log("语音播放完成");
            OnSpeechFinished?.Invoke();
            
            if (onSpeechCompleted != null)
            {
                onSpeechCompleted.Invoke();
                onSpeechCompleted = null;
            }
        }
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
            var jsonResponse = JsonUtility.FromJson<TokenResponse>(response);
            if (jsonResponse != null && !string.IsNullOrEmpty(jsonResponse.access_token))
            {
                accessToken = jsonResponse.access_token;
                tokenExpirationTime = DateTime.Now.AddSeconds(jsonResponse.expires_in - 60); // 提前60秒过期，避免边界问题
                Debug.Log("访问令牌获取成功，有效期: " + jsonResponse.expires_in + " 秒");
            }
            else
            {
                Debug.LogError("无法解析访问令牌响应");
            }
        }
    }

    /// <summary>
    /// 将文本转换为语音并播放
    /// </summary>
    /// <param name="text">要转换的文本</param>
    /// <param name="callback">语音播放完成后的回调</param>
    public void SpeakText(string text, UnityAction callback = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("语音合成文本为空");
            callback?.Invoke();
            return;
        }

        this.onSpeechCompleted = callback;
        StartCoroutine(SynthesizeSpeech(text));
    }

    /// <summary>
    /// 停止当前正在播放的语音
    /// </summary>
    public void StopSpeaking()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            isPlayingAudio = false;
            Debug.Log("语音播放已停止");
        }
    }

    /// <summary>
    /// 语音合成协程
    /// </summary>
    private IEnumerator SynthesizeSpeech(string text)
    {
        // 确保我们有有效的访问令牌
        if (string.IsNullOrEmpty(accessToken) || DateTime.Now >= tokenExpirationTime)
        {
            yield return StartCoroutine(GetAccessToken());
            
            // 如果获取令牌失败，终止合成
            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogError("无法获取访问令牌，语音合成失败");
                onSpeechCompleted?.Invoke();
                onSpeechCompleted = null;
                yield break;
            }
        }

        // 百度语音合成API地址
        string url = "https://tsn.baidu.com/text2audio";
        
        // 构建POST请求参数
        WWWForm form = new WWWForm();
        form.AddField("tex", text);
        form.AddField("tok", accessToken);
        form.AddField("cuid", SystemInfo.deviceUniqueIdentifier);
        form.AddField("ctp", "1");
        form.AddField("lan", "zh");
        form.AddField("spd", speed.ToString());
        form.AddField("pit", pitch.ToString());
        form.AddField("vol", volume.ToString());
        form.AddField("per", person.ToString());
        form.AddField("aue", "3"); // mp3格式

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"语音合成请求失败: {www.error}");
                onSpeechCompleted?.Invoke();
                onSpeechCompleted = null;
                yield break;
            }

            // 检查返回内容类型，确保返回的是音频而不是错误信息
            string contentType = www.GetResponseHeader("Content-Type");
            if (contentType.StartsWith("audio/"))
            {
                // 将音频数据转换为AudioClip
                byte[] audioData = www.downloadHandler.data;
                StartCoroutine(LoadAndPlayAudioClip(audioData));
            }
            else
            {
                // 如果返回的不是音频，可能是错误信息
                string errorResponse = Encoding.UTF8.GetString(www.downloadHandler.data);
                Debug.LogError($"语音合成返回错误: {errorResponse}");
                onSpeechCompleted?.Invoke();
                onSpeechCompleted = null;
            }
        }
    }

    /// <summary>
    /// 加载并播放音频剪辑
    /// </summary>
    private IEnumerator LoadAndPlayAudioClip(byte[] audioData)
    {
        // 创建临时文件路径
        string tempFilePath = $"{Application.temporaryCachePath}/temp_audio_{DateTime.Now.Ticks}.mp3";
        
        // 将音频数据写入临时文件
        System.IO.File.WriteAllBytes(tempFilePath, audioData);
        
        // 使用UnityWebRequest加载音频文件
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"加载音频失败: {www.error}");
                
                // 清理临时文件
                try
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"删除临时音频文件失败: {e.Message}");
                }
                
                onSpeechCompleted?.Invoke();
                onSpeechCompleted = null;
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            
            // 停止当前正在播放的音频
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // 播放新的音频
            audioSource.clip = audioClip;
            audioSource.Play();
            isPlayingAudio = true;
            
            Debug.Log("正在播放语音，音频时长: " + audioClip.length + "秒");
        }
        
        // 等待一段时间后删除临时文件
        yield return new WaitForSeconds(0.5f);
        
        // 清理临时文件
        try
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"删除临时音频文件失败: {e.Message}");
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
}
