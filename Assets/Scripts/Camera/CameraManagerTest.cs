using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CameraManager功能测试脚本
/// 用于测试自动寻找最近电塔的功能
/// </summary>
public class CameraManagerTest : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("是否在Start时自动测试")]
    public bool autoTestOnStart = true;
    
    [Tooltip("测试间隔时间（秒）")]
    public float testInterval = 5f;
    
    [Header("UI组件")]
    public Button testButton;
    public Text statusText;
    
    private CameraManager cameraManager;
    private float lastTestTime;
    
    void Start()
    {
        // 获取CameraManager组件
        cameraManager = FindObjectOfType<CameraManager>();
        
        if (cameraManager == null)
        {
            Debug.LogError("CameraManagerTest: 未找到CameraManager组件！");
            return;
        }
        
        // 设置UI按钮事件
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestCameraManager);
        }
        
        // 自动测试
        if (autoTestOnStart)
        {
            Invoke("TestCameraManager", 2f); // 延迟2秒开始测试
        }
        
        lastTestTime = Time.time;
    }
    
    void Update()
    {
        // 定期测试
        if (autoTestOnStart && Time.time - lastTestTime > testInterval)
        {
            TestCameraManager();
            lastTestTime = Time.time;
        }
    }
    
    /// <summary>
    /// 测试CameraManager功能
    /// </summary>
    public void TestCameraManager()
    {
        if (cameraManager == null)
        {
            UpdateStatus("错误：未找到CameraManager组件");
            return;
        }
        
        UpdateStatus("开始测试CameraManager功能...");
        
        // 测试1：检查当前视角
        int currentView = cameraManager.GetCurrentView();
        string viewName = cameraManager.GetCurrentViewName();
        UpdateStatus($"当前视角: {viewName} (索引: {currentView})");
        
        // 测试2：测试视角切换
        StartCoroutine(TestViewSwitching());
    }
    
    /// <summary>
    /// 测试视角切换功能
    /// </summary>
    private System.Collections.IEnumerator TestViewSwitching()
    {
        UpdateStatus("测试视角切换功能...");
        
        // 测试第一人称视角
        UpdateStatus("切换到第一人称视角...");
        cameraManager.SwitchView(0);
        yield return new WaitForSeconds(1f);
        
        // 测试上帝视角
        UpdateStatus("切换到上帝视角...");
        cameraManager.SwitchView(1);
        yield return new WaitForSeconds(1f);
        
        // 测试飞行视角
        UpdateStatus("切换到飞行视角...");
        cameraManager.SwitchView(2);
        yield return new WaitForSeconds(1f);
        
        UpdateStatus("视角切换测试完成！");
        
        // 测试刷新功能
        UpdateStatus("测试位置刷新功能...");
        cameraManager.RefreshCurrentViewPosition();
        yield return new WaitForSeconds(0.5f);
        
        UpdateStatus("所有测试完成！");
    }
    
    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatus(string message)
    {
        Debug.Log($"[CameraManagerTest] {message}");
        
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    /// <summary>
    /// 手动测试按钮点击事件
    /// </summary>
    public void OnTestButtonClick()
    {
        TestCameraManager();
    }
    
    /// <summary>
    /// 切换自动寻找电塔功能
    /// </summary>
    public void ToggleAutoFindTower()
    {
        if (cameraManager != null)
        {
            // 获取当前状态并切换
            bool currentState = cameraManager.GetAutoFindNearestTower();
            cameraManager.SetAutoFindNearestTower(!currentState);
            UpdateStatus($"自动寻找电塔功能已{(currentState ? "禁用" : "启用")}");
        }
    }
    
    /// <summary>
    /// 强制刷新当前视角位置
    /// </summary>
    public void RefreshCurrentView()
    {
        if (cameraManager != null)
        {
            cameraManager.RefreshCurrentViewPosition();
            UpdateStatus("已刷新当前视角位置");
        }
    }
}
