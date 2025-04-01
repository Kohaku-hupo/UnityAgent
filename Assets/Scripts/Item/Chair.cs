using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Events;

public class Chair : ItemBase
{
    private bool isOccupied = false;

    protected override void Awake()
    {
        itemType = ItemType.Chair;
        base.Awake();
    }

    protected override void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "坐下":
                if (!isOccupied)
                {
                    SitDown(role);
                    itemStatus = "使用中";
                }
                break;
            case "起身":
                if (isOccupied)
                {
                    StandUp(role);
                    itemStatus = "空闲";
                }
                break;
            default:
                Debug.LogWarning($"未知的动作：{actionName}");
                break;
        }
        
        StartCoroutine(Wait(2, callback));
    }

    private void SitDown(RoleBase role)
    {
        if (role != null && rolePos != null)
        {
            isOccupied = true;
            role.transform.position = rolePos.transform.position;
            role.transform.rotation = rolePos.transform.rotation;
            role.PlayEffect("sit");
            Debug.Log($"{role.name}坐在了{itemName}上");
        }
    }

    private void StandUp(RoleBase role)
    {
        if (role != null)
        {
            isOccupied = false;
            role.PlayEffect("stand");
            Debug.Log($"{role.name}从{itemName}上站了起来");
        }
    }

    private void UpdateStatus()
    {
        itemStatus = isOccupied ? "使用中" : "空闲";
    }

    private IEnumerator Wait(float time, UnityAction callback)
    {
        yield return new WaitForSeconds(time);
        callback?.Invoke();
    }
} 