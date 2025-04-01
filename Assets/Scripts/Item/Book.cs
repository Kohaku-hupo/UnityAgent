using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Book : ItemBase
{
    // public string itemName = "书";
    // public string itemId = "book_01";
    // public override string ItemName { get => itemName; }
    // public override string ItemId { get => itemId; }

    protected override void Awake()
    {
        itemType = ItemType.Book;
        interactionTime = 2f; // 设置书的交互时间为2秒
        base.Awake();
    }

    protected override void InitializeItem()
    {
        if (itemType != ItemType.Book)
        {
            itemType = ItemType.Book;
        }
        base.InitializeItem();
        itemName = "书";
        if (string.IsNullOrEmpty(itemStatus))
        {
            itemStatus = "空闲";
        }
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "阅读":
                if (itemStatus == "空闲")
                {
                    itemStatus = "阅读中";
                    Debug.Log($"{itemName}正在被阅读");
                }
                break;
            case "停止阅读":
                if (itemStatus == "阅读中")
                {
                    itemStatus = "空闲";
                    Debug.Log($"{itemName}已停止阅读");
                }
                break;
            default:
                base.ExecuteAction(actionName, role, callback);
                break;
        }
    }
}
