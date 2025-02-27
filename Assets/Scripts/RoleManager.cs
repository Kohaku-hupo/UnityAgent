using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Data;

public class RoleManager : MonoBehaviour
{
    public RoleBase role;
    public List<ItemBase> items = new();

    public UserData curUserData = new();

    void Awake()
    {
        StartSet();
        // GameManager.Instance.deepSeekAPI.SendMessageToDeepSeek("", A);
        // TestA();
    }

    // void Update()
    // {
    // }

    public void StartSet()
    {
        //读取初始设置
        string jsonString = File.ReadAllText("Assets/Resources/start_set.json");
        StartSetData startSetData = JsonConvert.DeserializeObject<StartSetData>(jsonString);

        // 初始化用户数据
        UserData userData = new()
        {
            environmentInfo = new EnvironmentInfo(),
            memory = new Memory(),
            updatePlan = new UpdatePlan(),
            userContent = ""
        };
        userData.environmentInfo.roomStatuss = new List<RoomStatus>();
        foreach (var item in items)
        {
            userData.environmentInfo.roomStatuss.Add(
                new RoomStatus
                {
                    itemId = item.ItemId,
                    name = item.ItemName,
                    status = item.ItemStatus
                }
            );
        }
        userData.memory = startSetData.memory;
        userData.updatePlan = startSetData.updatePlan;

        curUserData = userData;

        // // 序列化
        // string json = JsonConvert.SerializeObject(userData);
        // Debug.Log(json); // 输出序列化结果
        // // 如果需要保存到文件
        // File.WriteAllText("Assets/Resources/Test/temp_user_prompt.json", jsonString);
    }

    public void GetReturnData(string jsonString)
    {
        RoleData roleData = JsonConvert.DeserializeObject<RoleData>(jsonString);
        // Debug.Log(aa.target + aa.updatePlan.priorityTaskList[0].task);
        //  JsonUtility.FromJson<MyData>(jsonString);
        // JsonData jsondata = JsonMapper.ToObject(text.text);


        //更新信息
        // curUserData.environmentInfo.UpdateStatus(items);
        curUserData.environmentInfo.roomStatuss = roleData.updatedEnvironment;
        curUserData.memory.UpdateMemory(roleData.shortTermMemory, roleData.longTermMemory);
        curUserData.updatePlan = roleData.updatePlan;

        //执行任务
        role.PerformTask(roleData.tasks);

        UIManager.Instance.testPanel.SetTargetShow(roleData.target);
        UIManager.Instance.testPanel.SetResponseShow(roleData.responseToUser);

    }

    public void SubmitUserContent(string userinput)
    {
        curUserData.userContent = userinput;
        // 序列化
        string json = JsonConvert.SerializeObject(curUserData);
        Debug.Log(json); // 输出序列化结果
        GameManager.Instance.deepSeekAPI.SendMessageToDeepSeek(json, GameManager.Instance.roleManager.GetReturnData);
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
public class RoleData
{
    public string target;
    public string reason;
    public List<Task> tasks;
    public UpdatePlan updatePlan;
    public List<string> shortTermMemory;
    public List<string> longTermMemory;
    public string responseToUser;
    public List<RoomStatus> updatedEnvironment;
    public List<PriorityTask> updatedPriorityTaskList;
}

[System.Serializable]
public class Task
{
    public string action;
    public string itemId;
    public string interaction;
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
};
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
};