using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            // if (_instance == null)
            // {
            //     // 尝试查找场景中已存在的GameManager实例
            //     _instance = FindObjectOfType<GameManager>();

            //     if (_instance == null)
            //     {
            //         // 如果没有找到，则创建一个新的GameManager实例
            //         GameObject singletonObject = new GameObject("GameManager");
            //         _instance = singletonObject.AddComponent<GameManager>();
            //     }
            // }
            return _instance;
        }
    }

    public RoleManager roleManager;
    public DeepSeekAPI deepSeekAPI;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 防止场景切换时销毁GameManager
        }
        else
        {
            Destroy(gameObject);
        }

        roleManager = CTool.Find<RoleManager>(gameObject, "RoleManager");
        deepSeekAPI = CTool.Find<DeepSeekAPI>(gameObject, "DeepSeekAPI");

    }

    void Start()
    {
    }
}
