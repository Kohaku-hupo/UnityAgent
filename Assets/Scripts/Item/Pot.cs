using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pot : ItemBase
{
    // public string itemName = "锅";
    // public string itemId = "pot_03";
    // public override string ItemName { get => itemName; }
    // public override string ItemId { get => itemId; }

    protected override void Awake()
    {
        itemType = ItemType.Pot;
        interactionTime = 1.5f; // 设置锅的交互时间为1.5秒
        base.Awake();
    }

    protected override void InitializeItem()
    {
        if (itemType != ItemType.Pot)
        {
            itemType = ItemType.Pot;
        }
        base.InitializeItem();
        itemName = "锅";
        if (string.IsNullOrEmpty(itemStatus))
        {
            itemStatus = "空闲";
        }
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "使用":
            case "烹饪":
                if (itemStatus == "空闲")
                {
                    itemStatus = "使用中";
                    Debug.Log($"{itemName}正在被使用");
                }
                break;
            case "停止使用":
            case "停止烹饪":
                if (itemStatus == "使用中")
                {
                    itemStatus = "空闲";
                    Debug.Log($"{itemName}已停止使用");
                }
                break;
            default:
                base.ExecuteAction(actionName, role, callback);
                break;
        }
    }
}
