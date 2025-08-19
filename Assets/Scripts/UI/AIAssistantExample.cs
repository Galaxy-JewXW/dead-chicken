using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// AI助手使用示例 - 展示如何集成和使用AI助手系统
/// </summary>
public class AIAssistantExample : MonoBehaviour
{
    [Header("示例设置")]
    public bool showExampleOnStart = true;
    public bool enableDemoMode = true;
    
    [Header("组件引用")]
    public AIAssistantManager aiAssistant;
    public AIAssistantInitializer initializer;
    
    [Header("演示功能")]
    public bool autoShowWelcome = true;
    public float demoDelay = 2f;
    
    void Start()
    {
        if (showExampleOnStart)
        {
            SetupExample();
        }
    }
    
    /// <summary>
    /// 设置示例
    /// </summary>
    [ContextMenu("设置示例")]
    public void SetupExample()
    {
        // 查找或创建AI助手
        FindOrCreateAIAssistant();
        
        // 设置演示模式
        if (enableDemoMode)
        {
            SetupDemoMode();
        }
        
        Debug.Log("AI助手示例设置完成");
    }
    
    /// <summary>
    /// 查找或创建AI助手
    /// </summary>
    private void FindOrCreateAIAssistant()
    {
        // 首先尝试查找现有的AI助手
        aiAssistant = FindObjectOfType<AIAssistantManager>();
        
        if (aiAssistant == null)
        {
            // 查找初始化器
            initializer = FindObjectOfType<AIAssistantInitializer>();
            
            if (initializer == null)
            {
                // 创建初始化器
                GameObject initObject = new GameObject("AI Assistant Example");
                initializer = initObject.AddComponent<AIAssistantInitializer>();
                initObject.transform.SetParent(transform);
                
                Debug.Log("创建了AI助手示例初始化器");
            }
            
            // 初始化AI助手
            initializer.InitializeAIAssistant();
            aiAssistant = initializer.GetComponentInChildren<AIAssistantManager>();
        }
        
        if (aiAssistant == null)
        {
            Debug.LogError("无法创建或找到AI助手");
            return;
        }
    }
    
    /// <summary>
    /// 设置演示模式
    /// </summary>
    private void SetupDemoMode()
    {
        if (autoShowWelcome)
        {
            StartCoroutine(ShowWelcomeDelayed());
        }
    }
    
    /// <summary>
    /// 延迟显示欢迎信息
    /// </summary>
    private System.Collections.IEnumerator ShowWelcomeDelayed()
    {
        yield return new WaitForSeconds(demoDelay);
        
        if (aiAssistant != null)
        {
            // 显示AI助手
            aiAssistant.ToggleChatPanel(true);
            
            // 添加演示消息
            AddDemoMessages();
        }
    }
    
    /// <summary>
    /// 添加演示消息
    /// </summary>
    private void AddDemoMessages()
    {
        if (aiAssistant == null) return;
        
        // 这里可以添加一些演示消息
        Debug.Log("AI助手演示模式已启动");
    }
    
    /// <summary>
    /// 显示AI助手
    /// </summary>
    [ContextMenu("显示AI助手")]
    public void ShowAIAssistant()
    {
        if (aiAssistant != null)
        {
            aiAssistant.ToggleChatPanel(true);
        }
        else
        {
            Debug.LogWarning("AI助手未初始化，请先运行SetupExample");
        }
    }
    
    /// <summary>
    /// 隐藏AI助手
    /// </summary>
    [ContextMenu("隐藏AI助手")]
    public void HideAIAssistant()
    {
        if (aiAssistant != null)
        {
            aiAssistant.ToggleChatPanel(false);
        }
    }
    
    /// <summary>
    /// 测试AI助手功能
    /// </summary>
    [ContextMenu("测试AI助手")]
    public void TestAIAssistant()
    {
        if (aiAssistant == null)
        {
            Debug.LogWarning("AI助手未初始化");
            return;
        }
        
        // 显示AI助手
        ShowAIAssistant();
        
        // 这里可以添加更多测试逻辑
        Debug.Log("AI助手测试已启动");
    }
    
    /// <summary>
    /// 重置示例
    /// </summary>
    [ContextMenu("重置示例")]
    public void ResetExample()
    {
        // 清理现有AI助手
        if (aiAssistant != null)
        {
            DestroyImmediate(aiAssistant.gameObject);
            aiAssistant = null;
        }
        
        if (initializer != null)
        {
            DestroyImmediate(initializer.gameObject);
            initializer = null;
        }
        
        Debug.Log("AI助手示例已重置");
    }
    
    /// <summary>
    /// 在编辑器中显示状态
    /// </summary>
    void OnValidate()
    {
        // 更新Inspector显示
        if (aiAssistant != null)
        {
            // AI助手已存在
        }
    }
    
    void OnDestroy()
    {
        // 清理引用
        aiAssistant = null;
        initializer = null;
    }
}
