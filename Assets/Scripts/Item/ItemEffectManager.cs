using UnityEngine;
using System.Collections.Generic;

public class ItemEffectManager : MonoBehaviour
{
    private static ItemEffectManager instance;
    public static ItemEffectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ItemEffectManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ItemEffectManager");
                    instance = go.AddComponent<ItemEffectManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [System.Serializable]
    public class ItemEffect
    {
        public string itemId;
        public string action;
        public GameObject effectPrefab;
        public float duration = 2f;
        public Vector3 offset = Vector3.zero;
        public bool attachToItem = true;
    }

    [SerializeField] private List<ItemEffect> itemEffects = new List<ItemEffect>();
    private Dictionary<string, GameObject> activeEffects = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void PlayEffect(string itemId, string action, Transform itemTransform)
    {
        var effect = itemEffects.Find(e => e.itemId == itemId && e.action == action);
        if (effect == null)
        {
            Debug.LogWarning($"未找到物品 {itemId} 的动作 {action} 的效果配置");
            return;
        }

        // 如果已经有正在播放的效果，先停止它
        if (activeEffects.ContainsKey($"{itemId}_{action}"))
        {
            StopEffect(itemId, action);
        }

        // 创建新效果
        GameObject effectObj = Instantiate(effect.effectPrefab);
        if (effect.attachToItem)
        {
            effectObj.transform.SetParent(itemTransform);
            effectObj.transform.localPosition = effect.offset;
        }
        else
        {
            effectObj.transform.position = itemTransform.position + effect.offset;
        }

        activeEffects[$"{itemId}_{action}"] = effectObj;

        // 设置自动销毁
        Destroy(effectObj, effect.duration);
    }

    public void StopEffect(string itemId, string action)
    {
        string key = $"{itemId}_{action}";
        if (activeEffects.ContainsKey(key))
        {
            Destroy(activeEffects[key]);
            activeEffects.Remove(key);
        }
    }

    public void StopAllEffects()
    {
        foreach (var effect in activeEffects.Values)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeEffects.Clear();
    }
} 