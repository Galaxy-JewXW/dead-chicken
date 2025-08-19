using UnityEngine;

/// <summary>
/// AI API 管理器初始化器 - 简化版本
/// </summary>
public class AIAPIManagerInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    public bool autoInitializeOnStart = true;
    public bool createIfNotExists = true;
    
    void Start()
    {
        if (autoInitializeOnStart)
        {
            InitializeAIAPI();
        }
    }
    
    /// <summary>
    /// 初始化AI API
    /// </summary>
    [ContextMenu("初始化AI API")]
    public void InitializeAIAPI()
    {
        Debug.Log("开始初始化AI API...");
        
        // 查找现有的AIAPIManager
        AIAPIManager existingManager = FindObjectOfType<AIAPIManager>();
        
        if (existingManager == null && createIfNotExists)
        {
            // 创建新的AIAPIManager
            GameObject apiObject = new GameObject("AI API Manager");
            apiObject.AddComponent<AIAPIManager>();
            
            Debug.Log("✅ 创建了新的AIAPIManager");
        }
        else if (existingManager != null)
        {
            Debug.Log("✅ 找到现有的AIAPIManager");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到AIAPIManager且未启用自动创建");
        }
        
        Debug.Log("AI API初始化完成");
    }
    
    /// <summary>
    /// 测试AI API连接
    /// </summary>
    [ContextMenu("测试AI API连接")]
    public void TestAIAPI()
    {
        AIAPIManager manager = FindObjectOfType<AIAPIManager>();
        
        if (manager != null)
        {
            Debug.Log("开始测试AI API连接...");
            manager.TestConnection();
        }
        else
        {
            Debug.LogError("未找到AIAPIManager，请先初始化");
        }
    }
    
    /// <summary>
    /// 发送测试消息
    /// </summary>
    [ContextMenu("发送测试消息")]
    public void SendTestMessage()
    {
        AIAPIManager manager = FindObjectOfType<AIAPIManager>();
        
        if (manager != null)
        {
            Debug.Log("发送测试消息...");
            manager.SendMessage("你好，这是一个测试消息", 
                (response) => {
                    Debug.Log($"✅ 收到回复: {response}");
                },
                (error) => {
                    Debug.LogError($"❌ 发送失败: {error}");
                });
        }
        else
        {
            Debug.LogError("未找到AIAPIManager，请先初始化");
        }
    }
}
