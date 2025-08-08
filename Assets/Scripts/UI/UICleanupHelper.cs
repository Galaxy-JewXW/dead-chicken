using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI清理帮助工具
/// 用于删除重复的UI元素，确保只有一个主UI系统运行
/// </summary>
public class UICleanupHelper : MonoBehaviour
{
    void Start()
    {
        // 延迟清理，确保所有UI系统都已初始化
        Invoke("CleanupDuplicateUI", 0.5f);
    }
    
    /// <summary>
    /// 清理重复的UI元素
    /// </summary>
    public void CleanupDuplicateUI()
    {
        Debug.Log("开始清理重复的UI元素...");
        
        // 1. 查找所有UIDocument组件
        var allUIDocuments = FindObjectsOfType<UIDocument>();
        Debug.Log($"找到 {allUIDocuments.Length} 个UIDocument组件");
        
        // 2. 保留主UI管理器的UIDocument，禁用其他的
        var mainUIManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (mainUIManager != null)
        {
            var mainUIDocument = mainUIManager.GetComponent<UIDocument>();
            
            foreach (var uiDoc in allUIDocuments)
            {
                if (uiDoc != mainUIDocument)
                {
                    Debug.Log($"禁用重复的UIDocument: {uiDoc.gameObject.name}");
                    uiDoc.enabled = false;
                    
                    // 清空其rootVisualElement以确保不显示任何UI
                    if (uiDoc.rootVisualElement != null)
                    {
                        uiDoc.rootVisualElement.Clear();
                        uiDoc.rootVisualElement.style.display = DisplayStyle.None;
                    }
                }
            }
        }
        
        // 3. 查找并禁用可能的UGUI Canvas
        var allCanvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvases)
        {
            // 保留场景总览等特殊用途的Canvas，但禁用可能的重复UI Canvas
            if (canvas.gameObject.name.Contains("PointCloud") || 
                canvas.gameObject.name.Contains("UI") && 
                !canvas.gameObject.name.Contains("SceneOverview"))
            {
                Debug.Log($"禁用可能重复的Canvas: {canvas.gameObject.name}");
                canvas.gameObject.SetActive(false);
            }
        }
        
        // 4. 清理可能位于屏幕底部的UI元素
        CleanupBottomUI();
        
        Debug.Log("UI清理完成");
    }
    
    /// <summary>
    /// 清理底部UI元素
    /// </summary>
    private void CleanupBottomUI()
    {
        var mainUIManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (mainUIManager == null) return;
        
        var uiDocument = mainUIManager.GetComponent<UIDocument>();
        if (uiDocument?.rootVisualElement == null) return;
        
        // 遍历所有UI元素，查找可能的底部元素
        var rootElement = uiDocument.rootVisualElement;
        CleanupBottomElementsRecursive(rootElement);
    }
    
    /// <summary>
    /// 递归清理底部元素
    /// </summary>
    private void CleanupBottomElementsRecursive(VisualElement element)
    {
        if (element == null) return;
        
        // 检查是否是底部定位的元素
        var style = element.style;
        if (style.position.value == Position.Absolute)
        {
            // 检查是否位于底部（bottom值较小或top值较大）
            if (style.bottom.value.value < 100 || style.top.value.value > Screen.height - 200)
            {
                // 排除侧栏元素
                if (style.left.value.value < 400)
                {
                    Debug.Log($"清理可能的底部UI元素");
                    element.style.display = DisplayStyle.None;
                    return;
                }
            }
        }
        
        // 递归检查子元素
        for (int i = element.childCount - 1; i >= 0; i--)
        {
            CleanupBottomElementsRecursive(element[i]);
        }
    }
    
    /// <summary>
    /// 手动触发UI清理（可在Inspector中调用）
    /// </summary>
    [ContextMenu("清理重复UI")]
    public void ManualCleanup()
    {
        CleanupDuplicateUI();
    }
} 