using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

public class RoleManager : MonoBehaviour
{
    public RoleBase role;
    public List<ItemBase> items = new();
    [HideInInspector] public UserData curUserData = new();
    [SerializeField] private string autoSubmitText = "随便找点事做";
    [SerializeField] private float waitTime = 10f;
    private float curWaitTime = 0f;
    private bool waitIng = false;

    async void Awake()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"初始化失败: {e.Message}");
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            // 初始化物品列表
            if (items == null)
            {
                items = new List<ItemBase>();
            }
            
            // 查找场景中的所有物品
            var foundItems = FindObjectsOfType<ItemBase>();
            items.Clear();
            items.AddRange(foundItems);
            
            Debug.Log($"找到 {items.Count} 个物品");
            foreach (var item in items)
            {
                Debug.Log($"物品: {item.name}, ID: {item.ItemId}, 类型: {item.GetType().Name}");
            }

            await StartSet();
            StartWait();
        }
        catch (Exception e)
        {
            Debug.LogError($"初始化失败: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    void Update()
    {
        if (waitIng && curWaitTime > 0)
        {
            curWaitTime -= Time.deltaTime;
            if (curWaitTime <= 0)
            {
                waitIng = false;
                AutoSubmitContent();
            }
        }
    }

    private async Task StartSet()
    {
        try
        {
            // 验证必要的组件和数据
            if (role == null)
            {
                Debug.LogError("role对象为空");
                role = FindObjectOfType<RoleBase>();
                if (role == null)
                {
                    throw new System.Exception("场景中没有找到RoleBase组件");
                }
            }

            if (items == null || items.Count == 0)
            {
                Debug.LogError("物品列表为空，重新初始化物品列表");
                var foundItems = FindObjectsOfType<ItemBase>();
                items = new List<ItemBase>(foundItems);
                if (items.Count == 0)
                {
                    throw new System.Exception("场景中没有找到任何物品");
                }
            }

            // 确保curUserData已初始化
            if (curUserData == null)
            {
                Debug.Log("初始化用户数据");
                curUserData = InitializeDefaultUserData();
            }

            // 加载记忆数据
            await LoadMemories();

            Debug.Log("初始化完成");
            
            // 输出当前状态
            Debug.Log($"当前状态：\n" +
                     $"Role: {(role != null ? "已加载" : "未加载")}\n" +
                     $"物品数量: {items.Count}\n" +
                     $"UserData: {(curUserData != null ? "已初始化" : "未初始化")}");
        }
        catch (Exception e)
        {
            Debug.LogError($"初始化设置时出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
            throw;
        }
    }

    public void GetReturnData(string json)
    {
        try
        {
            RoleData roleData = JsonConvert.DeserializeObject<RoleData>(json);
            if (roleData == null)
            {
                Debug.LogError("解析JSON失败，返回的数据格式不正确");
                return;
            }

            Debug.Log($"收到大模型响应: {json}");

            // 开始处理任务
            if (roleData.tasks != null && roleData.tasks.Count > 0)
            {
                bool allItemsExist = true;
                foreach (var task in roleData.tasks)
                {
                    // 首先尝试直接匹配ItemId
                    var item = items.FirstOrDefault(i => i.ItemId.Equals(task.itemId, StringComparison.OrdinalIgnoreCase));
                    
                    // 如果找不到，尝试通过类型名称匹配
                    if (item == null)
                    {
                        // 将输入ID转换为小写以进行不区分大小写的比较
                        string lowerId = task.itemId.ToLower();
                        // 尝试匹配任何以该类型名称开头的物品ID
                        item = items.FirstOrDefault(i => i.ItemId.ToLower().StartsWith(lowerId + "_"));
                        
                        if (item != null)
                        {
                            // 更新任务中的itemId为实际的ID
                            Debug.Log($"找到匹配的物品 - 输入ID: {task.itemId}, 实际ID: {item.ItemId}");
                            task.itemId = item.ItemId;
                        }
                    }

                    if (item == null)
                    {
                        Debug.LogError($"任务中包含不存在的物品ID: {task.itemId}");
                        Debug.Log("当前场景中的可用物品：");
                        foreach (var availableItem in items)
                        {
                            Debug.Log($"- ID: {availableItem.ItemId}, 类型: {availableItem.GetType().Name}, 名称: {availableItem.ItemName}");
                        }
                        allItemsExist = false;
                    }
                    else
                    {
                        Debug.Log($"找到有效物品 - ID: {task.itemId}, 名称: {item.ItemName}");
                    }
                }

                if (allItemsExist)
                {
                    Debug.Log("所有物品验证通过，开始执行任务");
                    role.PerformTask(roleData.tasks);
                }
                else
                {
                    Debug.LogError("由于存在无效的物品ID，任务执行被取消");
                }
            }

            //更新信息
            curUserData.environmentInfo.roomStatuss = roleData.updatedEnvironment;
            curUserData.memory.UpdateMemory(roleData.shortTermMemory, roleData.longTermMemory);
            curUserData.updatePlan = roleData.updatePlan;

            UIManager.Instance.testPanel.SetTargetShow(roleData.target);
            UIManager.Instance.testPanel.SetResponseShow(roleData.responseToUser);
            
            // 使用语音合成播放大模型回复
            SpeakResponse(roleData.responseToUser);
            
            // 注意：现在不在这里调用StartWait，而是在语音播放完成后调用
        }
        catch (Exception e)
        {
            Debug.LogError($"处理返回数据时出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 使用语音合成播放文本
    /// </summary>
    /// <param name="text">要合成的文本</param>
    private void SpeakResponse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("回复内容为空，跳过语音合成");
            OnSpeechFinished();
            return;
        }
        
        // 如果回复内容为"none"，跳过语音合成
        if (text.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("回复内容为none，跳过语音合成");
            OnSpeechFinished();
            return;
        }
        
        // 尝试获取语音合成组件
        var speechSynthesizer = GameManager.Instance.speechSynthesizer;
        if (speechSynthesizer != null)
        {
            Debug.Log($"开始合成语音: {text.Substring(0, Math.Min(30, text.Length))}...");
            speechSynthesizer.SpeakText(text, OnSpeechFinished);
        }
        else
        {
            Debug.LogWarning("语音合成组件未初始化");
            OnSpeechFinished();
        }
    }
    
    /// <summary>
    /// 语音播放完成后的回调
    /// </summary>
    private void OnSpeechFinished()
    {
        Debug.Log("语音播放已完成，继续执行后续逻辑");
        StartWait(); // 语音播放完成后开始等待
    }

    private List<RoomStatus> GetCurrentEnvironmentStatus()
    {
        List<RoomStatus> currentStatus = new List<RoomStatus>();
        
        try
        {
            if (items == null)
            {
                Debug.LogError("items列表为空，重新初始化");
                items = new List<ItemBase>();
                var foundItems = FindObjectsOfType<ItemBase>();
                items.AddRange(foundItems);
            }

            if (items.Count == 0)
            {
                Debug.LogWarning("场景中没有找到任何物品");
                return currentStatus;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    Debug.LogWarning("发现空的物品引用，跳过");
                    continue;
                }

                try
                {
                    currentStatus.Add(new RoomStatus
                    {
                        itemId = item.ItemId,
                        name = item.ItemName,
                        status = item.ItemStatus
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"处理物品 {item.name} 状态时出错: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"获取环境状态时出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
        }
        
        return currentStatus;
    }

    public void SubmitUserContent(string userinput)
    {
        if (curUserData == null)
        {
            Debug.LogError("curUserData为空");
            curUserData = new UserData();
        }

        if (curUserData.environmentInfo == null)
        {
            Debug.LogError("environmentInfo为空");
            curUserData.environmentInfo = new EnvironmentInfo();
        }

        // 更新当前环境状态
        curUserData.environmentInfo.roomStatuss = GetCurrentEnvironmentStatus();
        curUserData.userContent = userinput;
        
        // 序列化
        string json = JsonConvert.SerializeObject(curUserData);
        Debug.Log(json); // 输出序列化结果
        GameManager.Instance.deepSeekAPI.SendMessageToDeepSeek(json, GameManager.Instance.roleManager.GetReturnData);
    }

    private void AutoSubmitContent()
    {
        Debug.Log("自动提交");
        SubmitUserContent(autoSubmitText);
    }

    public void OnTaskFinish()
    {
        StartWait();
    }

    public void OnUserInput()
    {
        waitIng = false;
    }

    private void StartWait()
    {
        waitIng = true;
        curWaitTime = waitTime;
        Debug.Log("开始等待");
    }

    private UserData InitializeDefaultUserData()
    {
        return new UserData
        {
            environmentInfo = new EnvironmentInfo 
            { 
                roomStatuss = items.Select(item => new RoomStatus
                {
                    itemId = item.ItemId,
                    name = item.ItemName,
                    status = item.ItemStatus
                }).ToList() 
            },
            memory = new Memory 
            { 
                shortTermMemory = new List<string>(),
                longTermMemory = new List<string>()
            },
            updatePlan = new UpdatePlan 
            { 
                priorityTaskList = new List<PriorityTask>()
            },
            userContent = ""
        };
    }

    private async Task LoadMemories()
    {
        try
        {
            // 获取最新的短期记忆
            var latestShortTermMemory = await MongoDBManager.Instance.GetRecentMemory("shortterm", 1);
            if (latestShortTermMemory != null && latestShortTermMemory.Contains("content"))
            {
                Debug.Log("加载最新短期记忆...");
                var content = latestShortTermMemory["content"];
                if (content.IsBsonArray)
                {
                    curUserData.memory.shortTermMemory = new List<string>();
                    foreach (var item in content.AsBsonArray)
                    {
                        curUserData.memory.shortTermMemory.Add(item.ToString());
                    }
                }
            }

            // 获取最新的长期记忆
            var latestLongTermMemory = await MongoDBManager.Instance.GetRecentMemory("longterm", 1);
            if (latestLongTermMemory != null && latestLongTermMemory.Contains("content"))
            {
                Debug.Log("加载最新长期记忆...");
                var content = latestLongTermMemory["content"];
                if (content.IsBsonArray)
                {
                    curUserData.memory.longTermMemory = new List<string>();
                    foreach (var item in content.AsBsonArray)
                    {
                        curUserData.memory.longTermMemory.Add(item.ToString());
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载记忆时出错: {e.Message}");
        }
    }

    public void Test()
    {
        string jsonString = File.ReadAllText("Assets/Resources/item.json");
        // string jsonString = "{\"目标\": \"满足用户需求并保持房间整洁\"},";
        RoleData aa = JsonConvert.DeserializeObject<RoleData>(jsonString);
        // Debug.Log(aa.target + aa.updatePlan.priorityTaskList[0].task);
        //  JsonUtility.FromJson<MyData>(jsonString);

        // JsonData jsondata = JsonMapper.ToObject(text.text);

        role.PerformTask(aa.tasks);

        UIManager.Instance.testPanel.SetTargetShow(aa.target);
        UIManager.Instance.testPanel.SetResponseShow(aa.responseToUser);

    }
}

[System.Serializable]
public class StartSetData
{
    public Memory memory;
    public UpdatePlan updatePlan;
}

[System.Serializable]
public class GameTask
{
    public string action;
    public string itemId;
    public string interaction;
}

[System.Serializable]
public class RoleData
{
    public string target;
    public string reason;
    public List<GameTask> tasks;
    public UpdatePlan updatePlan;
    public List<string> shortTermMemory;
    public List<string> longTermMemory;
    public string responseToUser;
    public List<RoomStatus> updatedEnvironment;
    public List<PriorityTask> updatedPriorityTaskList;
}

[System.Serializable]
public class UpdatePlan
{
    public string dailyGoal;
    public List<PriorityTask> priorityTaskList;
}

[System.Serializable]
public class PriorityTask
{
    public string task;
    public string status;
}

[System.Serializable]
public class RoomStatus
{
    public string itemId;
    public string name;
    public string status;

    public void UpdateStatus(ItemBase item)
    {
        itemId = item.ItemId;
        name = item.ItemName;
        status = item.ItemStatus;
    }
}

[System.Serializable]
public class UserData
{
    public EnvironmentInfo environmentInfo;
    public Memory memory;
    public UpdatePlan updatePlan;
    public string userContent;

    public void UpdateEnvironmentInfo(List<RoomStatus> roomStatus)
    {
        environmentInfo.roomStatuss = roomStatus;
    }
}

[System.Serializable]
public class EnvironmentInfo
{
    public List<RoomStatus> roomStatuss;
    public void UpdateStatus(List<ItemBase> items)
    {
        foreach (var item in items)
        {
            RoomStatus roomStatus = new();
            roomStatus.UpdateStatus(item);
            roomStatuss.Add(roomStatus);
        }
    }
}

[System.Serializable]
public class Memory
{
    public List<string> shortTermMemory;
    public List<string> longTermMemory;

    public void UpdateMemory(List<string> shortTermMemory, List<string> longTermMemory)
    {
        this.shortTermMemory = shortTermMemory;
        this.longTermMemory = longTermMemory;
    }
}