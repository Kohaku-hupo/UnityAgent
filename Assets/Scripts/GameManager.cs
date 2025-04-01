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
    public BaiduSpeechSynthesizer speechSynthesizer;

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
        
        // 初始化语音合成组件
        InitializeSpeechSynthesizer();
    }
    
    /// <summary>
    /// 初始化百度语音合成组件
    /// </summary>
    private void InitializeSpeechSynthesizer()
    {
        // 如果已经存在则不需要创建
        if (BaiduSpeechSynthesizer.Instance != null)
        {
            speechSynthesizer = BaiduSpeechSynthesizer.Instance;
            Debug.Log("已找到现有的语音合成组件");
            return;
        }
        
        // 创建用于语音合成的GameObject
        GameObject speechObject = new GameObject("BaiduSpeechSynthesizer");
        speechSynthesizer = speechObject.AddComponent<BaiduSpeechSynthesizer>();
        
        // 添加AudioSource组件
        AudioSource audioSource = speechObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        Debug.Log("语音合成组件已初始化");
    }
}
