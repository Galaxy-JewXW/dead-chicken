using UnityEngine;
using System.Collections;

/// <summary>
/// AI API 测试脚本 - 简化版本
/// </summary>
public class AIModelTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool autoTestOnStart = true;
    
    void Start()
    {
        if (autoTestOnStart)
        {
            StartCoroutine(DelayedTest());
        }
    }
    
    private IEnumerator DelayedTest()
    {
        // 等待2秒让AIAPIManager初始化
        yield return new WaitForSeconds(2f);
        
        Debug.Log("=== 开始AI API测试 ===");
        TestAIAPI();
    }
    
    /// <summary>
    /// 测试AI API
    /// </summary>
    [ContextMenu("测试AI API")]
    public void TestAIAPI()
    {
        if (AIAPIManager.Instance == null)
        {
            Debug.LogError("AIAPIManager实例不存在！");
            return;
        }
        
        Debug.Log("开始测试AI API...");
        
        AIAPIManager.Instance.SendMessage("你好，请简单回复'测试成功'", 
            (response) => {
                Debug.Log($"✅ AI API测试成功！回复: {response}");
            },
            (error) => {
                Debug.LogError($"❌ AI API测试失败: {error}");
            });
    }
    
    /// <summary>
    /// 发送自定义消息
    /// </summary>
    [ContextMenu("发送测试消息")]
    public void SendTestMessage()
    {
        if (AIAPIManager.Instance == null)
        {
            Debug.LogError("AIAPIManager实例不存在！");
            return;
        }
        
        AIAPIManager.Instance.SendMessage("请介绍一下你自己", 
            (response) => {
                Debug.Log($"AI回复: {response}");
            },
            (error) => {
                Debug.LogError($"发送失败: {error}");
            });
    }
    
    /// <summary>
    /// 清空对话历史
    /// </summary>
    [ContextMenu("清空对话历史")]
    public void ClearHistory()
    {
        if (AIAPIManager.Instance != null)
        {
            AIAPIManager.Instance.ClearHistory();
        }
    }
}
