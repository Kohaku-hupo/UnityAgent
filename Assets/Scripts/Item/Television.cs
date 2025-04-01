using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Television : ItemBase
{
    [SerializeField] private bool isPowered = false; // 是否通电
    [SerializeField] private float defaultWatchDuration = 30f; // 默认观看时间
    [SerializeField] private int currentChannel = 1; // 当前频道
    [SerializeField] private int maxChannel = 100; // 最大频道数
    private bool isWatching = false;
    private bool isOn = false;

    protected override void Awake()
    {
        itemType = ItemType.Television; // 在调用基类Awake之前设置类型
        interactionTime = 2f; // 设置电视的交互时间为2秒
        base.Awake();
        UpdateStatus();
    }

    protected override void Start()
    {
        // 不调用基类的Start，因为我们自己处理rolePos
        if (rolePos == null)
        {
            EnsureRolePosExists();
        }
        UpdateStatus();
    }

    private void EnsureRolePosExists()
    {
        if (rolePos == null)
        {
            Debug.Log($"{gameObject.name}的rolePos为空，尝试重新创建");
            GameObject rolePosition = new GameObject("RolePosition");
            rolePosition.transform.SetParent(transform, false);
            rolePosition.transform.localPosition = new Vector3(0, 0, -1.5f);
            rolePos = rolePosition;
        }
    }

    protected override void InitializeItem()
    {
        if (itemType != ItemType.Television)
        {
            itemType = ItemType.Television;
        }
        base.InitializeItem();
        itemName = "电视";
        
        // 再次检查rolePos
        if (rolePos == null)
        {
            Debug.LogError($"{gameObject.name}的rolePos为空，尝试重新创建");
            GameObject rolePosition = new GameObject("RolePosition");
            rolePosition.transform.SetParent(transform, false);
            rolePosition.transform.localPosition = new Vector3(0, 0, -1.5f);
            rolePos = rolePosition;
        }
        
        UpdateStatus();
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "打开":
                if (!isOn)
                {
                    TurnOn();
                    itemStatus = "开启";
                }
                break;
            case "关闭":
                if (isOn)
                {
                    TurnOff();
                    itemStatus = "关闭";
                }
                break;
            case "看电视":
                if (isOn && !isWatching)
                {
                    StartWatching(role);
                    itemStatus = "使用中";
                }
                break;
            case "停止看电视":
                if (isWatching)
                {
                    StopWatching(role);
                    itemStatus = "开启";
                }
                break;
            case "换台":
                if (isOn)
                {
                    ChangeChannel();
                }
                break;
            default:
                base.ExecuteAction(actionName, role, callback);
                break;
        }
    }

    private void TurnOn()
    {
        isOn = true;
        Debug.Log($"{itemName}已打开");
    }

    private void TurnOff()
    {
        isOn = false;
        isWatching = false;
        Debug.Log($"{itemName}已关闭");
    }

    private void StartWatching(RoleBase role)
    {
        if (role != null && rolePos != null)
        {
            isWatching = true;
            role.transform.position = rolePos.transform.position;
            role.transform.rotation = rolePos.transform.rotation;
            role.PlayEffect("watch");
            Debug.Log($"{role.name}开始观看{itemName}");
        }
    }

    private void StopWatching(RoleBase role)
    {
        if (role != null)
        {
            isWatching = false;
            role.PlayEffect("stopwatch");
            Debug.Log($"{role.name}停止观看{itemName}");
        }
    }

    private void ChangeChannel()
    {
        currentChannel = (currentChannel % maxChannel) + 1;
        Debug.Log($"{itemName}切换到频道 {currentChannel}");
    }

    private void UpdateStatus()
    {
        if (isPowered)
        {
            itemStatus = isWatching ? "使用中" : (isOn ? "开启" : "关闭");
        }
        else
        {
            itemStatus = "关机";
        }
    }
} 