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

    public override void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        if (actionName == "阅读")
        {
            Debug.Log("阅读");

            // role.NextTask();
        }
        StartCoroutine(Wait(2, callback));
    }
}
