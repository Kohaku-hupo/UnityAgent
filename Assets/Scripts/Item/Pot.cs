using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pot : ItemBase
{
    public string itemName = "锅";
    public string itemId = "pot_03";
    public override string ItemName { get => itemName; }
    public override string ItemId { get => itemId; }

     public override void RoleAction(string actionName, RoleBase role)
    {
        if (actionName == "烹饪")
        {
            Debug.Log("烹饪");

            role.NextTask();
        }

    }
}
