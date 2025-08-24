using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
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
        
        // 实时更新相关
        private Coroutine refreshCoroutine;
        private float refreshInterval = 2f; // 每2秒刷新一次
        private VisualElement currentContent = null;
        
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
        /// 启动实时刷新协程
        /// </summary>
        private void StartRefreshCoroutine()
        {
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
            }
            refreshCoroutine = StartCoroutine(RefreshUICoroutine());
            Debug.Log("实时刷新协程已启动");
        }
        
        /// <summary>
        /// 停止实时刷新协程
        /// </summary>
        private void StopRefreshCoroutine()
        {
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
                Debug.Log("实时刷新协程已停止");
            }
        }
        
        /// <summary>
        /// 实时刷新UI协程
        /// </summary>
        private System.Collections.IEnumerator RefreshUICoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                
                if (isDashboardVisible && currentContent != null)
                {
                    RefreshDashboardContent();
                }
            }
        }
        
        /// <summary>
        /// 刷新统计大屏内容
        /// </summary>
        private void RefreshDashboardContent()
        {
            if (currentContent == null) return;
            
            Debug.Log("开始刷新统计大屏内容");
            
            // 清除旧内容
            currentContent.Clear();
            
            // 重新创建完整的UI结构（包括标题栏和内容）
            CreateHeader(currentContent);
            CreateContent(currentContent);
            
            Debug.Log("统计大屏内容刷新完成");
        }
        
        /// <summary>
        /// 手动刷新统计大屏（供外部调用）
        /// </summary>
        public void ManualRefresh()
        {
            if (isDashboardVisible && currentContent != null)
            {
                Debug.Log("手动刷新统计大屏");
                RefreshDashboardContent();
            }
            else
            {
                Debug.LogWarning("统计大屏未显示，无法刷新");
            }
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
            
            // 启动实时刷新
            StartRefreshCoroutine();
        }
        
        /// <summary>
        /// 隐藏统计大屏
        /// </summary>
        public void HideStatisticsDashboard()
        {
            // 停止实时刷新
            StopRefreshCoroutine();
            
            if (currentOverlay != null && currentOverlay.parent != null)
            {
                currentOverlay.RemoveFromHierarchy();
                currentOverlay = null;
            }
            isDashboardVisible = false;
            
            // 恢复侧边栏显示（参考AI助手实现）
            var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (uiManager != null)
            {
                // 显示侧边栏
                var sidebar = uiManager.GetSidebar();
                if (sidebar != null)
                {
                    sidebar.style.display = DisplayStyle.Flex;
                    Debug.Log("侧边栏已恢复显示");
                }
                
                // 返回正常模式
                uiManager.SwitchMode(SimpleUIToolkitManager.UIMode.Normal);
            }
            
            Debug.Log("统计大屏已隐藏，侧边栏已恢复");
        }
        
        /// <summary>
        /// 应用统计大屏样式
        /// </summary>
        /// <param name="rootElement">根元素</param>
        private void ApplyDashboardStyles(VisualElement rootElement)
        {
            if (rootElement == null) return;
            
            try
            {
                // 直接应用内联样式 - 这是最可靠的方法
                ApplyInlineStyles(rootElement);
                
                Debug.Log("统计大屏样式已成功应用");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"应用统计大屏样式时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 应用内联样式
        /// </summary>
        /// <param name="rootElement">根元素</param>
        private void ApplyInlineStyles(VisualElement rootElement)
        {
            // 应用根容器样式
            rootElement.style.backgroundColor = new Color(0.04f, 0.055f, 0.1f, 1f); // rgb(10, 14, 26)
            rootElement.style.width = Length.Percent(100);
            rootElement.style.height = Length.Percent(100);
            rootElement.style.paddingLeft = 20;
            rootElement.style.paddingRight = 20;
            rootElement.style.paddingTop = 20;
            rootElement.style.paddingBottom = 20;
            rootElement.style.flexDirection = FlexDirection.Column;
            
            // 查找并应用各个面板的样式
            ApplyPanelStyles(rootElement);
        }
        
        /// <summary>
        /// 应用面板样式
        /// </summary>
        /// <param name="rootElement">根元素</param>
        private void ApplyPanelStyles(VisualElement rootElement)
        {
            // 查找所有面板并应用样式
            var panels = rootElement.Query<VisualElement>(className: "statistics-panel").ToList();
            foreach (var panel in panels)
            {
                // 统计面板样式
                panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.9f); // rgba(26, 26, 36, 0.9)
                panel.style.borderTopLeftRadius = 8;
                panel.style.borderTopRightRadius = 8;
                panel.style.borderBottomLeftRadius = 8;
                panel.style.borderBottomRightRadius = 8;
                panel.style.borderLeftWidth = 1;
                panel.style.borderRightWidth = 1;
                panel.style.borderTopWidth = 1;
                panel.style.borderBottomWidth = 1;
                panel.style.borderLeftColor = new Color(0.24f, 0.24f, 0.31f, 1f); // rgb(60, 60, 80)
                panel.style.borderRightColor = new Color(0.24f, 0.24f, 0.31f, 1f);
                panel.style.borderTopColor = new Color(0.24f, 0.24f, 0.31f, 1f);
                panel.style.borderBottomColor = new Color(0.24f, 0.24f, 0.31f, 1f);
                panel.style.paddingLeft = 15;
                panel.style.paddingRight = 15;
                panel.style.paddingTop = 15;
                panel.style.paddingBottom = 15;
                panel.style.width = Length.Percent(48);
                panel.style.flexGrow = 1;
                panel.style.marginLeft = 5;
                panel.style.marginRight = 5;
                panel.style.minHeight = 200;
                panel.style.flexDirection = FlexDirection.Column;
            }
            
            // 查找并应用标题样式
            var titles = rootElement.Query<Label>(className: "panel-title").ToList();
            foreach (var title in titles)
            {
                title.style.color = Color.white;
                title.style.fontSize = 16;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 15;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            
            // 查找并应用内容容器样式
            var contents = rootElement.Query<VisualElement>(className: "panel-content").ToList();
            foreach (var content in contents)
            {
                content.style.flexGrow = 1;
                content.style.justifyContent = Justify.Center;
                content.style.alignItems = Align.Center;
                content.style.minHeight = 0;
            }
            
            // 查找并应用图表容器样式
            var chartContainers = rootElement.Query<VisualElement>(className: "chart-container").ToList();
            foreach (var container in chartContainers)
            {
                container.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.9f); // rgba(26, 38, 51, 0.9)
                container.style.borderTopLeftRadius = 8;
                container.style.borderTopRightRadius = 8;
                container.style.borderBottomLeftRadius = 8;
                container.style.borderBottomRightRadius = 8;
                container.style.paddingLeft = 15;
                container.style.paddingRight = 15;
                container.style.paddingTop = 15;
                container.style.paddingBottom = 15;
                container.style.marginTop = 15;
                container.style.minHeight = 120;
                container.style.minWidth = 200;
                container.style.alignItems = Align.Center;
                container.style.justifyContent = Justify.Center;
            }
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
                
                // 保存对container的引用，用于实时刷新
                currentContent = container;
                
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
                    
                    // 应用统计大屏样式
                    ApplyDashboardStyles(overlay);
                    
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
            ApplyFont(title, FontSize.Title);
            header.Add(title);
            
            var datetimeLabel = new Label(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            datetimeLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            datetimeLabel.style.fontSize = 14;
            ApplyFont(datetimeLabel, FontSize.Small);
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
            ApplyFont(closeButton, FontSize.Title);
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
            CreateTreeDetectionPanel(bottomRow);
            Debug.Log("树木检测统计面板创建完成");
            
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
            
            // 获取真实电塔数据（参考场景总览的实现）
            Debug.Log("开始获取真实电塔数据");
            var stats = GetRealDeviceStats();
            Debug.Log($"真实数据获取完成: 总电塔数={stats.totalTowers}, 运行中={stats.operatingTowers}, 维护中={stats.maintenanceTowers}, 警告={stats.warningTowers}, 故障={stats.errorTowers}, 系统健康度={stats.systemHealth:F1}%");
            
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
            
            // 获取真实电力线性能数据
            Debug.Log("开始获取真实电力线性能数据");
            var stats = GetRealPerformanceStats();
            Debug.Log($"真实数据获取完成: 总线路长度={stats.totalLength:F2}km, 平均电压={stats.averageVoltage:F1}kV, 导线总数={stats.totalWires}, 线路数量={stats.lineCount}");
            
            // 创建统计项（根据PowerlineInfo类的实际属性调整）
            Debug.Log("开始创建电力线性能统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总线路长度", $"{stats.totalLength:F2} km" },
                { "平均电压", $"{stats.averageVoltage:F1} kV" },
                { "导线总数", $"{stats.totalWires} 根" },
                { "线路数量", $"{stats.lineCount} 条" }
            });
            Debug.Log("电力线性能统计项创建完成");
            
            // 创建仪表图（显示线路覆盖率）
            Debug.Log("开始创建电力线性能仪表图");
            var chartContainer = CreateChartContainerWithoutBackground();
            Debug.Log("图表容器创建完成");
            
            if (chartRenderer != null)
            {
                Debug.Log("ChartRenderer组件存在，开始调用CreateGaugeChart");
                // 使用电力线健康比例作为仪表图数据
                chartRenderer.CreateGaugeChart(chartContainer, stats.coverageRate, 100f, "电力线健康比例");
                Debug.Log("ChartRenderer.CreateGaugeChart调用完成");
            }
            else
            {
                Debug.LogError("ChartRenderer组件为null，无法创建仪表图");
            }
            
            panel.Add(chartContainer);
            Debug.Log("仪表图容器添加到面板完成");
            Debug.Log("=== 电力线性能统计面板创建完成 ===");
        }
        
        /// <summary>
        /// 创建树木检测统计面板
        /// </summary>
        void CreateTreeDetectionPanel(VisualElement parent)
        {
            Debug.Log("=== 开始创建树木检测统计面板 ===");
            
            var panel = CreatePanel("树木检测统计", new Color(0.2f, 0.6f, 0.4f, 1f));
            Debug.Log("树木检测统计面板基础结构创建完成");
            parent.Add(panel);
            Debug.Log("树木检测统计面板添加到父容器完成");
            
            // 获取真实树木检测数据
            Debug.Log("开始获取真实树木检测数据");
            var stats = GetRealTreeDetectionStats();
            Debug.Log($"真实数据获取完成: 总树木数={stats.totalTrees}, 安全={stats.safeTrees}, 警告={stats.warningTrees}, 危险={stats.criticalTrees}, 紧急={stats.emergencyTrees}, 危险比例={stats.dangerPercentage:F1}%");
            
            // 创建统计项
            Debug.Log("开始创建树木检测统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总树木数", stats.totalTrees.ToString() },
                { "安全树木", stats.safeTrees.ToString() },
                { "警告树木", stats.warningTrees.ToString() },
                { "危险树木", stats.criticalTrees.ToString() },
                { "紧急树木", stats.emergencyTrees.ToString() }
            });
            
            // 创建仪表图显示危险比例
            Debug.Log("开始创建树木检测仪表图");
            var chartContainer = new VisualElement();
            chartContainer.style.width = 120;
            chartContainer.style.height = 120;
            chartContainer.style.alignSelf = Align.Center;
            chartContainer.style.marginTop = 10;
            panel.Add(chartContainer);
            
            if (chartRenderer != null)
            {
                Debug.Log("ChartRenderer组件存在，开始调用CreateGaugeChart");
                // 使用危险比例作为仪表图数据
                chartRenderer.CreateGaugeChart(chartContainer, stats.dangerPercentage, 100f, "危险比例");
                Debug.Log("ChartRenderer.CreateGaugeChart调用完成");
            }
            else
            {
                Debug.LogWarning("ChartRenderer组件未找到，无法创建仪表图");
                // 创建文本显示作为备选
                var textLabel = new Label($"危险比例: {stats.dangerPercentage:F1}%");
                textLabel.style.color = Color.white;
                textLabel.style.fontSize = 16;
                textLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                textLabel.style.alignSelf = Align.Center;
                chartContainer.Add(textLabel);
            }
            
            Debug.Log("=== 树木检测统计面板创建完成 ===");
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
            
            // 获取真实危险监测数据
            Debug.Log("开始获取真实危险监测数据");
            var stats = GetRealHazardStats();
            Debug.Log($"真实数据获取完成: 总危险点={stats.totalDangers}, 风险评估={stats.riskAssessment:F1}分");
            
            // 创建统计项（重新设计，突出危险等级分布）
            Debug.Log("开始创建危险监测统计项");
            CreateStatsItems(panel, new Dictionary<string, string>
            {
                { "总危险点", stats.totalDangers.ToString() },
                { "高风险", stats.highRiskCount.ToString() },
                { "中风险", stats.mediumRiskCount.ToString() },
                { "低风险", stats.lowRiskCount.ToString() },
                { "风险评估", $"{stats.riskAssessment:F1}分" }
            });
            Debug.Log("危险监测统计项创建完成");
            
            // 创建柱状图（显示危险等级分布）
            Debug.Log("开始创建危险监测柱状图");
            var chartContainer = CreateChartContainerWithoutBackground();
            Debug.Log("图表容器创建完成");
            
            // 使用危险等级数据创建柱状图
            var data = new List<float> { stats.highRiskCount, stats.mediumRiskCount, stats.lowRiskCount };
            var labels = new List<string> { "高风险", "中风险", "低风险" };
            
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
            ApplyFont(titleLabel, FontSize.Subtitle);
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
            ApplyFont(labelElement, FontSize.Tiny);
            item.Add(labelElement);
            
            var valueElement = new Label(value);
            valueElement.style.color = Color.white;
            valueElement.style.fontSize = 18; // 从20px减少到18px，节省空间
            valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            ApplyFont(valueElement, FontSize.Subtitle);
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
            ApplyFont(progressLabel, FontSize.Subtitle);
            
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
        /// 应用字体到Label
        /// </summary>
        private void ApplyFont(Label label, FontSize size = FontSize.Body)
        {
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyFont(label, size);
            }
            else
            {
                Debug.LogWarning("FontManager实例不存在，使用默认字体设置");
            }
        }
        
        /// <summary>
        /// 应用字体到Button
        /// </summary>
        private void ApplyFont(Button button, FontSize size = FontSize.Body)
        {
            if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyFont(button, size);
            }
            else
            {
                Debug.LogWarning("FontManager实例不存在，使用默认字体设置");
            }
        }
        
        /// <summary>
        /// 获取真实电塔数据（参考场景总览的实现）
        /// </summary>
        private DeviceOperationStats GetRealDeviceStats()
        {
            var stats = new DeviceOperationStats();
            
            // 尝试从StatisticsDashboardManager获取真实数据
            var dashboardManager = FindObjectOfType<StatisticsDashboardManager>();
            if (dashboardManager != null)
            {
                var realStats = dashboardManager.GetDeviceStats();
                if (realStats.totalTowers > 0)
                {
                    Debug.Log("从StatisticsDashboardManager获取到真实数据");
                    return realStats;
                }
            }
            
            // 如果StatisticsDashboardManager没有数据，尝试从SceneOverviewManager获取
            var sceneOverviewManager = FindObjectOfType<SceneOverviewManager>();
            if (sceneOverviewManager != null)
            {
                var towers = sceneOverviewManager.GetTowerData();
                if (towers != null && towers.Count > 0)
                {
                    Debug.Log($"从SceneOverviewManager获取到{towers.Count}座电塔数据");
                    
                    stats.totalTowers = towers.Count;
                    stats.operatingTowers = towers.Count(t => t.status == "normal");
                    stats.warningTowers = towers.Count(t => t.status == "warning");
                    stats.errorTowers = towers.Count(t => t.status == "error");
                    stats.maintenanceTowers = stats.totalTowers - stats.operatingTowers - stats.warningTowers - stats.errorTowers;
                    
                    if (stats.totalTowers > 0)
                    {
                        stats.systemHealth = (float)stats.operatingTowers / stats.totalTowers * 100f;
                    }
                    
                    Debug.Log($"真实数据统计完成: 总电塔数={stats.totalTowers}, 运行中={stats.operatingTowers}, 维护中={stats.maintenanceTowers}, 警告={stats.warningTowers}, 故障={stats.errorTowers}, 系统健康度={stats.systemHealth:F1}%");
                    return stats;
                }
            }
            
            // 如果都没有真实数据，返回测试数据
            Debug.Log("未找到真实电塔数据，使用测试数据");
            return GenerateTestDeviceStats();
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
        
        private PowerlinePerformanceStats GetRealPerformanceStats()
        {
            var stats = new PowerlinePerformanceStats();
            
            // 尝试从StatisticsDashboardManager获取真实数据
            var dashboardManager = FindObjectOfType<StatisticsDashboardManager>();
            if (dashboardManager != null)
            {
                var realStats = dashboardManager.GetPerformanceStats();
                if (realStats.totalLength > 0)
                {
                    Debug.Log("从StatisticsDashboardManager获取到真实电力线性能数据");
                    return realStats;
                }
            }
            
            // 如果StatisticsDashboardManager没有数据，尝试从SceneInitializer直接获取
            var sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null && sceneInitializer.powerlines != null && sceneInitializer.powerlines.Count > 0)
            {
                Debug.Log($"从SceneInitializer获取到{sceneInitializer.powerlines.Count}条电力线数据");
                
                // 计算总长度（转换为公里）
                stats.totalLength = sceneInitializer.powerlines.Sum(p => p.length) / 1000f;
                
                // 获取平均电压
                stats.averageVoltage = sceneInitializer.powerlines.Average(p => p.voltage);
                
                // 计算导线总数（基于wireCount属性）
                stats.totalWires = sceneInitializer.powerlines.Sum(p => p.wireCount);
                
                // 线路数量
                stats.lineCount = sceneInitializer.powerlines.Count;
                
                // 计算电力线健康状态比例（优秀与良好线路数量总和/总线路数量）
                // 从PowerlineInteraction组件获取电力线状态，而不是从电塔数据获取
                var powerlineInteractions = FindObjectsOfType<PowerlineInteraction>();
                if (powerlineInteractions != null && powerlineInteractions.Length > 0)
                {
                    Debug.Log($"找到{powerlineInteractions.Length}个PowerlineInteraction组件");
                    
                    // 统计电力线状态
                    int excellentPowerlines = 0;
                    int goodPowerlines = 0;
                    int maintenancePowerlines = 0;
                    
                    foreach (var powerline in powerlineInteractions)
                    {
                        string condition = powerline.GetCurrentCondition();
                        Debug.Log($"电力线 {powerline.name} 状态: {condition}");
                        
                        if (condition == "优秀")
                        {
                            excellentPowerlines++;
                        }
                        else if (condition == "良好")
                        {
                            goodPowerlines++;
                        }
                        else if (condition == "需要维护")
                        {
                            maintenancePowerlines++;
                        }
                    }
                    
                    // 健康电力线数量（优秀+良好）
                    int healthyPowerlines = excellentPowerlines + goodPowerlines;
                    int totalPowerlines = powerlineInteractions.Length;
                    
                    // 健康状态比例 = 健康电力线数量 / 总电力线数量 × 100%
                    if (totalPowerlines > 0)
                    {
                        stats.coverageRate = Mathf.Clamp((float)healthyPowerlines / totalPowerlines * 100f, 0f, 100f);
                    }
                    else
                    {
                        stats.coverageRate = 100f; // 如果没有电力线，比例100%
                    }
                    
                    Debug.Log($"电力线状态统计: 优秀={excellentPowerlines}, 良好={goodPowerlines}, 需要维护={maintenancePowerlines}, 健康比例={stats.coverageRate:F1}%");
                }
                else
                {
                    Debug.LogWarning("未找到PowerlineInteraction组件，使用默认健康比例");
                    stats.coverageRate = 85f; // 默认健康比例
                }
                
                Debug.Log($"真实数据统计完成: 总线路长度={stats.totalLength:F2}km, 平均电压={stats.averageVoltage:F1}kV, 导线总数={stats.totalWires}, 线路数量={stats.lineCount}, 电力线健康比例={stats.coverageRate:F1}%");
                return stats;
            }
            
            // 如果都没有真实数据，返回测试数据
            Debug.Log("未找到真实电力线性能数据，使用测试数据");
            return GenerateTestPerformanceStats();
        }
        
        private PowerlinePerformanceStats GenerateTestPerformanceStats()
        {
            return new PowerlinePerformanceStats
            {
                totalLength = 89.5f,
                averageVoltage = 220.0f,
                totalWires = 216,        // 新增：导线总数
                lineCount = 72,          // 新增：线路数量
                coverageRate = 92.5f,    // 新增：线路覆盖率
                powerLoss = 2.8f,        // 保留原有属性以兼容
                efficiency = 97.2f,      // 保留原有属性以兼容
                lineEfficiency = new Dictionary<string, float>()
            };
        }
        
        /// <summary>
        /// 危险监测统计数据
        /// </summary>
        private class HazardMonitoringStats
        {
            public int totalDangers;           // 总危险点数量
            public int highRiskCount;          // 高风险数量
            public int mediumRiskCount;        // 中风险数量
            public int lowRiskCount;           // 低风险数量
            public float riskAssessment;       // 风险评估分数
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

        /// <summary>
        /// 获取真实树木检测数据
        /// </summary>
        private TreeDetectionStats GetRealTreeDetectionStats()
        {
            var stats = new TreeDetectionStats();
            
            // 尝试从StatisticsDashboardManager获取真实数据
            var dashboardManager = FindObjectOfType<StatisticsDashboardManager>();
            if (dashboardManager != null)
            {
                var realStats = dashboardManager.GetTreeDetectionStats();
                if (realStats.totalTrees > 0)
                {
                    Debug.Log("从StatisticsDashboardManager获取到真实树木检测数据");
                    return realStats;
                }
            }
            
            // 如果StatisticsDashboardManager没有数据，尝试从TreeDangerMonitor直接获取
            var treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
            if (treeDangerMonitor != null)
            {
                Debug.Log("从TreeDangerMonitor直接获取树木检测数据");
                
                // 获取危险统计信息
                var dangerStats = treeDangerMonitor.GetDangerStatistics();
                
                // 统计总树木数
                stats.totalTrees = 0;
                foreach (var kvp in dangerStats)
                {
                    stats.totalTrees += kvp.Value;
                }
                
                // 设置各状态树木数量
                stats.safeTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Safe) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Safe] : 0;
                stats.warningTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Warning) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Warning] : 0;
                stats.criticalTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Critical) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Critical] : 0;
                stats.emergencyTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Emergency) ? 
                    dangerStats[TreeDangerMonitor.TreeDangerLevel.Emergency] : 0;
                
                // 计算危险比例
                int totalDangerousTrees = stats.warningTrees + stats.criticalTrees + stats.emergencyTrees;
                if (stats.totalTrees > 0)
                {
                    stats.dangerPercentage = (float)totalDangerousTrees / stats.totalTrees * 100f;
                }
                else
                {
                    stats.dangerPercentage = 0f;
                }
                
                Debug.Log($"真实树木检测数据统计完成: 总树木数={stats.totalTrees}, 安全={stats.safeTrees}, 警告={stats.warningTrees}, 危险={stats.criticalTrees}, 紧急={stats.emergencyTrees}, 危险比例={stats.dangerPercentage:F1}%");
                return stats;
            }
            
            // 如果都没有真实数据，返回测试数据
            Debug.Log("未找到真实树木检测数据，使用测试数据");
            return GenerateTestTreeDetectionStats();
        }

        /// <summary>
        /// 生成测试树木检测数据
        /// </summary>
        private TreeDetectionStats GenerateTestTreeDetectionStats()
        {
            return new TreeDetectionStats
            {
                totalTrees = 1000,
                safeTrees = 950,
                warningTrees = 30,
                criticalTrees = 15,
                emergencyTrees = 5,
                dangerPercentage = 5f
            };
        }
        
        /// <summary>
        /// 获取真实危险监测数据
        /// </summary>
        private HazardMonitoringStats GetRealHazardStats()
        {
            var stats = new HazardMonitoringStats();
            
            // 尝试从StatisticsDashboardManager获取真实数据
            var dashboardManager = FindObjectOfType<StatisticsDashboardManager>();
            if (dashboardManager != null)
            {
                var realStats = dashboardManager.GetDangerStats();
                if (realStats.totalDangers > 0)
                {
                    Debug.Log("从StatisticsDashboardManager获取到真实危险监测数据");
                    
                    stats.totalDangers = realStats.totalDangers;
                    stats.highRiskCount = realStats.dangerByLevel.ContainsKey(DangerLevel.High) ? 
                        realStats.dangerByLevel[DangerLevel.High] : 0;
                    stats.mediumRiskCount = realStats.dangerByLevel.ContainsKey(DangerLevel.Medium) ? 
                        realStats.dangerByLevel[DangerLevel.Medium] : 0;
                    stats.lowRiskCount = realStats.dangerByLevel.ContainsKey(DangerLevel.Low) ? 
                        realStats.dangerByLevel[DangerLevel.Low] : 0;
                    stats.riskAssessment = realStats.riskAssessment;
                    
                    Debug.Log($"真实危险监测数据统计完成: 总危险点={stats.totalDangers}, 高风险={stats.highRiskCount}, 中风险={stats.mediumRiskCount}, 低风险={stats.lowRiskCount}, 风险评估={stats.riskAssessment:F1}分");
                    return stats;
                }
            }
            
            // 如果StatisticsDashboardManager没有数据，尝试从UIToolkitDangerController获取已创建的危险标记数据
            var dangerController = FindObjectOfType<UIToolkitDangerController>();
            if (dangerController != null)
            {
                Debug.Log("从UIToolkitDangerController获取已创建的危险标记数据");
                
                var dangerMarkers = dangerController.GetDangerMarkers();
                if (dangerMarkers != null && dangerMarkers.Count > 0)
                {
                    Debug.Log($"找到{dangerMarkers.Count}个危险标记");
                    
                    // 统计各等级危险数量
                    int highRisk = 0;
                    int mediumRisk = 0;
                    int lowRisk = 0;
                    
                    foreach (var marker in dangerMarkers)
                    {
                        if (marker != null)
                        {
                            switch (marker.dangerLevel)
                            {
                                case DangerLevel.High:
                                    highRisk++;
                                    break;
                                case DangerLevel.Medium:
                                    mediumRisk++;
                                    break;
                                case DangerLevel.Low:
                                    lowRisk++;
                                    break;
                            }
                        }
                    }
                    
                    stats.totalDangers = dangerMarkers.Count;
                    stats.highRiskCount = highRisk;
                    stats.mediumRiskCount = mediumRisk;
                    stats.lowRiskCount = lowRisk;
                    
                    // 计算风险评估分数（基于危险等级加权）
                    if (stats.totalDangers > 0)
                    {
                        float riskScore = (stats.highRiskCount * 3f + stats.mediumRiskCount * 2f + stats.lowRiskCount * 1f) / stats.totalDangers;
                        stats.riskAssessment = Mathf.Max(0f, 100f - riskScore * 20f); // 转换为0-100分制
                    }
                    else
                    {
                        stats.riskAssessment = 100f; // 没有危险时满分
                    }
                    
                    Debug.Log($"从危险标记数据统计完成: 总危险点={stats.totalDangers}, 高风险={stats.highRiskCount}, 中风险={stats.mediumRiskCount}, 低风险={stats.lowRiskCount}, 风险评估={stats.riskAssessment:F1}分");
                    return stats;
                }
                else
                {
                    Debug.Log("UIToolkitDangerController中没有危险标记");
                }
            }
            else
            {
                Debug.Log("未找到UIToolkitDangerController");
            }
            
            // 如果没有真实数据，返回不包含树木类型的测试数据
            Debug.Log("未找到真实危险监测数据，使用不包含树木类型的测试数据");
            return GenerateTestHazardStats();
        }
        
        /// <summary>
        /// 生成测试危险监测数据（不包含树木类型）
        /// </summary>
        private HazardMonitoringStats GenerateTestHazardStats()
        {
            return new HazardMonitoringStats
            {
                totalDangers = 0,  // 当前没有非树木类型的危险
                highRiskCount = 0,
                mediumRiskCount = 0,
                lowRiskCount = 0,
                riskAssessment = 100.0f  // 没有危险时满分
            };
        }
    }
}
