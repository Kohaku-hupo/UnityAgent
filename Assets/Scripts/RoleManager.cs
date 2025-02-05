using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class RoleManager : MonoBehaviour
{
    public RoleBase role;
    public List<ItemBase> items = new();

    void Start()
    {
        A();
    }

    void Update()
    {

    }




    public void A()
    {
        string jsonString = File.ReadAllText("Assets/Resources/item.json");
        // string jsonString = "{\"目标\": \"满足用户需求并保持房间整洁\"},";
        RoleData aa = JsonConvert.DeserializeObject<RoleData>(jsonString);
        // Debug.Log(aa.target + aa.updatePlan.priorityTaskList[0].task);
        //  JsonUtility.FromJson<MyData>(jsonString);

        // JsonData jsondata = JsonMapper.ToObject(text.text);

        role.PerformTask(aa.tasks);
    }


}

public class RoleData
{
    public string target;
    public string reason;
    public List<Task> tasks;
    public UpdatePlan updatePlan;
    public List<string> shortTermMemory;
    public List<string> longTermMemory;
    public string responseToUser;
}

public class Task
{
    public string action;
    public string itemId;
    public string interaction;
}

public class UpdatePlan
{
    public string dailyGoal;
    public List<PriorityTask> priorityTaskList;
}

public class PriorityTask
{
    public string task;
    public string status;
}

