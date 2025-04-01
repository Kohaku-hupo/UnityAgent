using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class ItemBase : MonoBehaviour
{
    [SerializeField]
    protected string itemName = "道具";
    [SerializeField]
    protected string itemId;
    [SerializeField]
    protected string itemStatus = "关闭";
    [SerializeField]
    protected float interactionDistance = 2f; // 可交互的距离
    [SerializeField]
    protected float defaultMoveSpeed = 5f; // 添加默认移动速度
    [SerializeField]
    protected float interactionTime = 2f; // 交互所需时间

    // 物品类型枚举
    public enum ItemType
    {
        None,  // 添加None作为默认值
        Lamp,
        Book,
        Pot,
        Television,
        Bed,
        Computer,
        Chair
    }

    [SerializeField]
    protected ItemType itemType = ItemType.None;  // 设置默认值为None

    public virtual string ItemName { get => itemName; set => itemName = value; }
    public virtual string ItemId 
    { 
        get 
        {
            if (string.IsNullOrEmpty(itemId))
            {
                GenerateItemId();
            }
            return itemId;
        }
        set => itemId = value; 
    }
    public virtual string ItemStatus { get => itemStatus; set => itemStatus = value; }

    [HideInInspector]
    public GameObject rolePos;

    private static bool isAnyRoleMoving = false; // 添加静态标志追踪任何角色是否正在移动
    private static bool isInteracting = false; // 是否正在交互

    protected virtual void Awake()
    {
        if (itemType == ItemType.None)
        {
            InitializeItem();
        }
        GenerateItemId();
        if (itemType != ItemType.None)
        {
            InitializeNameAndStatus();
        }
        EnsureRolePosExists();
    }

    protected virtual void Start()
    {
        if (rolePos == null)
        {
            EnsureRolePosExists();
        }
    }

    protected virtual void EnsureRolePosExists()
    {
        if (rolePos != null) return;

        Transform existingTransform = transform.Find("RolePosition");
        if (existingTransform != null)
        {
            rolePos = existingTransform.gameObject;
            return;
        }

        GameObject newRolePos = new GameObject("RolePosition");
        newRolePos.transform.SetParent(transform, false);
        
        // 根据物品类型设置不同的默认位置
        Vector3 defaultPosition = GetDefaultRolePosPosition();
        newRolePos.transform.localPosition = defaultPosition;
        rolePos = newRolePos;
        
        Debug.Log($"为{gameObject.name}创建了rolePos位置点，位置：{rolePos.transform.localPosition}");
    }

    protected virtual Vector3 GetDefaultRolePosPosition()
    {
        switch (itemType)
        {
            case ItemType.Television:
                return new Vector3(0, 0, -1.5f); // 电视前方1.5米
            case ItemType.Computer:
                return new Vector3(0, 0, -0.8f); // 电脑前方0.8米
            case ItemType.Chair:
                return new Vector3(0, 0.5f, 0);  // 椅子上方0.5米
            case ItemType.Bed:
                return new Vector3(0, 0.5f, 0);  // 床上方0.5米
            default:
                return new Vector3(0, 0, -1f);   // 默认前方1米
        }
    }

    protected virtual void InitializeItem()
    {
        if (itemType == ItemType.None)
        {
            Debug.LogError($"物品 {gameObject.name} 的itemType未设置！");
            return;
        }
        InitializeNameAndStatus();
    }

    protected virtual void InitializeNameAndStatus()
    {
        if (string.IsNullOrEmpty(itemName) || itemName == "道具")
        {
            itemName = GetDefaultItemName();
        }
        
        if (string.IsNullOrEmpty(itemStatus))
        {
            itemStatus = GetDefaultItemStatus();
        }
    }

    protected virtual string GetDefaultItemName()
    {
        return itemType switch
        {
            ItemType.Lamp => "台灯",
            ItemType.Book => "书",
            ItemType.Pot => "锅",
            ItemType.Television => "电视",
            ItemType.Bed => "床",
            ItemType.Computer => "电脑",
            ItemType.Chair => "椅子",
            _ => itemType.ToString()
        };
    }

    protected virtual string GetDefaultItemStatus()
    {
        return itemType switch
        {
            ItemType.Lamp => "关闭",
            ItemType.Television => "关机",
            ItemType.Computer => "关机",
            ItemType.Bed => "空闲",
            ItemType.Chair => "空闲",
            _ => "关闭"
        };
    }

    protected virtual void GenerateItemId()
    {
        if (itemType == ItemType.None)
        {
            Debug.LogError($"物品 {gameObject.name} 的itemType未设置，无法生成ItemId！");
            return;
        }

        if (!string.IsNullOrEmpty(itemId) && !itemId.StartsWith("item_"))
        {
            return;
        }

        string typePrefix = itemType.ToString().ToLower();
        var allItems = FindObjectsOfType<ItemBase>();
        var sameTypeItems = allItems
            .Where(item => item.itemType == this.itemType && item != this)
            .OrderBy(item => item.gameObject.name)
            .ToList();
            
        int newIndex = sameTypeItems.Count + 1;
        string newId = $"{typePrefix}_{newIndex:D2}";
        
        while (allItems.Any(item => item != this && item.ItemId == newId))
        {
            newIndex++;
            newId = $"{typePrefix}_{newIndex:D2}";
        }
        
        itemId = newId;
    }

    public virtual void RoleAction(string actionName, RoleBase role, UnityAction callback)
    {
        if (role == null)
        {
            Debug.LogError("Role is null!");
            callback?.Invoke();
            return;
        }

        // 如果角色正在移动或交互中，则忽略新的指令
        if (isAnyRoleMoving || isInteracting)
        {
            Debug.Log(isAnyRoleMoving ? "角色正在移动中，请等待当前动作完成..." : "正在进行其他交互，请稍候...");
            callback?.Invoke();
            return;
        }

        // 检查距离
        float distance = Vector3.Distance(role.transform.position, rolePos.transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"距离{itemName}太远，正在移动过去...");
            StartCoroutine(MoveToItemAndExecuteAction(actionName, role, callback));
            return;
        }

        // 如果距离合适，开始交互
        StartCoroutine(InteractionRoutine(actionName, role, callback));
    }

    protected virtual void ExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        // 基类中的默认实现，子类需要重写此方法来实现具体的交互逻辑
        Debug.Log($"正在与{itemName}进行{actionName}交互");
    }

    private IEnumerator InteractionRoutine(string actionName, RoleBase role, UnityAction callback)
    {
        isInteracting = true;
        try
        {
            // 开始交互
            Debug.Log($"开始与{itemName}进行{actionName}交互，预计需要{interactionTime}秒");
            
            // 执行实际的交互动作
            ExecuteAction(actionName, role, null); // 传入null因为我们会在交互结束后才调用真正的callback
            
            // 等待交互时间
            yield return new WaitForSeconds(interactionTime);
            
            // 交互完成
            Debug.Log($"完成与{itemName}的{actionName}交互");
        }
        finally
        {
            isInteracting = false;
            callback?.Invoke(); // 确保在交互结束后调用回调
        }
    }

    private IEnumerator MoveToItemAndExecuteAction(string actionName, RoleBase role, UnityAction callback)
    {
        isAnyRoleMoving = true;
        try
        {
            Vector3 targetPosition = rolePos.transform.position;
            role.transform.LookAt(transform.position);
            
            while (Vector3.Distance(role.transform.position, targetPosition) > 0.1f)
            {
                role.transform.position = Vector3.MoveTowards(
                    role.transform.position,
                    targetPosition,
                    defaultMoveSpeed * Time.deltaTime
                );
                yield return null;
            }

            Debug.Log($"已到达{itemName}附近，准备开始{actionName}");
            // 到达后开始交互流程
            yield return StartCoroutine(InteractionRoutine(actionName, role, callback));
        }
        finally
        {
            isAnyRoleMoving = false;
        }
    }
}

