using UnityEngine;

/// <summary>
/// 相机配置器 - 用于快速调整相机设置
/// </summary>
public class CameraConfigurator : MonoBehaviour
{
    [Header("相机管理器引用")]
    public CameraManager cameraManager;
    
    [Header("预设配置")]
    [Tooltip("电力线分析视角")]
    public bool usePowerlineAnalysisView = true;
    
    [Header("上帝视角配置")]
    [Tooltip("初始高度（米）")]
    public float initialGodViewHeight = 200f;
    [Tooltip("初始俯视角度（度）")]
    public float initialGodViewAngle = 45f;
    [Tooltip("初始位置偏移")]
    public Vector3 godViewOffset = new Vector3(0, 0, -100f);
    
    [Header("第一人称视角配置")]
    [Tooltip("初始高度（米）")]
    public float initialFirstPersonHeight = 3f;
    [Tooltip("初始位置偏移")]
    public Vector3 firstPersonOffset = new Vector3(0, 0, -10f);
    
    [Header("飞行视角配置")]
    [Tooltip("初始位置")]
    public Vector3 initialFlyViewPosition = new Vector3(30, 20, 30);
    
    void Start()
    {
        // 自动查找相机管理器
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }
        
        // 应用预设配置
        if (usePowerlineAnalysisView)
        {
            ApplyPowerlineAnalysisConfig();
        }
    }
    
    /// <summary>
    /// 应用电力线分析配置
    /// </summary>
    void ApplyPowerlineAnalysisConfig()
    {
        if (cameraManager == null) return;
        
        Debug.Log("应用电力线分析相机配置...");
        
        // 配置上帝视角位置
        if (cameraManager.godView != null)
        {
            Vector3 godViewPos = Vector3.zero + godViewOffset;
            godViewPos.y = initialGodViewHeight;
            cameraManager.godView.position = godViewPos;
            
            // 设置俯视角度
            Vector3 rotation = cameraManager.godView.eulerAngles;
            rotation.x = initialGodViewAngle;
            cameraManager.godView.eulerAngles = rotation;
        }
        
        // 配置第一人称视角位置
        if (cameraManager.firstPersonView != null)
        {
            Vector3 fpViewPos = Vector3.zero + firstPersonOffset;
            fpViewPos.y = initialFirstPersonHeight;
            cameraManager.firstPersonView.position = fpViewPos;
        }
        
        // 配置飞行视角位置
        if (cameraManager.flyView != null)
        {
            cameraManager.flyView.position = initialFlyViewPosition;
        }
        
        // 设置默认视角为飞行视角
        SetDefaultViewToFlyView();
        
        Debug.Log("电力线分析相机配置已应用");
    }
    
    /// <summary>
    /// 设置默认视角为飞行视角
    /// </summary>
    [ContextMenu("设置默认视角为飞行视角")]
    public void SetDefaultViewToFlyView()
    {
        if (cameraManager != null)
        {
            cameraManager.SwitchView(2);
            Debug.Log("默认视角已设置为飞行视角");
        }
    }
    
    /// <summary>
    /// 快速切换到上帝视角
    /// </summary>
    [ContextMenu("切换到上帝视角")]
    public void SwitchToGodView()
    {
        if (cameraManager != null)
        {
            cameraManager.SwitchView(1);
            Debug.Log("已切换到上帝视角");
        }
    }
    
    /// <summary>
    /// 快速切换到第一人称视角
    /// </summary>
    [ContextMenu("切换到第一人称视角")]
    public void SwitchToFirstPersonView()
    {
        if (cameraManager != null)
        {
            cameraManager.SwitchView(0);
            Debug.Log("已切换到第一人称视角");
        }
    }
    
    /// <summary>
    /// 快速切换到飞行视角
    /// </summary>
    [ContextMenu("切换到飞行视角")]
    public void SwitchToFlyView()
    {
        if (cameraManager != null)
        {
            cameraManager.SwitchView(2);
            Debug.Log("已切换到飞行视角");
        }
    }
    
    /// <summary>
    /// 重置相机位置
    /// </summary>
    [ContextMenu("重置相机位置")]
    public void ResetCameraPositions()
    {
        if (cameraManager == null) return;
        
        Debug.Log("重置相机位置...");
        
        // 重置上帝视角
        if (cameraManager.godView != null)
        {
            cameraManager.godView.position = new Vector3(0, initialGodViewHeight, -100);
            cameraManager.godView.eulerAngles = new Vector3(initialGodViewAngle, 0, 0);
        }
        
        // 重置第一人称视角
        if (cameraManager.firstPersonView != null)
        {
            cameraManager.firstPersonView.position = new Vector3(0, initialFirstPersonHeight, -10);
            cameraManager.firstPersonView.eulerAngles = Vector3.zero;
        }
        
        // 重置飞行视角
        if (cameraManager.flyView != null)
        {
            cameraManager.flyView.position = initialFlyViewPosition;
            cameraManager.flyView.eulerAngles = Vector3.zero;
        }
        
        Debug.Log("相机位置已重置");
    }
    
    /// <summary>
    /// 应用当前配置
    /// </summary>
    [ContextMenu("应用当前配置")]
    public void ApplyCurrentConfig()
    {
        ApplyPowerlineAnalysisConfig();
    }
    
    /// <summary>
    /// 设置上帝视角到指定位置
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="height">高度</param>
    /// <param name="angle">俯视角度</param>
    public void SetGodViewPosition(Vector3 position, float height, float angle)
    {
        if (cameraManager != null && cameraManager.godView != null)
        {
            Vector3 newPosition = position;
            newPosition.y = height;
            cameraManager.godView.position = newPosition;
            
            Vector3 rotation = cameraManager.godView.eulerAngles;
            rotation.x = angle;
            cameraManager.godView.eulerAngles = rotation;
            
            Debug.Log($"上帝视角位置已设置: {newPosition}, 角度: {angle}°");
        }
    }
    
    /// <summary>
    /// 设置第一人称视角到指定位置
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <param name="height">高度</param>
    public void SetFirstPersonPosition(Vector3 position, float height)
    {
        if (cameraManager != null && cameraManager.firstPersonView != null)
        {
            Vector3 newPosition = position;
            newPosition.y = height;
            cameraManager.firstPersonView.position = newPosition;
            
            Debug.Log($"第一人称视角位置已设置: {newPosition}");
        }
    }
    
    /// <summary>
    /// 获取当前视角信息
    /// </summary>
    [ContextMenu("获取当前视角信息")]
    public void GetCurrentViewInfo()
    {
        if (cameraManager != null)
        {
            int currentView = cameraManager.GetCurrentView();
            string viewName = cameraManager.GetCurrentViewName();
            
            Debug.Log($"当前视角: {viewName} (索引: {currentView})");
            
            // 显示当前相机位置
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Debug.Log($"相机位置: {mainCamera.transform.position}");
                Debug.Log($"相机旋转: {mainCamera.transform.eulerAngles}");
            }
        }
    }
} 