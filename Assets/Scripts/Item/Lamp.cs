using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Events;
public class Lamp : ItemBase
{
    public string itemName = "台灯";
    public string itemId = "lamp_01";
    public override string ItemName { get => itemName; }
    public override string ItemId { get => itemId; }



    public override void RoleAction(string actionName, RoleBase role)
    {
        if (actionName == "打开")
        {
            Debug.Log("打开台灯");
            StartCoroutine(Wait(2, role.NextTask));
            // role.NextTask();
        }
        else if (actionName == "关闭")
        {
            Debug.Log("关闭台灯");
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
