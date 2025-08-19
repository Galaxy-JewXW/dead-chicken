using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// 统计大屏测试脚本
    /// 用于验证新的动态创建UI元素的实现
    /// </summary>
    public class StatisticsDashboardTest : MonoBehaviour
    {
        [Header("测试配置")]
        public bool testOnStart = true;
        public KeyCode testKey = KeyCode.T;
        
        private StatisticsDashboardController dashboardController;
        
        void Start()
        {
            if (testOnStart)
            {
                Invoke("RunTest", 1f);
            }
        }
        
        void Update()
        {
            // 按T键测试显示统计大屏
            if (Input.GetKeyDown(testKey))
            {
                ShowStatisticsDashboard();
            }
        }
        
        /// <summary>
        /// 运行测试
        /// </summary>
        public void RunTest()
        {
            Debug.Log("=== 统计大屏测试开始 ===");
            
            // 查找组件
            dashboardController = FindObjectOfType<StatisticsDashboardController>();
            if (dashboardController == null)
            {
                Debug.LogError("未找到StatisticsDashboardController组件");
                return;
            }
            
            Debug.Log("StatisticsDashboardController组件已找到");
            
            // 检查ChartRenderer组件
            var chartRenderer = dashboardController.GetComponent<ChartRenderer>();
            if (chartRenderer == null)
            {
                Debug.LogWarning("未找到ChartRenderer组件，将自动创建");
            }
            else
            {
                Debug.Log("ChartRenderer组件已找到");
            }
            
            Debug.Log("=== 统计大屏测试完成 ===");
            Debug.Log($"按 {testKey} 键可以显示统计大屏");
        }
        
        /// <summary>
        /// 显示统计大屏
        /// </summary>
        public void ShowStatisticsDashboard()
        {
            if (dashboardController != null)
            {
                Debug.Log("显示统计大屏...");
                dashboardController.ShowStatisticsDashboard();
            }
            else
            {
                Debug.LogError("StatisticsDashboardController为空，无法显示统计大屏");
            }
        }
        
        /// <summary>
        /// 隐藏统计大屏
        /// </summary>
        public void HideStatisticsDashboard()
        {
            if (dashboardController != null)
            {
                Debug.Log("隐藏统计大屏...");
                dashboardController.HideStatisticsDashboard();
            }
        }
        
        /// <summary>
        /// 手动刷新面板
        /// </summary>
        [ContextMenu("显示统计大屏")]
        public void ManualShow()
        {
            ShowStatisticsDashboard();
        }
        
        /// <summary>
        /// 强制重新初始化
        /// </summary>
        [ContextMenu("隐藏统计大屏")]
        public void ManualHide()
        {
            HideStatisticsDashboard();
        }
    }
}
