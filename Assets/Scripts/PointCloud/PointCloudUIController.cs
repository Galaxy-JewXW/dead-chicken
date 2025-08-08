using UnityEngine;
using UnityEngine.UIElements;
using PowerlineSystem;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI
{
    /// <summary>
    /// 点云UI控制器
    /// 为电力线可视化系统提供点云控制界面
    /// </summary>
    public class PointCloudUIController : MonoBehaviour
    {
        [Header("UI配置")]
        public Font uiFont;
        public Color primaryColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        public Color accentColor = new Color(0.1f, 0.8f, 0.4f, 1f);
        public Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Color backgroundColor = new Color(0.97f, 0.99f, 1f, 1f);
        
        [Header("点云管理器")]
        public PowerlinePointCloudManager pointCloudManager;
        
        [Header("电力线提取管理器")]
        public PowerLineExtractorManager powerLineExtractorManager;
        
        [Header("场景构建器")]
        public PowerlineExtractionSceneBuilder sceneBuilder;
        
        // UI元素引用
        private VisualElement pointCloudPanel;
        private Slider opacitySlider;
        private Label statsLabel;
        private Label statusLabel;
        private ProgressBar loadingProgressBar;
        private VisualElement fileListContainer;
        private Label filePathDisplay;
        private string selectedFile = "";
        private string selectedFilePath = "";
        
        // 状态
        private bool isPanelVisible = false;
        private bool isInitialized = false;
        
        // 常量
        private const int POINT_CLOUD_VIEWER_LAYER = 31;
        
        void Start()
        {
            InitializeController();
        }
        
        void InitializeController()
        {
            if (isInitialized)
                return;
                
            // 初始化点云管理器
            if (pointCloudManager == null)
            {
                pointCloudManager = FindObjectOfType<PowerlinePointCloudManager>();
            }
            
            if (pointCloudManager != null)
            {
                // 订阅点云管理器事件
                pointCloudManager.OnLoadingComplete += OnPointCloudLoadingCompleted;
                pointCloudManager.OnLoadingError += OnPointCloudLoadingError;
                Debug.Log("已订阅点云管理器事件");
            }
                
            // 初始化电力线提取管理器
            if (powerLineExtractorManager == null)
            {
                powerLineExtractorManager = FindObjectOfType<PowerLineExtractorManager>();
                if (powerLineExtractorManager == null)
                {
                    GameObject extractorObj = new GameObject("PowerLineExtractorManager");
                    powerLineExtractorManager = extractorObj.AddComponent<PowerLineExtractorManager>();
                }
            }
            
            if (powerLineExtractorManager != null)
            {
                // 订阅电力线提取管理器事件
                powerLineExtractorManager.OnStatusChanged += OnExtractionStatusChanged;
                powerLineExtractorManager.OnExtractionCompleted += OnExtractionCompleted;
                powerLineExtractorManager.OnError += OnExtractionError;
            }
            
            // 初始化场景构建器
            if (sceneBuilder == null)
            {
                sceneBuilder = FindObjectOfType<PowerlineExtractionSceneBuilder>();
                if (sceneBuilder == null)
                {
                    GameObject builderObj = new GameObject("PowerlineExtractionSceneBuilder");
                    sceneBuilder = builderObj.AddComponent<PowerlineExtractionSceneBuilder>();
                }
            }
            
            // 初始化UI
            InitializeUI();
            
            isInitialized = true;
        }
        
        void InitializeUI()
        {
            // 不再创建独立的UI面板，只通过SimpleUIToolkitManager的侧栏显示
            // UI将通过CreatePointCloudPanel()方法在侧栏中创建
            Debug.Log("PointCloudUIController: UI初始化跳过，将通过侧栏显示");
        }
        
        /// <summary>
        /// 创建点云面板并返回（用于外部UI系统）
        /// </summary>
        public VisualElement CreatePointCloudPanel()
        {
            var panel = new VisualElement();
            // 设置为相对定位，表明这是用于侧栏的
            panel.style.position = Position.Relative;
            CreatePointCloudPanelInternal(panel);
            return pointCloudPanel; // 返回实际的面板内容
        }
        
        void CreatePointCloudPanel(VisualElement parent)
        {
            CreatePointCloudPanelInternal(parent);
        }
        
        void CreatePointCloudPanelInternal(VisualElement parent)
        {
            pointCloudPanel = new VisualElement();
            
            // 检查是否作为独立面板使用（为外部UI系统创建）
            bool isForSidebar = (parent != null && parent.style.position.value == Position.Relative);
            
            if (isForSidebar)
            {
                // 侧栏模式：使用相对定位，适应侧栏布局
                pointCloudPanel.style.position = Position.Relative;
                pointCloudPanel.style.width = Length.Percent(100);
                pointCloudPanel.style.backgroundColor = Color.white;
                pointCloudPanel.style.paddingTop = 15;
                pointCloudPanel.style.paddingBottom = 15;
                pointCloudPanel.style.paddingLeft = 15;
                pointCloudPanel.style.paddingRight = 15;
                pointCloudPanel.style.marginBottom = 10;
                
                // 添加边框样式，与其他侧栏面板保持一致
                pointCloudPanel.style.borderLeftWidth = 2;
                pointCloudPanel.style.borderRightWidth = 2;
                pointCloudPanel.style.borderTopWidth = 2;
                pointCloudPanel.style.borderBottomWidth = 2;
                pointCloudPanel.style.borderLeftColor = primaryColor;
                pointCloudPanel.style.borderRightColor = primaryColor;
                pointCloudPanel.style.borderTopColor = primaryColor;
                pointCloudPanel.style.borderBottomColor = primaryColor;
                
                // 添加事件处理，防止点击穿透
                pointCloudPanel.pickingMode = PickingMode.Position;
                pointCloudPanel.RegisterCallback<MouseDownEvent>(evt => {
                    evt.StopPropagation();
                    evt.PreventDefault();
                });
                pointCloudPanel.RegisterCallback<MouseUpEvent>(evt => {
                    evt.StopPropagation();
                    evt.PreventDefault();
                });
                pointCloudPanel.RegisterCallback<ClickEvent>(evt => {
                    evt.StopPropagation();
                    evt.PreventDefault();
                });
                pointCloudPanel.RegisterCallback<MouseMoveEvent>(evt => {
                    evt.StopPropagation();
                });
            }
            else
            {
                // 独立模式：使用绝对定位，显示在主界面上
                pointCloudPanel.style.position = Position.Absolute;
                pointCloudPanel.style.top = 20;
                pointCloudPanel.style.left = 20;
                pointCloudPanel.style.width = 300;
                pointCloudPanel.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
                pointCloudPanel.style.paddingTop = 12;
                pointCloudPanel.style.paddingBottom = 12;
                pointCloudPanel.style.paddingLeft = 12;
                pointCloudPanel.style.paddingRight = 12;
            }
            
            // 通用样式
            pointCloudPanel.style.borderTopLeftRadius = 8;
            pointCloudPanel.style.borderTopRightRadius = 8;
            pointCloudPanel.style.borderBottomLeftRadius = 8;
            pointCloudPanel.style.borderBottomRightRadius = 8;
            
            // 标题（仅在独立模式下显示）
            if (!isForSidebar)
            {
                var titleLabel = new Label("电力线点云系统");
                titleLabel.style.fontSize = 16;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                titleLabel.style.marginBottom = 12;
                ApplyFont(titleLabel);
                pointCloudPanel.Add(titleLabel);
            }
            
            // 文件选择区域
            CreateFileSelectionArea(pointCloudPanel);
            
            // 按钮区域
            CreateButtonArea(pointCloudPanel);
            
            // Python环境配置信息
            CreatePythonInfoArea(pointCloudPanel);
            
            // 状态显示区域
            CreateStatusArea(pointCloudPanel);
            
            parent.Add(pointCloudPanel);
        }
        
        void CreateFileSelectionArea(VisualElement parent)
        {
            var fileSection = new VisualElement();
            fileSection.style.marginBottom = 12;
            
            var fileLabel = new Label("文件选择");
            fileLabel.style.fontSize = 14;
            fileLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fileLabel.style.marginBottom = 8;
            ApplyFont(fileLabel);
            fileSection.Add(fileLabel);
            
            // 文件路径显示
            filePathDisplay = new Label("未选择文件");
            filePathDisplay.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            filePathDisplay.style.paddingTop = 8;
            filePathDisplay.style.paddingBottom = 8;
            filePathDisplay.style.paddingLeft = 8;
            filePathDisplay.style.paddingRight = 8;
            filePathDisplay.style.borderTopLeftRadius = 4;
            filePathDisplay.style.borderTopRightRadius = 4;
            filePathDisplay.style.borderBottomLeftRadius = 4;
            filePathDisplay.style.borderBottomRightRadius = 4;
            filePathDisplay.style.marginBottom = 8;
            filePathDisplay.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            filePathDisplay.style.fontSize = 12;
            ApplyFont(filePathDisplay);
            fileSection.Add(filePathDisplay);
            
            // 浏览文件按钮
            var browseButton = CreateButton("浏览文件", primaryColor);
            browseButton.RegisterCallback<ClickEvent>(evt => BrowseFile());
            fileSection.Add(browseButton);
            
            parent.Add(fileSection);
        }
        
        void CreateButtonArea(VisualElement parent)
        {
            var buttonSection = new VisualElement();
            buttonSection.style.marginBottom = 12;
            
            // 预览点云按钮
            var loadButton = CreateButton("预览点云", accentColor);
            loadButton.RegisterCallback<ClickEvent>(evt => LoadPointCloud());
            loadButton.style.marginBottom = 8;
            buttonSection.Add(loadButton);
            
            // 提取电力线按钮
            var extractButton = CreateButton("提取电力线", new Color(0.8f, 0.4f, 0.1f, 1f));
            extractButton.RegisterCallback<ClickEvent>(evt => StartPowerLineExtraction());
            extractButton.style.marginBottom = 8;
            buttonSection.Add(extractButton);
            
            // 清除选择按钮
            var clearButton = CreateButton("清除选择", dangerColor);
            clearButton.RegisterCallback<ClickEvent>(evt => ClearSelection());
            buttonSection.Add(clearButton);
            
            parent.Add(buttonSection);
        }
        
        void CreatePythonInfoArea(VisualElement parent)
        {
            var infoSection = new VisualElement();
            infoSection.style.marginBottom = 12;
            
            var infoTitle = new Label("Python环境配置");
            infoTitle.style.fontSize = 12;
            infoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoTitle.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            infoTitle.style.marginBottom = 6;
            ApplyFont(infoTitle);
            infoSection.Add(infoTitle);
            
            var infoText = new Label("电力线提取需要Python环境:\n• Python 3.11\n• pip install laspy numpy open3d scikit-learn scipy tqdm");
            infoText.style.fontSize = 10;
            infoText.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            infoText.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
            infoText.style.paddingTop = 6;
            infoText.style.paddingBottom = 6;
            infoText.style.paddingLeft = 8;
            infoText.style.paddingRight = 8;
            infoText.style.borderTopLeftRadius = 4;
            infoText.style.borderTopRightRadius = 4;
            infoText.style.borderBottomLeftRadius = 4;
            infoText.style.borderBottomRightRadius = 4;
            infoText.style.whiteSpace = WhiteSpace.Normal;
            ApplyFont(infoText);
            infoSection.Add(infoText);
            
            parent.Add(infoSection);
        }
        
        void CreateStatusArea(VisualElement parent)
        {
            var statusSection = new VisualElement();
            
            var statusTitle = new Label("状态");
            statusTitle.style.fontSize = 14;
            statusTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusTitle.style.marginBottom = 8;
            ApplyFont(statusTitle);
            statusSection.Add(statusTitle);
            
            statusLabel = new Label("准备就绪 - 请选择点云文件进行预览");
            statusLabel.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            statusLabel.style.paddingTop = 8;
            statusLabel.style.paddingBottom = 8;
            statusLabel.style.paddingLeft = 8;
            statusLabel.style.paddingRight = 8;
            statusLabel.style.borderTopLeftRadius = 4;
            statusLabel.style.borderTopRightRadius = 4;
            statusLabel.style.borderBottomLeftRadius = 4;
            statusLabel.style.borderBottomRightRadius = 4;
            statusLabel.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            statusLabel.style.fontSize = 12;
            ApplyFont(statusLabel);
            statusSection.Add(statusLabel);
            
            parent.Add(statusSection);
        }
        
        VisualElement CreateButton(string text, Color bgColor)
        {
            var button = new VisualElement();
            button.style.height = 35;
            button.style.backgroundColor = bgColor;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            
            var label = new Label(text);
            label.style.color = Color.white;
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            ApplyFont(label);
            button.Add(label);
            
            // 添加悬停效果
            button.RegisterCallback<MouseEnterEvent>(evt => {
                button.style.backgroundColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, bgColor.a);
            });
            button.RegisterCallback<MouseLeaveEvent>(evt => {
                button.style.backgroundColor = bgColor;
            });
            
            return button;
        }
        
        void BrowseFile()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("选择点云文件", "", "las,off");
            if (!string.IsNullOrEmpty(path))
            {
                SelectFile(path);
            }
#else
            // 运行时文件选择 - 使用Windows API，简化过滤器格式
            string path = RuntimeFileSelector.OpenFileDialog("选择点云文件", "LAS文件|*.las|OFF文件|*.off|所有文件|*.*");
            if (!string.IsNullOrEmpty(path))
            {
                SelectFile(path);
            }
#endif
        }
        
        void SelectFile(string filePath)
        {
            selectedFilePath = filePath;
            selectedFile = System.IO.Path.GetFileName(filePath);
            
            if (filePathDisplay != null)
            {
                filePathDisplay.text = $"已选择: {selectedFile}";
            }
            
            if (statusLabel != null)
            {
                statusLabel.text = $"已选择文件: {selectedFile}";
            }
            
            Debug.Log($"选择了文件: {filePath}");
        }
        
                void LoadPointCloud()
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "请先选择点云文件进行预览";
                }
                return;
            }

            // 立即更新状态为处理中
            if (statusLabel != null)
            {
                statusLabel.text = "点云处理中...";
            }

            // 使用与初始界面相同的预览方式
            StartCoroutine(PreviewPointCloudCoroutine());
        }
        
        /// <summary>
        /// 预览点云功能（与初始界面相同的方式）
        /// </summary>
        private System.Collections.IEnumerator PreviewPointCloudCoroutine()
        {
            if (statusLabel != null)
            {
                statusLabel.text = "正在转换LAS文件为OFF格式...";
            }

            bool conversionSuccess = false;
            string errorMessage = "";
            
            try
            {
                // 获取文件名（不包含扩展名）
                string fileName = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);
                
                // 检查对应的OFF文件是否已存在
                string offFilePath = $"Resources/pointcloud/{fileName}.off";
                string fullOffPath = System.IO.Path.Combine(Application.dataPath, offFilePath);
                
                if (System.IO.File.Exists(fullOffPath))
                {
                    Debug.Log($"OFF文件已存在: {fullOffPath}");
                    if (statusLabel != null)
                    {
                        statusLabel.text = "OFF文件已存在，直接预览...";
                    }
                }
                else
                {
                    // 检查LAS到OFF转换器依赖
                    if (statusLabel != null)
                    {
                        statusLabel.text = "检查转换依赖...";
                    }
                    
                    if (!PowerlineSystem.LasToOffConverter.CheckDependencies())
                    {
                        errorMessage = "Python或laspy库未安装";
                        if (statusLabel != null)
                        {
                            statusLabel.text = $"错误：{errorMessage}";
                        }
                        Debug.LogError("LAS到OFF转换依赖检查失败");
                        yield break;
                    }
                    
                    if (statusLabel != null)
                    {
                        statusLabel.text = "正在转换LAS文件...";
                    }
                    
                    // 调用LAS到OFF转换器
                    string convertedPath = PowerlineSystem.LasToOffConverter.ConvertLasToOff(selectedFilePath);
                    
                    if (string.IsNullOrEmpty(convertedPath))
                    {
                        errorMessage = "LAS文件转换失败";
                        if (statusLabel != null)
                        {
                            statusLabel.text = $"错误：{errorMessage}";
                        }
                        Debug.LogError("LAS文件转换失败");
                        yield break;
                    }
                    
                    if (statusLabel != null)
                    {
                        statusLabel.text = "LAS文件转换完成";
                    }
                    Debug.Log($"LAS文件转换成功: {convertedPath}");
                }
                
                // 验证OFF文件是否存在
                if (!System.IO.File.Exists(fullOffPath))
                {
                    errorMessage = $"OFF文件生成失败 {fileName}.off";
                    if (statusLabel != null)
                    {
                        statusLabel.text = $"错误：{errorMessage}";
                    }
                    Debug.LogError($"OFF文件不存在: {fullOffPath}");
                    yield break;
                }
                
                if (statusLabel != null)
                {
                    statusLabel.text = "正在启动点云预览...";
                }
                
                // 查找或创建点云查看器
                var pointCloudViewer = FindObjectOfType<UI.PointCloudViewer>();
                if (pointCloudViewer == null)
                {
                    GameObject viewerObj = new GameObject("PointCloudViewer");
                    pointCloudViewer = viewerObj.AddComponent<UI.PointCloudViewer>();
                }
                
                // 显示点云查看器
                pointCloudViewer.ShowPointCloudViewer(fileName);
                
                if (statusLabel != null)
                {
                    statusLabel.text = $"点云预览已打开: {fileName}";
                }
                Debug.Log($"点云预览已启动，文件: {fileName}");
                
                conversionSuccess = true;
                
            }
            catch (System.Exception ex)
            {
                errorMessage = ex.Message;
                if (statusLabel != null)
                {
                    statusLabel.text = $"预览失败: {errorMessage}";
                }
                Debug.LogError($"点云预览异常: {errorMessage}");
            }
            
            if (!conversionSuccess && !string.IsNullOrEmpty(errorMessage))
            {
                if (statusLabel != null)
                {
                    statusLabel.text = $"预览失败: {errorMessage}";
                }
            }
        }
        
        private IEnumerator LoadLasFileCoroutine()
        {
            if (statusLabel != null)
            {
                statusLabel.text = "正在转换LAS文件...";
            }

            // 使用 LasToOffConverter 进行转换
            string offFilePath = LasToOffConverter.ConvertLasToOff(selectedFilePath);

            if (!string.IsNullOrEmpty(offFilePath))
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "LAS转换完成，正在加载点云...";
                }

                // 转换成功，设置点云管理器的数据路径
                string fileName = System.IO.Path.GetFileNameWithoutExtension(offFilePath);
                string resourcePath = $"pointcloud/{fileName}";
                
                // 确保订阅了事件
                pointCloudManager.OnLoadingComplete -= OnPointCloudLoadingCompleted;
                pointCloudManager.OnLoadingComplete += OnPointCloudLoadingCompleted;
                pointCloudManager.OnLoadingError -= OnPointCloudLoadingError;
                pointCloudManager.OnLoadingError += OnPointCloudLoadingError;
                
                pointCloudManager.dataPath = resourcePath;
                pointCloudManager.LoadPointCloudAsync();
            }
            else
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "LAS文件转换失败，请检查Python环境";
                }
                Debug.LogError("LAS文件转换失败");
            }

            yield return null;
        }

        private string ProcessLocalFilePath(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
                return localPath;

            // 检查文件扩展名
            string extension = System.IO.Path.GetExtension(localPath).ToLower();
            
            if (extension == ".las")
            {
                // 如果是LAS文件，需要先转换为OFF文件
                // 生成对应的OFF文件路径（去掉扩展名，因为PowerlinePointCloudManager会自动添加.off）
                string fileName = System.IO.Path.GetFileNameWithoutExtension(localPath);
                string resourcePath = $"pointcloud/{fileName}";
                
                Debug.Log($"处理LAS文件：{localPath} -> Resources路径：{resourcePath}");
                return resourcePath;
            }
            else if (extension == ".off")
            {
                // 如果是OFF文件，提取Resources相对路径
                string fileName = System.IO.Path.GetFileNameWithoutExtension(localPath);
                string resourcePath = $"pointcloud/{fileName}";
                
                Debug.Log($"处理OFF文件：{localPath} -> Resources路径：{resourcePath}");
                return resourcePath;
            }
            else
            {
                // 其他情况直接返回原路径
                Debug.Log($"未知文件格式：{extension}，直接返回原路径");
                return localPath;
            }
        }
        
        void ClearSelection()
        {
            selectedFilePath = "";
            selectedFile = "";
            
            if (filePathDisplay != null)
            {
                filePathDisplay.text = "未选择文件";
            }
            
            if (statusLabel != null)
            {
                statusLabel.text = "请选择点云文件进行预览";
            }
        }
        
        private void StartPowerLineExtraction()
        {
            if (powerLineExtractorManager == null)
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "错误：电力线提取管理器未初始化";
                }
                Debug.LogError("PowerLineExtractorManager未初始化");
                return;
            }
            
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "错误：请先选择LAS文件";
                }
                Debug.LogError("未选择LAS文件");
                return;
            }
            
            // 检查文件是否为LAS格式
            string extension = System.IO.Path.GetExtension(selectedFilePath).ToLower();
            if (extension != ".las")
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "错误：只支持.las格式文件的电力线提取";
                }
                Debug.LogError("电力线提取只支持LAS格式文件");
                return;
            }
            
            if (powerLineExtractorManager.IsProcessing())
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "电力线提取正在进行中，请等待完成";
                }
                return;
            }
            
            // 设置LAS文件并开始提取
            powerLineExtractorManager.SelectLasFile(selectedFilePath);
            powerLineExtractorManager.StartPowerLineExtraction();
            
            if (statusLabel != null)
            {
                statusLabel.text = "正在启动电力线提取...";
            }
        }
        
        #region 电力线提取回调方法
        
        private void OnExtractionStatusChanged(string status)
        {
            if (statusLabel != null)
            {
                statusLabel.text = $"电力线提取: {status}";
            }
            Debug.Log($"电力线提取状态: {status}");
        }
        
        private void OnExtractionCompleted(string csvPath)
        {
            if (statusLabel != null)
            {
                statusLabel.text = "电力线提取完成！";
            }
            
            Debug.Log($"电力线提取完成，CSV输出路径：{csvPath}");
            
            // 注意：场景构建由InitialInterfaceManager处理，这里不再重复构建
            // 避免产生两组电塔的问题
        }
        
        private void OnExtractionError(string error)
        {
            if (statusLabel != null)
            {
                statusLabel.text = $"电力线提取失败: {error}";
            }
            Debug.LogError($"电力线提取错误: {error}");
        }
        
        #endregion
        

        
        #region 点云加载回调方法
        
        /// <summary>
        /// 点云加载完成回调
        /// </summary>
        private void OnPointCloudLoadingCompleted()
        {
            // 检查是否应该显示点云查看器
            bool shouldShowViewer = true;
            
            if (sceneBuilder != null)
            {
                shouldShowViewer = sceneBuilder.ShouldShowPointCloudViewer();
            }
            
            if (shouldShowViewer)
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "点云加载完成！正在打开查看器...";
                }
                
                Debug.Log("点云加载完成，自动弹出查看器");
                
                // 自动弹出点云查看器
                ShowPointCloudViewer();
            }
            else
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "点云加载完成！(电力线提取模式)";
                }
                
                Debug.Log("点云加载完成，电力线提取模式已禁用查看器弹窗");
            }
        }
        
        /// <summary>
        /// 点云加载错误回调
        /// </summary>
        private void OnPointCloudLoadingError(string error)
        {
            if (statusLabel != null)
            {
                statusLabel.text = $"点云加载失败: {error}";
            }
            Debug.LogError($"点云加载失败: {error}");
        }
        
        /// <summary>
        /// 显示点云查看器
        /// </summary>
        private void ShowPointCloudViewer()
        {
            // 查找或创建点云查看器
            PointCloudViewer viewer = FindObjectOfType<PointCloudViewer>();
            if (viewer == null)
            {
                GameObject viewerObj = new GameObject("PointCloudViewer");
                viewer = viewerObj.AddComponent<PointCloudViewer>();
            }
            
            // 获取当前加载的点云文件名
            string pointCloudFileName = "";
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                pointCloudFileName = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);
            }
            
            // 显示查看器
            viewer.ShowPointCloudViewer(pointCloudFileName);
            
            if (statusLabel != null)
            {
                statusLabel.text = "点云查看器已打开";
            }
            
            Debug.Log($"点云查看器已显示，文件: {pointCloudFileName}");
        }
        
        #endregion
        
        private void StartTowerReconstruction(string csvPath)
        {
            if (statusLabel != null)
            {
                statusLabel.text = "正在加载塔位置数据进行三维重建...";
            }
            
            try
            {
                // 读取CSV文件
                if (!System.IO.File.Exists(csvPath))
                {
                    Debug.LogError($"CSV文件不存在: {csvPath}");
                    if (statusLabel != null)
                    {
                        statusLabel.text = "错误：CSV文件不存在";
                    }
                    return;
                }
                
                string[] lines = System.IO.File.ReadAllLines(csvPath);
                int validLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Count();
                Debug.Log($"读取到 {validLines} 个有效塔位置");
                
                // 查找或创建TowerPinpointSystem
                TowerPinpointSystem towerSystem = FindObjectOfType<TowerPinpointSystem>();
                if (towerSystem == null)
                {
                    // 如果没有找到，创建一个
                    GameObject towerSystemObj = new GameObject("TowerPinpointSystem");
                    towerSystem = towerSystemObj.AddComponent<TowerPinpointSystem>();
                    Debug.Log("创建了新的TowerPinpointSystem");
                }
                
                if (statusLabel != null)
                {
                    statusLabel.text = "正在创建电力塔...";
                }
                
                // 调用TowerPinpointSystem的LoadTowersFromCsv方法
                Debug.Log("开始调用TowerPinpointSystem.LoadTowersFromCsv...");
                towerSystem.LoadTowersFromCsv(csvPath);
                
                if (statusLabel != null)
                {
                    statusLabel.text = $"三维重建完成！生成了 {validLines} 个电力塔";
                }
                
                Debug.Log($"电力塔三维重建完成，CSV文件: {csvPath}");
                
                // 延迟加载点云，确保塔已经创建完成
                StartCoroutine(DelayedPointCloudLoad());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"塔三维重建失败: {ex.Message}");
                if (statusLabel != null)
                {
                    statusLabel.text = $"三维重建失败: {ex.Message}";
                }
            }
        }
        
        private IEnumerator DelayedPointCloudLoad()
        {
            yield return new WaitForSeconds(1f);
            
            // 如果还有原始点云文件，可以重新加载显示
            if (!string.IsNullOrEmpty(selectedFilePath) && pointCloudManager != null)
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "正在重新加载点云数据...";
                }
                
                // 重新处理点云文件路径
                string processedPath = ProcessLocalFilePath(selectedFilePath);
                if (!string.IsNullOrEmpty(processedPath))
                {
                    // 确保订阅了事件
                    pointCloudManager.OnLoadingComplete -= OnPointCloudLoadingCompleted;
                    pointCloudManager.OnLoadingComplete += OnPointCloudLoadingCompleted;
                    pointCloudManager.OnLoadingError -= OnPointCloudLoadingError;
                    pointCloudManager.OnLoadingError += OnPointCloudLoadingError;
                    
                    pointCloudManager.dataPath = processedPath;
                    pointCloudManager.LoadPointCloudAsync();
                }
            }
            
            if (statusLabel != null)
            {
                statusLabel.text = "电力线提取和三维重建流程完成！";
            }
        }
        
        /// <summary>
        /// 显示点云面板
        /// </summary>
        public void ShowPointCloudPanel()
        {
            if (pointCloudPanel != null)
            {
                pointCloudPanel.style.display = DisplayStyle.Flex;
                isPanelVisible = true;
            }
        }
        
        /// <summary>
        /// 隐藏点云面板
        /// </summary>
        public void HidePointCloudPanel()
        {
            if (pointCloudPanel != null)
            {
                pointCloudPanel.style.display = DisplayStyle.None;
                isPanelVisible = false;
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
                    if (uiFont != null)
                    {
                        element.style.unityFont = uiFont;
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
                        label.style.color = Color.black;
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
        
        void OnDestroy()
        {
            if (powerLineExtractorManager != null)
            {
                powerLineExtractorManager.OnStatusChanged -= OnExtractionStatusChanged;
                powerLineExtractorManager.OnExtractionCompleted -= OnExtractionCompleted;
                powerLineExtractorManager.OnError -= OnExtractionError;
            }
        }
    }
} 