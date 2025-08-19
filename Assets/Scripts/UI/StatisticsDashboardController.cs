using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System;
using UI;

namespace UI
{
    /// <summary>
    /// 统计大屏UI控制器 - 采用动态创建UI元素的思路
    /// 参考场景总览的实现方式
    /// </summary>
    public class StatisticsDashboardController : MonoBehaviour
    {
        [Header("UI引用")]
        public UIDocument statisticsUIDocument;
        
        [Header("数据源")]
        public StatisticsDashboardManager statisticsManager;
        
        // UI状态管理
        private bool isDashboardVisible = false;
        private VisualElement currentOverlay = null;
        
        // 图表渲染器
        private ChartRenderer chartRenderer;
        
        // 日志系统
        private static string logFilePath;
        private static bool logInitialized = false;
        
        void Start()
        {
            Debug.Log("=== StatisticsDashboardController Start方法开始 ===");
            InitializeLogSystem();
            
            // 自动查找组件
            if (statisticsUIDocument == null)
            {
                statisticsUIDocument = FindObjectOfType<UIDocument>();
                Debug.Log($"自动查找UIDocument结果: {(statisticsUIDocument != null ? "成功" : "失败")}");
            }
            
            if (chartRenderer == null)
            {
                Debug.Log("ChartRenderer为null，开始自动创建");
                chartRenderer = gameObject.AddComponent<ChartRenderer>();
                Debug.Log($"ChartRenderer自动创建结果: {(chartRenderer != null ? "成功" : "失败")}");
            }
            else
            {
                Debug.Log("ChartRenderer已存在，无需创建");
            }
            
            if (statisticsManager == null)
            {
                Debug.Log("StatisticsDashboardManager为null，开始自动创建");
                var managerGO = new GameObject("StatisticsDashboardManager");
                statisticsManager = managerGO.AddComponent<StatisticsDashboardManager>();
                Debug.Log($"StatisticsDashboardManager自动创建结果: {(statisticsManager != null ? "成功" : "失败")}");
            }
            else
            {
                Debug.Log("StatisticsDashboardManager已存在，无需创建");
            }
            
            Debug.Log($"最终组件状态 - UIDocument: {(statisticsUIDocument != null ? "存在" : "null")}, ChartRenderer: {(chartRenderer != null ? "存在" : "null")}, StatisticsManager: {(statisticsManager != null ? "存在" : "null")}");
            Debug.Log("=== StatisticsDashboardController Start方法完成 ===");
        }
        
        /// <summary>
        /// 显示统计大屏
        /// </summary>
        public void ShowStatisticsDashboard()
        {
            if (isDashboardVisible)
            {
                HideStatisticsDashboard();
                return;
            }
            
            Debug.Log("显示统计大屏");
            ShowStatisticsDashboardPanel();
        }
        
        /// <summary>
        /// 隐藏统计大屏
        /// </summary>
        public void HideStatisticsDashboard()
        {
            if (currentOverlay != null && currentOverlay.parent != null)
            {
                currentOverlay.RemoveFromHierarchy();
                currentOverlay = null;
            }
            isDashboardVisible = false;
            Debug.Log("统计大屏已隐藏");
        }
        
        /// <summary>
        /// 显示统计大屏面板
        /// </summary>
        void ShowStatisticsDashboardPanel()
        {
            try
            {
                Debug.Log("=== 开始创建统计大屏面板 ===");
                
                // 优先使用已设置的statisticsUIDocument
                UIDocument uiDocument = null;
                if (statisticsUIDocument != null)
                {
                    uiDocument = statisticsUIDocument;
                    Debug.Log("使用已设置的statisticsUIDocument");
                }
                else
                {
                    Debug.LogWarning("statisticsUIDocument为null，尝试通过FindObjectOfType查找");
                    uiDocument = FindObjectOfType<UIDocument>();
                }
                
                if (uiDocument == null)
                {
                    Debug.LogError("未找到UIDocument");
                    return;
                }
                Debug.Log("UIDocument查找成功");
                
                // 等待UIDocument完全初始化
                if (uiDocument.rootVisualElement == null)
                {
                    Debug.LogWarning("UIDocument.rootVisualElement为null，等待初始化...");
                    // 延迟一帧等待初始化
                    StartCoroutine(WaitForUIDocumentInitialization(uiDocument));
                    return;
                }
                
                Debug.Log($"UIDocument.rootVisualElement已就绪，子元素数量: {uiDocument.rootVisualElement.childCount}");
                
                // 创建统计大屏弹窗
                var overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.top = 0;
                overlay.style.right = 0;
                overlay.style.bottom = 0;
                overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
                overlay.style.justifyContent = Justify.Center;
                overlay.style.alignItems = Align.Center;
                Debug.Log("覆盖层创建完成");
                
                var container = new VisualElement();
                container.style.width = Length.Percent(98);
                container.style.height = Length.Percent(95);
                container.style.maxWidth = 1600;
                container.style.maxHeight = 1000;
                container.style.backgroundColor = new Color(0.1f, 0.14f, 0.26f, 1f);
                container.style.borderTopLeftRadius = 12;
                container.style.borderTopRightRadius = 12;
                container.style.borderBottomLeftRadius = 12;
                container.style.borderBottomRightRadius = 12;
                // 使用正确的边框属性
                container.style.borderLeftWidth = 2;
                container.style.borderRightWidth = 2;
                container.style.borderTopWidth = 2;
                container.style.borderBottomWidth = 2;
                container.style.borderLeftColor = new Color(0.3f, 0.4f, 0.6f, 1f);
                container.style.borderRightColor = new Color(0.3f, 0.4f, 0.6f, 1f);
                container.style.borderTopColor = new Color(0.3f, 0.4f, 0.6f, 1f);
                container.style.borderBottomColor = new Color(0.3f, 0.4f, 0.6f, 1f);
                Debug.Log("容器样式设置完成");
                
                // 创建标题栏
                Debug.Log("开始创建标题栏");
                CreateHeader(container);
                Debug.Log("标题栏创建完成");
                
                // 创建内容区域
                Debug.Log("开始创建内容区域");
                CreateContent(container);
                Debug.Log("内容区域创建完成");
                
                overlay.Add(container);
                Debug.Log("容器添加到覆盖层完成");
                
                // 点击背景关闭
                overlay.RegisterCallback<ClickEvent>(evt => {
                    if (evt.target == overlay)
                    {
                        HideStatisticsDashboard();
                    }
                });
                Debug.Log("背景点击事件注册完成");
                
                // 安全地添加到UIDocument
                if (uiDocument.rootVisualElement != null)
                {
                    uiDocument.rootVisualElement.Add(overlay);
                    currentOverlay = overlay;
                    isDashboardVisible = true;
                    Debug.Log("=== 统计大屏弹窗创建成功 ===");
                }
                else
                {
                    Debug.LogError("UIDocument.rootVisualElement仍然为null，无法添加覆盖层");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建统计大屏面板时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 等待UIDocument初始化完成
        /// </summary>
        private System.Collections.IEnumerator WaitForUIDocumentInitialization(UIDocument uiDoc)
        {
            Debug.Log("开始等待UIDocument初始化...");
            
            // 等待最多10帧
            int maxFrames = 10;
            int currentFrame = 0;
            
            while (uiDoc.rootVisualElement == null && currentFrame < maxFrames)
            {
                yield return null; // 等待一帧
                currentFrame++;
                Debug.Log($"等待第{currentFrame}帧，rootVisualElement: {(uiDoc.rootVisualElement != null ? "已就绪" : "仍为null")}");
            }
            
            if (uiDoc.rootVisualElement != null)
            {
                Debug.Log("UIDocument初始化完成，重新调用ShowStatisticsDashboardPanel");
                ShowStatisticsDashboardPanel();
            }
            else
            {
                Debug.LogError("UIDocument初始化超时，无法显示统计大屏");
            }
        }
        
        /// <summary>
        /// 创建标题栏
        /// </summary>
        void CreateHeader(VisualElement container)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 80;
            header.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 1f);
            header.style.paddingLeft = 30;
            header.style.paddingRight = 30;
            header.style.borderTopLeftRadius = 12;
            header.style.borderTopRightRadius = 12;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            
            var title = new Label("电力线运行监测统计大屏");
            title.style.color = Color.white;
            title.style.fontSize = 28;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            
            var datetimeLabel = new Label(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            datetimeLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            datetimeLabel.style.fontSize = 14;
            header.Add(datetimeLabel);
            
            var closeButton = new Button(() => HideStatisticsDashboard());
            closeButton.text = "×";
            closeButton.style.width = 40;
            closeButton.style.height = 40;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            closeButton.style.color = Color.white;
            closeButton.style.fontSize = 28;
            closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeButton.style.borderTopLeftRadius = 20;
            closeButton.style.borderTopRightRadius = 20;
            closeButton.style.borderBottomLeftRadius = 20;
            closeButton.style.borderBottomRightRadius = 20;
            // 使用正确的边框属性
            closeButton.style.borderLeftWidth = 0;
            closeButton.style.borderRightWidth = 0;
            closeButton.style.borderTopWidth = 0;
            closeButton.style.borderBottomWidth = 0;
            closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(closeButton);
            
            container.Add(header);
        }
        
        /// <summary>
        /// 创建内容区域
        /// </summary>
        void CreateContent(VisualElement container)
        {
            Debug.Log("=== 开始创建内容区域 ===");
            
            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Column;
            content.style.flexGrow = 1;
            content.style.paddingTop = 30;
            content.style.paddingBottom = 30;
            content.style.paddingLeft = 30;
            content.style.paddingRight = 30;
            Debug.Log("内容区域基础样式设置完成");
            
            // 第一行：设备统计 + 性能统计
            Debug.Log("开始创建第一行面板");
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.marginBottom = 30;
            topRow.style.height = 380;
            Debug.Log("第一行容器样式设置完成");
            
            // 设备运行统计面板
            Debug.Log("开始创建设备运行统计面板");
            CreateDeviceStatsPanel(topRow);
            Debug.Log("设备运行统计面板创建完成");
            
            // 电力线性能统计面板
            Debug.Log("开始创建电力线性能统计面板");
            CreatePerformancePanel(topRow);
            Debug.Log("电力线性能统计面板创建完成");
            
            content.Add(topRow);
            Debug.Log("第一行面板添加到内容区域完成");
            
            // 第二行：巡检统计 + 危险监测
            Debug.Log("开始创建第二行面板");
            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.justifyContent = Justify.SpaceBetween;
            bottomRow.style.height = 380;
            Debug.Log("第二行容器样式设置完成");
            
            // 巡检数据统计面板
            Debug.Log("开始创建巡检数据统计面板");
            CreateInspectionPanel(bottomRow);
            Debug.Log("巡检数据统计面板创建完成");
            
            // 危险监测统计面板
            Debug.Log("开始创建危险监测统计面板");
            CreateHazardPanel(bottomRow);
            Debug.Log("危险监测统计面板创建完成");
            
            content.Add(bottomRow);
            Debug.Log("第二行面板添加到内容区域完成");
            
            container.Add(content);
            Debug.Log("内容区域添加到容器完成");
            Debug.Log("=== 内容区域创建完成 ===");
        }
        
        /// <summary>
        /// 创建设备运行统计面板
        /// </summary>
        void CreateDeviceStatsPanel(VisualElement parent)
        {
            Debug.Log("=== 开始创建设备运行统计面板 ===");
            
            var panel = CreatePanel("设备运行统计", new Color(0.2f, 0.4f, 0.6f, 1f));
            Debug.Log("设备运行统计面板基础结构创建完成");
            parent.Add(panel);
            Debug.Log("设备运行统计面板添加到父容器完成");
            
            // 生成测试数据
            Debug.Log("开始生成设备运行测试数据");
            var stats = GenerateTestDeviceStats();
            Debug.Log($"测试数据生成完成: 总电塔数={stats.totalTowers}, 运行中={stats.operatingTowers}, 维护中={stats.maintenanceTowers}, 警告={stats.warningTowers}, 故障={stats.errorTowers}, 系统健康度={stats.systemHealth:F1}%");
            
            // 创建统计项
            Debug.Log("开始创建设备运行统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总电塔数", stats.totalTowers.ToString() },
                { "运行中", stats.operatingTowers.ToString() },
                { "维护中", stats.maintenanceTowers.ToString() },
                { "警告", stats.warningTowers.ToString() },
                { "故障", stats.errorTowers.ToString() },
                { "系统健康度", $"{stats.systemHealth:F1}%" }
            });
            Debug.Log("设备运行统计项创建完成");
            
            // 创建柱状图（无背景容器）
            Debug.Log("开始创建设备运行柱状图");
            var chartContainer = CreateChartContainerWithoutBackground();
            Debug.Log("图表容器创建完成");
            
            var data = new List<float> { stats.operatingTowers, stats.maintenanceTowers, stats.warningTowers, stats.errorTowers };
            var labels = new List<string> { "运行中", "维护中", "警告", "故障" };
            Debug.Log($"准备创建柱状图，数据: [{string.Join(", ", data)}], 标签: [{string.Join(", ", labels)}]");
            
            if (chartRenderer != null)
            {
                Debug.Log("ChartRenderer组件存在，开始调用CreateBarChart");
                chartRenderer.CreateBarChart(chartContainer, data, labels);
                Debug.Log("ChartRenderer.CreateBarChart调用完成");
            }
            else
            {
                Debug.LogError("ChartRenderer组件为null，无法创建柱状图");
            }
            
            panel.Add(chartContainer);
            Debug.Log("图表容器添加到面板完成");
            Debug.Log("=== 设备运行统计面板创建完成 ===");
        }
        
        /// <summary>
        /// 创建电力线性能统计面板
        /// </summary>
        void CreatePerformancePanel(VisualElement parent)
        {
            Debug.Log("=== 开始创建电力线性能统计面板 ===");
            
            var panel = CreatePanel("电力线性能统计", new Color(0.8f, 0.4f, 0.2f, 1f));
            Debug.Log("电力线性能统计面板基础结构创建完成");
            parent.Add(panel);
            Debug.Log("电力线性能统计面板添加到父容器完成");
            
            // 生成测试数据
            Debug.Log("开始生成电力线性能测试数据");
            var stats = GenerateTestPerformanceStats();
            Debug.Log($"测试数据生成完成: 总线路长度={stats.totalLength:F2}km, 平均电压={stats.averageVoltage:F1}kV, 功率损耗={stats.powerLoss:F1}%, 传输效率={stats.efficiency:F1}%");
            
            // 创建统计项（水平紧凑布局）
            Debug.Log("开始创建电力线性能统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总线路长度", $"{stats.totalLength:F2} km" },
                { "平均电压", $"{stats.averageVoltage:F1} kV" },
                { "功率损耗", $"{stats.powerLoss:F1}%" },
                { "传输效率", $"{stats.efficiency:F1}%" }
            });
            Debug.Log("电力线性能统计项创建完成");
            
            // 创建仪表图（无背景容器）
            Debug.Log("开始创建电力线性能仪表图");
            var chartContainer = CreateChartContainerWithoutBackground();
            Debug.Log("图表容器创建完成");
            
            if (chartRenderer != null)
            {
                Debug.Log("ChartRenderer组件存在，开始调用CreateGaugeChart");
                chartRenderer.CreateGaugeChart(chartContainer, stats.efficiency, 100f, "传输效率");
                Debug.Log("ChartRenderer.CreateGaugeChart调用完成");
            }
            else
            {
                Debug.LogError("ChartRenderer组件为null，无法创建仪表图");
            }
            
            panel.Add(chartContainer);
            Debug.Log("图表容器添加到面板完成");
            Debug.Log("=== 电力线性能统计面板创建完成 ===");
        }
        
        /// <summary>
        /// 创建巡检数据统计面板
        /// </summary>
        void CreateInspectionPanel(VisualElement parent)
        {
            Debug.Log("=== 开始创建巡检数据统计面板 ===");
            
            var panel = CreatePanel("巡检数据统计", new Color(0.2f, 0.6f, 0.4f, 1f));
            Debug.Log("巡检数据统计面板基础结构创建完成");
            parent.Add(panel);
            Debug.Log("巡检数据统计面板添加到父容器完成");
            
            // 生成测试数据
            Debug.Log("开始生成巡检数据测试数据");
            var stats = GenerateTestInspectionStats();
            Debug.Log($"测试数据生成完成: 巡检次数={stats.totalInspections}, 已完成={stats.completedInspections}, 待巡检={stats.pendingInspections}, 巡检覆盖率={stats.inspectionCoverage:F1}%");
            
            // 创建统计项
            Debug.Log("开始创建巡检数据统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "巡检次数", stats.totalInspections.ToString() },
                { "已完成", stats.completedInspections.ToString() },
                { "待巡检", stats.pendingInspections.ToString() },
                { "巡检覆盖率", $"{stats.inspectionCoverage:F1}%" }
            });
            Debug.Log("巡检数据统计项创建完成");
            
            // 创建进度条（无背景容器）
            Debug.Log("开始创建巡检进度条");
            var progressContainer = CreateChartContainerWithoutBackground();
            Debug.Log("进度条容器创建完成");
            
            var progressBar = CreateProgressBar($"巡检进度: {stats.inspectionCoverage:F1}%", stats.inspectionCoverage / 100f);
            progressContainer.Add(progressBar);
            Debug.Log("进度条添加到容器完成");
            
            panel.Add(progressContainer);
            Debug.Log("进度条容器添加到面板完成");
            Debug.Log("=== 巡检数据统计面板创建完成 ===");
        }
        
        /// <summary>
        /// 创建危险监测统计面板
        /// </summary>
        void CreateHazardPanel(VisualElement parent)
        {
            Debug.Log("=== 开始创建危险监测统计面板 ===");
            
            var panel = CreatePanel("危险监测统计", new Color(0.8f, 0.2f, 0.2f, 1f));
            Debug.Log("危险监测统计面板基础结构创建完成");
            parent.Add(panel);
            Debug.Log("危险监测统计面板添加到父容器完成");
            
            // 生成测试数据
            Debug.Log("开始生成危险监测测试数据");
            var stats = GenerateTestDangerStats();
            Debug.Log($"测试数据生成完成: 总危险点={stats.totalDangers}, 风险评估={stats.riskAssessment:F1}分");
            
            // 创建统计项
            Debug.Log("开始创建危险监测统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总危险点", stats.totalDangers.ToString() },
                { "风险评估", $"{stats.riskAssessment:F1}分" }
            });
            Debug.Log("危险监测统计项创建完成");
            
            // 创建柱状图（无背景容器）
            Debug.Log("开始创建危险监测柱状图");
            var chartContainer = CreateChartContainerWithoutBackground();
            Debug.Log("图表容器创建完成");
            
            // 从dangerByType字典中提取数据
            var data = new List<float>();
            var labels = new List<string>();
            
            if (stats.dangerByType != null && stats.dangerByType.Count > 0)
            {
                foreach (var kvp in stats.dangerByType)
                {
                    data.Add(kvp.Value);
                    labels.Add(kvp.Key.ToString());
                }
            }
            else
            {
                // 如果没有数据，显示默认值
                data = new List<float> { 0, 0, 0, 0 };
                labels = new List<string> { "无数据", "无数据", "无数据", "无数据" };
            }
            
            Debug.Log($"准备创建柱状图，数据: [{string.Join(", ", data)}], 标签: [{string.Join(", ", labels)}]");
            
            if (chartRenderer != null)
            {
                Debug.Log("ChartRenderer组件存在，开始调用CreateBarChart");
                chartRenderer.CreateBarChart(chartContainer, data, labels);
                Debug.Log("ChartRenderer.CreateBarChart调用完成");
            }
            else
            {
                Debug.LogError("ChartRenderer组件为null，无法创建柱状图");
            }
            
            panel.Add(chartContainer);
            Debug.Log("图表容器添加到面板完成");
            Debug.Log("=== 危险监测统计面板创建完成 ===");
        }
        
        /// <summary>
        /// 创建面板基础结构
        /// </summary>
        VisualElement CreatePanel(string title, Color titleColor)
        {
            var panel = new VisualElement();
            panel.style.width = Length.Percent(48);
            panel.style.backgroundColor = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            // 使用正确的圆角属性
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            // 使用正确的边框属性
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            panel.style.borderRightColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            panel.style.borderTopColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            panel.style.borderBottomColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.paddingLeft = 20;
            panel.style.paddingRight = 20;
            panel.style.flexDirection = FlexDirection.Column;
            
            // 标题
            var titleLabel = new Label(title);
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.marginBottom = 20;
            titleLabel.style.backgroundColor = titleColor;
            titleLabel.style.paddingTop = 12;
            titleLabel.style.paddingBottom = 12;
            // 使用正确的圆角属性
            titleLabel.style.borderTopLeftRadius = 4;
            titleLabel.style.borderTopRightRadius = 4;
            titleLabel.style.borderBottomLeftRadius = 4;
            titleLabel.style.borderBottomRightRadius = 4;
            panel.Add(titleLabel);
            
            return panel;
        }
        
        /// <summary>
        /// 创建统计项
        /// </summary>
        void CreateStatsItems(VisualElement panel, Dictionary<string, string> items)
        {
            var statsContainer = new VisualElement();
            statsContainer.style.flexDirection = FlexDirection.Row;
            statsContainer.style.flexWrap = Wrap.NoWrap; // 改为不换行，强制一行显示
            statsContainer.style.justifyContent = Justify.SpaceAround; // 使用SpaceAround确保兼容性
            statsContainer.style.marginBottom = 25;
            statsContainer.style.minHeight = 80; // 从100px减少到80px，节省垂直空间
            statsContainer.style.paddingTop = 8; // 从10px减少到8px
            statsContainer.style.paddingBottom = 8; // 从10px减少到8px
            
            foreach (var item in items)
            {
                var statItem = CreateStatItem(item.Key, item.Value);
                statsContainer.Add(statItem);
            }
            
            panel.Add(statsContainer);
        }

        /// <summary>
        /// 创建垂直布局的统计项
        /// </summary>
        void CreateVerticalStatsItems(VisualElement panel, Dictionary<string, string> items)
        {
            var statsContainer = new VisualElement();
            statsContainer.style.flexDirection = FlexDirection.Column;
            statsContainer.style.flexGrow = 1;
            statsContainer.style.justifyContent = Justify.SpaceAround;
            statsContainer.style.marginBottom = 25;
            statsContainer.style.minHeight = 80; // 从100px减少到80px，节省垂直空间
            statsContainer.style.paddingTop = 8; // 从10px减少到8px
            statsContainer.style.paddingBottom = 8; // 从10px减少到8px
            
            foreach (var item in items)
            {
                var statItem = CreateStatItem(item.Key, item.Value);
                statsContainer.Add(statItem);
            }
            
            panel.Add(statsContainer);
        }
        
        /// <summary>
        /// 创建单个统计项
        /// </summary>
        VisualElement CreateStatItem(string label, string value)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Column;
            item.style.alignItems = Align.Center;
            item.style.justifyContent = Justify.Center;
            item.style.minWidth = 90; // 从120px减少到90px，使其更紧凑
            item.style.minHeight = 60; // 从70px减少到60px，节省垂直空间
            item.style.backgroundColor = new Color(0.1f, 0.15f, 0.25f, 0.8f);
            // 使用正确的圆角属性
            item.style.borderTopLeftRadius = 6;
            item.style.borderTopRightRadius = 6;
            item.style.borderBottomLeftRadius = 6;
            item.style.borderBottomRightRadius = 6;
            item.style.marginLeft = 6; // 从10px减少到6px，减少项目间距
            item.style.marginRight = 6; // 从10px减少到6px，减少项目间距
            item.style.paddingTop = 12; // 从15px减少到12px，节省内边距
            item.style.paddingBottom = 12; // 从15px减少到12px，节省内边距
            
            var labelElement = new Label(label);
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            labelElement.style.fontSize = 11; // 从13px减少到11px，节省空间
            labelElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            labelElement.style.marginBottom = 6; // 从8px减少到6px，减少标签和数值间距
            item.Add(labelElement);
            
            var valueElement = new Label(value);
            valueElement.style.color = Color.white;
            valueElement.style.fontSize = 18; // 从20px减少到18px，节省空间
            valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            item.Add(valueElement);
            
            return item;
        }
        
        /// <summary>
        /// 创建图表容器
        /// </summary>
        VisualElement CreateChartContainer()
        {
            var container = new VisualElement();
            container.style.height = 160; // 进一步增加图表容器高度
            container.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.9f);
            // 使用正确的圆角属性
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.paddingTop = 20;
            container.style.paddingBottom = 20;
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.marginTop = 20;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            
            return container;
        }

        /// <summary>
        /// 创建图表容器（无背景）
        /// </summary>
        VisualElement CreateChartContainerWithoutBackground()
        {
            var container = new VisualElement();
            container.style.height = 160; // 进一步增加图表容器高度
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            
            return container;
        }
        
        /// <summary>
        /// 创建进度条
        /// </summary>
        VisualElement CreateProgressBar(string label, float percentage)
        {
            // 创建进度条容器
            var progressContainer = new VisualElement();
            progressContainer.style.flexDirection = FlexDirection.Column;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.justifyContent = Justify.Center;
            
            // 创建进度标签
            var progressLabel = new Label(label);
            progressLabel.style.color = Color.white;
            progressLabel.style.fontSize = 18;
            progressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            progressLabel.style.marginBottom = 20;
            
            // 创建进度条背景
            var progressBar = new VisualElement();
            progressBar.style.height = 35;
            progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            // 使用正确的圆角属性
            progressBar.style.borderTopLeftRadius = 6;
            progressBar.style.borderTopRightRadius = 6;
            progressBar.style.borderBottomLeftRadius = 6;
            progressBar.style.borderBottomRightRadius = 6;
            progressBar.style.minWidth = 250;
            
            // 创建进度条填充
            var progressFill = new VisualElement();
            progressFill.style.height = Length.Percent(100);
            progressFill.style.width = Length.Percent(percentage * 100); // 确保宽度与百分比一致
            progressFill.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f);
            // 使用正确的圆角属性
            progressFill.style.borderTopLeftRadius = 6;
            progressFill.style.borderTopRightRadius = 6;
            progressFill.style.borderBottomLeftRadius = 6;
            progressFill.style.borderBottomRightRadius = 6;
            
            // 组装进度条
            progressBar.Add(progressFill);
            
            // 组装容器
            progressContainer.Add(progressLabel);
            progressContainer.Add(progressBar);
            
            return progressContainer;
        }
        
        /// <summary>
        /// 初始化日志系统
        /// </summary>
        private void InitializeLogSystem()
        {
            if (!logInitialized)
            {
                try
                {
                    string logsFolder = Path.Combine(Application.dataPath, "..", "Logs");
                    if (!Directory.Exists(logsFolder))
                    {
                        Directory.CreateDirectory(logsFolder);
                    }
                    
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    logFilePath = Path.Combine(logsFolder, $"StatisticsDashboard_{timestamp}.log");
                    
                    string header = $"=== 统计大屏调试日志 ===\n" +
                                  $"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"Unity版本: {Application.unityVersion}\n" +
                                  $"平台: {Application.platform}\n" +
                                  $"数据路径: {Application.dataPath}\n" +
                                  $"日志文件: {logFilePath}\n" +
                                  "================================\n\n";
                    
                    File.WriteAllText(logFilePath, header);
                    logInitialized = true;
                    
                    Debug.Log($"统计大屏日志系统已初始化，日志文件: {logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"初始化日志系统失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 生成测试数据（当StatisticsDashboardManager不可用时使用）
        /// </summary>
        private DeviceOperationStats GenerateTestDeviceStats()
        {
            return new DeviceOperationStats
            {
                totalTowers = 156,
                operatingTowers = 142,
                maintenanceTowers = 8,
                warningTowers = 4,
                errorTowers = 2,
                systemHealth = 89.7f
            };
        }
        
        private PowerlinePerformanceStats GenerateTestPerformanceStats()
        {
            return new PowerlinePerformanceStats
            {
                totalLength = 89.5f,
                averageVoltage = 220.0f,
                powerLoss = 2.8f,
                efficiency = 97.2f,
                lineEfficiency = new Dictionary<string, float>()
            };
        }
        
        private InspectionStats GenerateTestInspectionStats()
        {
            return new InspectionStats
            {
                totalInspections = 324,
                completedInspections = 298,
                pendingInspections = 26,
                inspectionCoverage = 92.0f,
                recentInspections = new List<InspectionRecord>()
            };
        }
        
        private DangerMonitoringStats GenerateTestDangerStats()
        {
            return new DangerMonitoringStats
            {
                totalDangers = 23,
                dangerByType = new Dictionary<DangerType, int> 
                { 
                    { DangerType.Vegetation, 12 }, 
                    { DangerType.Equipment, 6 },
                    { DangerType.Building, 3 },
                    { DangerType.Other, 2 }
                },
                dangerByLevel = new Dictionary<DangerLevel, int> 
                { 
                    { DangerLevel.High, 5 }, 
                    { DangerLevel.Medium, 12 }, 
                    { DangerLevel.Low, 6 } 
                },
                riskAssessment = 82.5f,
                monthlyTrends = new List<DangerTrend>()
            };
        }
    }
}
