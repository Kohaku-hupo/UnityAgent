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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        roleManager = CTool.Find<RoleManager>(gameObject, "RoleManager");
        deepSeekAPI = CTool.Find<DeepSeekAPI>(gameObject, "DeepSeekAPI");

        // 初始化MongoDB管理器
        _ = MongoDBManager.Instance;
    }

    void Start()
    {
        Debug.Log("GameManager started");
    }
}
