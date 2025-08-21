using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static UI.SceneOverviewManager;

namespace UI
{
    /// <summary>
    /// 无人机巡检路径规划测试器
    /// 用于测试和验证智能路径规划功能
    /// </summary>
    public class DronePatrolPathTester : MonoBehaviour
    {
        [Header("测试配置")]
        public bool enableTesting = true;
        public bool showDebugInfo = true;
        public bool testOnStart = false;
        
        [Header("测试数据")]
        public List<Vector3> testTowerPositions = new List<Vector3>();
        public List<int> testGroupIds = new List<int>();
        public List<int> testOrders = new List<int>();
        
        private DronePatrolManager dronePatrolManager;
        private SceneOverviewManager sceneOverviewManager;
        
        void Start()
        {
            if (!enableTesting) return;
            
            // 获取组件引用
            dronePatrolManager = FindObjectOfType<DronePatrolManager>();
            sceneOverviewManager = FindObjectOfType<SceneOverviewManager>();
            
            if (testOnStart)
            {
                Invoke("RunPathPlanningTest", 2f);
            }
        }
        
        /// <summary>
        /// 运行路径规划测试
        /// </summary>
        public void RunPathPlanningTest()
        {
            if (!enableTesting) return;
            
            Debug.Log("=== 开始无人机巡检路径规划测试 ===");
            
            // 测试1：基本路径规划
            TestBasicPathPlanning();
            
            // 测试2：智能路径规划
            TestSmartPathPlanning();
            
            // 测试3：距离优化排序
            TestDistanceOptimization();
            
            Debug.Log("=== 无人机巡检路径规划测试完成 ===");
        }
        
        /// <summary>
        /// 测试基本路径规划
        /// </summary>
        private void TestBasicPathPlanning()
        {
            Debug.Log("--- 测试基本路径规划 ---");
            
            if (sceneOverviewManager == null)
            {
                Debug.LogWarning("SceneOverviewManager未找到，跳过测试");
                return;
            }
            
            var towers = sceneOverviewManager.GetTowerData();
            if (towers == null || towers.Count == 0)
            {
                Debug.LogWarning("没有电塔数据，跳过测试");
                return;
            }
            
            // 测试X坐标排序
            var xSortedTowers = towers.OrderBy(t => t.position.x).ToList();
            Debug.Log($"X坐标排序结果：");
            for (int i = 0; i < Mathf.Min(xSortedTowers.Count, 5); i++)
            {
                var tower = xSortedTowers[i];
                Debug.Log($"  电塔{i + 1}: {tower.name} - 位置: {tower.position} - 高度: {tower.height}");
            }
            
            // 计算总路径长度
            float totalPathLength = CalculatePathLength(xSortedTowers);
            Debug.Log($"X坐标排序总路径长度: {totalPathLength:F2}");
        }
        
        /// <summary>
        /// 测试智能路径规划
        /// </summary>
        private void TestSmartPathPlanning()
        {
            Debug.Log("--- 测试智能路径规划 ---");
            
            if (dronePatrolManager == null)
            {
                Debug.LogWarning("DronePatrolManager未找到，跳过测试");
                return;
            }
            
            // 使用反射调用私有方法进行测试
            var method = dronePatrolManager.GetType().GetMethod("PlanOptimalPatrolPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                Debug.LogWarning("PlanOptimalPatrolPath方法未找到，跳过测试");
                return;
            }
            
            if (sceneOverviewManager != null)
            {
                var towers = sceneOverviewManager.GetTowerData();
                if (towers != null && towers.Count > 0)
                {
                    try
                    {
                        var optimizedTowers = (List<TowerData>)method.Invoke(dronePatrolManager, new object[] { towers });
                        if (optimizedTowers != null)
                        {
                            Debug.Log($"智能路径规划结果：");
                            for (int i = 0; i < Mathf.Min(optimizedTowers.Count, 5); i++)
                            {
                                var tower = optimizedTowers[i];
                                Debug.Log($"  电塔{i + 1}: {tower.name} - 位置: {tower.position} - 高度: {tower.height}");
                            }
                            
                            float totalPathLength = CalculatePathLength(optimizedTowers);
                            Debug.Log($"智能路径规划总路径长度: {totalPathLength:F2}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"智能路径规划测试失败: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 测试距离优化排序
        /// </summary>
        private void TestDistanceOptimization()
        {
            Debug.Log("--- 测试距离优化排序 ---");
            
            if (dronePatrolManager == null)
            {
                Debug.LogWarning("DronePatrolManager未找到，跳过测试");
                return;
            }
            
            // 使用反射调用私有方法进行测试
            var method = dronePatrolManager.GetType().GetMethod("SortTowersByProximity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                Debug.LogWarning("SortTowersByProximity方法未找到，跳过测试");
                return;
            }
            
            if (sceneOverviewManager != null)
            {
                var towers = sceneOverviewManager.GetTowerData();
                if (towers != null && towers.Count > 0)
                {
                    try
                    {
                        var optimizedTowers = (List<TowerData>)method.Invoke(dronePatrolManager, new object[] { towers });
                        if (optimizedTowers != null)
                        {
                            Debug.Log($"距离优化排序结果：");
                            for (int i = 0; i < Mathf.Min(optimizedTowers.Count, 5); i++)
                            {
                                var tower = optimizedTowers[i];
                                Debug.Log($"  电塔{i + 1}: {tower.name} - 位置: {tower.position} - 高度: {tower.height}");
                            }
                            
                            float totalPathLength = CalculatePathLength(optimizedTowers);
                            Debug.Log($"距离优化排序总路径长度: {totalPathLength:F2}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"距离优化排序测试失败: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 计算路径总长度
        /// </summary>
        private float CalculatePathLength(List<TowerData> towers)
        {
            if (towers == null || towers.Count < 2) return 0f;
            
            float totalLength = 0f;
            for (int i = 0; i < towers.Count - 1; i++)
            {
                totalLength += Vector3.Distance(towers[i].position, towers[i + 1].position);
            }
            
            return totalLength;
        }
        
        /// <summary>
        /// 在Inspector中显示测试按钮
        /// </summary>
        [ContextMenu("运行路径规划测试")]
        private void RunTestFromContextMenu()
        {
            RunPathPlanningTest();
        }
        
        /// <summary>
        /// 在运行时显示测试信息
        /// </summary>
        void OnGUI()
        {
            if (!enableTesting || !showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("无人机巡检路径规划测试器", GUI.skin.box);
            
            if (GUILayout.Button("运行路径规划测试"))
            {
                RunPathPlanningTest();
            }
            
            if (GUILayout.Button("测试基本路径规划"))
            {
                TestBasicPathPlanning();
            }
            
            if (GUILayout.Button("测试智能路径规划"))
            {
                TestSmartPathPlanning();
            }
            
            if (GUILayout.Button("测试距离优化排序"))
            {
                TestDistanceOptimization();
            }
            
            GUILayout.EndArea();
        }
    }
}
