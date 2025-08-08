using UnityEngine;
using PowerlineSystem;

/// <summary>
/// 场景启动器 - 负责在场景启动时配置初始界面和相关组件
/// </summary>
public class SceneStarter : MonoBehaviour
{
    [Header("组件配置")]
    [Tooltip("场景初始化器")]
    public SceneInitializer sceneInitializer;
    
    [Tooltip("电力线提取管理器")]
    public PowerLineExtractorManager powerLineExtractorManager;
    
    [Tooltip("UI管理器")]
    public SimpleUIToolkitManager uiManager;
    
    [Tooltip("初始界面管理器")]
    public InitialInterfaceManager initialInterfaceManager;
    
    [Header("启动设置")]
    [Tooltip("是否在启动时显示初始界面")]
    public bool showInitialInterface = true;
    
    [Tooltip("是否禁用SceneInitializer的自动初始化")]
    public bool disableAutoInitialization = true;
    
    void Awake()
    {
        ConfigureComponents();
    }
    
    void Start()
    {
        // 延迟显示初始界面，确保所有组件都已初始化
        if (showInitialInterface)
        {
            Invoke("ShowInitialInterface", 0.5f);
        }
    }
    
    /// <summary>
    /// 配置组件
    /// </summary>
    void ConfigureComponents()
    {
        // 查找或创建SceneInitializer
        if (sceneInitializer == null)
        {
            sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer == null)
            {
                GameObject sceneInitObj = new GameObject("SceneInitializer");
                sceneInitializer = sceneInitObj.AddComponent<SceneInitializer>();
                Debug.Log("已创建SceneInitializer");
            }
        }
        
        // 配置SceneInitializer
        if (sceneInitializer != null && disableAutoInitialization)
        {
            sceneInitializer.autoInitializeOnStart = false;
        }
        
        // 查找或创建PowerLineExtractorManager
        if (powerLineExtractorManager == null)
        {
            powerLineExtractorManager = FindObjectOfType<PowerLineExtractorManager>();
            if (powerLineExtractorManager == null)
            {
                GameObject extractorObj = new GameObject("PowerLineExtractorManager");
                powerLineExtractorManager = extractorObj.AddComponent<PowerLineExtractorManager>();
                Debug.Log("已创建PowerLineExtractorManager");
            }
        }
        
        // 查找或创建SimpleUIToolkitManager
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (uiManager == null)
            {
                GameObject uiObj = new GameObject("SimpleUIToolkitManager");
                uiManager = uiObj.AddComponent<SimpleUIToolkitManager>();
                Debug.Log("已创建SimpleUIToolkitManager");
            }
        }
        
        // 查找或创建InitialInterfaceManager
        if (initialInterfaceManager == null)
        {
            initialInterfaceManager = FindObjectOfType<InitialInterfaceManager>();
            if (initialInterfaceManager == null)
            {
                GameObject initialObj = new GameObject("InitialInterfaceManager");
                initialInterfaceManager = initialObj.AddComponent<InitialInterfaceManager>();
                Debug.Log("已创建InitialInterfaceManager");
            }
        }
        
        // 配置InitialInterfaceManager的组件引用
        if (initialInterfaceManager != null)
        {
            initialInterfaceManager.sceneInitializer = sceneInitializer;
            initialInterfaceManager.powerLineExtractorManager = powerLineExtractorManager;
            initialInterfaceManager.uiManager = uiManager;
        }
        
        // 配置SimpleUIToolkitManager
        if (uiManager != null)
        {
            uiManager.showInitialInterfaceOnStart = showInitialInterface;
        }
        
        Debug.Log("场景启动器配置完成");
    }
    
    /// <summary>
    /// 显示初始界面
    /// </summary>
    void ShowInitialInterface()
    {
        if (initialInterfaceManager != null)
        {
            initialInterfaceManager.ShowInitialInterface();
        }
    }
    
    /// <summary>
    /// 隐藏初始界面
    /// </summary>
    public void HideInitialInterface()
    {
        if (initialInterfaceManager != null)
        {
            initialInterfaceManager.HideInitialInterface();
        }
    }
    
    /// <summary>
    /// 手动初始化场景（用于测试）
    /// </summary>
    [ContextMenu("手动初始化场景")]
    public void ManualInitializeScene()
    {
        if (sceneInitializer != null)
        {
            sceneInitializer.InitializeScene();
        }
    }
    
    /// <summary>
    /// 重置场景（清理所有对象）
    /// </summary>
    [ContextMenu("重置场景")]
    public void ResetScene()
    {
        // 清理电塔
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (var tower in towers)
        {
            DestroyImmediate(tower);
        }
        
        // 清理电力线
        GameObject[] powerlines = GameObject.FindGameObjectsWithTag("Powerline");
        foreach (var powerline in powerlines)
        {
            DestroyImmediate(powerline);
        }
        
        // 清理其他相关对象
        GameObject[] powerlineObjects = GameObject.FindGameObjectsWithTag("PowerlineObject");
        foreach (var obj in powerlineObjects)
        {
            DestroyImmediate(obj);
        }
        
        Debug.Log("场景已重置");
    }
} 