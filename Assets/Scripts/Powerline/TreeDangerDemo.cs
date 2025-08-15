using UnityEngine;
using System.Collections;

/// <summary>
/// 树木危险监测系统启动器
/// 负责初始化、配置和启动树木危险监测系统
/// </summary>
public class TreeDangerDemo : MonoBehaviour
{
    [Header("系统配置")]
    public bool autoStartSystem = true;
    public bool enableDebugMode = false;
    
    [Header("危险评估参数")]
    public float criticalDistance = 10f;  // 危险距离
    public float warningDistance = 30f;   // 警告距离
    public float safeDistance = 50f;      // 安全距离
    
    [Header("监测配置")]
    public float monitoringInterval = 5f;
    public float maxDetectionDistance = 100f;
    
    [Header("树木生长参数")]
    public float baseGrowthRate = 0.1f;
    public float maxTreeHeight = 50f;
    public float seasonalGrowthFactor = 0.2f;
    
    [Header("电力线安全参数")]
    public float powerlineHeight = 20f;
    public float powerlineSag = 2f;
    public float windSwayFactor = 1.5f;
    
    private TreeDangerMonitor treeDangerMonitor;
    private bool systemInitialized = false;
    
    void Start()
    {
        if (autoStartSystem)
        {
            StartCoroutine(InitializeSystem());
        }
    }
    
    /// <summary>
    /// 初始化监测系统
    /// </summary>
    IEnumerator InitializeSystem()
    {
        Debug.Log("=== 启动树木危险监测系统 ===");
        
        // 等待一帧确保所有组件都已初始化
        yield return null;
        
        // 查找或创建TreeDangerMonitor
        treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        if (treeDangerMonitor == null)
        {
            GameObject monitorObj = new GameObject("TreeDangerMonitor");
            treeDangerMonitor = monitorObj.AddComponent<TreeDangerMonitor>();
            Debug.Log("已创建TreeDangerMonitor组件");
        }
        
        // 配置监测参数
        ConfigureMonitoringSystem();
        
        // 启动监测
        StartMonitoring();
        
        systemInitialized = true;
        Debug.Log("=== 树木危险监测系统启动完成 ===");
        
        // 显示系统状态
        ShowSystemStatus();
    }
    
    /// <summary>
    /// 配置监测系统参数
    /// </summary>
    void ConfigureMonitoringSystem()
    {
        if (treeDangerMonitor == null) return;
        
        // 设置危险评估参数
        treeDangerMonitor.criticalDistance = criticalDistance;
        treeDangerMonitor.warningDistance = warningDistance;
        treeDangerMonitor.safeDistance = safeDistance;
        
        // 设置监测配置
        treeDangerMonitor.monitoringInterval = monitoringInterval;
        treeDangerMonitor.maxDetectionDistance = maxDetectionDistance;
        
        // 设置树木生长参数
        treeDangerMonitor.baseGrowthRate = baseGrowthRate;
        treeDangerMonitor.maxTreeHeight = maxTreeHeight;
        treeDangerMonitor.seasonalGrowthFactor = seasonalGrowthFactor;
        
        // 设置电力线安全参数
        treeDangerMonitor.powerlineHeight = powerlineHeight;
        treeDangerMonitor.powerlineSag = powerlineSag;
        treeDangerMonitor.windSwayFactor = windSwayFactor;
        
        Debug.Log("监测系统参数配置完成");
    }
    
    /// <summary>
    /// 启动监测
    /// </summary>
    void StartMonitoring()
    {
        if (treeDangerMonitor == null) return;
        
        Debug.Log("启动树木危险监测...");
        treeDangerMonitor.enableAutoMonitoring = true;
        
        // 手动触发一次监测
        treeDangerMonitor.ManualMonitoring();
    }
    
    /// <summary>
    /// 手动触发监测
    /// </summary>
    [ContextMenu("手动触发监测")]
    public void ManualTriggerMonitoring()
    {
        if (treeDangerMonitor != null)
        {
            treeDangerMonitor.ManualMonitoring();
            Debug.Log("手动触发监测完成");
        }
        else
        {
            Debug.LogWarning("TreeDangerMonitor未找到");
        }
    }
    
    /// <summary>
    /// 显示系统状态
    /// </summary>
    [ContextMenu("显示系统状态")]
    public void ShowSystemStatus()
    {
        if (treeDangerMonitor == null)
        {
            Debug.Log("TreeDangerMonitor未找到");
            return;
        }
        
        var stats = treeDangerMonitor.GetDangerStatistics();
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        
        Debug.Log($"=== 系统状态 ===\n" +
                 $"系统初始化: {systemInitialized}\n" +
                 $"自动监测: {treeDangerMonitor.enableAutoMonitoring}\n" +
                 $"监测间隔: {treeDangerMonitor.monitoringInterval}秒\n" +
                 $"危险统计: {stats.Count} 个等级\n" +
                 $"危险情况: {allDangerInfo.Count} 个");
        
        if (allDangerInfo.Count > 0)
        {
            Debug.Log("危险树木列表:");
            foreach (var dangerInfo in allDangerInfo)
            {
                Debug.Log($"- {dangerInfo.tree.name}: {dangerInfo.dangerLevel} (距离: {dangerInfo.currentDistance:F1}m)");
            }
        }
    }
    
    /// <summary>
    /// 重新配置系统
    /// </summary>
    [ContextMenu("重新配置系统")]
    public void ReconfigureSystem()
    {
        if (treeDangerMonitor != null)
        {
            ConfigureMonitoringSystem();
            Debug.Log("系统重新配置完成");
        }
    }
    
    /// <summary>
    /// 重置系统
    /// </summary>
    [ContextMenu("重置系统")]
    public void ResetSystem()
    {
        Debug.Log("重置树木危险监测系统...");
        
        if (treeDangerMonitor != null)
        {
            treeDangerMonitor.enableAutoMonitoring = false;
            DestroyImmediate(treeDangerMonitor.gameObject);
            treeDangerMonitor = null;
        }
        
        systemInitialized = false;
        
        // 重新初始化
        StartCoroutine(InitializeSystem());
    }
    
    /// <summary>
    /// 导出监测数据
    /// </summary>
    [ContextMenu("导出监测数据")]
    public void ExportMonitoringData()
    {
        if (treeDangerMonitor != null)
        {
            var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
            Debug.Log($"导出监测数据: {allDangerInfo.Count} 条记录");
            
            // 这里可以添加CSV导出逻辑
            // treeDangerMonitor.ExportToCSV();
        }
    }
    
    void OnValidate()
    {
        // 在Inspector中修改参数时自动更新系统配置
        if (Application.isPlaying && treeDangerMonitor != null && systemInitialized)
        {
            ConfigureMonitoringSystem();
        }
    }
}
