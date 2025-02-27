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

    public override void RoleAction(string actionName, RoleBase role)
    {
        if (actionName == "烹饪")
        {
            Debug.Log("烹饪");

            StartCoroutine(Wait(2, role.NextTask));
            // role.NextTask();
        }

    }

    private IEnumerator Wait(float time, UnityAction callback)
    {

        yield return new WaitForSeconds(2); // 等待2秒
        callback();
    }
}
