using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Computer : ItemBase
{
    [SerializeField] private bool isPowered = false;
    private bool isOn = false;
    private bool isUsing = false;

    protected override void Awake()
    {
        itemType = ItemType.Computer;
        interactionTime = 3f; // 设置电脑的交互时间为3秒
        base.Awake();
        UpdateStatus();
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "打开":
                if (!isOn)
                {
                    PowerOn();
                    itemStatus = "开启";
                }
                break;
            case "关闭":
                if (isOn)
                {
                    PowerOff();
                    itemStatus = "关闭";
                }
                break;
            case "使用":
                if (isOn && !isUsing)
                {
                    StartUse(role);
                    itemStatus = "使用中";
                }
                break;
            case "停止使用":
                if (isUsing)
                {
                    StopUse(role);
                    itemStatus = "开启";
                }
                break;
            default:
                base.ExecuteAction(actionName, role, callback);
                break;
        }
    }

    private void PowerOn()
    {
        isOn = true;
        Debug.Log($"{itemName}已打开");
    }

    private void PowerOff()
    {
        isOn = false;
        isUsing = false;
        Debug.Log($"{itemName}已关闭");
    }

    private void StartUse(RoleBase role)
    {
        if (role != null && rolePos != null)
        {
            isUsing = true;
            role.transform.position = rolePos.transform.position;
            role.transform.rotation = rolePos.transform.rotation;
            role.PlayEffect("use");
            Debug.Log($"{role.name}开始使用{itemName}");
        }
    }

    private void StopUse(RoleBase role)
    {
        if (role != null)
        {
            isUsing = false;
            role.PlayEffect("stopuse");
            Debug.Log($"{role.name}停止使用{itemName}");
        }
    }

    private void UpdateStatus()
    {
        if (isPowered)
        {
            itemStatus = isUsing ? "使用中" : (isOn ? "开启" : "关闭");
        }
        else
        {
            itemStatus = "关机";
        }
    }
} 