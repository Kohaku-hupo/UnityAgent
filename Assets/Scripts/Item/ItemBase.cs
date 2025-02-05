using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBase : MonoBehaviour
{
    public virtual string ItemName { get; set; }
    public virtual string ItemId { get; set; }

    [SerializeField]
    public GameObject rolePos;


    void Start()
    {
        rolePos = CTool.Find(gameObject, "rolePos");

    }

    void Update()
    {

    }

    public virtual void RoleAction(string actionName, RoleBase role)
    {

    }
}
