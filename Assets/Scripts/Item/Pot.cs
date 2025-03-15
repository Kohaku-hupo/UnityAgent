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

    public override void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        if (actionName == "烹饪")
        {
            Debug.Log("烹饪");

            // role.NextTask();
        }
        StartCoroutine(Wait(2, callback));
    }

}
