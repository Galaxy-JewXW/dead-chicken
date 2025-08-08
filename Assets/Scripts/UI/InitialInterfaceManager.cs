using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PowerlineSystem;
using UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 初始界面管理器 - 系统启动时的主界面
/// 提供用户选择使用现有电塔数据或上传LAS文件进行电力线提取
/// </summary>
public class InitialInterfaceManager : MonoBehaviour
{
    [Header("界面配置")]
    public Font uiFont;
    public Color primaryColor = new Color(0.2f, 0.3f, 0.8f, 1f); // 更深的蓝色
    public Color accentColor = new Color(0.12f, 0.85f, 0.38f, 1f);
    public Color backgroundColor = new Color(0.95f, 0.97f, 1f, 1f);
    public Color secondaryColor = new Color(0.6f, 0.7f, 0.9f, 1f); // 次要蓝色
    public Color gradientStart = new Color(0.9f, 0.95f, 1f, 1f); // 渐变开始色
    public Color gradientEnd = new Color(0.8f, 0.9f, 1f, 1f); // 渐变结束色
    
    [Header("组件引用")]
    public SceneInitializer sceneInitializer;
    public PowerLineExtractorManager powerLineExtractorManager;
    public SimpleUIToolkitManager uiManager;
    
    [Header("UI组件")]
    private UIDocument uiDocument;
    private VisualElement rootElement;
    
    // UI元素
    private VisualElement initialPanel;
    private VisualElement fileUploadArea;
    private VisualElement pythonGuideArea;
    private Label statusLabel;
    private ProgressBar progressBar;
    private VisualElement uploadLasButton;
    private Button startExtractionButton;
    
    // 状态
    private bool isInitialized = false;
    private string selectedLasFile = "";
    private bool isProcessing = false;
    
    void Start()
    {
        InitializeManager();
    }
    
    void InitializeManager()
    {
        // 获取组件引用
        if (sceneInitializer == null)
            sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (powerLineExtractorManager == null)
            powerLineExtractorManager = FindObjectOfType<PowerLineExtractorManager>();
        if (uiManager == null)
            uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            
        // 创建独立的UIDocument
        CreateUIDocument();
            
        // 注册事件
        if (powerLineExtractorManager != null)
        {
            powerLineExtractorManager.OnStatusChanged += OnExtractionStatusChanged;
            powerLineExtractorManager.OnExtractionCompleted += OnExtractionCompleted;
            powerLineExtractorManager.OnError += OnExtractionError;
        }
        
        isInitialized = true;
        Debug.Log("初始界面管理器初始化完成");
    }
    
    /// <summary>
    /// 创建独立的UIDocument
    /// </summary>
    void CreateUIDocument()
    {
        // 创建UIDocument组件
        uiDocument = gameObject.AddComponent<UIDocument>();
        
        // 创建PanelSettings
        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.name = "InitialInterfacePanelSettings";
        
        // 设置渲染顺序，确保初始界面在最前面
        panelSettings.sortingOrder = 100;
        
        // 设置目标纹理
        panelSettings.targetTexture = null; // 使用屏幕空间
        
        // 应用PanelSettings
        uiDocument.panelSettings = panelSettings;
        
        // 获取根元素并设置为全屏
        rootElement = uiDocument.rootVisualElement;
        rootElement.style.width = Length.Percent(100);
        rootElement.style.height = Length.Percent(100);
        rootElement.style.position = Position.Absolute;
        rootElement.style.top = 0;
        rootElement.style.left = 0;
        rootElement.style.right = 0;
        rootElement.style.bottom = 0;
        
        Debug.Log("初始界面UIDocument已创建");
    }
    
    /// <summary>
    /// 显示初始界面
    /// </summary>
    public void ShowInitialInterface()
    {
        if (!isInitialized)
            InitializeManager();
        
        // 确保UIDocument被启用
        if (uiDocument != null)
        {
            uiDocument.enabled = true;
        }
            
        CreateInitialInterface();
        Debug.Log("初始界面已显示");
    }
    
    /// <summary>
    /// 隐藏初始界面
    /// </summary>
    public void HideInitialInterface()
    {
        if (uiDocument != null)
        {
            uiDocument.enabled = false;
        }
        Debug.Log("初始界面已隐藏");
    }
    
    void CreateInitialInterface()
    {
        // 创建主面板 - 全屏显示
        initialPanel = new VisualElement();
        initialPanel.style.position = Position.Absolute;
        initialPanel.style.top = 0;
        initialPanel.style.left = 0;
        initialPanel.style.right = 0;
        initialPanel.style.bottom = 0;
        initialPanel.style.width = Length.Percent(100);
        initialPanel.style.height = Length.Percent(100);
        initialPanel.style.backgroundColor = backgroundColor;
        initialPanel.style.flexDirection = FlexDirection.Column;
        initialPanel.style.justifyContent = Justify.Center;
        initialPanel.style.alignItems = Align.Center;
        initialPanel.style.paddingTop = 50;
        initialPanel.style.paddingBottom = 50;
        initialPanel.style.paddingLeft = 50;
        initialPanel.style.paddingRight = 50;
        
        // 添加背景装饰
        CreateBackgroundDecoration(initialPanel);
        
        // 创建标题
        CreateTitle(initialPanel);
        
        // 创建选择区域
        CreateSelectionArea(initialPanel);
        
        // 创建文件上传区域
        CreateFileUploadArea(initialPanel);
        
        // 创建Python引导区域
        CreatePythonGuideArea(initialPanel);
        
        // 创建状态显示区域
        CreateStatusArea(initialPanel);
        
        // 创建底部信息
        CreateFooterInfo(initialPanel);
        
        // 添加到独立的根元素
        if (rootElement != null)
        {
            rootElement.Add(initialPanel);
            Debug.Log($"初始界面已创建，根元素子元素数量: {rootElement.childCount}");
        }
        else
        {
            Debug.LogError("根元素为空，无法添加初始界面");
        }
    }
    
    void CreateTitle(VisualElement parent)
    {
        var titleContainer = new VisualElement();
        titleContainer.style.marginBottom = 40;
        titleContainer.style.alignItems = Align.Center;
        
        // 比赛信息
        var competitionInfo = new Label("Software Cup 2025");
        competitionInfo.style.color = accentColor;
        competitionInfo.style.marginBottom = 12;
        competitionInfo.style.unityTextAlign = TextAnchor.MiddleCenter;
        competitionInfo.style.paddingTop = 5;
        competitionInfo.style.paddingBottom = 5;
        competitionInfo.style.paddingLeft = 15;
        competitionInfo.style.paddingRight = 15;
        competitionInfo.style.backgroundColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.1f);
        competitionInfo.style.borderTopLeftRadius = 8;
        competitionInfo.style.borderTopRightRadius = 8;
        competitionInfo.style.borderBottomLeftRadius = 8;
        competitionInfo.style.borderBottomRightRadius = 8;
        ApplyFont(competitionInfo, FontSize.Subtitle);
        titleContainer.Add(competitionInfo);
        
        // 队伍信息
        var teamInfo = new Label("Team: Dead Chicken");
        teamInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        teamInfo.style.marginBottom = 25;
        teamInfo.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(teamInfo, FontSize.Body);
        titleContainer.Add(teamInfo);
        
        // 主标题
        var mainTitle = new Label("基于机载LiDAR点云的电力线提取与三维重建系统");
        mainTitle.style.color = primaryColor;
        mainTitle.style.marginBottom = 15;
        mainTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        mainTitle.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(mainTitle, FontSize.LargeTitle);
        titleContainer.Add(mainTitle);
        
        // 英文副标题
        var subtitle = new Label("Powerline Extraction and 3D Reconstruction System\nBased on Airborne LiDAR Point Cloud");
        subtitle.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        subtitle.style.marginBottom = 25;
        subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        subtitle.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(subtitle, FontSize.Subtitle);
        titleContainer.Add(subtitle);
        
        // 欢迎信息
        var welcomeText = new Label("欢迎使用电力线可视化系统！");
        welcomeText.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        welcomeText.style.unityTextAlign = TextAnchor.MiddleCenter;
        welcomeText.style.marginBottom = 10;
        ApplyFont(welcomeText, FontSize.Title);
        titleContainer.Add(welcomeText);
        
        // 提示信息
        var hintText = new Label("请选择您要使用的方式：");
        hintText.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        hintText.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(hintText, FontSize.Body);
        titleContainer.Add(hintText);
        
        parent.Add(titleContainer);
    }
    
    void CreateSelectionArea(VisualElement parent)
    {
        var selectionContainer = new VisualElement();
        selectionContainer.style.flexDirection = FlexDirection.Row;
        selectionContainer.style.justifyContent = Justify.Center;
        selectionContainer.style.alignItems = Align.Center;
        selectionContainer.style.marginBottom = 40;
        selectionContainer.style.width = Length.Percent(90);
        
        // 数据集A按钮
        var dataSetAButton = CreateOptionButton(
            "标准数据集A",
            "使用预设的标准电塔数据集\n包含基础电力线配置\n适合演示和测试",
            "",
            () => OnUseExistingDataClicked("A")
        );
        selectionContainer.Add(dataSetAButton);
        
        // 分隔符1
        var separator1 = new VisualElement();
        separator1.style.width = 25;
        separator1.style.height = 200;
        selectionContainer.Add(separator1);
        
        // 数据集B按钮
        var dataSetBButton = CreateOptionButton(
            "复杂数据集B",
            "使用预设的复杂电塔数据集\n包含多层级电力线配置\n适合高级功能展示",
            "",
            () => OnUseExistingDataClicked("B")
        );
        selectionContainer.Add(dataSetBButton);
        
        // 分隔符2
        var separator2 = new VisualElement();
        separator2.style.width = 25;
        separator2.style.height = 200;
        selectionContainer.Add(separator2);
        
        // 上传LAS文件按钮
        uploadLasButton = CreateOptionButton(
            "LiDAR点云提取",
            "上传机载LiDAR点云文件\n进行电力线自动提取\n并生成三维重建场景",
            "",
            () => OnUploadLasClicked()
        );
        selectionContainer.Add(uploadLasButton);
        

        
        parent.Add(selectionContainer);
    }
    
    VisualElement CreateOptionButton(string title, string description, string icon, System.Action onClick)
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.width = 320;
        buttonContainer.style.height = 240; // 增加高度
        buttonContainer.style.backgroundColor = Color.white;
        buttonContainer.style.borderTopLeftRadius = 16;
        buttonContainer.style.borderTopRightRadius = 16;
        buttonContainer.style.borderBottomLeftRadius = 16;
        buttonContainer.style.borderBottomRightRadius = 16;
        buttonContainer.style.borderLeftWidth = 2;
        buttonContainer.style.borderRightWidth = 2;
        buttonContainer.style.borderTopWidth = 2;
        buttonContainer.style.borderBottomWidth = 2;
        buttonContainer.style.borderLeftColor = primaryColor;
        buttonContainer.style.borderRightColor = primaryColor;
        buttonContainer.style.borderTopColor = primaryColor;
        buttonContainer.style.borderBottomColor = primaryColor;
        buttonContainer.style.paddingTop = 30; // 增加内边距
        buttonContainer.style.paddingBottom = 30;
        buttonContainer.style.paddingLeft = 25;
        buttonContainer.style.paddingRight = 25;
        buttonContainer.style.marginLeft = 15;
        buttonContainer.style.marginRight = 15;
        buttonContainer.style.alignItems = Align.Center;
        buttonContainer.style.justifyContent = Justify.Center;
        buttonContainer.style.flexDirection = FlexDirection.Column; // 确保垂直布局
        
        // 移除不支持的boxShadow属性，使用其他方式实现阴影效果
        
        // 图标（只在有图标时显示）
        if (!string.IsNullOrEmpty(icon))
        {
            var iconLabel = new Label(icon);
            iconLabel.style.fontSize = 48;
            iconLabel.style.marginBottom = 15;
            iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            iconLabel.style.minHeight = 50; // 设置最小高度
            ApplyFont(iconLabel);
            buttonContainer.Add(iconLabel);
        }
        
        // 标题
        var titleLabel = new Label(title);
        titleLabel.style.color = primaryColor;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.minHeight = 25; // 设置最小高度
        ApplyFont(titleLabel, FontSize.Subtitle);
        buttonContainer.Add(titleLabel);
        
        // 描述
        var descLabel = new Label(description);
        descLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        descLabel.style.whiteSpace = WhiteSpace.Normal;
        descLabel.style.minHeight = 40; // 设置最小高度，确保多行文字显示
        descLabel.style.flexGrow = 1; // 允许描述文字占用剩余空间
        ApplyFont(descLabel, FontSize.Small);
        buttonContainer.Add(descLabel);
        
        // 鼠标悬停效果
        buttonContainer.RegisterCallback<MouseEnterEvent>(evt => {
            buttonContainer.style.backgroundColor = new Color(0.98f, 0.98f, 1f, 1f);
            buttonContainer.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
            buttonContainer.style.borderLeftColor = accentColor;
            buttonContainer.style.borderRightColor = accentColor;
            buttonContainer.style.borderTopColor = accentColor;
            buttonContainer.style.borderBottomColor = accentColor;
            buttonContainer.style.borderLeftWidth = 3;
            buttonContainer.style.borderRightWidth = 3;
            buttonContainer.style.borderTopWidth = 3;
            buttonContainer.style.borderBottomWidth = 3;
        });
        
        buttonContainer.RegisterCallback<MouseLeaveEvent>(evt => {
            buttonContainer.style.backgroundColor = Color.white;
            buttonContainer.style.scale = new Scale(new Vector3(1f, 1f, 1f));
            buttonContainer.style.borderLeftColor = primaryColor;
            buttonContainer.style.borderRightColor = primaryColor;
            buttonContainer.style.borderTopColor = primaryColor;
            buttonContainer.style.borderBottomColor = primaryColor;
            buttonContainer.style.borderLeftWidth = 2;
            buttonContainer.style.borderRightWidth = 2;
            buttonContainer.style.borderTopWidth = 2;
            buttonContainer.style.borderBottomWidth = 2;
        });
        
        // 点击事件
        buttonContainer.RegisterCallback<ClickEvent>(evt => {
            onClick?.Invoke();
        });
        
        return buttonContainer;
    }
    
    void CreateFileUploadArea(VisualElement parent)
    {
        fileUploadArea = new VisualElement();
        fileUploadArea.style.display = DisplayStyle.None;
        fileUploadArea.style.width = Length.Percent(60);
        fileUploadArea.style.backgroundColor = Color.white;
        fileUploadArea.style.borderTopLeftRadius = 8;
        fileUploadArea.style.borderTopRightRadius = 8;
        fileUploadArea.style.borderBottomLeftRadius = 8;
        fileUploadArea.style.borderBottomRightRadius = 8;
        fileUploadArea.style.paddingTop = 30; // 增加内边距
        fileUploadArea.style.paddingBottom = 30;
        fileUploadArea.style.paddingLeft = 30;
        fileUploadArea.style.paddingRight = 30;
        fileUploadArea.style.marginBottom = 20;
        fileUploadArea.style.alignItems = Align.Center;
        fileUploadArea.style.minHeight = 300; // 增加最小高度以容纳预览按钮
        
        // 标题
        var uploadTitle = new Label("上传LAS点云文件");
        uploadTitle.style.color = primaryColor;
        uploadTitle.style.fontSize = 20;
        uploadTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        uploadTitle.style.marginBottom = 15;
        ApplyFont(uploadTitle);
        fileUploadArea.Add(uploadTitle);
        
        // 文件选择按钮
        var selectFileButton = new Button(() => SelectLasFile());
        selectFileButton.text = "选择LAS文件";
        selectFileButton.style.backgroundColor = accentColor;
        selectFileButton.style.color = Color.white;
        selectFileButton.style.fontSize = 16;
        selectFileButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        selectFileButton.style.paddingTop = 15; // 增加内边距
        selectFileButton.style.paddingBottom = 15;
        selectFileButton.style.paddingLeft = 24;
        selectFileButton.style.paddingRight = 24;
        selectFileButton.style.borderTopLeftRadius = 6;
        selectFileButton.style.borderTopRightRadius = 6;
        selectFileButton.style.borderBottomLeftRadius = 6;
        selectFileButton.style.borderBottomRightRadius = 6;
        selectFileButton.style.marginBottom = 15;
        selectFileButton.style.minHeight = 50; // 设置最小高度
        ApplyFont(selectFileButton);
        fileUploadArea.Add(selectFileButton);
        
        // 文件路径显示
        var filePathLabel = new Label("未选择文件");
        filePathLabel.name = "file-path";
        filePathLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        filePathLabel.style.fontSize = 14;
        filePathLabel.style.marginBottom = 15;
        ApplyFont(filePathLabel);
        fileUploadArea.Add(filePathLabel);
        
        // 预览点云按钮
        var previewButton = new Button(() => PreviewPointCloud());
        previewButton.text = "预览点云";
        previewButton.name = "preview-button";
        previewButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.9f, 1f); // 蓝色
        previewButton.style.color = Color.white;
        previewButton.style.fontSize = 16;
        previewButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        previewButton.style.paddingTop = 15;
        previewButton.style.paddingBottom = 15;
        previewButton.style.paddingLeft = 24;
        previewButton.style.paddingRight = 24;
        previewButton.style.borderTopLeftRadius = 6;
        previewButton.style.borderTopRightRadius = 6;
        previewButton.style.borderBottomLeftRadius = 6;
        previewButton.style.borderBottomRightRadius = 6;
        previewButton.style.marginBottom = 15;
        previewButton.style.minHeight = 50;
        previewButton.style.display = DisplayStyle.None; // 初始隐藏
        ApplyFont(previewButton);
        fileUploadArea.Add(previewButton);
        
        // 开始提取按钮
        startExtractionButton = new Button(() => StartExtraction());
        startExtractionButton.text = "开始电力线提取";
        startExtractionButton.style.backgroundColor = primaryColor;
        startExtractionButton.style.color = Color.white;
        startExtractionButton.style.fontSize = 16;
        startExtractionButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        startExtractionButton.style.paddingTop = 15; // 增加内边距
        startExtractionButton.style.paddingBottom = 15;
        startExtractionButton.style.paddingLeft = 24;
        startExtractionButton.style.paddingRight = 24;
        startExtractionButton.style.borderTopLeftRadius = 6;
        startExtractionButton.style.borderTopRightRadius = 6;
        startExtractionButton.style.borderBottomLeftRadius = 6;
        startExtractionButton.style.borderBottomRightRadius = 6;
        startExtractionButton.style.display = DisplayStyle.None;
        startExtractionButton.style.minHeight = 50; // 设置最小高度
        ApplyFont(startExtractionButton);
        fileUploadArea.Add(startExtractionButton);
        
        // 返回按钮
        var backButton = new Button(() => ShowMainInterface());
        backButton.text = "返回主界面";
        backButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 灰色
        backButton.style.color = Color.white;
        backButton.style.fontSize = 14;
        backButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        backButton.style.paddingTop = 10;
        backButton.style.paddingBottom = 10;
        backButton.style.paddingLeft = 20;
        backButton.style.paddingRight = 20;
        backButton.style.borderTopLeftRadius = 6;
        backButton.style.borderTopRightRadius = 6;
        backButton.style.borderBottomLeftRadius = 6;
        backButton.style.borderBottomRightRadius = 6;
        backButton.style.marginTop = 20;
        backButton.style.minHeight = 40;
        ApplyFont(backButton);
        fileUploadArea.Add(backButton);
        
        parent.Add(fileUploadArea);
    }
    
    void CreatePythonGuideArea(VisualElement parent)
    {
        pythonGuideArea = new VisualElement();
        pythonGuideArea.style.display = DisplayStyle.None;
        pythonGuideArea.style.width = Length.Percent(80);
        pythonGuideArea.style.alignItems = Align.Center;
        pythonGuideArea.style.flexDirection = FlexDirection.Column;
        
        // 标题
        var titleLabel = new Label("Python环境配置向导");
        titleLabel.style.color = primaryColor;
        titleLabel.style.fontSize = 24;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 40; // 增加底部间距
        titleLabel.style.marginTop = 20; // 增加顶部间距
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(titleLabel, FontSize.LargeTitle);
        pythonGuideArea.Add(titleLabel);
        
        // 说明文本
        var descriptionLabel = new Label("本系统需要Python 3.11环境支持点云处理功能。请确保已安装以下Python库：");
        descriptionLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        descriptionLabel.style.fontSize = 16;
        descriptionLabel.style.marginBottom = 20;
        descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(descriptionLabel);
        pythonGuideArea.Add(descriptionLabel);
        
        // 必需的Python库列表
        var librariesContainer = new VisualElement();
        librariesContainer.style.width = Length.Percent(100);
        librariesContainer.style.marginBottom = 30;
        
        string[] requiredLibraries = {
            "laspy - LAS点云文件读取库",
            "numpy - 数值计算库",
            "open3d - 3D点云处理库",
            "scipy - 科学计算库",
            "scikit-learn - 机器学习库",
            "tqdm - 进度条库",
            "matplotlib - 绘图库（可选）"
        };
        
        foreach (string library in requiredLibraries)
        {
            var libraryItem = new Label($"• {library}");
            libraryItem.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            libraryItem.style.fontSize = 14;
            libraryItem.style.marginBottom = 8;
            libraryItem.style.unityTextAlign = TextAnchor.MiddleLeft;
            ApplyFont(libraryItem);
            librariesContainer.Add(libraryItem);
        }
        
        pythonGuideArea.Add(librariesContainer);
        
        // 安装命令
        var installCommandsContainer = new VisualElement();
        installCommandsContainer.style.width = Length.Percent(100);
        installCommandsContainer.style.marginBottom = 30;
        
        var installTitle = new Label("安装命令：");
        installTitle.style.color = primaryColor;
        installTitle.style.fontSize = 16;
        installTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        installTitle.style.marginBottom = 10;
        installTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
        ApplyFont(installTitle);
        installCommandsContainer.Add(installTitle);
        
        var installCommandContainer = new VisualElement();
        installCommandContainer.style.flexDirection = FlexDirection.Row;
        installCommandContainer.style.alignItems = Align.Center;
        installCommandContainer.style.marginBottom = 10;
        
        var installCommand = new Label("pip install -i https://pypi.tuna.tsinghua.edu.cn/simple laspy numpy open3d scipy scikit-learn tqdm matplotlib");
        installCommand.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        installCommand.style.fontSize = 14;
        installCommand.style.unityFontStyleAndWeight = FontStyle.Bold;
        installCommand.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        installCommand.style.paddingTop = 8;
        installCommand.style.paddingBottom = 8;
        installCommand.style.paddingLeft = 12;
        installCommand.style.paddingRight = 12;
        installCommand.style.borderTopLeftRadius = 4;
        installCommand.style.borderTopRightRadius = 4;
        installCommand.style.borderBottomLeftRadius = 4;
        installCommand.style.borderBottomRightRadius = 4;
        installCommand.style.unityTextAlign = TextAnchor.MiddleLeft;
        installCommand.style.flexGrow = 1;
        ApplyFont(installCommand);
        installCommandContainer.Add(installCommand);
        
        // 复制按钮
        var copyButton = new Button(() => CopyToClipboard("pip install -i https://pypi.tuna.tsinghua.edu.cn/simple laspy numpy open3d scipy scikit-learn tqdm matplotlib"));
        copyButton.text = "复制";
        copyButton.style.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        copyButton.style.color = Color.white;
        copyButton.style.fontSize = 12;
        copyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        copyButton.style.marginLeft = 8;
        copyButton.style.minHeight = 32;
        copyButton.style.minWidth = 60;
        copyButton.style.borderTopLeftRadius = 4;
        copyButton.style.borderTopRightRadius = 4;
        copyButton.style.borderBottomLeftRadius = 4;
        copyButton.style.borderBottomRightRadius = 4;
        copyButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(copyButton, FontSize.Small);
        installCommandContainer.Add(copyButton);
        
        installCommandsContainer.Add(installCommandContainer);
        
        pythonGuideArea.Add(installCommandsContainer);
        
        // 添加额外的间距容器
        var spacingContainer = new VisualElement();
        spacingContainer.style.height = 10; // 减少间距，让按钮更靠近上方内容
        pythonGuideArea.Add(spacingContainer);
        
        // 检查按钮
        var checkButton = new Button(() => CheckPythonEnvironment());
        checkButton.text = "检查Python环境";
        checkButton.style.backgroundColor = accentColor;
        checkButton.style.color = Color.white;
        checkButton.style.fontSize = 16;
        checkButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        checkButton.style.marginBottom = 25; // 增加底部间距
        checkButton.style.marginTop = 30; // 增加顶部间距，让按钮往上移
        checkButton.style.minHeight = 45;
        checkButton.style.minWidth = 200;
        checkButton.style.unityTextAlign = TextAnchor.MiddleCenter; // 文字居中
        ApplyFont(checkButton);
        pythonGuideArea.Add(checkButton);
        
        // 返回按钮
        var backButton = new Button(() => ShowMainInterface());
        backButton.text = "返回主界面";
        backButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        backButton.style.color = Color.white;
        backButton.style.fontSize = 16;
        backButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        backButton.style.marginTop = 15; // 增加顶部间距
        backButton.style.minHeight = 45;
        backButton.style.minWidth = 200;
        backButton.style.unityTextAlign = TextAnchor.MiddleCenter; // 文字居中
        ApplyFont(backButton);
        pythonGuideArea.Add(backButton);
        
        parent.Add(pythonGuideArea);
    }
    
    void CreateStatusArea(VisualElement parent)
    {
        var statusContainer = new VisualElement();
        statusContainer.style.width = Length.Percent(60);
        statusContainer.style.alignItems = Align.Center;
        
        // 状态指示器容器
        var statusIndicatorContainer = new VisualElement();
        statusIndicatorContainer.style.flexDirection = FlexDirection.Row;
        statusIndicatorContainer.style.alignItems = Align.Center;
        statusIndicatorContainer.style.marginBottom = 15;
        
        // 状态指示点
        var statusDot = new VisualElement();
        statusDot.style.width = 12;
        statusDot.style.height = 12;
        statusDot.style.borderTopLeftRadius = 6;
        statusDot.style.borderTopRightRadius = 6;
        statusDot.style.borderBottomLeftRadius = 6;
        statusDot.style.borderBottomRightRadius = 6;
        statusDot.style.backgroundColor = accentColor;
        statusDot.style.marginRight = 10;
        statusIndicatorContainer.Add(statusDot);
        
        // 状态标签
        statusLabel = new Label("系统就绪");
        statusLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        statusLabel.style.fontSize = 16;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(statusLabel);
        statusIndicatorContainer.Add(statusLabel);
        
        statusContainer.Add(statusIndicatorContainer);
        
                // 进度条
        progressBar = new ProgressBar();
        progressBar.style.width = Length.Percent(100);
        progressBar.style.height = 8;
        progressBar.style.display = DisplayStyle.None;
        statusContainer.Add(progressBar);
        
        // Python环境检查链接
        var pythonCheckLink = new Button(() => OnPythonGuideClicked());
        pythonCheckLink.text = "检查Python环境";
        pythonCheckLink.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        pythonCheckLink.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        pythonCheckLink.style.fontSize = 11;
        pythonCheckLink.style.marginTop = 10;
        pythonCheckLink.style.minHeight = 25;
        pythonCheckLink.style.minWidth = 100;
        pythonCheckLink.style.borderTopLeftRadius = 3;
        pythonCheckLink.style.borderTopRightRadius = 3;
        pythonCheckLink.style.borderBottomLeftRadius = 3;
        pythonCheckLink.style.borderBottomRightRadius = 3;
        pythonCheckLink.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(pythonCheckLink, FontSize.Small);
        statusContainer.Add(pythonCheckLink);
        
        parent.Add(statusContainer);
    }
    
    void OnUseExistingDataClicked(string dataSetType = "A")
    {
        Debug.Log($"用户选择使用现有电塔数据 - 数据集{dataSetType}");
        
        // 隐藏Python引导区域（如果正在显示）
        if (pythonGuideArea != null)
        {
            pythonGuideArea.style.display = DisplayStyle.None;
        }
        
        // 隐藏文件上传区域（如果正在显示）
        if (fileUploadArea != null)
        {
            fileUploadArea.style.display = DisplayStyle.None;
        }
        
        UpdateStatus($"正在加载数据集{dataSetType}...");
        
        // 初始化场景（使用指定的数据集）
        if (sceneInitializer != null)
        {
            // 使用指定的数据集
            StartCoroutine(InitializeWithSpecificData(dataSetType));
        }
        else
        {
            Debug.LogError("SceneInitializer未找到，无法加载现有电塔数据");
            UpdateStatus("错误：SceneInitializer未找到");
        }
    }
    
    /// <summary>
    /// 使用指定的数据集初始化场景
    /// </summary>
    System.Collections.IEnumerator InitializeWithSpecificData(string dataSetType)
    {
        UpdateStatus($"正在加载数据集{dataSetType}...");
        
        // 根据数据集类型确定CSV文件名
        string csvFileName = $"{dataSetType}";
        SceneInitializer.CSVFormat csvFormat = SceneInitializer.CSVFormat.SimpleTowers;
        
        // 检查文件是否存在 - 修复打包后的路径问题
        bool fileExists = false;
        string resourcesPath = "";
        
        // 方法1：检查Resources目录（编辑器模式）
        resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources", csvFileName + ".csv");
        fileExists = System.IO.File.Exists(resourcesPath);
        
        // 方法2：如果方法1失败，尝试使用Resources.Load检查（运行时模式）
        if (!fileExists)
        {
            TextAsset csvAsset = Resources.Load<TextAsset>(csvFileName);
            if (csvAsset != null)
            {
                fileExists = true;
                Debug.Log($"通过Resources.Load找到数据集{dataSetType}文件: {csvFileName}.csv");
            }
        }
        
        // 方法3：检查StreamingAssets目录（打包后）
        if (!fileExists)
        {
            string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Resources", csvFileName + ".csv");
            fileExists = System.IO.File.Exists(streamingAssetsPath);
            if (fileExists)
            {
                resourcesPath = streamingAssetsPath;
                Debug.Log($"在StreamingAssets中找到数据集{dataSetType}文件: {streamingAssetsPath}");
            }
        }
        
        // 方法4：检查应用程序数据目录
        if (!fileExists)
        {
            string appDataPath = System.IO.Path.Combine(Application.persistentDataPath, "Resources", csvFileName + ".csv");
            fileExists = System.IO.File.Exists(appDataPath);
            if (fileExists)
            {
                resourcesPath = appDataPath;
                Debug.Log($"在PersistentDataPath中找到数据集{dataSetType}文件: {appDataPath}");
            }
        }
        
        if (!fileExists)
        {
            Debug.LogError($"数据集{dataSetType}的CSV文件不存在，已尝试以下路径:");
            Debug.LogError($"1. {System.IO.Path.Combine(Application.dataPath, "Resources", csvFileName + ".csv")}");
            Debug.LogError($"2. Resources.Load(\"{csvFileName}\")");
            Debug.LogError($"3. {System.IO.Path.Combine(Application.streamingAssetsPath, "Resources", csvFileName + ".csv")}");
            Debug.LogError($"4. {System.IO.Path.Combine(Application.persistentDataPath, "Resources", csvFileName + ".csv")}");
            UpdateStatus($"错误：数据集{dataSetType}文件不存在");
            yield break;
        }
        
        Debug.Log($"找到数据集{dataSetType}文件: {csvFileName}.csv");
        
        // 自动检测CSV格式
        csvFormat = DetectCsvFormat(csvFileName);
        
        UpdateStatus($"正在加载 {csvFileName}.csv...");
        
        // 初始化地形系统
        InitializeTerrainSystem();
        
        // 设置SceneInitializer
        sceneInitializer.SetCsvFileName(csvFileName);
        sceneInitializer.csvFormat = csvFormat;
        
        Debug.Log($"开始初始化场景，文件: {csvFileName}, 格式: {csvFormat}");
        
        // 初始化场景
        sceneInitializer.InitializeScene();
        
        // 延迟跳转到第一个塔的位置
        StartCoroutine(JumpToFirstTowerDelayed());
        
        // 隐藏初始界面
        HideInitialInterface();
        
        // 显示主UI
        if (uiManager != null)
        {
            uiManager.ShowMainUI();
        }
    }
    
    /// <summary>
    /// 自动检测并初始化可用的电塔数据（备用方法）
    /// </summary>
    System.Collections.IEnumerator InitializeWithAvailableData()
    {
        UpdateStatus("正在检测可用的电塔数据文件...");
        
        // 检查Resources目录中的CSV文件
        string[] availableFiles = GetAvailableCsvFiles();
        
        if (availableFiles.Length == 0)
        {
            Debug.LogError("未找到任何CSV文件");
            UpdateStatus("错误：未找到电塔数据文件");
            yield break;
        }
        
        Debug.Log($"找到 {availableFiles.Length} 个CSV文件: {string.Join(", ", availableFiles)}");
        
        // 优先使用tower_centers.csv，如果没有则使用simple_towers.csv
        string selectedFile = null;
        SceneInitializer.CSVFormat selectedFormat = SceneInitializer.CSVFormat.SimpleTowers;
        
        if (availableFiles.Contains("tower_centers"))
        {
            selectedFile = "tower_centers";
            selectedFormat = SceneInitializer.CSVFormat.TowerCenters;
            Debug.Log("选择使用 tower_centers.csv 格式");
        }
        else if (availableFiles.Contains("simple_towers"))
        {
            selectedFile = "simple_towers";
            selectedFormat = SceneInitializer.CSVFormat.SimpleTowers;
            Debug.Log("选择使用 simple_towers.csv 格式");
        }
        else
        {
            // 使用第一个可用的文件，尝试自动判断格式
            selectedFile = availableFiles[0];
            selectedFormat = DetectCsvFormat(selectedFile);
            Debug.Log($"使用第一个可用文件: {selectedFile}, 检测格式: {selectedFormat}");
        }
        
        UpdateStatus($"正在加载 {selectedFile}.csv...");
        
        // 初始化地形系统
        InitializeTerrainSystem();
        
        // 设置SceneInitializer
        sceneInitializer.SetCsvFileName(selectedFile);
        sceneInitializer.csvFormat = selectedFormat;
        
        Debug.Log($"开始初始化场景，文件: {selectedFile}, 格式: {selectedFormat}");
        
        // 初始化场景
        sceneInitializer.InitializeScene();
        
        // 延迟跳转到第一个塔的位置
        StartCoroutine(JumpToFirstTowerDelayed());
        
        // 隐藏初始界面
        HideInitialInterface();
        
        // 显示主UI
        if (uiManager != null)
        {
            uiManager.ShowMainUI();
        }
    }
    
    /// <summary>
    /// 获取Resources目录中可用的CSV文件列表
    /// </summary>
    string[] GetAvailableCsvFiles()
    {
        List<string> availableFiles = new List<string>();
        
        // 方法1：通过Resources.Load获取所有TextAsset（推荐用于打包后）
        TextAsset[] allTextAssets = Resources.LoadAll<TextAsset>("");
        foreach (TextAsset asset in allTextAssets)
        {
            if (asset.name.EndsWith(".csv") || asset.name.Contains("tower_centers") || 
                asset.name == "A" || asset.name == "B" || asset.name == "simple_towers")
            {
                string fileName = asset.name;
                if (fileName.EndsWith(".csv"))
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                }
                if (!availableFiles.Contains(fileName))
                {
                    availableFiles.Add(fileName);
                }
            }
        }
        
        // 方法2：检查文件系统（编辑器模式）
        string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources");
        if (System.IO.Directory.Exists(resourcesPath))
        {
            string[] csvFiles = System.IO.Directory.GetFiles(resourcesPath, "*.csv");
            foreach (string file in csvFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!availableFiles.Contains(fileName))
                {
                    availableFiles.Add(fileName);
                }
            }
        }
        
        // 方法3：检查StreamingAssets目录
        string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Resources");
        if (System.IO.Directory.Exists(streamingAssetsPath))
        {
            string[] csvFiles = System.IO.Directory.GetFiles(streamingAssetsPath, "*.csv");
            foreach (string file in csvFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!availableFiles.Contains(fileName))
                {
                    availableFiles.Add(fileName);
                }
            }
        }
        
        Debug.Log($"找到的CSV文件: {string.Join(", ", availableFiles.ToArray())}");
        return availableFiles.ToArray();
    }
    
    /// <summary>
    /// 自动检测CSV文件格式
    /// </summary>
    SceneInitializer.CSVFormat DetectCsvFormat(string fileName)
    {
        try
        {
            // 尝试多种方式读取CSV文件
            string[] lines = null;
            
            // 方法1：通过Resources.Load读取（推荐用于打包后）
            TextAsset csvAsset = Resources.Load<TextAsset>(fileName);
            if (csvAsset != null)
            {
                lines = csvAsset.text.Split('\n');
                Debug.Log($"通过Resources.Load读取CSV文件: {fileName}");
            }
            else
            {
                // 方法2：直接文件系统读取（编辑器模式）
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources", fileName + ".csv");
                if (System.IO.File.Exists(resourcesPath))
                {
                    lines = System.IO.File.ReadAllLines(resourcesPath);
                    Debug.Log($"通过文件系统读取CSV文件: {resourcesPath}");
                }
                else
                {
                    // 方法3：检查StreamingAssets目录
                    string streamingAssetsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Resources", fileName + ".csv");
                    if (System.IO.File.Exists(streamingAssetsPath))
                    {
                        lines = System.IO.File.ReadAllLines(streamingAssetsPath);
                        Debug.Log($"通过StreamingAssets读取CSV文件: {streamingAssetsPath}");
                    }
                    else
                    {
                        // 方法4：检查应用程序数据目录
                        string appDataPath = System.IO.Path.Combine(Application.persistentDataPath, "Resources", fileName + ".csv");
                        if (System.IO.File.Exists(appDataPath))
                        {
                            lines = System.IO.File.ReadAllLines(appDataPath);
                            Debug.Log($"通过PersistentDataPath读取CSV文件: {appDataPath}");
                        }
                    }
                }
            }
            
            if (lines != null && lines.Length > 1) // 至少有一行数据（跳过标题行）
            {
                string firstDataLine = lines[1]; // 假设第一行是标题，第二行是数据
                string[] tokens = firstDataLine.Split(',');
                
                // 检查是否为B.csv格式（6列：group_id,order,x,y,z,line_count）
                if (tokens.Length == 6)
                {
                    // 进一步检查是否为B.csv格式：第一列和第二列应该是整数（group_id和order）
                    if (int.TryParse(tokens[0], out _) && int.TryParse(tokens[1], out _))
                    {
                        Debug.Log($"检测到6列数据，判断为B.csv格式（支持group分组连线）");
                        return SceneInitializer.CSVFormat.B;
                    }
                    else
                    {
                        Debug.Log($"检测到6列数据但非B.csv格式，判断为SimpleTowers格式");
                        return SceneInitializer.CSVFormat.SimpleTowers;
                    }
                }
                else if (tokens.Length == 3)
                {
                    // 3列数据，可能是tower_centers格式 (x,z,height)
                    Debug.Log($"检测到3列数据，判断为TowerCenters格式");
                    return SceneInitializer.CSVFormat.TowerCenters;
                }
                else if (tokens.Length >= 4)
                {
                    // 4列或更多数据，可能是simple_towers格式 (x,y,z,height)
                    Debug.Log($"检测到{tokens.Length}列数据，判断为SimpleTowers格式");
                    return SceneInitializer.CSVFormat.SimpleTowers;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"检测CSV格式时出错: {ex.Message}");
        }
        
        // 默认使用SimpleTowers格式
        Debug.Log("无法检测格式，默认使用SimpleTowers格式");
        return SceneInitializer.CSVFormat.SimpleTowers;
    }
    
    void OnUploadLasClicked()
    {
        Debug.Log("用户选择上传LAS文件");
        
        // 隐藏Python引导区域（如果正在显示）
        if (pythonGuideArea != null)
        {
            pythonGuideArea.style.display = DisplayStyle.None;
        }
        
        // 显示文件上传区域
        fileUploadArea.style.display = DisplayStyle.Flex;
        
        // 隐藏选择按钮（现在需要隐藏数据集A和B按钮以及上传按钮）
        HideSelectionButtons();
        
        UpdateStatus("请选择LAS文件");
    }
    
    void OnPythonGuideClicked()
    {
        Debug.Log("用户选择Python环境配置");
        
        // 显示Python引导区域
        ShowPythonGuideArea();
        
        // 隐藏选择按钮
        HideSelectionButtons();
        
        UpdateStatus("Python环境配置向导");
    }
    
    /// <summary>
    /// 显示主界面（返回功能）
    /// </summary>
    void ShowMainInterface()
    {
        Debug.Log("用户选择返回主界面");
        
        // 隐藏文件上传区域
        fileUploadArea.style.display = DisplayStyle.None;
        
        // 隐藏Python引导区域
        if (pythonGuideArea != null)
        {
            pythonGuideArea.style.display = DisplayStyle.None;
        }
        
        // 显示选择按钮（不显示标题，避免重复）
        ShowSelectionButtonsOnly();
        
        // 重置文件选择状态
        selectedLasFile = "";
        UpdateFileDisplay();
        
        UpdateStatus("系统就绪");
    }
    
    void ShowPythonGuideArea()
    {
        Debug.Log("显示Python引导区域");
        
        // 隐藏选择按钮（包括上面的三个功能选择框）
        HideSelectionButtons();
        
        // 显示Python引导区域
        if (pythonGuideArea != null)
        {
            pythonGuideArea.style.display = DisplayStyle.Flex;
        }
        
        // 隐藏文件上传区域
        if (fileUploadArea != null)
        {
            fileUploadArea.style.display = DisplayStyle.None;
        }
        
        UpdateStatus("Python环境配置向导");
    }
    
    void CheckPythonEnvironment()
    {
        Debug.Log("开始检查Python环境");
        UpdateStatus("正在检查Python环境...");
        
        // 启动Python环境检查协程
        StartCoroutine(CheckPythonEnvironmentCoroutine());
    }
    
    System.Collections.IEnumerator CheckPythonEnvironmentCoroutine()
    {
        bool pythonAvailable = false;
        bool laspyAvailable = false;
        bool numpyAvailable = false;
        bool scipyAvailable = false;
        bool sklearnAvailable = false;
        
        try
        {
            // 检查Python是否可用
            pythonAvailable = PowerlineSystem.LasToOffConverter.CheckDependencies();
            
            if (pythonAvailable)
            {
                UpdateStatus("Python环境检查完成 - 所有依赖库已安装");
                Debug.Log("Python环境检查成功");
            }
            else
            {
                UpdateStatus("Python环境检查失败 - 请安装必要的Python库");
                Debug.Log("Python环境检查失败");
            }
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Python环境检查出错: {ex.Message}");
            Debug.LogError($"Python环境检查异常: {ex.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// 隐藏选择按钮
    /// </summary>
    void HideSelectionButtons()
    {
        // 隐藏选择区域中的所有按钮
        if (initialPanel != null)
        {
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 状态(5) -> 底部(6)
            
            // 隐藏标题区域（第二个子元素，索引为1）
            if (initialPanel.childCount > 1)
            {
                var titleContainer = initialPanel[1];
                if (titleContainer != null)
                {
                    titleContainer.style.display = DisplayStyle.None;
                    Debug.Log("已隐藏标题区域");
                }
            }
            
            // 隐藏选择区域（第三个子元素，索引为2）
            if (initialPanel.childCount > 2)
            {
                var selectionContainer = initialPanel[2];
                if (selectionContainer != null)
                {
                    selectionContainer.style.display = DisplayStyle.None;
                    Debug.Log("已隐藏选择按钮区域");
                }
            }
        }
    }
    
    /// <summary>
    /// 显示选择按钮
    /// </summary>
    void ShowSelectionButtons()
    {
        // 显示选择区域中的所有按钮
        if (initialPanel != null)
        {
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 状态(5) -> 底部(6)
            
            // 显示标题区域（第二个子元素，索引为1）
            if (initialPanel.childCount > 1)
            {
                var titleContainer = initialPanel[1];
                if (titleContainer != null)
                {
                    titleContainer.style.display = DisplayStyle.Flex;
                    Debug.Log("已显示标题区域");
                }
            }
            
            // 显示选择区域（第三个子元素，索引为2）
            if (initialPanel.childCount > 2)
            {
                var selectionContainer = initialPanel[2];
                if (selectionContainer != null)
                {
                    selectionContainer.style.display = DisplayStyle.Flex;
                    Debug.Log("已显示选择按钮区域");
                }
            }
        }
    }
    
    /// <summary>
    /// 只显示选择按钮（不显示标题，避免重复）
    /// </summary>
    void ShowSelectionButtonsOnly()
    {
        // 只显示选择区域中的按钮，不显示标题
        if (initialPanel != null)
        {
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 状态(5) -> 底部(6)
            
            // 隐藏标题区域（第二个子元素，索引为1）
            if (initialPanel.childCount > 1)
            {
                var titleContainer = initialPanel[1];
                if (titleContainer != null)
                {
                    titleContainer.style.display = DisplayStyle.None;
                    Debug.Log("已隐藏标题区域（避免重复）");
                }
            }
            
            // 显示选择区域（第三个子元素，索引为2）
            if (initialPanel.childCount > 2)
            {
                var selectionContainer = initialPanel[2];
                if (selectionContainer != null)
                {
                    selectionContainer.style.display = DisplayStyle.Flex;
                    Debug.Log("已显示选择按钮区域（仅选择区域）");
                }
            }
        }
    }
    
    void SelectLasFile()
    {
        #if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("选择LAS文件", "", "las");
        if (!string.IsNullOrEmpty(path))
        {
            selectedLasFile = path;
            UpdateFileDisplay();
            UpdateStatus($"已选择文件: {Path.GetFileName(path)}");
            if (fileUploadArea != null)
            {
                var filePathLabel = fileUploadArea.Q<Label>("file-path");
                if (filePathLabel != null)
                {
                    filePathLabel.text = Path.GetFileName(path);
                    filePathLabel.style.color = accentColor;
                }
            }
        }
        #else
        // 运行时文件选择 - 使用Windows API
        string path = RuntimeFileSelector.OpenFileDialog("选择LAS文件", "LAS文件|*.las|所有文件|*.*");
        if (!string.IsNullOrEmpty(path))
        {
            selectedLasFile = path;
            UpdateFileDisplay();
            UpdateStatus($"已选择文件: {Path.GetFileName(path)}");
            if (fileUploadArea != null)
            {
                var filePathLabel = fileUploadArea.Q<Label>("file-path");
                if (filePathLabel != null)
                {
                    filePathLabel.text = Path.GetFileName(path);
                    filePathLabel.style.color = accentColor;
                }
            }
        }
        else
        {
            UpdateStatus("未选择文件");
        }
        #endif
    }
    
    /// <summary>
    /// 预览点云功能
    /// </summary>
    void PreviewPointCloud()
    {
        if (string.IsNullOrEmpty(selectedLasFile))
        {
            UpdateStatus("请先选择LAS文件");
            return;
        }
        
        if (isProcessing)
        {
            UpdateStatus("正在处理中，请稍候...");
            return;
        }
        
        // 立即更新状态为处理中
        UpdateStatus("点云处理中...");
        
        // 延迟一帧再开始处理，确保状态更新先显示
        StartCoroutine(DelayedStartPreview());
    }
    
    /// <summary>
    /// 延迟开始预览，确保状态先更新
    /// </summary>
    System.Collections.IEnumerator DelayedStartPreview()
    {
        // 等待一帧，确保状态更新先显示
        yield return null;
        
        isProcessing = true;
        progressBar.style.display = DisplayStyle.Flex;
        
        // 启动异步转换过程
        StartCoroutine(ConvertAndPreviewPointCloud());
    }
    
    System.Collections.IEnumerator ConvertAndPreviewPointCloud()
    {
        bool conversionSuccess = false;
        string errorMessage = "";
        bool needWait = false;
        bool needFinalWait = false;
        string fileName = "";
        string fullOffPath = "";
        
        // 获取文件名（不包含扩展名）
        fileName = Path.GetFileNameWithoutExtension(selectedLasFile);
        
        // 检查对应的OFF文件是否已存在
        string offFilePath = $"Resources/pointcloud/{fileName}.off";
        fullOffPath = Path.Combine(Application.dataPath, offFilePath);
        
        if (File.Exists(fullOffPath))
        {
            Debug.Log($"OFF文件已存在: {fullOffPath}");
            UpdateStatus("OFF文件已存在，直接预览...");
            progressBar.value = 50f;
        }
        else
        {
            // 先更新状态，再开始转换
            progressBar.value = 10f;
            UpdateStatus("正在转换LAS文件为OFF格式...");
            
            // 等待一帧确保状态更新显示
            yield return null;
            
            progressBar.value = 15f;
            UpdateStatus("检查转换依赖...");
            
            if (!PowerlineSystem.LasToOffConverter.CheckDependencies())
            {
                errorMessage = "Python或laspy库未安装";
                UpdateStatus($"错误：{errorMessage}");
                Debug.LogError("LAS到OFF转换依赖检查失败");
                isProcessing = false;
                progressBar.style.display = DisplayStyle.None;
                yield break;
            }
            
            progressBar.value = 20f;
            UpdateStatus("正在转换LAS文件...");
            
            // 等待一帧确保状态更新显示
            yield return null;
            
            // 调用LAS到OFF转换器
            string convertedPath = "";
            try
            {
                convertedPath = PowerlineSystem.LasToOffConverter.ConvertLasToOff(selectedLasFile);
            }
            catch (System.Exception ex)
            {
                errorMessage = $"转换异常: {ex.Message}";
                UpdateStatus($"错误：{errorMessage}");
                Debug.LogError($"LAS到OFF转换异常: {errorMessage}");
                isProcessing = false;
                progressBar.style.display = DisplayStyle.None;
                yield break;
            }
            
            if (string.IsNullOrEmpty(convertedPath))
            {
                errorMessage = "LAS文件转换失败";
                UpdateStatus($"错误：{errorMessage}");
                Debug.LogError("LAS到OFF转换失败");
                isProcessing = false;
                progressBar.style.display = DisplayStyle.None;
                yield break;
            }
            
            progressBar.value = 80f;
            UpdateStatus("LAS文件转换完成");
            Debug.Log($"LAS文件转换成功: {convertedPath}");
            
            // 标记需要等待
            needWait = true;
        }
        
        // 验证OFF文件是否存在
        if (!File.Exists(fullOffPath))
        {
            errorMessage = $"OFF文件生成失败 {fileName}.off";
            UpdateStatus($"错误：{errorMessage}");
            Debug.LogError($"OFF文件不存在: {fullOffPath}");
            isProcessing = false;
            progressBar.style.display = DisplayStyle.None;
            yield break;
        }
        
        progressBar.value = 90f;
        UpdateStatus("正在启动点云预览...");
        
        // 查找或创建点云查看器
        var pointCloudViewer = FindObjectOfType<UI.PointCloudViewer>();
        if (pointCloudViewer == null)
        {
            GameObject viewerObj = new GameObject("PointCloudViewer");
            pointCloudViewer = viewerObj.AddComponent<UI.PointCloudViewer>();
        }
        
        // 显示点云查看器
        pointCloudViewer.ShowPointCloudViewer(fileName);
        
        progressBar.value = 100f;
        UpdateStatus($"点云预览已打开: {fileName}");
        Debug.Log($"点云预览已启动，文件: {fileName}");
        
        conversionSuccess = true;
        needFinalWait = true;
        
        // 清理处理状态
        isProcessing = false;
        progressBar.style.display = DisplayStyle.None;
        
        // 在try-catch块外执行yield return
        if (needWait)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        if (needFinalWait)
        {
            yield return new WaitForSeconds(1f);
        }
    }
    
    void UpdateFileDisplay()
    {
        if (fileUploadArea != null)
        {
            var filePathLabel = fileUploadArea.Q<Label>("file-path");
            var previewButton = fileUploadArea.Q<Button>("preview-button");
            
            if (!string.IsNullOrEmpty(selectedLasFile))
            {
                // 更新文件路径显示
                if (filePathLabel != null)
                {
                    filePathLabel.text = Path.GetFileName(selectedLasFile);
                    filePathLabel.style.color = accentColor;
                }
                
                // 显示预览按钮
                if (previewButton != null)
                {
                    previewButton.style.display = DisplayStyle.Flex;
                }
                
                // 显示开始提取按钮
                if (startExtractionButton != null)
                {
                    startExtractionButton.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                // 隐藏预览按钮
                if (previewButton != null)
                {
                    previewButton.style.display = DisplayStyle.None;
                }
                
                // 隐藏开始提取按钮
                if (startExtractionButton != null)
                {
                    startExtractionButton.style.display = DisplayStyle.None;
                }
            }
        }
    }
    
    void StartExtraction()
    {
        if (string.IsNullOrEmpty(selectedLasFile))
        {
            UpdateStatus("请先选择LAS文件");
            return;
        }
        
        if (isProcessing)
        {
            UpdateStatus("正在处理中，请稍候...");
            return;
        }
        
        isProcessing = true;
        progressBar.style.display = DisplayStyle.Flex;
        startExtractionButton.style.display = DisplayStyle.None;
        
        UpdateStatus("开始电力线提取...");
        
        // 调用电力线提取管理器
        if (powerLineExtractorManager != null)
        {
            powerLineExtractorManager.SelectLasFile(selectedLasFile);
            powerLineExtractorManager.StartPowerLineExtraction();
        }
        else
        {
            UpdateStatus("错误：未找到电力线提取管理器");
            isProcessing = false;
        }
    }
    
    void OnExtractionStatusChanged(string status)
    {
        UpdateStatus(status);
    }
    
    void OnExtractionCompleted(string csvPath)
    {
        UpdateStatus("电力线提取完成！正在构建场景...");
        progressBar.value = 100f;
        
        // 使用生成的CSV文件初始化场景
        StartCoroutine(BuildSceneFromExtractedData(csvPath));
    }
    
    System.Collections.IEnumerator BuildSceneFromExtractedData(string csvPath)
    {
        yield return new WaitForSeconds(0.3f);
        
        if (sceneInitializer != null && !string.IsNullOrEmpty(csvPath))
        {
            // 检查源文件是否存在
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"源CSV文件不存在: {csvPath}");
                UpdateStatus($"错误：源CSV文件不存在: {csvPath}");
                yield break;
            }
            
            Debug.Log($"CSV文件已存在，直接使用: {csvPath}");
            Debug.Log($"文件大小: {new FileInfo(csvPath).Length} 字节");
            Debug.Log($"文件最后修改时间: {File.GetLastWriteTime(csvPath)}");
            
            // 获取文件名（不包含扩展名）
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            
            // 统一初始化场景
            InitializeSceneWithCsvFile(fileName, csvPath);
        }
        
        // 延迟切换到正常模式
        yield return StartCoroutine(DelayedSwitchToNormalMode());
    }
    
    /// <summary>
    /// 统一的场景初始化方法
    /// </summary>
    /// <param name="fileName">CSV文件名（不包含扩展名）</param>
    /// <param name="csvPath">CSV文件完整路径</param>
    void InitializeSceneWithCsvFile(string fileName, string csvPath)
    {
        try
        {
            // 清除之前的场景
            Debug.Log("清除之前的场景...");
            sceneInitializer.ClearAllWires();
            
            // 设置SceneInitializer使用CSV文件
            sceneInitializer.SetCsvFileName(fileName);
            
            // 自动检测CSV格式，而不是强制设置为TowerCenters
            SceneInitializer.CSVFormat detectedFormat = DetectCsvFormat(fileName);
            sceneInitializer.csvFormat = detectedFormat;
            
            Debug.Log($"使用CSV文件构建场景: {fileName}");
            Debug.Log($"自动检测到CSV格式: {detectedFormat}");
            
            // 根据格式设置不同的参数
            if (detectedFormat == SceneInitializer.CSVFormat.B)
            {
                Debug.Log("使用B.csv格式，支持group分组连线");
                sceneInitializer.towerScale = 3.0f; // B.csv格式使用较小的缩放
            }
            else
            {
                Debug.Log("使用标准格式");
                sceneInitializer.towerScale = 5.0f; // 标准格式使用较大的缩放
            }
            sceneInitializer.baseTowerHeight = 2f;
            
            // 初始化地形系统
            InitializeTerrainSystem();
            
            // 初始化场景（唯一的地方）
            sceneInitializer.InitializeScene();
            
            // 延迟跳转到第一个塔的位置
            StartCoroutine(JumpToFirstTowerDelayed());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"构建场景时出错: {ex.Message}");
            UpdateStatus($"构建场景失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 延迟跳转到第一个塔的位置
    /// </summary>
    System.Collections.IEnumerator JumpToFirstTowerDelayed()
    {
        // 等待场景初始化完成
        yield return new WaitForSeconds(1.0f); // 增加等待时间
        
        Debug.Log("[InitialInterfaceManager] 开始查找第一个电塔...");
        
        // 确保相机管理器存在
        EnsureCameraManagerExists();
        
        // 方法1：通过TowerOverviewManager跳转
        var towerManager = FindObjectOfType<TowerOverviewManager>();
        if (towerManager != null)
        {
            Debug.Log("[InitialInterfaceManager] 找到TowerOverviewManager，尝试获取电塔列表...");
            
            // 确保TowerOverviewManager已初始化
            towerManager.InitializeTowerData();
            
            var allTowers = towerManager.GetAllTowers();
            if (allTowers != null && allTowers.Count > 0)
            {
                var firstTower = allTowers[0];
                Debug.Log($"[InitialInterfaceManager] 通过TowerOverviewManager跳转到第一个塔: {firstTower.name}");
                towerManager.JumpToTower(firstTower);
                UpdateStatus($"已跳转到第一个塔: {firstTower.name}");
                yield break;
            }
            else
            {
                Debug.LogWarning("[InitialInterfaceManager] TowerOverviewManager中没有找到电塔数据");
            }
        }
        else
        {
            Debug.LogWarning("[InitialInterfaceManager] 未找到TowerOverviewManager");
        }
        
        // 方法2：直接查找场景中的电塔对象
        Debug.Log("[InitialInterfaceManager] 尝试直接查找场景中的电塔...");
        yield return StartCoroutine(FindAndJumpToFirstTower());
    }
    
    /// <summary>
    /// 查找并跳转到第一个电塔
    /// </summary>
    System.Collections.IEnumerator FindAndJumpToFirstTower()
    {
        // 等待场景完全加载
        yield return new WaitForSeconds(0.2f);
        
        // 查找电塔对象
        GameObject firstTower = null;
        
        // 方法1：通过标签查找
        GameObject[] taggedTowers = GameObject.FindGameObjectsWithTag("Tower");
        if (taggedTowers.Length > 0)
        {
            firstTower = taggedTowers[0];
            Debug.Log($"[InitialInterfaceManager] 通过Tower标签找到电塔: {firstTower.name}");
        }
        else
        {
            // 方法2：通过名称查找
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            var towerObjects = allObjects.Where(obj => 
                obj.name.Contains("Tower") || 
                obj.name.Contains("GoodTower") ||
                obj.name.StartsWith("Tower_")).ToArray();
            
            if (towerObjects.Length > 0)
            {
                firstTower = towerObjects[0];
                Debug.Log($"[InitialInterfaceManager] 通过名称找到电塔: {firstTower.name}");
            }
        }
        
        if (firstTower != null)
        {
            // 跳转到电塔
            yield return StartCoroutine(JumpToTowerPosition(firstTower.transform.position));
        }
        else
        {
            Debug.LogWarning("[InitialInterfaceManager] 未找到任何电塔，无法跳转");
            UpdateStatus("未找到电塔，无法跳转");
        }
    }
    
    /// <summary>
    /// 跳转到电塔位置
    /// </summary>
    System.Collections.IEnumerator JumpToTowerPosition(Vector3 towerPosition)
    {
        Debug.Log($"[InitialInterfaceManager] 开始跳转到电塔位置: {towerPosition}");
        
        // 查找相机管理器
        var cameraManager = FindObjectOfType<CameraManager>();
        Camera targetCamera = null;
        
        if (cameraManager != null && cameraManager.mainCamera != null)
        {
            targetCamera = cameraManager.mainCamera;
            Debug.Log("[InitialInterfaceManager] 使用CameraManager的相机");
        }
        else
        {
            targetCamera = Camera.main;
            Debug.Log("[InitialInterfaceManager] 使用主相机");
        }
        
        if (targetCamera == null)
        {
            Debug.LogError("[InitialInterfaceManager] 未找到可用的相机");
            yield break;
        }
        
        // 计算跳转位置
        Vector3 jumpPosition = CalculateJumpPosition(towerPosition, cameraManager);
        
        // 执行跳转
        yield return StartCoroutine(SmoothCameraJump(targetCamera, jumpPosition, towerPosition));
        
        Debug.Log($"[InitialInterfaceManager] 跳转完成，相机位置: {jumpPosition}");
        UpdateStatus("已跳转到第一个电塔");
    }
    
    /// <summary>
    /// 计算跳转位置
    /// </summary>
    Vector3 CalculateJumpPosition(Vector3 towerPosition, CameraManager cameraManager)
    {
        Vector3 jumpPosition;
        
        if (cameraManager != null)
        {
            // 根据当前视角计算跳转位置
            int currentView = cameraManager.GetCurrentView();
            switch (currentView)
            {
                case 1: // 上帝视角 - 在电塔上方俯视
                    jumpPosition = towerPosition + new Vector3(5f, 50f, 5f);
                    break;
                    
                case 2: // 飞行视角 - 在电塔旁边平视
                    jumpPosition = towerPosition + new Vector3(30f, 25f, 30f);
                    break;
                    
                default: // 第一人称视角 - 近距离观察
                    jumpPosition = towerPosition + new Vector3(15f, 5f, 15f);
                    break;
            }
        }
        else
        {
            // 默认跳转位置
            jumpPosition = towerPosition + new Vector3(20f, 15f, 20f);
        }
        
        // 确保位置在地面之上
        jumpPosition.y = Mathf.Max(jumpPosition.y, 5f);
        
        return jumpPosition;
    }
    
    /// <summary>
    /// 平滑相机跳转
    /// </summary>
    System.Collections.IEnumerator SmoothCameraJump(Camera camera, Vector3 targetPosition, Vector3 lookAtPosition)
    {
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;
        
        // 计算目标旋转
        Vector3 lookDirection = (lookAtPosition - targetPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        
        float duration = 1f;
        float elapsedTime = 0f;
        
        Debug.Log($"[InitialInterfaceManager] 开始相机跳转动画，持续时间: {duration}秒");
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 使用平滑曲线
            t = Mathf.SmoothStep(0f, 1f, t);
            
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        // 确保最终位置准确
        camera.transform.position = targetPosition;
        camera.transform.rotation = targetRotation;
        
        Debug.Log($"[InitialInterfaceManager] 相机跳转完成");
    }
    

    
    void OnExtractionError(string error)
    {
        UpdateStatus($"提取失败: {error}");
        isProcessing = false;
        progressBar.style.display = DisplayStyle.None;
        startExtractionButton.style.display = DisplayStyle.Flex;
    }
    
    System.Collections.IEnumerator DelayedSwitchToNormalMode()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 隐藏初始界面
        HideInitialInterface();
        
        // 显示主UI
        if (uiManager != null)
        {
            uiManager.ShowMainUI();
        }
        
        isProcessing = false;
    }
    
    void UpdateStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
        Debug.Log($"[初始界面] {message}");
    }
    
    void ApplyFont(Label label, FontSize size = FontSize.Body)
    {
        // 使用字体管理器
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyFont(label, size);
        }
        else
        {
            // 备用方案
            if (uiFont != null)
            {
                label.style.unityFont = uiFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    label.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    void ApplyFont(Button button, FontSize size = FontSize.Body)
    {
        // 使用字体管理器
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyFont(button, size);
        }
        else
        {
            // 备用方案
            if (uiFont != null)
            {
                button.style.unityFont = uiFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    button.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    void CreateBackgroundDecoration(VisualElement parent)
    {
        // 创建装饰性背景元素
        var decorationContainer = new VisualElement();
        decorationContainer.style.position = Position.Absolute;
        decorationContainer.style.top = 0;
        decorationContainer.style.left = 0;
        decorationContainer.style.right = 0;
        decorationContainer.style.bottom = 0;
        
        // 创建渐变背景
        var gradientBackground = new VisualElement();
        gradientBackground.style.position = Position.Absolute;
        gradientBackground.style.top = 0;
        gradientBackground.style.left = 0;
        gradientBackground.style.right = 0;
        gradientBackground.style.bottom = 0;
        gradientBackground.style.backgroundColor = gradientStart;
        decorationContainer.Add(gradientBackground);
        
        // 添加装饰性圆圈 - 更多和更大
        var circlePositions = new[] {
            new { width = 120, top = 5, left = 5, alpha = 0.08f },
            new { width = 180, top = 15, left = 75, alpha = 0.06f },
            new { width = 90, top = 65, left = 3, alpha = 0.07f },
            new { width = 150, top = 10, left = 65, alpha = 0.05f },
            new { width = 110, top = 55, left = 80, alpha = 0.06f },
            new { width = 200, top = 80, left = 10, alpha = 0.04f },
            new { width = 80, top = 30, left = 90, alpha = 0.08f },
            new { width = 160, top = 40, left = 15, alpha = 0.05f }
        };
        
        for (int i = 0; i < circlePositions.Length; i++)
        {
            var pos = circlePositions[i];
            var circle = new VisualElement();
            circle.style.position = Position.Absolute;
            circle.style.width = pos.width;
            circle.style.height = pos.width;
            circle.style.borderTopLeftRadius = 50;
            circle.style.borderTopRightRadius = 50;
            circle.style.borderBottomLeftRadius = 50;
            circle.style.borderBottomRightRadius = 50;
            circle.style.backgroundColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, pos.alpha);
            circle.style.top = pos.top;
            circle.style.left = pos.left;
            decorationContainer.Add(circle);
        }
        
        // 添加装饰性线条
        var linePositions = new[] {
            new { width = 150, top = 25, left = 20, rotation = 15 },
            new { width = 120, top = 75, left = 70, rotation = -10 },
            new { width = 180, top = 45, left = 5, rotation = 25 }
        };
        
        for (int i = 0; i < linePositions.Length; i++)
        {
            var pos = linePositions[i];
            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.width = pos.width;
            line.style.height = 3;
            line.style.backgroundColor = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.3f);
            line.style.top = pos.top;
            line.style.left = pos.left;
            // 移除旋转效果，因为Rotate构造函数不支持这种用法
            decorationContainer.Add(line);
        }
        
        parent.Add(decorationContainer);
    }
    
    void CreateFooterInfo(VisualElement parent)
    {
        var footerContainer = new VisualElement();
        footerContainer.style.marginTop = 30;
        footerContainer.style.alignItems = Align.Center;
        
        // 分隔线
        var separator = new VisualElement();
        separator.style.width = 300;
        separator.style.height = 2;
        separator.style.backgroundColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.3f);
        separator.style.marginTop = 10;
        separator.style.marginBottom = 15;
        footerContainer.Add(separator);
        
        // 技术信息
        var techInfo = new Label("基于Unity 3D + C# + python开发");
        techInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        techInfo.style.fontSize = 12;
        techInfo.style.unityTextAlign = TextAnchor.MiddleCenter;
        techInfo.style.marginBottom = 8;
        ApplyFont(techInfo);
        footerContainer.Add(techInfo);
        
        // 版本信息
        var versionText = new Label("Version 1.0.0 | Software Cup 2025");
        versionText.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        versionText.style.fontSize = 12;
        versionText.style.unityTextAlign = TextAnchor.MiddleCenter;
        versionText.style.marginBottom = 8;
        ApplyFont(versionText);
        footerContainer.Add(versionText);
        
        // 版权信息
        var copyrightText = new Label("© 2025 Dead Chicken Team | All Rights Reserved");
        copyrightText.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        copyrightText.style.fontSize = 11;
        copyrightText.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(copyrightText);
        footerContainer.Add(copyrightText);
        
        // 添加退出程序按钮
        var exitButton = new Button(() => {
            ExitApplication();
        });
        exitButton.text = "退出程序";
        exitButton.style.marginTop = 20;
        exitButton.style.width = 120;
        exitButton.style.height = 35;
        exitButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 红色
        exitButton.style.color = Color.white;
        exitButton.style.borderBottomLeftRadius = 5;
        exitButton.style.borderBottomRightRadius = 5;
        exitButton.style.borderTopLeftRadius = 5;
        exitButton.style.borderTopRightRadius = 5;
        exitButton.style.borderBottomWidth = 1;
        exitButton.style.borderTopWidth = 1;
        exitButton.style.borderLeftWidth = 1;
        exitButton.style.borderRightWidth = 1;
        exitButton.style.borderBottomColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderTopColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderLeftColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderRightColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.paddingLeft = 8;
        exitButton.style.paddingRight = 8;
        exitButton.style.paddingTop = 6;
        exitButton.style.paddingBottom = 6;
        exitButton.style.fontSize = 13;
        exitButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        exitButton.style.whiteSpace = WhiteSpace.NoWrap;
        ApplyFont(exitButton);
        
        // 添加悬停效果
        exitButton.RegisterCallback<MouseEnterEvent>(evt => {
            exitButton.style.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        });
        
        exitButton.RegisterCallback<MouseLeaveEvent>(evt => {
            exitButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        });
        
        footerContainer.Add(exitButton);
        
        parent.Add(footerContainer);
    }
    
    /// <summary>
    /// 初始化地形系统
    /// </summary>
    void InitializeTerrainSystem()
    {
        Debug.Log("初始化地形系统...");
        
        // 查找现有的地形自动初始化器
        var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
        
        if (terrainAutoInitializer == null)
        {
            // 创建地形自动初始化器
            GameObject terrainInitializerObj = new GameObject("TerrainAutoInitializer");
            terrainAutoInitializer = terrainInitializerObj.AddComponent<TerrainAutoInitializer>();
            Debug.Log("已创建地形自动初始化器");
        }
        
        // 在exe中延迟初始化地形系统
        #if !UNITY_EDITOR
        StartCoroutine(InitializeTerrainSystemDelayed(terrainAutoInitializer));
        #else
        // 在编辑器中直接初始化
        terrainAutoInitializer.InitializeTerrainSystem();
        #endif
        
        Debug.Log("地形系统初始化完成");
    }
    
    /// <summary>
    /// 延迟初始化地形系统（用于exe）
    /// </summary>
    System.Collections.IEnumerator InitializeTerrainSystemDelayed(TerrainAutoInitializer terrainAutoInitializer)
    {
        // 等待一帧，确保所有组件都已初始化
        yield return new WaitForEndOfFrame();
        
        // 在exe中多等待一些时间
        yield return new WaitForSeconds(0.3f);
        
        // 初始化地形系统
        terrainAutoInitializer.InitializeTerrainSystem();
        
        Debug.Log("地形系统延迟初始化完成");
    }
    
    /// <summary>
    /// 确保相机管理器存在
    /// </summary>
    void EnsureCameraManagerExists()
    {
        Debug.Log("确保相机管理器存在...");
        
        // 查找现有的相机管理器
        var cameraManager = FindObjectOfType<CameraManager>();
        
        if (cameraManager == null)
        {
            // 创建相机管理器
            GameObject cameraManagerObj = new GameObject("CameraManager");
            cameraManager = cameraManagerObj.AddComponent<CameraManager>();
            Debug.Log("已创建相机管理器");
        }
        
        // 确保主相机存在
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogWarning("未找到主相机，可能影响跳转功能");
            }
        }
        
        Debug.Log("相机管理器检查完成");
    }
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    void ExitApplication()
    {
        Debug.Log("用户选择退出程序");
        
        #if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建的应用程序中退出
        Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        // 取消事件注册
        if (powerLineExtractorManager != null)
        {
            powerLineExtractorManager.OnStatusChanged -= OnExtractionStatusChanged;
            powerLineExtractorManager.OnExtractionCompleted -= OnExtractionCompleted;
            powerLineExtractorManager.OnError -= OnExtractionError;
        }
    }
    
    /// <summary>
    /// 复制文本到剪贴板
    /// </summary>
    void CopyToClipboard(string text)
    {
        try
        {
            GUIUtility.systemCopyBuffer = text;
            UpdateStatus("安装命令已复制到剪贴板");
            Debug.Log($"已复制到剪贴板: {text}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"复制到剪贴板失败: {ex.Message}");
            UpdateStatus("复制失败，请手动复制");
        }
    }
} 