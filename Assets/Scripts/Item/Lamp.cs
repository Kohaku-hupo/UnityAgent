using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Events;
public class Lamp : ItemBase
{
    // public string itemName = "台灯";
    // public string itemId = "lamp_01";
    // public override string ItemName { get => itemName; }
    // public override string ItemId { get => itemId; }

    protected override void Awake()
    {
        itemType = ItemType.Lamp;
        interactionTime = 1f; // 设置台灯的交互时间为1秒
        base.Awake();
    }

    protected override void InitializeItem()
    {
        if (itemType != ItemType.Lamp)
        {
            itemType = ItemType.Lamp;
        }
        base.InitializeItem();
        itemName = "台灯";
        if (string.IsNullOrEmpty(itemStatus))
        {
            itemStatus = "关闭";
        }
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "打开":
                if (itemStatus == "关闭")
                {
                    itemStatus = "开启";
                    Debug.Log($"{itemName}已打开");
                }
                break;
            case "关闭":
                if (itemStatus == "开启")
                {
                    itemStatus = "关闭";
                    Debug.Log($"{itemName}已关闭");
                }
                break;
            default:
                base.ExecuteAction(actionName, role, callback);
                break;
        }
    }

}
