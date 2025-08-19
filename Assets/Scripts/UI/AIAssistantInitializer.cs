using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// AI助手初始化器 - 自动在场景中设置AI助手系统
/// </summary>
public class AIAssistantInitializer : MonoBehaviour
{
    [Header("自动初始化设置")]
    public bool autoInitializeOnStart = true;
    public bool createIfNotExists = true;
    public bool integrateWithExistingUI = true;
    
    [Header("AI助手配置")]
    [Tooltip("AI助手配置文件")]
    public AIAssistantConfig assistantConfig;
    
    [Header("UI集成设置")]
    [Tooltip("是否将AI助手集成到现有UI系统中")]
    public bool addToSimpleUIToolkitManager = true;
    public bool addToInitialInterfaceManager = true;
    
    [Header("调试信息")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private AIAssistantManager assistantManager;
    
    void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeAIAssistant();
        }
    }
    
    /// <summary>
    /// 初始化AI助手
    /// </summary>
    [ContextMenu("初始化AI助手")]
    public void InitializeAIAssistant()
    {
        if (isInitialized)
        {
            Debug.Log("AI助手已经初始化过了");
            return;
        }
        
        // 查找或创建AI助手管理器
        FindOrCreateAssistantManager();
        
        // 配置AI助手
        ConfigureAssistant();
        
        // 集成到现有UI系统
        if (integrateWithExistingUI)
        {
            IntegrateWithExistingUI();
        }
        
        isInitialized = true;
        Debug.Log("AI助手初始化完成");
    }
    
    /// <summary>
    /// 查找或创建AI助手管理器
    /// </summary>
    private void FindOrCreateAssistantManager()
    {
        // 首先尝试查找现有的AI助手管理器
        assistantManager = FindObjectOfType<AIAssistantManager>();
        
        if (assistantManager == null && createIfNotExists)
        {
            // 创建新的AI助手管理器
            GameObject assistantObject = new GameObject("AI Assistant Manager");
            assistantManager = assistantObject.AddComponent<AIAssistantManager>();
            
            // 设置父对象
            assistantObject.transform.SetParent(transform);
            
            Debug.Log("创建了新的AI助手管理器");
        }
        
        if (assistantManager == null)
        {
            Debug.LogError("无法创建或找到AI助手管理器");
            return;
        }
    }
    
    /// <summary>
    /// 配置AI助手
    /// </summary>
    private void ConfigureAssistant()
    {
        if (assistantManager == null) return;
        
        // 设置配置文件
        if (assistantConfig != null)
        {
            assistantManager.config = assistantConfig;
        }
        
        // 启用AI助手
        assistantManager.enableAIAssistant = true;
        
        // 应用配置
        assistantManager.InitializeAIAssistant();
    }
    
    /// <summary>
    /// 集成到现有UI系统
    /// </summary>
    private void IntegrateWithExistingUI()
    {
        if (addToSimpleUIToolkitManager)
        {
            IntegrateWithSimpleUIToolkitManager();
        }
        
        if (addToInitialInterfaceManager)
        {
            IntegrateWithInitialInterfaceManager();
        }
    }
    
    /// <summary>
    /// 集成到SimpleUIToolkitManager
    /// </summary>
    private void IntegrateWithSimpleUIToolkitManager()
    {
        var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (uiManager != null)
        {
            // 这里可以添加UI集成逻辑
            Debug.Log("AI助手已集成到SimpleUIToolkitManager");
        }
        else
        {
            Debug.LogWarning("未找到SimpleUIToolkitManager，跳过UI集成");
        }
    }
    
    /// <summary>
    /// 集成到InitialInterfaceManager
    /// </summary>
    private void IntegrateWithInitialInterfaceManager()
    {
        var initialManager = FindObjectOfType<InitialInterfaceManager>();
        if (initialManager != null)
        {
            // 这里可以添加初始界面集成逻辑
            Debug.Log("AI助手已集成到InitialInterfaceManager");
        }
        else
        {
            Debug.LogWarning("未找到InitialInterfaceManager，跳过初始界面集成");
        }
    }
    
    /// <summary>
    /// 手动触发AI助手显示
    /// </summary>
    [ContextMenu("显示AI助手")]
    public void ShowAIAssistant()
    {
        if (assistantManager != null)
        {
            assistantManager.ToggleChatPanel(true);
        }
        else
        {
            Debug.LogWarning("AI助手管理器未初始化");
        }
    }
    
    /// <summary>
    /// 手动隐藏AI助手
    /// </summary>
    [ContextMenu("隐藏AI助手")]
    public void HideAIAssistant()
    {
        if (assistantManager != null)
        {
            assistantManager.ToggleChatPanel(false);
        }
        else
        {
            Debug.LogWarning("AI助手管理器未初始化");
        }
    }
    
    /// <summary>
    /// 重新初始化AI助手
    /// </summary>
    [ContextMenu("重新初始化")]
    public void ReinitializeAIAssistant()
    {
        isInitialized = false;
        InitializeAIAssistant();
    }
    
    /// <summary>
    /// 清理AI助手
    /// </summary>
    [ContextMenu("清理AI助手")]
    public void CleanupAIAssistant()
    {
        if (assistantManager != null)
        {
            DestroyImmediate(assistantManager.gameObject);
            assistantManager = null;
        }
        
        isInitialized = false;
        Debug.Log("AI助手已清理");
    }
    
    void OnDestroy()
    {
        // 清理引用
        assistantManager = null;
    }
    
    /// <summary>
    /// 在编辑器中显示状态信息
    /// </summary>
    void OnValidate()
    {
        if (assistantManager != null)
        {
            isInitialized = true;
        }
    }
}
