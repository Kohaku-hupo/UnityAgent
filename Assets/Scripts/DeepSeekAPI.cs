using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
using System.Threading.Tasks;
using System;

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
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("Response: " + responseJson);

            var response = JsonConvert.DeserializeObject<DeepSeekResponse>(responseJson);
            if (response != null && response.choices.Length > 0)
            {
                string reply = response.choices[0].message.content;
                Debug.Log("DeepSeek reply: " + reply);
                
                // 存储响应内容
                try
                {
                    Debug.Log("Attempting to parse and store response in MongoDB...");
                    
                    // 创建完整的存储对象
                    var storageObject = new
                    {
                        userMessage = message,
                        systemPrompt = systemPrompt,
                        modelResponse = new
                        {
                            rawResponse = responseJson,
                            parsedReply = reply,
                            role = response.choices[0].message.role,
                            model = "deepseek-chat"
                        }
                    };

                    // 如果回复是JSON格式，则解析并添加到存储对象中
                    try
                    {
                        JObject parsedContent = JObject.Parse(reply);
                        Debug.Log("Successfully parsed response as JSON");
                        
                        // 保存模型响应
                        _ = MongoDBManager.Instance.SaveModelResponse(
                            JsonConvert.SerializeObject(storageObject),
                            new
                            {
                                metadata = storageObject,
                                parsedContent = parsedContent
                            }
                        );

                        // 检查并保存记忆相关内容
                        if (parsedContent["shortTermMemory"] != null)
                        {
                            _ = MongoDBManager.Instance.SaveMemory("shortTerm", parsedContent["shortTermMemory"]);
                        }
                        if (parsedContent["longTermMemory"] != null)
                        {
                            _ = MongoDBManager.Instance.SaveMemory("longTerm", parsedContent["longTermMemory"]);
                        }

                        // 检查并保存环境更新
                        if (parsedContent["updatedEnvironment"] != null)
                        {
                            _ = MongoDBManager.Instance.SaveEnvironmentUpdate(parsedContent["updatedEnvironment"]);
                        }

                        // 检查并保存任务列表更新
                        if (parsedContent["updatedPriorityTaskList"] != null)
                        {
                            _ = MongoDBManager.Instance.SaveTaskListUpdate(parsedContent["updatedPriorityTaskList"]);
                        }
                    }
                    catch (JsonReaderException)
                    {
                        Debug.Log("Response is not in JSON format, storing as raw content");
                        _ = MongoDBManager.Instance.SaveModelResponse(
                            JsonConvert.SerializeObject(storageObject),
                            new
                            {
                                metadata = storageObject,
                                rawContent = reply
                            }
                        );
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to store response: {e.Message}");
                }
                
                callback(reply);
            }
            else
            {
                Debug.LogError("Failed to parse response.");
            }
        }
    }

    private IEnumerator WaitForMongoSave(Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            Debug.LogError($"MongoDB save failed: {task.Exception}");
        }
        else
        {
            Debug.Log("MongoDB save completed successfully");
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