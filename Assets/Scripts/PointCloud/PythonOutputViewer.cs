using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Linq;

namespace UI
{
    /// <summary>
    /// Python输出查看器 - 优化版（解决边界溢出和滚动问题）
    /// 专门用于显示Python脚本的实时输出，具有现代化UI设计和流畅滚动
    /// </summary>
    public class PythonOutputViewer : MonoBehaviour
    {
        [Header("输出显示设置")]
        public bool showOnStart = false;
        public bool autoScroll = true;
        public int maxOutputLines = 20; // 限制为20条输出，超过就滚动
        
        [Header("滚动优化设置")]
        [Tooltip("滚动动画持续时间（秒）")]
        public float scrollAnimationDuration = 0.2f;
        [Tooltip("滚动速度倍数")]
        public float scrollSpeedMultiplier = 2.0f;
        [Tooltip("是否启用平滑滚动")]
        public bool enableSmoothScrolling = true;
        
        [Header("UI样式")]
        public Color backgroundColor = new Color(0.06f, 0.07f, 0.1f, 0.95f);
        public Color cardColor = new Color(0.1f, 0.11f, 0.14f, 1f);
        public Color accentColor = new Color(0.25f, 0.51f, 0.96f, 1f);
        public Color textColor = new Color(0.9f, 0.9f, 0.92f, 1f);
        public Color textSecondaryColor = new Color(0.6f, 0.6f, 0.65f, 1f);
        public Color successColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        public Color warningColor = new Color(1f, 0.7f, 0.2f, 1f);
        public Color errorColor = new Color(0.95f, 0.35f, 0.35f, 1f);
        
        [Header("字体设置")]
        [Tooltip("自定义字体，如果不设置将使用系统默认字体")]
        public Font customFont;
        
        // UI组件
        private VisualElement outputPanel;
        private VisualElement titleBar;
        private ScrollView outputScrollView;
        private VisualElement outputContainer;
        private ProgressBar progressBar;
        private Label titleLabel;
        private Label statusLabel;
        private Button closeButton;
        private Button clearButton;
        private Button minimizeButton;
        private Label minimizeLabel;
        private VisualElement statusIndicator;
        private Label timeLabel;
        private VisualElement headerCard;
        
        // 输出管理
        private List<string> outputLines = new List<string>();
        private StringBuilder currentOutput = new StringBuilder();
        private bool isVisible = false;
        private bool isMinimized = false;
        private ExecutionStatus currentStatus = ExecutionStatus.Idle;
        
        // 动画相关
        private float animationSpeed = 0.3f;
        
        // 滚动优化相关
        private Coroutine scrollCoroutine;
        private float targetScrollValue = 0f;
        private bool userScrolling = false; // 标记用户是否正在手动滚动
        private float lastUserScrollTime = 0f; // 用户最后滚动时间
        
        // 输出频率控制
        private float lastOutputTime = 0f;
        private float outputThrottleInterval = 0.02f; // 减少到20ms间隔，提高响应速度
        private Queue<string> outputQueue = new Queue<string>();
        private bool isProcessingOutput = false;
        
        // 状态枚举
        public enum ExecutionStatus
        {
            Idle,       // 空闲
            Running,    // 运行中
            Success,    // 成功
            Warning,    // 警告
            Error       // 错误
        }
        
        // 单例模式
        private static PythonOutputViewer instance;
        public static PythonOutputViewer Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("PythonOutputViewer");
                    instance = go.AddComponent<PythonOutputViewer>();
                }
                return instance;
            }
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            if (showOnStart)
            {
                ShowOutputViewer();
            }
        }
        
        /// <summary>
        /// 应用字体到UI元素
        /// </summary>
        private void ApplyFont(VisualElement element)
        {
            if (element != null)
            {
                // 优先使用FontManager
                if (FontManager.Instance != null)
                {
                    var currentFont = FontManager.Instance.GetCurrentFont();
                    if (currentFont != null)
                    {
                        element.style.unityFont = currentFont;
                    }
                }
                else
                {
                    // 备用方案：使用自定义字体
                    if (customFont != null)
                    {
                        element.style.unityFont = customFont;
                    }
                    else
                    {
                        // 使用Unity内建字体确保文本可见
                        var defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        if (defaultFont != null)
                        {
                            element.style.unityFont = defaultFont;
                        }
                    }
                }
                
                // 确保字体大小设置正确
                if (element is Label label)
                {
                    if (label.style.fontSize.value.value <= 0)
                    {
                        label.style.fontSize = 14;
                    }
                    // 确保文本颜色可见
                    if (label.style.color.value.a < 0.1f)
                    {
                        label.style.color = textColor;
                    }
                }
                else if (element is Button button)
                {
                    if (button.style.fontSize.value.value <= 0)
                    {
                        button.style.fontSize = 14;
                    }
                    // 确保按钮文本颜色可见
                    if (button.style.color.value.a < 0.1f)
                    {
                        button.style.color = Color.white;
                    }
                }
            }
        }
        
        /// <summary>
        /// 显示Python输出查看器
        /// </summary>
        public void ShowOutputViewer()
        {
            if (isVisible) return;
            
            CreateOutputViewer();
            isVisible = true;
            
            // 添加淡入动画效果
            StartCoroutine(FadeInAnimation());
        }
        
        /// <summary>
        /// 隐藏Python输出查看器
        /// </summary>
        public void HideOutputViewer()
        {
            // 停止滚动协程
            StopScrolling();
            
            if (outputPanel != null)
            {
                StartCoroutine(FadeOutAnimation(false));
            }
        }
        
        /// <summary>
        /// 关闭Python输出查看器
        /// </summary>
        public void CloseOutputViewer()
        {
            // 停止滚动协程
            StopScrolling();
            
            if (outputPanel != null)
            {
                StartCoroutine(FadeOutAnimation(true));
            }
        }
        
        /// <summary>
        /// 最小化/还原窗口
        /// </summary>
        public void ToggleMinimize()
        {
            isMinimized = !isMinimized;
            StartCoroutine(MinimizeAnimation());
        }
        
        /// <summary>
        /// 淡入动画
        /// </summary>
        private IEnumerator FadeInAnimation()
        {
            if (outputPanel == null) yield break;
            
            outputPanel.style.opacity = 0f;
            
            float elapsedTime = 0f;
            while (elapsedTime < animationSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationSpeed;
                t = Mathf.SmoothStep(0f, 1f, t);
                
                outputPanel.style.opacity = t;
                
                yield return null;
            }
            
            outputPanel.style.opacity = 1f;
        }
        
        /// <summary>
        /// 淡出动画
        /// </summary>
        private IEnumerator FadeOutAnimation(bool destroy)
        {
            if (outputPanel == null) yield break;
            
            float elapsedTime = 0f;
            while (elapsedTime < animationSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = 1f - (elapsedTime / animationSpeed);
                t = Mathf.SmoothStep(0f, 1f, t);
                
                outputPanel.style.opacity = t;
                
                yield return null;
            }
            
            if (destroy)
            {
                outputPanel.RemoveFromHierarchy();
                outputPanel = null;
            }
            else
            {
                outputPanel.style.display = DisplayStyle.None;
            }
            
            isVisible = false;
        }
        
        /// <summary>
        /// 最小化动画
        /// </summary>
        private IEnumerator MinimizeAnimation()
        {
            if (outputPanel == null) yield break;
            
            // 简单的显示/隐藏切换，避免复杂的高度动画
            if (outputScrollView != null)
                outputScrollView.style.display = isMinimized ? DisplayStyle.None : DisplayStyle.Flex;
            if (headerCard != null)
                headerCard.style.display = isMinimized ? DisplayStyle.None : DisplayStyle.Flex;
            
            // 更新最小化按钮文本
            if (minimizeLabel != null)
                minimizeLabel.text = isMinimized ? "□" : "—";
                
            yield return null;
        }
        
        /// <summary>
        /// 设置执行状态
        /// </summary>
        public void SetExecutionStatus(ExecutionStatus status, string message = "")
        {
            currentStatus = status;
            
            if (statusIndicator != null)
            {
                Color statusColor = GetStatusColor(status);
                statusIndicator.style.backgroundColor = statusColor;
                
                // 添加脉冲动画效果（运行中时）
                if (status == ExecutionStatus.Running)
                {
                    StartCoroutine(PulseAnimation(statusIndicator));
                }
            }
            
            if (statusLabel != null)
            {
                string statusText = GetStatusText(status);
                
                if (!string.IsNullOrEmpty(message))
                    statusText += $" - {message}";
                    
                statusLabel.text = statusText;
            }
        }
        
        /// <summary>
        /// 获取状态颜色
        /// </summary>
        private Color GetStatusColor(ExecutionStatus status)
        {
            switch (status)
            {
                case ExecutionStatus.Running: return accentColor;
                case ExecutionStatus.Success: return successColor;
                case ExecutionStatus.Warning: return warningColor;
                case ExecutionStatus.Error: return errorColor;
                default: return textSecondaryColor;
            }
        }
        
        /// <summary>
        /// 获取状态文本
        /// </summary>
        private string GetStatusText(ExecutionStatus status)
        {
            switch (status)
            {
                case ExecutionStatus.Running: return "执行中...";
                case ExecutionStatus.Success: return "执行完成";
                case ExecutionStatus.Warning: return "执行警告";
                case ExecutionStatus.Error: return "执行错误";
                default: return "就绪";
            }
        }
        
        /// <summary>
        /// 脉冲动画
        /// </summary>
        private IEnumerator PulseAnimation(VisualElement element)
        {
            while (currentStatus == ExecutionStatus.Running && element != null)
            {
                float time = 0f;
                while (time < 1f)
                {
                    time += Time.deltaTime * 2f;
                    float alpha = Mathf.PingPong(time, 1f);
                    Color currentColor = element.style.backgroundColor.value;
                    currentColor.a = 0.3f + alpha * 0.7f;
                    element.style.backgroundColor = currentColor;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 添加输出行（修复乱码问题）
        /// </summary>
        /// <param name="line">原始输出行</param>
        /// <param name="isError">是否为错误信息</param>
        public void AddOutputLine(string line, bool isError = false)
        {
            if (string.IsNullOrEmpty(line)) return;
            
            // 清理乱码和特殊字符
            string cleanedLine = CleanOutputLine(line);
            
            // 提取进度信息
            float progress = ExtractProgress(cleanedLine);
            if (progress >= 0)
            {
                UpdateProgress(progress, cleanedLine);
            }
            
            // 添加到输出列表
            outputLines.Add($"[{System.DateTime.Now:HH:mm:ss}] {cleanedLine}");
            
            // 限制输出行数
            if (outputLines.Count > maxOutputLines)
            {
                outputLines.RemoveAt(0);
            }
            
            // 更新UI显示
            AddOutput(cleanedLine, isError);
            
            // 自动滚动到底部
            if (autoScroll && outputScrollView != null)
            {
                StartCoroutine(ScrollToBottomDelayed());
            }
        }
        
        /// <summary>
        /// 清理输出行中的乱码和特殊字符
        /// </summary>
        private string CleanOutputLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";
            
            // 移除ANSI转义序列（颜色代码等）
            line = Regex.Replace(line, @"\x1B\[[0-9;]*[mK]", "");
            
            // 移除或替换常见的乱码字符
            line = line.Replace("♦", "█");
            line = line.Replace("◊", "▓");
            line = line.Replace("♠", "▒");
            line = line.Replace("♣", "░");
            
            // 移除其他不可打印字符，但保留中文
            StringBuilder cleaned = new StringBuilder();
            foreach (char c in line)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                {
                    continue;
                }
                else if (c >= 32 && c <= 126) // ASCII可打印字符
                {
                    cleaned.Append(c);
                }
                else if (c >= 0x4E00 && c <= 0x9FFF) // 中文字符范围
                {
                    cleaned.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    cleaned.Append(c);
                }
                else
                {
                    cleaned.Append('.');
                }
            }
            
            return cleaned.ToString().Trim();
        }
        
        /// <summary>
        /// 从输出中提取进度信息
        /// </summary>
        private float ExtractProgress(string line)
        {
            // 匹配百分比模式：XX%
            Match percentMatch = Regex.Match(line, @"(\d+)%");
            if (percentMatch.Success)
            {
                if (float.TryParse(percentMatch.Groups[1].Value, out float percent))
                {
                    return percent / 100f;
                }
            }
            
            // 匹配分数模式：XX/XX
            Match fractionMatch = Regex.Match(line, @"(\d+)/(\d+)");
            if (fractionMatch.Success)
            {
                if (float.TryParse(fractionMatch.Groups[1].Value, out float current) &&
                    float.TryParse(fractionMatch.Groups[2].Value, out float total) &&
                    total > 0)
                {
                    return current / total;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgress(float progress, string statusText)
        {
            if (progressBar != null)
            {
                progressBar.value = progress * 100f;
            }
            
            // 更新状态
            if (progress >= 1f)
            {
                SetExecutionStatus(ExecutionStatus.Success, "执行完成");
            }
            else if (progress > 0f)
            {
                SetExecutionStatus(ExecutionStatus.Running, $"进度 {progress * 100:F1}%");
            }
        }
        
        /// <summary>
        /// 延迟滚动到底部
        /// </summary>
        private System.Collections.IEnumerator ScrollToBottomDelayed()
        {
            yield return new WaitForEndOfFrame();
            if (outputScrollView != null)
            {
                outputScrollView.scrollOffset = new Vector2(0, outputScrollView.contentContainer.layout.height);
            }
        }
        
        /// <summary>
        /// 创建输出查看器UI
        /// </summary>
        private void CreateOutputViewer()
        {
            // 获取或创建UI根节点
            UIDocument uiDocument = FindObjectOfType<UIDocument>();
            if (uiDocument == null)
            {
                GameObject uiObj = new GameObject("PythonOutputUI");
                uiDocument = uiObj.AddComponent<UIDocument>();
            }
            
            VisualElement rootElement = uiDocument.rootVisualElement;
            
            // 创建主面板
            outputPanel = new VisualElement();
            outputPanel.name = "PythonOutputPanel";
            outputPanel.style.position = Position.Absolute;
            outputPanel.style.left = new Length(5, LengthUnit.Percent);
            outputPanel.style.top = new Length(5, LengthUnit.Percent);
            outputPanel.style.width = new Length(90, LengthUnit.Percent);
            outputPanel.style.height = new Length(90, LengthUnit.Percent);
            outputPanel.style.backgroundColor = backgroundColor;
            outputPanel.style.borderTopLeftRadius = 16;
            outputPanel.style.borderTopRightRadius = 16;
            outputPanel.style.borderBottomLeftRadius = 16;
            outputPanel.style.borderBottomRightRadius = 16;
            
            // 设置主面板为垂直布局，确保子元素正确排列
            outputPanel.style.flexDirection = FlexDirection.Column;
            outputPanel.style.overflow = Overflow.Hidden; // 防止内容溢出
            
            // 创建标题栏
            CreateModernTitleBar();
            
            // 创建信息卡片
            CreateInfoCard();
            
            // 创建输出区域
            CreateModernOutputArea();
            
            // 创建控制按钮区域
            CreateModernControlButtons();
            
            rootElement.Add(outputPanel);
            
            // 初始化状态
            SetExecutionStatus(ExecutionStatus.Idle);
            UpdateTimeLabel();
            
            // 开始时间更新协程
            StartCoroutine(UpdateTimeCoroutine());
        }
        
        /// <summary>
        /// 创建现代化标题栏
        /// </summary>
        private void CreateModernTitleBar()
        {
            titleBar = new VisualElement();
            titleBar.style.flexDirection = FlexDirection.Row;
            titleBar.style.justifyContent = Justify.SpaceBetween;
            titleBar.style.alignItems = Align.Center;
            titleBar.style.height = 56;
            titleBar.style.paddingLeft = 20;
            titleBar.style.paddingRight = 20;
            titleBar.style.backgroundColor = cardColor;
            titleBar.style.borderTopLeftRadius = 16;
            titleBar.style.borderTopRightRadius = 16;
            
            // 左侧：状态指示器 + 标题 + 状态
            VisualElement leftSection = new VisualElement();
            leftSection.style.flexDirection = FlexDirection.Row;
            leftSection.style.alignItems = Align.Center;
            
            // 状态指示器
            statusIndicator = new VisualElement();
            statusIndicator.style.width = 8;
            statusIndicator.style.height = 8;
            statusIndicator.style.backgroundColor = textSecondaryColor;
            statusIndicator.style.borderTopLeftRadius = 4;
            statusIndicator.style.borderTopRightRadius = 4;
            statusIndicator.style.borderBottomLeftRadius = 4;
            statusIndicator.style.borderBottomRightRadius = 4;
            statusIndicator.style.marginRight = 12;
            
            // 标题
            titleLabel = new Label("🐍 Python 脚本执行");
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = textColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginRight = 16;
            ApplyFont(titleLabel);
            
            // 状态文字
            statusLabel = new Label("就绪");
            statusLabel.style.fontSize = 12;
            statusLabel.style.color = textSecondaryColor;
            ApplyFont(statusLabel);
            
            leftSection.Add(statusIndicator);
            leftSection.Add(titleLabel);
            leftSection.Add(statusLabel);
            
            // 右侧：控制按钮
            VisualElement rightSection = new VisualElement();
            rightSection.style.flexDirection = FlexDirection.Row;
            rightSection.style.alignItems = Align.Center;
            
            // 最小化按钮
            minimizeButton = new Button(() => ToggleMinimize());
            minimizeLabel = new Label("—");
            minimizeLabel.style.color = textSecondaryColor;
            minimizeLabel.style.fontSize = 14;
            minimizeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            minimizeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(minimizeLabel);
            minimizeButton.Add(minimizeLabel);
            StyleButton(minimizeButton, 32, 32, textSecondaryColor);
            minimizeButton.style.marginRight = 8;
            
            // 关闭按钮
            closeButton = new Button(() => CloseOutputViewer());
            var closeLabel = new Label("✕");
            closeLabel.style.color = errorColor;
            closeLabel.style.fontSize = 14;
            closeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(closeLabel);
            closeButton.Add(closeLabel);
            StyleButton(closeButton, 32, 32, errorColor);
            
            rightSection.Add(minimizeButton);
            rightSection.Add(closeButton);
            
            titleBar.Add(leftSection);
            titleBar.Add(rightSection);
            outputPanel.Add(titleBar);
        }
        
        /// <summary>
        /// 创建信息卡片
        /// </summary>
        private void CreateInfoCard()
        {
            headerCard = new VisualElement();
            headerCard.style.backgroundColor = cardColor;
            headerCard.style.marginTop = 0;
            headerCard.style.marginLeft = 16;
            headerCard.style.marginRight = 16;
            headerCard.style.marginBottom = 12;
            headerCard.style.paddingTop = 12;
            headerCard.style.paddingBottom = 12;
            headerCard.style.paddingLeft = 20;
            headerCard.style.paddingRight = 20;
            headerCard.style.borderTopLeftRadius = 8;
            headerCard.style.borderTopRightRadius = 8;
            headerCard.style.borderBottomLeftRadius = 8;
            headerCard.style.borderBottomRightRadius = 8;
            
            // 进度条容器
            VisualElement progressContainer = new VisualElement();
            progressContainer.style.marginBottom = 8;
            
            Label progressLabel = new Label("执行进度");
            progressLabel.style.fontSize = 14; // 从12增加到14
            progressLabel.style.color = textSecondaryColor;
            progressLabel.style.marginBottom = 8;
            progressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(progressLabel);
            
            progressBar = new ProgressBar();
            progressBar.style.height = 6;
            progressBar.style.borderTopLeftRadius = 3;
            progressBar.style.borderTopRightRadius = 3;
            progressBar.style.borderBottomLeftRadius = 3;
            progressBar.style.borderBottomRightRadius = 3;
            progressBar.value = 0;
            
            progressContainer.Add(progressLabel);
            progressContainer.Add(progressBar);
            
            // 时间信息
            VisualElement timeContainer = new VisualElement();
            timeContainer.style.flexDirection = FlexDirection.Row;
            timeContainer.style.justifyContent = Justify.SpaceBetween;
            timeContainer.style.alignItems = Align.Center;
            
            timeLabel = new Label();
            timeLabel.style.fontSize = 12; // 从11增加到12
            timeLabel.style.color = textSecondaryColor;
            ApplyFont(timeLabel);
            
            Label infoLabel = new Label("实时输出监控");
            infoLabel.style.fontSize = 12; // 从11增加到12
            infoLabel.style.color = accentColor;
            infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(infoLabel);
            
            timeContainer.Add(timeLabel);
            timeContainer.Add(infoLabel);
            
            headerCard.Add(progressContainer);
            headerCard.Add(timeContainer);
            outputPanel.Add(headerCard);
        }
        
        /// <summary>
        /// 创建现代化输出区域（优化版 - 解决边界溢出和滚动问题）
        /// </summary>
        private void CreateModernOutputArea()
        {
            outputScrollView = new ScrollView();
            outputScrollView.style.flexGrow = 1; // 占据剩余空间
            outputScrollView.style.flexShrink = 1; // 允许收缩
            outputScrollView.style.minHeight = 300; // 进一步增加最小高度
            outputScrollView.style.maxHeight = 600; // 进一步增加最大高度
            outputScrollView.style.marginLeft = 16;
            outputScrollView.style.marginRight = 16;
            outputScrollView.style.marginBottom = 16;
            outputScrollView.style.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            outputScrollView.style.borderTopLeftRadius = 8;
            outputScrollView.style.borderTopRightRadius = 8;
            outputScrollView.style.borderBottomLeftRadius = 8;
            outputScrollView.style.borderBottomRightRadius = 8;
            outputScrollView.style.paddingTop = 8; // 增加内边距，让内容更易读
            outputScrollView.style.paddingBottom = 8;
            outputScrollView.style.paddingLeft = 12; // 增加内边距
            outputScrollView.style.paddingRight = 12;
            
            // 配置滚动模式和速度 - 优化滚动体验
            outputScrollView.mode = ScrollViewMode.Vertical;
            outputScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible; // 始终显示滚动条
            outputScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden; // 隐藏水平滚动条，强制换行
            
            // 确保滚动视图可以接收滚轮事件
            outputScrollView.focusable = true;
            
            // 设置内容容器的样式以支持自动换行和防止溢出
            outputScrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            outputScrollView.contentContainer.style.overflow = Overflow.Hidden;
            outputScrollView.contentContainer.style.width = Length.Percent(100); // 确保内容容器占满宽度
            // 移除minHeight设置，避免可能的API兼容性问题
            
            // 创建输出容器 - 严格约束布局
            outputContainer = new VisualElement();
            outputContainer.name = "OutputContainer";
            outputContainer.style.whiteSpace = WhiteSpace.Normal; // 允许文本换行
            outputContainer.style.flexGrow = 1; // 允许内容增长
            outputContainer.style.flexShrink = 0; // 防止内容被压缩
            outputContainer.style.width = Length.Percent(100); // 确保占满宽度
            outputContainer.style.overflow = Overflow.Hidden; // 严格防止溢出
            outputContainer.style.flexDirection = FlexDirection.Column; // 垂直排列
            outputContainer.style.alignItems = Align.Stretch; // 拉伸对齐，确保子元素占满宽度
            outputContainer.style.minWidth = 0; // 允许收缩到0
            outputScrollView.Add(outputContainer);
            
            // 简单的滚动条样式
            if (outputScrollView.verticalScroller != null)
            {
                outputScrollView.verticalScroller.style.width = 16;
                outputScrollView.verticalScroller.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            }
            
            // 添加占位符文本
            CreatePlaceholder();
            
            // 添加鼠标滚轮事件处理（确保只注册一次）
            outputScrollView.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
            
            // 添加键盘事件处理，支持PageUp/PageDown滚动
            outputScrollView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            outputPanel.Add(outputScrollView);
        }
        
        /// <summary>
        /// 处理滚轮事件，提供流畅的滚动体验
        /// </summary>
        private void OnScrollWheel(WheelEvent evt)
        {
            Debug.Log($"[PythonOutputViewer] 滚轮事件触发: delta.y={evt.delta.y}");
            
            if (outputScrollView != null && outputScrollView.verticalScroller != null)
            {
                // 标记用户正在手动滚动
                userScrolling = true;
                lastUserScrollTime = Time.time;
                
                // 使用配置的滚动速度倍数
                float currentValue = outputScrollView.verticalScroller.value;
                float newValue = currentValue + (evt.delta.y * scrollSpeedMultiplier);
                
                // 限制在有效范围内
                newValue = Mathf.Clamp(newValue, outputScrollView.verticalScroller.lowValue, outputScrollView.verticalScroller.highValue);
                
                Debug.Log($"[PythonOutputViewer] 滚轮滚动: {currentValue} -> {newValue}");
                
                if (enableSmoothScrolling)
                {
                    // 使用平滑滚动
                    SmoothScrollTo(newValue);
                }
                else
                {
                    // 直接设置滚动位置
                    outputScrollView.verticalScroller.value = newValue;
                }
                
                evt.StopPropagation();
            }
            else
            {
                Debug.LogWarning("[PythonOutputViewer] 滚轮事件处理失败: outputScrollView或verticalScroller为空");
            }
        }
        
        /// <summary>
        /// 处理键盘事件，支持PageUp/PageDown滚动
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (outputScrollView != null && outputScrollView.verticalScroller != null)
            {
                float currentValue = outputScrollView.verticalScroller.value;
                float pageSize = outputScrollView.verticalScroller.highValue * 0.8f; // 页面大小
                
                switch (evt.keyCode)
                {
                    case KeyCode.PageUp:
                        // 向上翻页
                        float newValueUp = currentValue - pageSize;
                        newValueUp = Mathf.Clamp(newValueUp, outputScrollView.verticalScroller.lowValue, outputScrollView.verticalScroller.highValue);
                        SmoothScrollTo(newValueUp);
                        evt.StopPropagation();
                        break;
                        
                    case KeyCode.PageDown:
                        // 向下翻页
                        float newValueDown = currentValue + pageSize;
                        newValueDown = Mathf.Clamp(newValueDown, outputScrollView.verticalScroller.lowValue, outputScrollView.verticalScroller.highValue);
                        SmoothScrollTo(newValueDown);
                        evt.StopPropagation();
                        break;
                        
                    case KeyCode.Home:
                        // 滚动到顶部
                        ScrollToTop();
                        evt.StopPropagation();
                        break;
                        
                    case KeyCode.End:
                        // 滚动到底部
                        ScrollToBottom();
                        evt.StopPropagation();
                        break;
                }
            }
        }
        
        /// <summary>
        /// 平滑滚动到指定位置
        /// </summary>
        private void SmoothScrollTo(float targetValue)
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }
            
            targetScrollValue = targetValue;
            scrollCoroutine = StartCoroutine(SmoothScrollCoroutine());
        }
        
        /// <summary>
        /// 平滑滚动协程
        /// </summary>
        private IEnumerator SmoothScrollCoroutine()
        {
            if (outputScrollView?.verticalScroller == null) yield break;
            
            float startValue = outputScrollView.verticalScroller.value;
            float elapsedTime = 0f;
            
            while (elapsedTime < scrollAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scrollAnimationDuration;
                
                // 使用缓动函数使滚动更自然
                t = Mathf.SmoothStep(0f, 1f, t);
                
                float currentValue = Mathf.Lerp(startValue, targetScrollValue, t);
                outputScrollView.verticalScroller.value = currentValue;
                
                yield return null;
            }
            
            // 确保最终位置准确
            outputScrollView.verticalScroller.value = targetScrollValue;
            scrollCoroutine = null;
        }
        
        /// <summary>
        /// 创建占位符
        /// </summary>
        private void CreatePlaceholder()
        {
            Label placeholderLabel = new Label("等待 Python 脚本开始执行...");
            placeholderLabel.style.color = textSecondaryColor;
            placeholderLabel.style.fontSize = 14; // 从12增加到14
            placeholderLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            placeholderLabel.style.marginTop = 40;
            placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter; // 居中显示
            ApplyFont(placeholderLabel);
            outputContainer.Add(placeholderLabel);
        }
        
        /// <summary>
        /// 创建现代化控制按钮
        /// </summary>
        private void CreateModernControlButtons()
        {
            VisualElement buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.FlexEnd;
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.paddingLeft = 20;
            buttonContainer.style.paddingRight = 20;
            buttonContainer.style.paddingBottom = 20;
            buttonContainer.style.paddingTop = 8;
            
            // 清空按钮
            clearButton = new Button(() => ClearOutput());
            var clearLabel = new Label("🗑️ 清空");
            clearLabel.style.color = warningColor;
            clearLabel.style.fontSize = 11;
            clearLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            clearLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(clearLabel);
            clearButton.Add(clearLabel);
            StyleButton(clearButton, 80, 36, warningColor);
            clearButton.style.marginRight = 12;
            
            // 隐藏按钮
            Button hideButton = new Button(() => HideOutputViewer());
            var hideLabel = new Label("📌 隐藏");
            hideLabel.style.color = textSecondaryColor;
            hideLabel.style.fontSize = 11;
            hideLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            hideLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(hideLabel);
            hideButton.Add(hideLabel);
            StyleButton(hideButton, 80, 36, textSecondaryColor);
            
            buttonContainer.Add(clearButton);
            buttonContainer.Add(hideButton);
            outputPanel.Add(buttonContainer);
        }
        
        /// <summary>
        /// 设置按钮样式
        /// </summary>
        private void StyleButton(Button button, int width, int height, Color color)
        {
            button.style.width = width;
            button.style.height = height;
            button.style.backgroundColor = new Color(color.r, color.g, color.b, 0.15f);
            button.style.color = color;
            button.style.borderTopLeftRadius = 8;
            button.style.borderTopRightRadius = 8;
            button.style.borderBottomLeftRadius = 8;
            button.style.borderBottomRightRadius = 8;
            button.style.fontSize = 11;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        
        /// <summary>
        /// 更新时间标签
        /// </summary>
        private void UpdateTimeLabel()
        {
            if (timeLabel != null)
            {
                timeLabel.text = $"开始时间: {DateTime.Now:HH:mm:ss}";
            }
        }
        
        /// <summary>
        /// 时间更新协程
        /// </summary>
        private IEnumerator UpdateTimeCoroutine()
        {
            while (timeLabel != null)
            {
                timeLabel.text = $"当前时间: {DateTime.Now:HH:mm:ss}";
                yield return new WaitForSeconds(1f);
            }
        }

        // 公共API方法 - 保持向后兼容性
        
        /// <summary>
        /// 显示Python输出窗口
        /// </summary>
        public void ShowWindow(string title = "Python脚本执行")
        {
            if (outputPanel == null)
            {
                CreateOutputViewer();
            }
            
            SetTitle(title);
            outputPanel.style.display = DisplayStyle.Flex;
            isVisible = true;
            
            ClearOutput();
            SetExecutionStatus(ExecutionStatus.Running, "准备执行...");
            StartCoroutine(FadeInAnimation());
            
            Debug.Log("[PythonOutputViewer] 显示Python输出窗口");
        }
        
        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow()
        {
            StartCoroutine(FadeOutAnimation(false));
            Debug.Log("[PythonOutputViewer] 隐藏Python输出窗口");
        }
        
        /// <summary>
        /// 设置窗口标题
        /// </summary>
        public void SetTitle(string title)
        {
            if (titleLabel != null)
            {
                titleLabel.text = $"🐍 {title}";
            }
        }
        
        /// <summary>
        /// 添加输出文本（最终优化版 - 彻底解决边界溢出问题）
        /// </summary>
        public void AddOutput(string text, bool isError = false)
        {
            if (string.IsNullOrEmpty(text) || outputContainer == null) 
            {
                Debug.LogWarning($"[PythonOutputViewer] 无法添加输出: outputContainer={outputContainer}, text={text}");
                return;
            }
            
            // 输出频率控制 - 避免输出过快
            if (Time.time - lastOutputTime < outputThrottleInterval)
            {
                // 将输出加入队列，稍后处理
                outputQueue.Enqueue($"{text}|{isError}");
                if (!isProcessingOutput)
                {
                    StartCoroutine(ProcessOutputQueue());
                }
                return;
            }
            
            lastOutputTime = Time.time;
            
            // 实际添加输出
            AddOutputInternal(text, isError);
        }
        
        /// <summary>
        /// 内部输出方法
        /// </summary>
        private void AddOutputInternal(string text, bool isError = false)
        {
            // 移除占位符（如果存在）
            if (outputContainer.childCount > 0 && outputContainer.Children().First().name != "OutputLine")
            {
                outputContainer.Clear();
            }
            
            // 清理乱码和特殊字符
            string cleanText = CleanOutput(text);
            if (string.IsNullOrEmpty(cleanText)) return;
            
            // 创建输出行容器（超紧凑布局，适合500条输出）
            var outputLine = new VisualElement();
            outputLine.name = "OutputLine";
            outputLine.style.flexDirection = FlexDirection.Row;
            outputLine.style.marginBottom = 0; // 无行间距
            outputLine.style.paddingLeft = 2; // 最小内边距
            outputLine.style.paddingRight = 2;
            outputLine.style.paddingTop = 3;
            outputLine.style.paddingBottom = 3;
            outputLine.style.backgroundColor = new Color(1f, 1f, 1f, 0.01f); // 更淡的背景
            outputLine.style.borderTopLeftRadius = 2;
            outputLine.style.borderTopRightRadius = 2;
            outputLine.style.borderBottomLeftRadius = 2;
            outputLine.style.borderBottomRightRadius = 2;
            outputLine.style.width = Length.Percent(100);
            outputLine.style.overflow = Overflow.Hidden; // 严格防止溢出
            outputLine.style.minHeight = 20; // 增加高度，适应更大的字体
            outputLine.style.alignItems = Align.FlexStart;
            
            // 添加类型指示器（超紧凑）
            var typeIndicator = new VisualElement();
            typeIndicator.style.width = 1; // 最小宽度
            typeIndicator.style.backgroundColor = isError ? errorColor : accentColor;
            typeIndicator.style.borderTopLeftRadius = 0;
            typeIndicator.style.borderTopRightRadius = 0;
            typeIndicator.style.borderBottomLeftRadius = 0;
            typeIndicator.style.borderBottomRightRadius = 0;
            typeIndicator.style.marginRight = 4; // 最小右边距
            typeIndicator.style.marginTop = 1;
            typeIndicator.style.marginBottom = 1;
            typeIndicator.style.flexShrink = 0;
            typeIndicator.style.flexGrow = 0;
            outputLine.Add(typeIndicator);
            
            // 添加时间戳（超紧凑）
            var timeStamp = new Label($"[{DateTime.Now:HH:mm:ss}]");
            timeStamp.style.color = textSecondaryColor;
            timeStamp.style.fontSize = 12; // 增大字体
            timeStamp.style.width = 70; // 增加固定宽度
            timeStamp.style.marginRight = 4; // 最小右边距
            timeStamp.style.unityFontStyleAndWeight = FontStyle.Bold;
            timeStamp.style.flexShrink = 0;
            timeStamp.style.flexGrow = 0;
            timeStamp.style.overflow = Overflow.Hidden;
            ApplyFont(timeStamp);
            outputLine.Add(timeStamp);
            
            // 创建内容容器（严格约束宽度）
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexShrink = 1;
            contentContainer.style.overflow = Overflow.Hidden;
            contentContainer.style.width = Length.Percent(100);
            contentContainer.style.maxWidth = Length.Percent(100); // 严格限制最大宽度
            contentContainer.style.minWidth = 0; // 允许收缩到0
            
            // 添加内容标签（超紧凑显示）
            var contentLabel = new Label(cleanText);
            contentLabel.style.color = isError ? errorColor : textColor;
            contentLabel.style.fontSize = 13; // 增大字体
            contentLabel.style.whiteSpace = WhiteSpace.Normal;
            contentLabel.style.overflow = Overflow.Hidden; // 严格防止溢出
            contentLabel.style.width = Length.Percent(100);
            contentLabel.style.maxWidth = Length.Percent(100);
            contentLabel.style.minWidth = 0; // 允许收缩到0
            contentLabel.style.textOverflow = TextOverflow.Ellipsis; // 超出时显示省略号
            contentLabel.style.flexWrap = Wrap.Wrap;
            contentLabel.style.flexShrink = 1; // 允许收缩
            // 移除lineHeight属性，因为它在当前Unity版本中不可用
            // 使用CSS样式确保文本换行
            contentLabel.style.unityTextAlign = TextAnchor.UpperLeft; // 左对齐，有助于换行
            ApplyFont(contentLabel);
            
            contentContainer.Add(contentLabel);
            outputLine.Add(contentContainer);
            
            outputContainer.Add(outputLine);
            
            // 限制输出行数
            while (outputContainer.childCount > maxOutputLines)
            {
                outputContainer.RemoveAt(0);
            }
            
            // 强制滚动逻辑：每次添加输出后立即滚动到底部
            if (autoScroll && !userScrolling)
            {
                // 立即强制滚动到底部
                ForceScrollToBottom();
                
                // 启动延迟滚动协程，确保内容完全渲染后再次滚动
                if (scrollCoroutine != null)
                {
                    StopCoroutine(scrollCoroutine);
                }
                scrollCoroutine = StartCoroutine(DelayedScrollToBottom());
            }
            else if (autoScroll && userScrolling)
            {
                // 如果用户正在手动滚动，检查是否已经停止滚动一段时间
                if (Time.time - lastUserScrollTime > 2.0f) // 2秒后恢复自动滚动
                {
                    userScrolling = false;
                    // 恢复自动滚动到底部
                    ForceScrollToBottom();
                }
            }
            
            // 检查错误状态
            if (isError)
            {
                SetExecutionStatus(ExecutionStatus.Error, "发现错误");
            }
            
            Debug.Log($"[PythonOutputViewer] 添加输出: {cleanText}");
        }
        
        /// <summary>
        /// 处理输出队列
        /// </summary>
        private IEnumerator ProcessOutputQueue()
        {
            isProcessingOutput = true;
            
            while (outputQueue.Count > 0)
            {
                yield return new WaitForSeconds(outputThrottleInterval);
                
                if (outputQueue.Count > 0)
                {
                    string queuedOutput = outputQueue.Dequeue();
                    string[] parts = queuedOutput.Split('|');
                    if (parts.Length == 2)
                    {
                        string text = parts[0];
                        bool isError = bool.Parse(parts[1]);
                        AddOutputInternal(text, isError);
                    }
                }
            }
            
            isProcessingOutput = false;
        }
        
        /// <summary>
        /// 强制延迟滚动到底部，确保内容完全渲染后滚动
        /// </summary>
        private IEnumerator DelayedScrollToBottom()
        {
            // 等待一帧，让UI更新
            yield return new WaitForEndOfFrame();
            
            // 强制滚动到底部
            if (outputScrollView?.verticalScroller != null)
            {
                outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
            }
            
            // 再等待一帧，确保滚动位置稳定
            yield return new WaitForEndOfFrame();
            
            // 再次强制滚动，确保位置正确
            if (outputScrollView?.verticalScroller != null)
            {
                outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
            }
            
            // 最后等待一帧，再次确认滚动位置
            yield return new WaitForEndOfFrame();
            
            // 最终强制滚动
            if (outputScrollView?.verticalScroller != null)
            {
                outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
            }
        }
        
        /// <summary>
        /// 停止滚动
        /// </summary>
        private void StopScrolling()
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
        }
        
        /// <summary>
        /// 强制滚动到底部（公共方法）
        /// </summary>
        public void ForceScrollToBottom()
        {
            if (outputScrollView?.verticalScroller != null)
            {
                outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
                
                // 强制更新UI
                outputScrollView.schedule.Execute(() => {
                    if (outputScrollView?.verticalScroller != null)
                    {
                        outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
                    }
                }).ExecuteLater(1);
            }
        }
        
        /// <summary>
        /// 启用/禁用自动滚动
        /// </summary>
        public void SetAutoScroll(bool enabled)
        {
            autoScroll = enabled;
            if (!enabled)
            {
                StopScrolling();
            }
            else
            {
                // 启用自动滚动时，重置用户滚动状态
                userScrolling = false;
            }
        }
        
        /// <summary>
        /// 滚动到顶部
        /// </summary>
        public void ScrollToTop()
        {
            if (outputScrollView?.verticalScroller != null)
            {
                outputScrollView.verticalScroller.value = 0;
            }
        }
        
        /// <summary>
        /// 滚动到底部（优化版）
        /// </summary>
        public void ScrollToBottom()
        {
            if (outputScrollView != null)
            {
                if (enableSmoothScrolling)
                {
                    SmoothScrollToBottom();
                }
                else
                {
                    StartCoroutine(ScrollToBottomCoroutine());
                }
            }
        }
        
        /// <summary>
        /// 获取当前滚动位置信息
        /// </summary>
        public string GetScrollInfo()
        {
            if (outputScrollView?.verticalScroller != null)
            {
                float currentValue = outputScrollView.verticalScroller.value;
                float maxValue = outputScrollView.verticalScroller.highValue;
                float percentage = maxValue > 0 ? (currentValue / maxValue) * 100 : 0;
                string userScrollStatus = userScrolling ? " (用户滚动中)" : " (自动滚动)";
                return $"滚动位置: {currentValue:F0}/{maxValue:F0} ({percentage:F1}%){userScrollStatus}";
            }
            return "滚动信息不可用";
        }
        
        /// <summary>
        /// 重置用户滚动状态，恢复自动滚动
        /// </summary>
        public void ResetUserScroll()
        {
            userScrolling = false;
            lastUserScrollTime = 0f;
            if (autoScroll)
            {
                ForceScrollToBottom();
            }
        }
        

        
        /// <summary>
        /// 手动触发滚轮事件测试
        /// </summary>
        [ContextMenu("手动测试滚轮事件")]
        public void TestWheelEventManually()
        {
            if (outputScrollView != null && outputScrollView.verticalScroller != null)
            {
                Debug.Log("[PythonOutputViewer] 手动测试滚轮事件");
                
                // 直接模拟滚轮滚动效果
                float currentValue = outputScrollView.verticalScroller.value;
                float newValue = currentValue - 100; // 向上滚动100像素
                newValue = Mathf.Clamp(newValue, outputScrollView.verticalScroller.lowValue, outputScrollView.verticalScroller.highValue);
                
                // 标记用户正在手动滚动
                userScrolling = true;
                lastUserScrollTime = Time.time;
                
                // 设置新的滚动位置
                outputScrollView.verticalScroller.value = newValue;
                
                Debug.Log($"[PythonOutputViewer] 手动滚轮事件已发送: {currentValue} -> {newValue}");
            }
            else
            {
                Debug.LogWarning("[PythonOutputViewer] outputScrollView或verticalScroller为空，无法测试滚轮事件");
            }
        }
        
        /// <summary>
        /// 设置进度（美化版）
        /// </summary>
        public void SetProgress(float progress, string statusText = "")
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp(progress, 0f, 100f);
            }
            
            // 更新状态
            if (progress >= 100f)
            {
                SetExecutionStatus(ExecutionStatus.Success, "执行完成");
            }
            else if (progress > 0f)
            {
                SetExecutionStatus(ExecutionStatus.Running, $"进度 {progress:F1}%");
            }
            
            Debug.Log($"[PythonOutputViewer] 设置进度: {progress}% - {statusText}");
        }
        
        /// <summary>
        /// 清空输出（美化版）
        /// </summary>
        public void ClearOutput()
        {
            if (outputContainer != null)
            {
                outputContainer.Clear();
                CreatePlaceholder();
            }
            
            // 重置滚动状态
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
            
            // 重置用户滚动状态
            userScrolling = false;
            lastUserScrollTime = 0f;
            
            SetProgress(0f, "准备执行...");
            SetExecutionStatus(ExecutionStatus.Idle);
            UpdateTimeLabel();
            
            Debug.Log("[PythonOutputViewer] 清空输出");
        }
        

        
        /// <summary>
        /// 平滑滚动到底部
        /// </summary>
        private void SmoothScrollToBottom()
        {
            if (outputScrollView?.verticalScroller != null)
            {
                float targetValue = outputScrollView.verticalScroller.highValue;
                SmoothScrollTo(targetValue);
            }
        }
        
        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame();
            if (outputScrollView != null)
            {
                outputScrollView.verticalScroller.value = outputScrollView.verticalScroller.highValue;
            }
        }
        
        /// <summary>
        /// 清理输出文本中的乱码和特殊字符（增强版）
        /// </summary>
        private string CleanOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            try
            {
                // 尝试多种编码方式处理文本，解决中文乱码问题
                if (DetectEncoding(text) == "GBK")
                {
                    // 如果检测到GBK编码，尝试转换
                    try
                    {
                        byte[] gbkBytes = System.Text.Encoding.GetEncoding("GBK").GetBytes(text);
                        text = System.Text.Encoding.UTF8.GetString(
                            System.Text.Encoding.Convert(
                                System.Text.Encoding.GetEncoding("GBK"), 
                                System.Text.Encoding.UTF8, 
                                gbkBytes
                            )
                        );
                    }
                    catch
                    {
                        // GBK转换失败，继续使用原文本
                    }
                }
                else
                {
                    // 默认使用UTF-8编码处理
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
                    text = System.Text.Encoding.UTF8.GetString(bytes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PythonOutputViewer] 编码转换失败: {ex.Message}，使用原始文本");
            }
            
            // 移除ANSI转义序列（包括颜色代码和光标控制）
            string cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"\x1B\[[0-9;]*[a-zA-Z]", "");
            
            // 移除tqdm进度条字符和特殊符号
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[█▓▒░▌▐▀▄■□▪▫●○◆◇♦♧♠♣]", "");
            
            // 移除退格符和回车符导致的重叠文本
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[\b\r]+", "");
            
            // 移除其他控制字符，但保留换行符、制表符
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // 处理多个连续的空格，但保留单个换行
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[ \t]+", " ");
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\n\s*\n", "\n");
            
            // 处理超长行，防止界面溢出 - 超严格限制版本
            if (cleanText.Length > 120) // 进一步减少最大长度，确保不溢出
            {
                // 智能截断，尝试在合适的位置截断
                int maxLength = 120;
                int truncatePos = maxLength;
                
                // 尝试在空格或标点符号处截断
                for (int i = maxLength - 15; i < maxLength; i++)
                {
                    if (i < cleanText.Length && (cleanText[i] == ' ' || cleanText[i] == ',' || cleanText[i] == '.' || cleanText[i] == ';' || cleanText[i] == '|'))
                    {
                        truncatePos = i + 1;
                        break;
                    }
                }
                
                cleanText = cleanText.Substring(0, truncatePos) + "...";
            }
            
            // 处理进度条文本的特殊情况
            if (cleanText.Contains("Computing linear features:"))
            {
                // 简化进度条显示，只保留关键信息
                var match = System.Text.RegularExpressions.Regex.Match(cleanText, @"Computing linear features: (\d+)%");
                if (match.Success)
                {
                    cleanText = $"Computing linear features: {match.Groups[1].Value}%";
                }
            }
            
            // 移除多余的空白字符
            cleanText = cleanText.Trim();
            
            if (string.IsNullOrWhiteSpace(cleanText)) return string.Empty;
            
            return cleanText;
        }
        
        /// <summary>
        /// 简单的编码检测
        /// </summary>
        private string DetectEncoding(string text)
        {
            // 简单的中文编码检测逻辑
            if (text.Contains("��") || // 常见的乱码字符
                System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u00C0-\u00FF]{2,}")) // 可能的GBK乱码特征
            {
                return "GBK";
            }
            return "UTF8";
        }
        
        /// <summary>
        /// 测试方法 - 添加一些测试输出来验证字体显示
        /// </summary>
        [ContextMenu("测试文字显示")]
        public void TestTextDisplay()
        {
            ShowOutputViewer();
            
            AddOutputLine("✅ 测试消息 1: 正常输出文字显示", false);
            AddOutputLine("⚠️ 测试消息 2: 警告信息显示", false);
            AddOutputLine("❌ 测试消息 3: 错误信息显示", true);
            AddOutputLine("🔄 测试消息 4: 中文字符显示正常", false);
            AddOutputLine("📊 测试消息 5: 进度显示 [50%]", false);
            
            SetExecutionStatus(ExecutionStatus.Running);
        }
        
        /// <summary>
        /// 测试持续滚动功能
        /// </summary>
        [ContextMenu("测试持续滚动")]
        public void TestContinuousScroll()
        {
            ShowOutputViewer();
            ClearOutput();
            
            // 添加大量测试输出，验证滚动功能
            StartCoroutine(AddTestOutputs());
        }
        
        private IEnumerator AddTestOutputs()
        {
            for (int i = 1; i <= 200; i++)
            {
                AddOutput($"测试输出行 {i}: 这是一条很长的测试消息，用来验证滚动视图是否正常工作。当前时间: {DateTime.Now:HH:mm:ss.fff}", false);
                
                // 每10行暂停一下，模拟真实输出
                if (i % 10 == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            Debug.Log("测试输出完成，滚动功能应该保持最新内容可见");
        }
        
        /// <summary>
        /// 测试滚动视图功能
        /// </summary>
        [ContextMenu("测试滚动视图")]
        public void TestScrollView()
        {
            ShowOutputViewer();
            
            // 添加大量测试输出，验证滚动功能
            for (int i = 1; i <= 50; i++)
            {
                AddOutputLine($"测试输出行 {i}: 这是一条很长的测试消息，用来验证滚动视图是否正常工作。当前时间: {DateTime.Now:HH:mm:ss}", false);
            }
            
            Debug.Log($"滚动视图信息:");
            Debug.Log($"- 输出容器子元素数量: {outputContainer?.childCount}");
            Debug.Log($"- 滚动条可见性: {outputScrollView?.verticalScrollerVisibility}");
            if (outputScrollView?.verticalScroller != null)
            {
                Debug.Log($"- 滚动条值: {outputScrollView.verticalScroller.value} / {outputScrollView.verticalScroller.highValue}");
            }
        }
        
        /// <summary>
        /// 测试输出框大小
        /// </summary>
        [ContextMenu("测试输出框大小")]
        public void TestOutputBoxSize()
        {
            ShowOutputViewer();
            ClearOutput();
            
            // 添加一些测试输出来验证输出框大小
            AddOutput("=== 输出框大小测试 ===");
            AddOutput("这是一条普通输出信息");
            AddOutput("这是一条错误信息", true);
            AddOutput("这是一条很长的输出信息，用来测试文本换行和显示效果。这条信息应该能够正确换行并显示在输出框中。");
            AddOutput("进度信息: 50%");
            AddOutput("完成测试");
            
            Debug.Log("输出框大小测试完成，请检查显示效果");
        }
        
        /// <summary>
        /// 测试字体大小和滚动功能
        /// </summary>
        [ContextMenu("测试字体和滚动")]
        public void TestFontAndScroll()
        {
            ShowOutputViewer();
            ClearOutput();
            
            // 添加大量测试输出来验证字体大小和滚动功能
            AddOutput("=== 字体大小和滚动功能测试 ===");
            AddOutput("当前字体大小: 时间戳12px, 内容13px");
            AddOutput("滚动条宽度: 16px, 始终可见");
            
            for (int i = 1; i <= 30; i++)
            {
                AddOutput($"测试行 {i}: 这是一条测试消息，用来验证字体大小和滚动功能。当前时间: {DateTime.Now:HH:mm:ss.fff}");
            }
            
            AddOutput("=== 滚动控制测试 ===");
            AddOutput("可以使用以下方法控制滚动:");
            AddOutput("- ScrollToTop(): 滚动到顶部");
            AddOutput("- ScrollToBottom(): 滚动到底部");
            AddOutput("- GetScrollInfo(): 获取滚动位置信息");
            AddOutput("=== 键盘快捷键 ===");
            AddOutput("- PageUp: 向上翻页");
            AddOutput("- PageDown: 向下翻页");
            AddOutput("- Home: 滚动到顶部");
            AddOutput("- End: 滚动到底部");
            AddOutput("- 鼠标滚轮: 平滑滚动");
            
            Debug.Log($"滚动信息: {GetScrollInfo()}");
            Debug.Log("字体和滚动测试完成，请检查显示效果和滚动功能");
        }
        
        /// <summary>
        /// 测试滚动修复效果
        /// </summary>
        [ContextMenu("测试滚动修复")]
        public void TestScrollFix()
        {
            ShowOutputViewer();
            ClearOutput();
            
            AddOutput("=== 滚动修复测试 ===");
            AddOutput("测试新的强制滚动逻辑，确保每次输出后立即滚动到底部");
            AddOutput("输出限制: 15条，超过就滚动");
            AddOutput("输出频率: 20ms间隔，比之前的50ms更快");
            AddOutput("滚动策略: 强制滚动 + 延迟确认滚动");
            
            // 快速添加大量输出，测试滚动响应
            StartCoroutine(RapidOutputTest());
        }
        
        private IEnumerator RapidOutputTest()
        {
            for (int i = 1; i <= 100; i++)
            {
                AddOutput($"快速输出测试 {i}: 这是一条测试消息，验证滚动是否及时响应。时间: {DateTime.Now:HH:mm:ss.fff}");
                
                // 每5行暂停一下，模拟真实输出场景
                if (i % 5 == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            AddOutput("=== 滚动修复测试完成 ===");
            AddOutput("如果能看到最新的输出始终在底部，说明滚动修复成功");
            AddOutput($"当前滚动位置: {GetScrollInfo()}");
            
            Debug.Log("滚动修复测试完成，请检查最新输出是否始终可见");
        }
        
        /// <summary>
        /// 测试20条输出限制和滚动
        /// </summary>
        [ContextMenu("测试20条输出限制")]
        public void Test20LineLimit()
        {
            ShowOutputViewer();
            ClearOutput();
            
            AddOutput("=== 20条输出限制测试 ===");
            AddOutput("当前设置: 最多显示20条输出，超过就滚动");
            AddOutput("测试目标: 验证滚动是否正常工作");
            AddOutput("滚轮功能: 可以使用鼠标滚轮查看之前的输出");
            
            // 添加25条输出，验证滚动效果
            StartCoroutine(Test20LineLimitCoroutine());
        }
        
        private IEnumerator Test20LineLimitCoroutine()
        {
            for (int i = 1; i <= 25; i++)
            {
                AddOutput($"输出行 {i}: 这是一条测试消息，验证20条限制和滚动功能。时间: {DateTime.Now:HH:mm:ss.fff}");
                
                // 每3行暂停一下，让用户观察滚动效果
                if (i % 3 == 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            AddOutput("=== 20条输出限制测试完成 ===");
            AddOutput($"当前输出行数: {outputContainer?.childCount}");
            AddOutput("如果看到滚动条在底部，说明滚动功能正常");
            AddOutput("可以使用鼠标滚轮向上滚动查看之前的输出");
            AddOutput("手动滚动后，2秒内不会自动滚动，之后会恢复自动滚动");
            AddOutput($"滚动位置: {GetScrollInfo()}");
            
            Debug.Log("20条输出限制测试完成，请检查滚动效果和滚轮功能");
        }
        
        /// <summary>
        /// 测试滚轮功能
        /// </summary>
        [ContextMenu("测试滚轮功能")]
        public void TestWheelFunction()
        {
            ShowOutputViewer();
            ClearOutput();
            
            AddOutput("=== 滚轮功能测试 ===");
            AddOutput("测试目标: 验证鼠标滚轮是否正常工作");
            AddOutput("使用方法: 在输出区域使用鼠标滚轮上下滚动");
            AddOutput("预期效果: 可以看到之前的输出内容");
            
            // 添加足够多的输出来测试滚轮
            for (int i = 1; i <= 30; i++)
            {
                AddOutput($"测试输出行 {i}: 这是一条测试消息，用来验证滚轮功能。时间: {DateTime.Now:HH:mm:ss.fff}");
            }
            
            AddOutput("=== 滚轮功能测试说明 ===");
            AddOutput("1. 现在应该看到滚动条在底部");
            AddOutput("2. 使用鼠标滚轮向上滚动，应该能看到之前的输出");
            AddOutput("3. 手动滚动后，2秒内不会自动滚动");
            AddOutput("4. 2秒后会自动恢复滚动到底部");
            AddOutput($"当前滚动状态: {GetScrollInfo()}");
            
            Debug.Log("滚轮功能测试完成，请尝试使用鼠标滚轮");
        }
        

        
        /// <summary>
        /// 销毁时清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (outputPanel?.parent != null)
            {
                outputPanel.parent.Remove(outputPanel);
            }
            
            Debug.Log("[PythonOutputViewer] 组件已销毁，清理UI资源");
        }
    }
} 