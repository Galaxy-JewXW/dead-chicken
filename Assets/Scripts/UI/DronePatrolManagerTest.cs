using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UI
{
    /// <summary>
    /// 无人机巡检管理器测试脚本
    /// 用于验证修复后的功能是否正常工作
    /// </summary>
    public class DronePatrolManagerTest : MonoBehaviour
    {
        [Header("测试配置")]
        public bool runTestOnStart = false;
        public bool showDebugInfo = true;
        
        private DronePatrolManager dronePatrolManager;
        
        void Start()
        {
            if (runTestOnStart)
            {
                Invoke("RunBasicTest", 1f);
            }
        }
        
        /// <summary>
        /// 运行基本测试
        /// </summary>
        public void RunBasicTest()
        {
            Debug.Log("=== 开始DronePatrolManager基本测试 ===");
            
            // 查找DronePatrolManager组件
            dronePatrolManager = FindObjectOfType<DronePatrolManager>();
            if (dronePatrolManager == null)
            {
                Debug.LogError("未找到DronePatrolManager组件");
                return;
            }
            
            Debug.Log("DronePatrolManager组件找到，测试通过");
            
            // 测试配置参数
            Debug.Log($"无人机速度: {dronePatrolManager.droneSpeed}");
            Debug.Log($"无人机高度比例: {dronePatrolManager.droneHeight}");
            Debug.Log($"智能路径规划: {dronePatrolManager.useSmartPathPlanning}");
            
            Debug.Log("=== DronePatrolManager基本测试完成 ===");
        }
        
        /// <summary>
        /// 测试路径规划功能
        /// </summary>
        public void TestPathPlanning()
        {
            if (dronePatrolManager == null)
            {
                Debug.LogError("DronePatrolManager未初始化，请先运行基本测试");
                return;
            }
            
            Debug.Log("=== 测试路径规划功能 ===");
            
            // 测试开始巡检（这会触发路径规划）
            try
            {
                dronePatrolManager.StartDronePatrol();
                Debug.Log("StartDronePatrol调用成功");
                
                // 立即停止，避免实际开始巡检
                dronePatrolManager.StopDronePatrol();
                Debug.Log("StopDronePatrol调用成功");
                
                Debug.Log("路径规划功能测试通过");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"路径规划功能测试失败: {e.Message}");
            }
            
            Debug.Log("=== 路径规划功能测试完成 ===");
        }
        
        /// <summary>
        /// 在Inspector中显示测试按钮
        /// </summary>
        [ContextMenu("运行基本测试")]
        private void RunTestFromContextMenu()
        {
            RunBasicTest();
        }
        
        [ContextMenu("测试路径规划")]
        private void TestPathPlanningFromContextMenu()
        {
            TestPathPlanning();
        }
        
        /// <summary>
        /// 在运行时显示测试界面
        /// </summary>
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("DronePatrolManager测试器", GUI.skin.box);
            
            if (GUILayout.Button("运行基本测试"))
            {
                RunBasicTest();
            }
            
            if (GUILayout.Button("测试路径规划"))
            {
                TestPathPlanning();
            }
            
            if (dronePatrolManager != null)
            {
                GUILayout.Label($"状态: 已初始化");
                GUILayout.Label($"巡检中: {dronePatrolManager.IsPatrolling}");
                GUILayout.Label($"已暂停: {dronePatrolManager.IsPaused}");
            }
            else
            {
                GUILayout.Label("状态: 未初始化");
            }
            
            GUILayout.EndArea();
        }
    }
}
