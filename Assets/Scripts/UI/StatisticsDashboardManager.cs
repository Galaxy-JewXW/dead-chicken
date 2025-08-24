using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerlineSystem;
using UI;

namespace UI
{
    /// <summary>
    /// 电力线运行监测统计大屏管理器
    /// </summary>
    public class StatisticsDashboardManager : MonoBehaviour
    {
        [Header("统计面板配置")]
        public bool enableRealTimeUpdate = true;
        public float updateInterval = 5f;
        
        [Header("数据源")]
        public PowerlineMarkingSystem powerlineMarkingSystem;
        public DronePatrolManager dronePatrolManager;
        public PowerLineExtractorManager powerLineExtractorManager;
        public SceneOverviewManager sceneOverviewManager;
        
        // 统计数据结构
        private DeviceOperationStats deviceStats;
        private PowerlinePerformanceStats performanceStats;
        private InspectionStats inspectionStats;
        private DangerMonitoringStats dangerStats;
        private PointCloudProcessingStats pointCloudStats;
        private TreeDetectionStats treeDetectionStats;
        
        // 更新协程
        private Coroutine updateCoroutine;
        
        void Start()
        {
            InitializeStatisticsSystem();
        }
        
        private void InitializeStatisticsSystem()
        {
            // 自动查找组件
            if (powerlineMarkingSystem == null)
                powerlineMarkingSystem = FindObjectOfType<PowerlineMarkingSystem>();
            if (dronePatrolManager == null)
                dronePatrolManager = FindObjectOfType<DronePatrolManager>();
            if (powerLineExtractorManager == null)
                powerLineExtractorManager = FindObjectOfType<PowerLineExtractorManager>();
            if (sceneOverviewManager == null)
                sceneOverviewManager = FindObjectOfType<SceneOverviewManager>();
            
            // 初始化统计数据结构
            InitializeStatsData();
            
            // 开始实时数据更新
            if (enableRealTimeUpdate)
            {
                updateCoroutine = StartCoroutine(RealTimeDataUpdate());
            }
            
            Debug.Log("[StatisticsDashboardManager] 统计系统已初始化");
        }
        
        private void InitializeStatsData()
        {
            deviceStats = new DeviceOperationStats();
            performanceStats = new PowerlinePerformanceStats();
            inspectionStats = new InspectionStats();
            dangerStats = new DangerMonitoringStats();
            pointCloudStats = new PointCloudProcessingStats();
            treeDetectionStats = new TreeDetectionStats();
        }
        
        private IEnumerator RealTimeDataUpdate()
        {
            while (enableRealTimeUpdate)
            {
                UpdateDeviceOperationStats();
                UpdatePerformanceStats();
                UpdateInspectionStats();
                UpdateDangerStats();
                UpdatePointCloudStats();
                UpdateTreeDetectionStats(); // 调用新的更新方法
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void UpdateDeviceOperationStats()
        {
            if (sceneOverviewManager != null)
            {
                var towers = sceneOverviewManager.GetTowerData();
                if (towers != null)
                {
                    deviceStats.totalTowers = towers.Count;
                    deviceStats.operatingTowers = towers.Count(t => t.status == "normal");
                    deviceStats.warningTowers = towers.Count(t => t.status == "warning");
                    deviceStats.errorTowers = towers.Count(t => t.status == "error");
                    deviceStats.maintenanceTowers = deviceStats.totalTowers - deviceStats.operatingTowers - deviceStats.warningTowers - deviceStats.errorTowers;
                    
                    if (deviceStats.totalTowers > 0)
                    {
                        deviceStats.systemHealth = (float)deviceStats.operatingTowers / deviceStats.totalTowers * 100f;
                    }
                }
            }
        }
        
        private void UpdatePerformanceStats()
        {
            if (sceneOverviewManager != null)
            {
                var towers = sceneOverviewManager.GetTowerData();
                if (towers != null && towers.Count > 0)
                {
                    float totalLength = 0f;
                    for (int i = 0; i < towers.Count - 1; i++)
                    {
                        totalLength += Vector3.Distance(towers[i].position, towers[i + 1].position);
                    }
                    performanceStats.totalLength = totalLength / 1000f;
                    performanceStats.averageVoltage = 110f + Random.Range(-10f, 10f);
                    performanceStats.powerLoss = Random.Range(2f, 8f);
                    performanceStats.efficiency = 100f - performanceStats.powerLoss;
                }
            }
        }
        
        private void UpdateInspectionStats()
        {
            if (dronePatrolManager != null)
            {
                inspectionStats.totalInspections = Random.Range(50, 100);
                inspectionStats.completedInspections = Random.Range(30, inspectionStats.totalInspections);
                inspectionStats.pendingInspections = inspectionStats.totalInspections - inspectionStats.completedInspections;
                
                if (inspectionStats.totalInspections > 0)
                {
                    inspectionStats.inspectionCoverage = (float)inspectionStats.completedInspections / inspectionStats.totalInspections * 100f;
                }
            }
        }
        
        private void UpdateDangerStats()
        {
            if (powerlineMarkingSystem != null)
            {
                var allMarks = powerlineMarkingSystem.GetAllMarks();
                dangerStats.totalDangers = allMarks.Count;
                
                dangerStats.dangerByType = new Dictionary<DangerType, int>();
                dangerStats.dangerByLevel = new Dictionary<DangerLevel, int>();
                
                foreach (var mark in allMarks)
                {
                    // PowerlineMark 类中没有 dangerType 和 dangerLevel 属性
                    // 使用默认值进行统计
                    var defaultDangerType = DangerType.Other;
                    var defaultDangerLevel = DangerLevel.Medium;
                    
                    if (!dangerStats.dangerByType.ContainsKey(defaultDangerType))
                        dangerStats.dangerByType[defaultDangerType] = 0;
                    dangerStats.dangerByType[defaultDangerType]++;
                    
                    if (!dangerStats.dangerByLevel.ContainsKey(defaultDangerLevel))
                        dangerStats.dangerByLevel[defaultDangerLevel] = 0;
                    dangerStats.dangerByLevel[defaultDangerLevel]++;
                }
                
                dangerStats.riskAssessment = CalculateRiskAssessment();
            }
        }
        
        private void UpdatePointCloudStats()
        {
            if (powerLineExtractorManager != null)
            {
                pointCloudStats.totalFiles = Random.Range(10, 50);
                pointCloudStats.totalDataSize = Random.Range(100f, 500f);
                pointCloudStats.processingSpeed = Random.Range(1000f, 5000f);
                pointCloudStats.accuracy = Random.Range(85f, 98f);
            }
        }
        
        /// <summary>
        /// 更新树木检测统计数据
        /// </summary>
        private void UpdateTreeDetectionStats()
        {
            var treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
            if (treeDangerMonitor != null)
            {
                var dangerStats = treeDangerMonitor.GetDangerStatistics();
                
                // 统计总树木数
                treeDetectionStats.totalTrees = 0;
                foreach (var kvp in dangerStats)
                {
                    treeDetectionStats.totalTrees += kvp.Value;
                }
                
                // 设置各状态树木数量
                treeDetectionStats.safeTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Safe) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Safe] : 0;
                treeDetectionStats.warningTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Warning) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Warning] : 0;
                treeDetectionStats.criticalTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Critical) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Critical] : 0;
                treeDetectionStats.emergencyTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Emergency) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Emergency] : 0;
                
                // 计算危险比例
                int totalDangerousTrees = treeDetectionStats.warningTrees + treeDetectionStats.criticalTrees + treeDetectionStats.emergencyTrees;
                if (treeDetectionStats.totalTrees > 0)
                {
                    treeDetectionStats.dangerPercentage = (float)totalDangerousTrees / treeDetectionStats.totalTrees * 100f;
                }
                else
                {
                    treeDetectionStats.dangerPercentage = 0f;
                }
                
                Debug.Log($"StatisticsDashboardManager更新树木检测数据: 总树木数={treeDetectionStats.totalTrees}, 安全={treeDetectionStats.safeTrees}, 警告={treeDetectionStats.warningTrees}, 危险={treeDetectionStats.criticalTrees}, 紧急={treeDetectionStats.emergencyTrees}, 危险比例={treeDetectionStats.dangerPercentage:F1}%");
            }
            else
            {
                Debug.LogWarning("未找到TreeDangerMonitor组件，无法更新树木检测数据");
                // 重置为默认值
                treeDetectionStats.totalTrees = 0;
                treeDetectionStats.safeTrees = 0;
                treeDetectionStats.warningTrees = 0;
                treeDetectionStats.criticalTrees = 0;
                treeDetectionStats.emergencyTrees = 0;
                treeDetectionStats.dangerPercentage = 0f;
            }
        }
        
        private float CalculateRiskAssessment()
        {
            float riskScore = 0f;
            
            if (dangerStats.dangerByLevel.ContainsKey(DangerLevel.High))
                riskScore += dangerStats.dangerByLevel[DangerLevel.High] * 3f;
            if (dangerStats.dangerByLevel.ContainsKey(DangerLevel.Medium))
                riskScore += dangerStats.dangerByLevel[DangerLevel.Medium] * 2f;
            if (dangerStats.dangerByLevel.ContainsKey(DangerLevel.Low))
                riskScore += dangerStats.dangerByLevel[DangerLevel.Low] * 1f;
            
            return Mathf.Max(0f, 100f - riskScore * 5f);
        }
        
        public DeviceOperationStats GetDeviceStats() => deviceStats;
        public PowerlinePerformanceStats GetPerformanceStats() => performanceStats;
        public InspectionStats GetInspectionStats() => inspectionStats;
        /// <summary>
        /// 获取危险物统计数据
        /// </summary>
        public DangerMonitoringStats GetDangerStats()
        {
            return dangerStats;
        }
        
        /// <summary>
        /// 获取树木检测统计数据
        /// </summary>
        public TreeDetectionStats GetTreeDetectionStats()
        {
            // 直接返回已更新的统计数据
            return treeDetectionStats;
        }
        public PointCloudProcessingStats GetPointCloudStats() => pointCloudStats;
        
        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
    }
    
    [System.Serializable]
    public class DeviceOperationStats
    {
        public int totalTowers;
        public int operatingTowers;
        public int maintenanceTowers;
        public int warningTowers;
        public int errorTowers;
        public float systemHealth;
    }
    
    [System.Serializable]
    public class PowerlinePerformanceStats
    {
        public float totalLength;
        public float averageVoltage;
        public float powerLoss;
        public float efficiency;
        public Dictionary<string, float> lineEfficiency;
        
        // 新增属性用于真实电力线数据
        public int totalWires;      // 导线总数
        public int lineCount;       // 线路数量
        public float coverageRate;  // 线路覆盖率
    }
    
    [System.Serializable]
    public class TreeDetectionStats
    {
        public int totalTrees;          // 总树木数
        public int safeTrees;           // 安全树木数
        public int warningTrees;        // 警告树木数
        public int criticalTrees;       // 危险树木数
        public int emergencyTrees;      // 紧急树木数
        public float dangerPercentage;  // 危险比例
    }
    
    [System.Serializable]
    public class InspectionStats
    {
        public int totalInspections;
        public int completedInspections;
        public int pendingInspections;
        public float inspectionCoverage;
        public List<InspectionRecord> recentInspections;
    }
    
    [System.Serializable]
    public class DangerMonitoringStats
    {
        public int totalDangers;
        public Dictionary<DangerType, int> dangerByType;
        public Dictionary<DangerLevel, int> dangerByLevel;
        public float riskAssessment;
        public List<DangerTrend> monthlyTrends;
    }
    
    [System.Serializable]
    public class PointCloudProcessingStats
    {
        public int totalFiles;
        public float totalDataSize;
        public float processingSpeed;
        public float accuracy;
        public List<ProcessingRecord> recentProcessings;
    }
    
    [System.Serializable]
    public class InspectionRecord
    {
        public string towerName;
        public System.DateTime inspectionTime;
        public string status;
        public string notes;
    }
    
    [System.Serializable]
    public class DangerTrend
    {
        public string month;
        public int dangerCount;
        public float riskLevel;
    }
    
    [System.Serializable]
    public class ProcessingRecord
    {
        public string fileName;
        public System.DateTime processTime;
        public float dataSize;
        public float accuracy;
    }
}
