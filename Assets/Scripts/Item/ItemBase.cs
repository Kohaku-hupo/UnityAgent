using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ItemBase : MonoBehaviour
{
    [SerializeField]
    protected string itemName = "道具";
    [SerializeField]
    protected string itemId = "item_01";
    [SerializeField]
    protected string itemStatus = "关闭";
    public virtual string ItemName { get => itemName; set => itemName = value; }
    public virtual string ItemId { get => itemId; set => itemId = value; }
    public virtual string ItemStatus { get => itemStatus; set => itemStatus = value; }

    [HideInInspector]
    public GameObject rolePos;


    void Start()
    {
        rolePos = CTool.Find(gameObject, "rolePos");

    }

    void Update()
    {

    }

    public virtual void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        StartCoroutine(Wait(2, callback));
    }

    protected virtual IEnumerator Wait(float time, UnityAction callback)
    {
        yield return new WaitForSeconds(2); // 等待2秒
        callback();
    }
}
