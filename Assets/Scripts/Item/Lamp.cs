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



    public override void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        if (actionName == "打开")
        {
            Debug.Log("打开台灯");
            // role.NextTask();
        }
        else if (actionName == "关闭")
        {
            Debug.Log("关闭台灯");
            // role.NextTask();
        }
        StartCoroutine(Wait(2, callback));
    }

}
