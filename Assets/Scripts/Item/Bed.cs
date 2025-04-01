using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Bed : ItemBase
{
    [SerializeField] private Vector3 sleepRotation = new Vector3(-90, 0, 0); // 睡觉时的旋转
    [SerializeField] private float sleepDuration = 3f; // 默认睡眠持续时间
    private bool isOccupied = false;

    protected override void InitializeItem()
    {
        itemType = ItemType.Bed;
        itemName = "床";
        itemStatus = "空闲";
    }

    protected override void Awake()
    {
        base.Awake();
        if (rolePos == null)
        {
            GameObject rolePosition = new GameObject("RolePosition");
            rolePosition.transform.parent = transform;
            rolePosition.transform.localPosition = new Vector3(0, 0.5f, 0); // 在床的上方0.5米
            rolePosition.transform.localRotation = Quaternion.identity;
            rolePos = rolePosition;
            Debug.Log($"{itemName}: 创建了RolePosition");
        }
    }

    public override void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        switch (actionName.ToLower())
        {
            case "躺下":
                if (!isOccupied)
                {
                    StartCoroutine(LayDown(role, callback));
                    isOccupied = true;
                    itemStatus = "使用中";
                    return;
                }
                break;
            case "起床":
                if (isOccupied)
                {
                    GetUp(role);
                    isOccupied = false;
                    itemStatus = "空闲";
                }
                break;
            case "整理":
                if (!isOccupied)
                {
                    StartCoroutine(MakeBed(callback));
                }
                else
                {
                    Debug.Log($"{itemName}正在使用中，无法整理");
                    callback?.Invoke();
                }
                break;
            default:
                base.RoleAction(actionName, role, callback);
                break;
        }
    }

    private IEnumerator LayDown(RoleBase role, UnityAction callback)
    {
        // 移动到床的位置
        role.transform.position = rolePos.transform.position;
        role.transform.rotation = rolePos.transform.rotation;
        
        // 播放躺下动画
        role.PlayEffect("laydown");
        
        yield return new WaitForSeconds(sleepDuration);
        callback?.Invoke();
    }

    private void GetUp(RoleBase role)
    {
        role.PlayEffect("getup");
        Debug.Log($"从{itemName}上起来");
    }

    private System.Collections.IEnumerator MakeBed(UnityAction callback)
    {
        // 播放整理效果
        ItemEffectManager.Instance.PlayEffect(ItemId, "整理", transform);
        
        // 等待整理动画完成
        yield return new WaitForSeconds(2f);
        
        itemStatus = "整理好的";
        callback?.Invoke();
    }
}