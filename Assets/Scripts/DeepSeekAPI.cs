using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;

public class DeepSeekAPI : MonoBehaviour
{
    private string apiKey = "sk-9072ef319dbf495f9bf3c6a4285c3d11";
    private string apiUrl = "https://api.deepseek.com/v1/chat/completions";



    public void SendMessageToDeepSeek(string message, UnityAction<string> callback)
    {
        StartCoroutine(PostRequest(message, callback));
    }

    IEnumerator PostRequest(string message, UnityAction<string> callback)
    {
        //         string systemPrompt = @"
        // The user will provide some exam text. Please parse the 'question' and 'answer' and output them in JSON format.

        // EXAMPLE INPUT: 
        // Which is the highest mountain in the world? Mount Everest.

        // EXAMPLE JSON OUTPUT:
        // {
        //     'question': 'Which is the highest mountain in the world?',
        //     'answer': 'Mount Everest'
        // }";

        string systemPrompt = File.ReadAllText("Assets/Resources/system_prompt.txt");
        // string userPrompt = File.ReadAllText("Assets/Resources/user_prompt.txt");
        // 创建请求体
        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                // new { role = "user", content = userPrompt }
                new { role = "user", content = message }
            },
            response_format = new { type = "json_object" }
        };

        // 使用Newtonsoft.Json序列化
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        // 创建UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        UIManager.Instance.testPanel.SetWaitShow(true);
        // 发送请求
        yield return request.SendWebRequest();

        UIManager.Instance.testPanel.SetWaitShow(false);

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text); // 打印详细错误信息
        }
        else
        {
            // 处理响应
            string responseJson = request.downloadHandler.text;
            Debug.Log("Response: " + responseJson);


            // 解析响应
            var response = JsonConvert.DeserializeObject<DeepSeekResponse>(responseJson);
            if (response != null && response.choices.Length > 0)
            {
                string reply = response.choices[0].message.content;
                Debug.Log("DeepSeek reply: " + reply);
                callback(reply);

                // var response1 = JsonConvert.DeserializeObject<AA>(reply);
                // Debug.Log("question" + response1.question);
                // Debug.Log("answer" + response1.answer);

            }
            else
            {
                Debug.LogError("Failed to parse response.");
            }
        }
    }

    [System.Serializable]
    private class AA
    {
        public string question;
        public string answer;
    }

    // 定义响应数据结构
    [System.Serializable]
    private class DeepSeekResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
    }

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }
}