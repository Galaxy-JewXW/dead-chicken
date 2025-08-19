using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PowerlineSystem;
using UI;
using UserAuth;
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
    public SimpleUserAuth authSystem;
    
    [Header("UI组件")]
    private UIDocument uiDocument;
    private VisualElement rootElement;
    
    // UI元素
    private VisualElement loginPanel;
    private VisualElement registerPanel;
    private VisualElement initialPanel;
    private VisualElement fileUploadArea;
    private VisualElement pythonGuideArea;
    private VisualElement authArea;
    private VisualElement loginFormInAuthArea;
    private VisualElement registerFormInAuthArea;
    private Label statusLabel;
    private ProgressBar progressBar;
    private VisualElement uploadLasButton;
    private Button startExtractionButton;
    
    // 状态
    private bool isInitialized = false;
    private bool isUserLoggedIn = false;
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
        if (authSystem == null)
            authSystem = FindObjectOfType<SimpleUserAuth>();
            
        // 创建独立的UIDocument
        CreateUIDocument();
            
        // 注册事件
        if (powerLineExtractorManager != null)
        {
            powerLineExtractorManager.OnStatusChanged += OnExtractionStatusChanged;
            powerLineExtractorManager.OnExtractionCompleted += OnExtractionCompleted;
            powerLineExtractorManager.OnError += OnExtractionError;
        }
        
        // 注册认证事件
        if (authSystem != null)
        {
            authSystem.OnUserLoggedIn += OnUserLoggedIn;
            authSystem.OnUserLoggedOut += OnUserLoggedOut;
            authSystem.OnAuthMessage += OnAuthMessage;
        }
        
        isInitialized = true;
        Debug.Log("初始界面管理器初始化完成");
        
        // 注意：此时不要立即检查用户登录状态，因为UI界面还在创建中
        // 用户登录状态检查将在SetInitialDisplayState协程完成后进行
    }
    
    /// <summary>
    /// 检查用户登录状态
    /// </summary>
    private void CheckUserLoginStatus()
    {
        if (authSystem != null && authSystem.IsUserLoggedIn())
        {
            isUserLoggedIn = true;
            // 不自动显示主界面，保持当前状态
            Debug.Log("用户已登录，保持当前界面状态");
        }
        else
        {
            isUserLoggedIn = false;
            // 不自动显示登录界面，保持当前状态
            Debug.Log("用户未登录，保持当前界面状态");
        }
    }
    
    /// <summary>
    /// 显示登录界面
    /// </summary>
    private void ShowLoginInterface()
    {
        if (rootElement == null) return;
        
        Debug.Log("正在显示登录界面...");
        
        // 隐藏主界面，显示登录界面
        if (initialPanel != null) 
        {
            initialPanel.style.display = DisplayStyle.None;
            Debug.Log("主界面已隐藏");
        }
        if (loginPanel != null) 
        {
            loginPanel.style.display = DisplayStyle.Flex;
            Debug.Log("登录界面已显示");
        }
        if (registerPanel != null) 
        {
            registerPanel.style.display = DisplayStyle.None;
            Debug.Log("注册界面已隐藏");
        }
        
        
        Debug.Log("登录界面显示完成");
    }
    
    /// <summary>
    /// 显示主界面
    /// </summary>
    private void ShowMainInterface()
    {
        if (rootElement == null) return;
        
        Debug.Log("正在显示主界面...");
        
        // 隐藏登录界面，显示主界面
        if (loginPanel != null) 
        {
            loginPanel.style.display = DisplayStyle.None;
            Debug.Log("登录界面已隐藏");
        }
        if (registerPanel != null) 
        {
            registerPanel.style.display = DisplayStyle.None;
            Debug.Log("注册界面已隐藏");
        }
        if (initialPanel != null) 
        {
            initialPanel.style.display = DisplayStyle.Flex;
            Debug.Log("主界面已显示");
        }
        
        Debug.Log("主界面显示完成");
    }
    
    /// <summary>
    /// 用户登录成功事件
    /// </summary>
    private void OnUserLoggedIn(UserAuth.UserData user)
    {
        isUserLoggedIn = true;
        BackToMainInterface(); // 登录成功后返回主界面
        Debug.Log($"用户 {user.Username} 登录成功，返回主界面");
    }
    
    /// <summary>
    /// 用户登出事件
    /// </summary>
    private void OnUserLoggedOut()
    {
        isUserLoggedIn = false;
        BackToMainInterface(); // 登出后返回主界面
        Debug.Log("用户登出，返回主界面");
    }
    
    /// <summary>
    /// 认证消息事件
    /// </summary>
    private void OnAuthMessage(string message)
    {
        Debug.Log($"[认证系统] {message}");
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
        
        Debug.Log("根元素已设置，开始创建UI界面");
        
        // 创建UI界面
        CreateUI();
        
        Debug.Log("初始界面UIDocument已创建");
    }
    
    /// <summary>
    /// 创建UI界面
    /// </summary>
    void CreateUI()
    {
        Debug.Log("开始创建UI界面...");
        Debug.Log($"rootElement状态: {(rootElement != null ? "已设置" : "为空")}");
        
        // 创建登录面板
        Debug.Log("创建登录面板...");
        CreateLoginPanel();
        Debug.Log($"登录面板创建完成，引用: {(loginPanel != null ? "已设置" : "未设置")}");
        
        // 创建注册面板
        Debug.Log("创建注册面板...");
        CreateRegisterPanel();
        Debug.Log($"注册面板创建完成，引用: {(registerPanel != null ? "已设置" : "未设置")}");
        
        // 创建主界面面板
        Debug.Log("创建主界面面板...");
        CreateInitialInterface();
        Debug.Log($"主界面面板创建完成，引用: {(initialPanel != null ? "已设置" : "未设置")}");
        
        Debug.Log("所有UI界面创建完成");
        
        // 查找UI元素引用
        Debug.Log("开始查找UI元素引用...");
        FindUIElements();
        Debug.Log("UI元素查找完成");
        
        // 等待一帧后设置初始显示状态，确保所有界面都已创建完成
        Debug.Log("启动初始显示状态设置协程...");
        StartCoroutine(SetInitialDisplayState());
    }
    
    /// <summary>
    /// 设置初始显示状态
    /// </summary>
    private System.Collections.IEnumerator SetInitialDisplayState()
    {
        // 等待一帧，确保所有界面都已创建完成
        yield return new WaitForEndOfFrame();
        
        Debug.Log("设置初始显示状态...");
        
        // 检查面板是否已创建
        Debug.Log($"检查面板创建状态:");
        Debug.Log($"主界面面板: {(initialPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"登录面板: {(loginPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"注册面板: {(registerPanel != null ? "已创建" : "未创建")}");
        
        // 始终显示主界面，无论用户是否登录
        if (loginPanel != null) 
        {
            loginPanel.style.display = DisplayStyle.None;
            Debug.Log("登录界面设置为隐藏");
        }
        else
        {
            Debug.LogWarning("登录面板为空");
        }
        
        if (registerPanel != null) 
        {
            registerPanel.style.display = DisplayStyle.None;
            Debug.Log("注册界面设置为隐藏");
        }
        else
        {
            Debug.LogWarning("注册面板为空");
        }
        
        if (initialPanel != null) 
        {
            initialPanel.style.display = DisplayStyle.Flex;
            Debug.Log("主界面设置为可见");
        }
        else
        {
            Debug.LogWarning("主界面面板为空");
        }
        
        // 检查用户登录状态，用于更新主界面的显示内容
        if (authSystem != null)
        {
            bool userAlreadyLoggedIn = authSystem.IsUserLoggedIn();
            isUserLoggedIn = userAlreadyLoggedIn;
            Debug.Log($"用户登录状态: {(userAlreadyLoggedIn ? "已登录" : "未登录")}");
        }
        
        Debug.Log("用户未登录，强制显示主界面");
        
        Debug.Log("初始显示状态设置完成");
    }
    
    /// <summary>
    /// 查找UI元素
    /// </summary>
    void FindUIElements()
    {
        Debug.Log("开始查找UI元素...");
        Debug.Log($"rootElement状态: {(rootElement != null ? "已设置" : "为空")}");
        if (rootElement != null)
        {
            Debug.Log($"根元素子元素数量: {rootElement.childCount}");
            foreach (var child in rootElement.Children())
            {
                Debug.Log($"子元素: {child.name}, 类型: {child.GetType()}");
            }
        }
        
        // 检查面板是否已创建（避免覆盖已创建的引用）
        Debug.Log($"面板创建状态检查:");
        Debug.Log($"登录面板: {(loginPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"注册面板: {(registerPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"主界面面板: {(initialPanel != null ? "已创建" : "未创建")}");
        
        // 如果面板未创建，则尝试查找
        if (loginPanel == null)
        {
            loginPanel = rootElement.Q<VisualElement>("login-panel");
            Debug.Log($"登录面板查找结果: {(loginPanel != null ? "成功" : "失败")}");
        }
        
        if (registerPanel == null)
        {
            registerPanel = rootElement.Q<VisualElement>("register-panel");
            Debug.Log($"注册面板查找结果: {(registerPanel != null ? "成功" : "失败")}");
        }
        
        if (initialPanel == null)
        {
            initialPanel = rootElement.Q<VisualElement>("initial-panel");
            Debug.Log($"主界面面板查找结果: {(initialPanel != null ? "成功" : "失败")}");
        }
        
        // 查找其他UI元素
        fileUploadArea = rootElement.Q<VisualElement>("file-upload-area");
        pythonGuideArea = rootElement.Q<VisualElement>("python-guide-area");
        statusLabel = rootElement.Q<Label>("status-label");
        // progressBar 现在通过代码创建，不需要查询
        uploadLasButton = rootElement.Q<VisualElement>("upload-las-button");
        startExtractionButton = rootElement.Q<Button>("start-extraction-button");
        
        Debug.Log("UI元素查找完成");
    }
    
    /// <summary>
    /// 创建登录面板
    /// </summary>
    void CreateLoginPanel()
    {
        loginPanel = new VisualElement();
        loginPanel.name = "login-panel";
        loginPanel.style.position = Position.Absolute;
        loginPanel.style.top = 0;
        loginPanel.style.left = 0;
        loginPanel.style.right = 0;
        loginPanel.style.bottom = 0;
        loginPanel.style.width = Length.Percent(100);
        loginPanel.style.height = Length.Percent(100);
        loginPanel.style.backgroundColor = backgroundColor;
        loginPanel.style.flexDirection = FlexDirection.Column;
        loginPanel.style.justifyContent = Justify.Center;
        loginPanel.style.alignItems = Align.Center;
        loginPanel.style.paddingTop = 50;
        loginPanel.style.paddingBottom = 50;
        loginPanel.style.paddingLeft = 50;
        loginPanel.style.paddingRight = 50;
        
        // 添加背景装饰，让登录界面更美观
        CreateBackgroundDecoration(loginPanel);
        
        // 创建登录表单
        CreateLoginForm(loginPanel);
        
        // 添加到根元素
        if (rootElement != null)
        {
            rootElement.Add(loginPanel);
            Debug.Log("登录面板已添加到根元素");
        }
        else
        {
            Debug.LogError("根元素为空，无法添加登录面板");
        }
    }
    
    /// <summary>
    /// 创建注册面板
    /// </summary>
    void CreateRegisterPanel()
    {
        registerPanel = new VisualElement();
        registerPanel.name = "register-panel";
        registerPanel.style.position = Position.Absolute;
        registerPanel.style.top = 0;
        registerPanel.style.left = 0;
        registerPanel.style.right = 0;
        registerPanel.style.bottom = 0;
        registerPanel.style.width = Length.Percent(100);
        registerPanel.style.height = Length.Percent(100);
        registerPanel.style.backgroundColor = backgroundColor;
        registerPanel.style.flexDirection = FlexDirection.Column;
        registerPanel.style.justifyContent = Justify.Center;
        registerPanel.style.alignItems = Align.Center;
        registerPanel.style.paddingTop = 50;
        registerPanel.style.paddingBottom = 50;
        registerPanel.style.paddingLeft = 50;
        registerPanel.style.paddingRight = 50;
        
        // 添加背景装饰，让注册界面更美观
        CreateBackgroundDecoration(registerPanel);
        
        // 创建注册表单
        CreateRegisterForm(registerPanel);
        
        // 添加到根元素
        if (rootElement != null)
        {
            rootElement.Add(registerPanel);
            Debug.Log("注册面板已添加到根元素");
        }
        else
        {
            Debug.LogError("根元素为空，无法添加注册面板");
        }
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
        Debug.Log("开始创建初始界面...");
        Debug.Log($"rootElement状态: {(rootElement != null ? "已设置" : "为空")}");
        
        // 创建主面板 - 全屏显示
        initialPanel = new VisualElement();
        initialPanel.name = "initial-panel";
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
        
        Debug.Log("初始面板样式设置完成");
        
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
        
        // 创建登录/注册区域（作为主界面的子元素）
        CreateAuthArea(initialPanel);
        
        // 创建状态显示区域，包含进度条等重要元素
        CreateStatusArea(initialPanel);
        
        // 创建底部信息
        CreateFooterInfo(initialPanel);
        
        Debug.Log("初始界面所有组件创建完成，准备添加到根元素");
        
        // 添加到独立的根元素
        if (rootElement != null)
        {
            rootElement.Add(initialPanel);
            Debug.Log($"初始界面已创建，根元素子元素数量: {rootElement.childCount}");
        }
        else
        {
            Debug.LogError("根元素为空，无法添加初始界面");
            Debug.LogError($"uidocument状态: {(uiDocument != null ? "已设置" : "为空")}");
            if (uiDocument != null)
            {
                Debug.LogError($"uidocument.rootVisualElement状态: {(uiDocument.rootVisualElement != null ? "已设置" : "为空")}");
            }
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
        
        // 分隔符3
        var separator3 = new VisualElement();
        separator3.style.width = 25;
        separator3.style.height = 200;
        selectionContainer.Add(separator3);
        
        // 用户认证按钮
        var authButton = CreateOptionButton(
            "用户认证",
            "登录或注册账户\n管理您的个人信息\n获取个性化服务",
            "🔐",
            () => OnAuthButtonClicked()
        );
        selectionContainer.Add(authButton);
        
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
        var backButton = new Button(() => ReturnToMainInterface());
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
        pythonGuideArea.name = "python-guide-area";
        pythonGuideArea.style.display = DisplayStyle.None;
        pythonGuideArea.style.flexDirection = FlexDirection.Column;
        pythonGuideArea.style.alignItems = Align.Center;
        pythonGuideArea.style.justifyContent = Justify.Center;
        pythonGuideArea.style.width = Length.Percent(100);
        pythonGuideArea.style.height = Length.Percent(100);
        pythonGuideArea.style.paddingTop = 50;
        pythonGuideArea.style.paddingBottom = 50;
        pythonGuideArea.style.paddingLeft = 50;
        pythonGuideArea.style.paddingRight = 50;
        
        // 添加背景装饰
        CreateBackgroundDecoration(pythonGuideArea);
        
        // 创建Python引导标题
        var pythonTitle = new Label("Python环境配置向导");
        pythonTitle.style.color = primaryColor;
        pythonTitle.style.marginBottom = 30;
        pythonTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(pythonTitle, FontSize.LargeTitle);
        pythonGuideArea.Add(pythonTitle);
        
        // 创建Python环境检查按钮
        var checkPythonButton = new Button(() => CheckPythonEnvironment()) { text = "检查Python环境" };
        checkPythonButton.style.width = 300;
        checkPythonButton.style.height = 50;
        checkPythonButton.style.backgroundColor = primaryColor;
        checkPythonButton.style.color = Color.white;
        checkPythonButton.style.borderTopLeftRadius = 8;
        checkPythonButton.style.borderTopRightRadius = 8;
        checkPythonButton.style.borderBottomLeftRadius = 8;
        checkPythonButton.style.borderBottomRightRadius = 8;
        checkPythonButton.style.marginBottom = 20;
        ApplyFont(checkPythonButton, FontSize.Title);
        pythonGuideArea.Add(checkPythonButton);
        
        // 创建返回主界面按钮
        var returnToMainButton = new Button(() => ReturnToMainInterface()) { text = "返回主界面" };
        returnToMainButton.style.width = 300;
        returnToMainButton.style.height = 50;
        returnToMainButton.style.backgroundColor = Color.clear;
        returnToMainButton.style.color = primaryColor;
        returnToMainButton.style.borderLeftWidth = 2;
        returnToMainButton.style.borderRightWidth = 2;
        returnToMainButton.style.borderTopWidth = 2;
        returnToMainButton.style.borderBottomWidth = 2;
        returnToMainButton.style.borderLeftColor = primaryColor;
        returnToMainButton.style.borderRightColor = primaryColor;
        returnToMainButton.style.borderTopColor = primaryColor;
        returnToMainButton.style.borderBottomColor = primaryColor;
        returnToMainButton.style.borderTopLeftRadius = 8;
        returnToMainButton.style.borderTopRightRadius = 8;
        returnToMainButton.style.borderBottomLeftRadius = 8;
        returnToMainButton.style.borderBottomRightRadius = 8;
        ApplyFont(returnToMainButton, FontSize.Title);
        pythonGuideArea.Add(returnToMainButton);
        
        parent.Add(pythonGuideArea);
    }
    
    /// <summary>
    /// 创建登录/注册区域（作为主界面的子元素）
    /// </summary>
    void CreateAuthArea(VisualElement parent)
    {
        authArea = new VisualElement();
        authArea.name = "auth-area";
        authArea.style.display = DisplayStyle.None;
        authArea.style.flexDirection = FlexDirection.Column;
        authArea.style.alignItems = Align.Center;
        authArea.style.justifyContent = Justify.Center;
        authArea.style.width = Length.Percent(100);
        authArea.style.height = Length.Percent(100);
        authArea.style.paddingTop = 20;
        authArea.style.paddingBottom = 20;
        authArea.style.paddingLeft = 20;
        authArea.style.paddingRight = 20;
        
        // 添加背景装饰
        CreateBackgroundDecoration(authArea);
        
        // 创建主认证卡片容器
        var authCard = new VisualElement();
        authCard.name = "auth-card";
        authCard.style.width = 420;
        authCard.style.backgroundColor = new Color(1f, 1f, 1f, 0.95f);
        authCard.style.borderTopLeftRadius = 20;
        authCard.style.borderTopRightRadius = 20;
        authCard.style.borderBottomLeftRadius = 20;
        authCard.style.borderBottomRightRadius = 20;
        authCard.style.paddingTop = 40;
        authCard.style.paddingBottom = 40;
        authCard.style.paddingLeft = 40;
        authCard.style.paddingRight = 40;
        // Unity UI Toolkit 不支持 boxShadow，使用其他方式创建阴影效果
        // authCard.style.boxShadow = new StyleBoxShadow(
        //     new Color(0f, 0f, 0f, 0.1f), 
        //     new Vector2(0, 10), 
        //     30, 
        //     new Color(0f, 0f, 0f, 0.1f)
        // );
        authCard.style.borderLeftWidth = 1;
        authCard.style.borderRightWidth = 1;
        authCard.style.borderTopWidth = 1;
        authCard.style.borderBottomWidth = 1;
        authCard.style.borderLeftColor = new Color(1f, 1f, 1f, 0.3f);
        authCard.style.borderRightColor = new Color(1f, 1f, 1f, 0.3f);
        authCard.style.borderTopColor = new Color(1f, 1f, 1f, 0.3f);
        authCard.style.borderBottomColor = new Color(1f, 1f, 1f, 0.3f);
        
        // 创建认证区域标题容器
        var titleContainer = new VisualElement();
        titleContainer.style.flexDirection = FlexDirection.Row;
        titleContainer.style.alignItems = Align.Center;
        titleContainer.style.justifyContent = Justify.Center;
        titleContainer.style.marginBottom = 35;
        titleContainer.style.marginTop = 10;
        
        // 添加一个小图标装饰
        var iconContainer = new VisualElement();
        iconContainer.style.width = 40;
        iconContainer.style.height = 40;
        iconContainer.style.borderTopLeftRadius = 20;
        iconContainer.style.borderTopRightRadius = 20;
        iconContainer.style.borderBottomLeftRadius = 20;
        iconContainer.style.borderBottomRightRadius = 20;
        iconContainer.style.backgroundColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.1f);
        iconContainer.style.borderLeftWidth = 2;
        iconContainer.style.borderRightWidth = 2;
        iconContainer.style.borderTopWidth = 2;
        iconContainer.style.borderBottomWidth = 2;
        iconContainer.style.borderLeftColor = primaryColor;
        iconContainer.style.borderRightColor = primaryColor;
        iconContainer.style.borderTopColor = primaryColor;
        iconContainer.style.borderBottomColor = primaryColor;
        iconContainer.style.marginRight = 15;
        
        // 在图标中添加一个简单的用户符号
        var userSymbol = new Label("👤");
        userSymbol.style.fontSize = 20;
        userSymbol.style.unityTextAlign = TextAnchor.MiddleCenter;
        userSymbol.style.color = primaryColor;
        userSymbol.style.marginTop = 8;
        iconContainer.Add(userSymbol);
        
        titleContainer.Add(iconContainer);
        
        // 创建认证区域标题
        var authTitle = new Label("用户认证");
        authTitle.style.color = primaryColor;
        authTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        authTitle.style.fontSize = 28;
        authTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(authTitle, FontSize.LargeTitle);
        titleContainer.Add(authTitle);
        
        authCard.Add(titleContainer);
        
        // 创建登录表单
        CreateLoginFormInAuthArea(authCard);
        
        // 创建注册表单
        CreateRegisterFormInAuthArea(authCard);
        
        // 创建返回主界面按钮
        var returnToMainButton = new Button(() => BackToMainInterface()) { text = "返回主界面" };
        returnToMainButton.style.width = 340;
        returnToMainButton.style.height = 45;
        returnToMainButton.style.backgroundColor = Color.clear;
        returnToMainButton.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        returnToMainButton.style.borderLeftWidth = 2;
        returnToMainButton.style.borderRightWidth = 2;
        returnToMainButton.style.borderTopWidth = 2;
        returnToMainButton.style.borderBottomWidth = 2;
        returnToMainButton.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        returnToMainButton.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        returnToMainButton.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        returnToMainButton.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        returnToMainButton.style.borderTopLeftRadius = 12;
        returnToMainButton.style.borderTopRightRadius = 12;
        returnToMainButton.style.borderBottomLeftRadius = 12;
        returnToMainButton.style.borderBottomRightRadius = 12;
        returnToMainButton.style.marginTop = 25;
        returnToMainButton.style.fontSize = 16;
        returnToMainButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(returnToMainButton, FontSize.Body);
        
        // 添加悬停效果
        returnToMainButton.RegisterCallback<MouseEnterEvent>(evt => {
            returnToMainButton.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            returnToMainButton.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            returnToMainButton.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            returnToMainButton.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            returnToMainButton.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            returnToMainButton.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        });
        
        returnToMainButton.RegisterCallback<MouseLeaveEvent>(evt => {
            returnToMainButton.style.backgroundColor = Color.clear;
            returnToMainButton.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            returnToMainButton.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            returnToMainButton.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            returnToMainButton.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            returnToMainButton.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        });
        
        authCard.Add(returnToMainButton);
        
        authArea.Add(authCard);
        parent.Add(authArea);
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
        
        // 隐藏认证区域（如果正在显示）
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
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
    /// 用户认证按钮点击事件
    /// </summary>
    void OnAuthButtonClicked()
    {
        Debug.Log("用户点击了认证按钮，准备显示认证界面");
        
        // 检查面板状态
        Debug.Log($"主界面面板状态: {(initialPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"认证区域状态: {(authArea != null ? "已创建" : "未创建")}");
        
        if (rootElement != null)
        {
            Debug.Log($"根元素子元素数量: {rootElement.childCount}");
            foreach (var child in rootElement.Children())
            {
                Debug.Log($"子元素: {child.name}, 类型: {child.GetType()}, 显示状态: {child.style.display}");
            }
        }
        
        // 参考其他按钮的实现方式：隐藏选择按钮区域，显示认证区域
        HideSelectionButtons();
        
        // 显示认证区域
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.Flex;
            Debug.Log("认证区域已显示");
        }
        else
        {
            Debug.LogError("认证区域为空，无法显示");
        }
        
        UpdateStatus("请登录或注册您的账户");
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
        
        // 检查sceneInitializer是否存在
        if (sceneInitializer == null)
        {
            Debug.LogError("SceneInitializer未找到，无法加载电塔数据");
            UpdateStatus("错误：SceneInitializer未找到");
            yield break;
        }
        
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
        
        // 检查sceneInitializer是否存在
        if (sceneInitializer == null)
        {
            Debug.LogError("SceneInitializer未找到，无法加载电塔数据");
            UpdateStatus("错误：SceneInitializer未找到");
            yield break;
        }
        
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
        
        // 隐藏认证区域（如果正在显示）
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
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
        
        // 隐藏认证区域（如果正在显示）
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
        }
        
        UpdateStatus("Python环境配置向导");
    }
    
    /// <summary>
    /// 返回主界面（从其他区域返回）
    /// </summary>
    void ReturnToMainInterface()
    {
        Debug.Log("用户选择返回主界面");
        
        // 隐藏文件上传区域
        fileUploadArea.style.display = DisplayStyle.None;
        
        // 隐藏Python引导区域
        if (pythonGuideArea != null)
        {
            pythonGuideArea.style.display = DisplayStyle.None;
        }
        
        // 隐藏认证区域
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
        }
        
        // 显示选择按钮区域（只显示选择按钮，不显示认证区域）
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
        
        // 隐藏认证区域
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
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
        // 这些变量在后续版本中可能会用到，暂时注释掉以避免警告
        // bool laspyAvailable = false;
        // bool numpyAvailable = false;
        // bool scipyAvailable = false;
        // bool sklearnAvailable = false;
        
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
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 认证区域(5) -> 状态(6) -> 底部(7)
            
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
            
            // 隐藏认证区域（第六个子元素，索引为5）
            if (initialPanel.childCount > 5)
            {
                var authContainer = initialPanel[5];
                if (authContainer != null)
                {
                    authContainer.style.display = DisplayStyle.None;
                    Debug.Log("已隐藏认证区域");
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
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 认证区域(5) -> 状态(6) -> 底部(7)
            
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
            
            // 显示认证区域（第六个子元素，索引为5）
            if (initialPanel.childCount > 5)
            {
                var authContainer = initialPanel[5];
                if (authContainer != null)
                {
                    authContainer.style.display = DisplayStyle.Flex;
                    Debug.Log("已显示认证区域");
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
            // 界面结构：背景装饰(0) -> 标题区域(1) -> 选择区域(2) -> 文件上传(3) -> Python引导(4) -> 认证区域(5) -> 状态(6) -> 底部(7)
            
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
            
            // 隐藏认证区域（第六个子元素，索引为5）
            if (initialPanel.childCount > 5)
            {
                var authContainer = initialPanel[5];
                if (authContainer != null)
                {
                    authContainer.style.display = DisplayStyle.None;
                    Debug.Log("已隐藏认证区域（仅选择区域）");
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
        
        // 检查进度条是否已初始化
        if (progressBar == null)
        {
            Debug.LogError("进度条未初始化，无法开始预览");
            UpdateStatus("错误：UI组件未正确初始化");
            yield break;
        }
        
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
            if (progressBar != null)
            {
                progressBar.value = 50f;
            }
        }
        else
        {
            // 先更新状态，再开始转换
            if (progressBar != null)
            {
                progressBar.value = 10f;
            }
            UpdateStatus("正在转换LAS文件为OFF格式...");
            
            // 等待一帧确保状态更新显示
            yield return null;
            
            if (progressBar != null)
            {
                progressBar.value = 15f;
            }
            UpdateStatus("检查转换依赖...");
            
            if (!PowerlineSystem.LasToOffConverter.CheckDependencies())
            {
                errorMessage = "Python或laspy库未安装";
                UpdateStatus($"错误：{errorMessage}");
                Debug.LogError("LAS到OFF转换依赖检查失败");
                isProcessing = false;
                if (progressBar != null)
                {
                    progressBar.style.display = DisplayStyle.None;
                }
                yield break;
            }
            
            if (progressBar != null)
            {
                progressBar.value = 20f;
            }
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
                if (progressBar != null)
                {
                    progressBar.style.display = DisplayStyle.None;
                }
                yield break;
            }
            
            if (string.IsNullOrEmpty(convertedPath))
            {
                errorMessage = "LAS文件转换失败";
                UpdateStatus($"错误：{errorMessage}");
                Debug.LogError("LAS到OFF转换失败");
                isProcessing = false;
                if (progressBar != null)
                {
                    progressBar.style.display = DisplayStyle.None;
                }
                yield break;
            }
            
            if (progressBar != null)
            {
                progressBar.value = 80f;
            }
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
            if (progressBar != null)
            {
                progressBar.style.display = DisplayStyle.None;
            }
            yield break;
        }
        
        if (progressBar != null)
        {
            progressBar.value = 90f;
        }
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
        
        if (progressBar != null)
        {
            progressBar.value = 100f;
        }
        UpdateStatus($"点云预览已打开: {fileName}");
        Debug.Log($"点云预览已启动，文件: {fileName}");
        
        conversionSuccess = true;
        needFinalWait = true;
        
        // 清理处理状态
        isProcessing = false;
        if (progressBar != null)
        {
            progressBar.style.display = DisplayStyle.None;
        }
        
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
        
        // 检查必要的组件是否存在
        if (progressBar == null)
        {
            Debug.LogError("进度条未初始化");
            UpdateStatus("错误：UI组件未正确初始化");
            return;
        }
        
        if (startExtractionButton == null)
        {
            Debug.LogError("开始提取按钮未初始化");
            UpdateStatus("错误：UI组件未正确初始化");
            return;
        }
        
        isProcessing = true;
        if (progressBar != null)
        {
            progressBar.style.display = DisplayStyle.Flex;
        }
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
            if (progressBar != null)
            {
                progressBar.style.display = DisplayStyle.None;
            }
        }
    }
    
    void OnExtractionStatusChanged(string status)
    {
        UpdateStatus(status);
    }
    
    void OnExtractionCompleted(string csvPath)
    {
        UpdateStatus("电力线提取完成！正在构建场景...");
        if (progressBar != null)
        {
            progressBar.value = 100f;
        }
        
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
            // 检查sceneInitializer是否存在
            if (sceneInitializer == null)
            {
                Debug.LogError("SceneInitializer未找到，无法构建场景");
                UpdateStatus("错误：SceneInitializer未找到");
                return;
            }
            
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
        if (progressBar != null)
        {
            progressBar.style.display = DisplayStyle.None;
        }
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
    
    /// <summary>
    /// 应用字体到文本输入框
    /// </summary>
    void ApplyFont(TextField textField, FontSize size = FontSize.Body)
    {
        // 直接应用字体，因为FontManager没有TextField的ApplyFont方法
        if (uiFont != null)
        {
            textField.style.unityFont = uiFont;
        }
        else
        {
            var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
            {
                textField.style.unityFont = builtinFont;
            }
        }
        
        // 应用字体大小
        switch (size)
        {
            case FontSize.LargeTitle:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.largeTitleSize : 48;
                break;
            case FontSize.Title:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.titleSize : 24;
                break;
            case FontSize.Subtitle:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.subtitleSize : 18;
                break;
            case FontSize.Body:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.bodySize : 16;
                break;
            case FontSize.Small:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.smallSize : 14;
                break;
            case FontSize.Tiny:
                textField.style.fontSize = FontManager.Instance != null ? FontManager.Instance.tinySize : 12;
                break;
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
        
        // 添加装饰性圆圈 - 更美观的布局
        var circlePositions = new[] {
            new { width = 150, top = 10, left = 10, alpha = 0.06f, color = primaryColor },
            new { width = 200, top = 20, left = 80, alpha = 0.04f, color = accentColor },
            new { width = 100, top = 70, left = 5, alpha = 0.08f, color = primaryColor },
            new { width = 180, top = 15, left = 70, alpha = 0.05f, color = secondaryColor },
            new { width = 120, top = 60, left = 85, alpha = 0.07f, color = accentColor },
            new { width = 220, top = 85, left = 15, alpha = 0.03f, color = primaryColor },
            new { width = 90, top = 35, left = 95, alpha = 0.09f, color = secondaryColor },
            new { width = 170, top = 45, left = 20, alpha = 0.06f, color = accentColor },
            new { width = 140, top = 90, left = 75, alpha = 0.05f, color = primaryColor }
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
            circle.style.backgroundColor = new Color(pos.color.r, pos.color.g, pos.color.b, pos.alpha);
            circle.style.top = pos.top;
            circle.style.left = pos.left;
            decorationContainer.Add(circle);
        }
        
        // 添加装饰性线条 - 更优雅的设计
        var linePositions = new[] {
            new { width = 180, top = 30, left = 25, height = 2, alpha = 0.4f, color = primaryColor },
            new { width = 140, top = 80, left = 75, height = 2, alpha = 0.3f, color = accentColor },
            new { width = 200, top = 50, left = 8, height = 2, alpha = 0.35f, color = secondaryColor },
            new { width = 120, top = 95, left = 60, height = 2, alpha = 0.25f, color = primaryColor }
        };
        
        for (int i = 0; i < linePositions.Length; i++)
        {
            var pos = linePositions[i];
            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.width = pos.width;
            line.style.height = pos.height;
            line.style.backgroundColor = new Color(pos.color.r, pos.color.g, pos.color.b, pos.alpha);
            line.style.top = pos.top;
            line.style.left = pos.left;
            line.style.borderTopLeftRadius = 1;
            line.style.borderTopRightRadius = 1;
            line.style.borderBottomLeftRadius = 1;
            line.style.borderBottomRightRadius = 1;
            decorationContainer.Add(line);
        }
        
        // 添加一些小的装饰点
        var dotPositions = new[] {
            new { size = 6, top = 25, left = 45, alpha = 0.6f, color = accentColor },
            new { size = 8, top = 55, left = 90, alpha = 0.5f, color = primaryColor },
            new { size = 5, top = 85, left = 35, alpha = 0.7f, color = secondaryColor },
            new { size = 7, top = 15, left = 65, alpha = 0.55f, color = accentColor }
        };
        
        for (int i = 0; i < dotPositions.Length; i++)
        {
            var pos = dotPositions[i];
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.width = pos.size;
            dot.style.height = pos.size;
            dot.style.borderTopLeftRadius = 50;
            dot.style.borderTopRightRadius = 50;
            dot.style.borderBottomLeftRadius = 50;
            dot.style.borderBottomRightRadius = 50;
            dot.style.backgroundColor = new Color(pos.color.r, pos.color.g, pos.color.b, pos.alpha);
            dot.style.top = pos.top;
            dot.style.left = pos.left;
            decorationContainer.Add(dot);
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
    
    /// <summary>
    /// 创建登录表单
    /// </summary>
    void CreateLoginForm(VisualElement parent)
    {
        // 创建标题
        var titleLabel = new Label("用户登录");
        titleLabel.style.fontSize = 32;
        titleLabel.style.color = primaryColor;
        titleLabel.style.marginBottom = 30;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        parent.Add(titleLabel);
        
        // 创建用户名输入框
        var usernameField = new TextField("用户名");
        usernameField.name = "login-username-field";
        usernameField.style.width = 300;
        usernameField.style.marginBottom = 20;
        parent.Add(usernameField);
        
        // 创建密码输入框
        var passwordField = new TextField("密码");
        passwordField.name = "login-password-field";
        passwordField.isPasswordField = true;
        passwordField.style.width = 300;
        passwordField.style.marginBottom = 30;
        parent.Add(passwordField);
        
        // 创建登录按钮
        var loginButton = new Button(() => OnLoginButtonClicked()) { text = "登录" };
        loginButton.name = "login-button";
        loginButton.style.width = 300;
        loginButton.style.height = 40;
        loginButton.style.backgroundColor = primaryColor;
        loginButton.style.color = Color.white;
        loginButton.style.borderTopLeftRadius = 5;
        loginButton.style.borderTopRightRadius = 5;
        loginButton.style.borderBottomLeftRadius = 5;
        loginButton.style.borderBottomRightRadius = 5;
        loginButton.style.marginBottom = 20;
        parent.Add(loginButton);
        
        // 创建切换到注册的按钮
        var switchToRegisterButton = new Button(() => ShowRegisterPanel()) { text = "没有账号？点击注册" };
        switchToRegisterButton.name = "switch-to-register-button";
        switchToRegisterButton.style.width = 300;
        switchToRegisterButton.style.height = 30;
        switchToRegisterButton.style.backgroundColor = Color.clear;
        switchToRegisterButton.style.color = primaryColor;
        switchToRegisterButton.style.borderLeftWidth = 1;
        switchToRegisterButton.style.borderRightWidth = 1;
        switchToRegisterButton.style.borderTopWidth = 1;
        switchToRegisterButton.style.borderBottomWidth = 1;
        switchToRegisterButton.style.borderLeftColor = primaryColor;
        switchToRegisterButton.style.borderRightColor = primaryColor;
        switchToRegisterButton.style.borderTopColor = primaryColor;
        switchToRegisterButton.style.borderBottomColor = primaryColor;
        switchToRegisterButton.style.borderTopLeftRadius = 5;
        switchToRegisterButton.style.borderTopRightRadius = 5;
        switchToRegisterButton.style.borderBottomLeftRadius = 5;
        switchToRegisterButton.style.borderBottomRightRadius = 5;
        parent.Add(switchToRegisterButton);
        
        // 创建返回主界面的按钮
        var backToMainButton = new Button(() => BackToMainInterface()) { text = "返回主界面" };
        backToMainButton.name = "back-to-main-button";
        backToMainButton.style.width = 300;
        backToMainButton.style.height = 30;
        backToMainButton.style.backgroundColor = Color.clear;
        backToMainButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderLeftWidth = 1;
        backToMainButton.style.borderRightWidth = 1;
        backToMainButton.style.borderTopWidth = 1;
        backToMainButton.style.borderBottomWidth = 1;
        backToMainButton.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderTopLeftRadius = 5;
        backToMainButton.style.borderTopRightRadius = 5;
        backToMainButton.style.borderBottomLeftRadius = 5;
        backToMainButton.style.borderBottomRightRadius = 5;
        parent.Add(backToMainButton);
    }
    
    /// <summary>
    /// 创建注册表单
    /// </summary>
    void CreateRegisterForm(VisualElement parent)
    {
        // 创建标题
        var titleLabel = new Label("用户注册");
        titleLabel.style.fontSize = 32;
        titleLabel.style.color = primaryColor;
        titleLabel.style.marginBottom = 30;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        parent.Add(titleLabel);
        
        // 创建用户名输入框
        var usernameField = new TextField("用户名");
        usernameField.name = "register-username-field";
        usernameField.style.width = 300;
        usernameField.style.marginBottom = 20;
        parent.Add(usernameField);
        

        
        // 创建密码输入框
        var passwordField = new TextField("密码");
        passwordField.name = "register-password-field";
        passwordField.isPasswordField = true;
        passwordField.style.width = 300;
        passwordField.style.marginBottom = 20;
        parent.Add(passwordField);
        
        // 创建确认密码输入框
        var confirmPasswordField = new TextField("确认密码");
        confirmPasswordField.name = "register-confirm-password-field";
        confirmPasswordField.isPasswordField = true;
        confirmPasswordField.style.width = 300;
        confirmPasswordField.style.marginBottom = 30;
        parent.Add(confirmPasswordField);
        
        // 创建注册按钮
        var registerButton = new Button(() => OnRegisterButtonClicked()) { text = "注册" };
        registerButton.name = "register-button";
        registerButton.style.width = 300;
        registerButton.style.height = 40;
        registerButton.style.backgroundColor = accentColor;
        registerButton.style.color = Color.white;
        registerButton.style.borderTopLeftRadius = 5;
        registerButton.style.borderTopRightRadius = 5;
        registerButton.style.borderBottomLeftRadius = 5;
        registerButton.style.borderBottomRightRadius = 5;
        registerButton.style.marginBottom = 20;
        parent.Add(registerButton);
        
        // 创建切换到登录的按钮
        var switchToLoginButton = new Button(() => ShowLoginPanel()) { text = "已有账号？点击登录" };
        switchToLoginButton.name = "switch-to-login-button";
        switchToLoginButton.style.width = 300;
        switchToLoginButton.style.height = 30;
        switchToLoginButton.style.backgroundColor = Color.clear;
        switchToLoginButton.style.color = accentColor;
        switchToLoginButton.style.borderLeftWidth = 1;
        switchToLoginButton.style.borderRightWidth = 1;
        switchToLoginButton.style.borderTopWidth = 1;
        switchToLoginButton.style.borderBottomWidth = 1;
        switchToLoginButton.style.borderLeftColor = accentColor;
        switchToLoginButton.style.borderRightColor = accentColor;
        switchToLoginButton.style.borderTopColor = accentColor;
        switchToLoginButton.style.borderBottomColor = accentColor;
        switchToLoginButton.style.borderTopLeftRadius = 5;
        switchToLoginButton.style.borderTopRightRadius = 5;
        switchToLoginButton.style.borderBottomLeftRadius = 5;
        switchToLoginButton.style.borderBottomRightRadius = 5;
        parent.Add(switchToLoginButton);
        
        // 创建返回主界面的按钮
        var backToMainButton = new Button(() => BackToMainInterface()) { text = "返回主界面" };
        backToMainButton.name = "back-to-main-button";
        backToMainButton.style.width = 300;
        backToMainButton.style.height = 30;
        backToMainButton.style.backgroundColor = Color.clear;
        backToMainButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderLeftWidth = 1;
        backToMainButton.style.borderRightWidth = 1;
        backToMainButton.style.borderTopWidth = 1;
        backToMainButton.style.borderBottomWidth = 1;
        backToMainButton.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        backToMainButton.style.borderTopLeftRadius = 5;
        backToMainButton.style.borderTopRightRadius = 5;
        backToMainButton.style.borderBottomLeftRadius = 5;
        backToMainButton.style.borderBottomRightRadius = 5;
        parent.Add(backToMainButton);
    }
    
    /// <summary>
    /// 显示注册面板
    /// </summary>
    private void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.style.display = DisplayStyle.None;
        if (registerPanel != null) registerPanel.style.display = DisplayStyle.Flex;
        Debug.Log("切换到注册面板");
    }
    
    /// <summary>
    /// 显示登录面板
    /// </summary>
    private void ShowLoginPanel()
    {
        if (registerPanel != null) registerPanel.style.display = DisplayStyle.None;
        if (loginPanel != null) loginPanel.style.display = DisplayStyle.Flex;
        Debug.Log("切换到登录面板");
    }
    
    /// <summary>
    /// 返回主界面
    /// </summary>
    private void BackToMainInterface()
    {
        Debug.Log("用户点击返回主界面按钮");
        
        // 隐藏认证区域
        if (authArea != null)
        {
            authArea.style.display = DisplayStyle.None;
            Debug.Log("认证区域已隐藏");
        }
        
        // 显示选择按钮区域（只显示选择按钮，不显示认证区域）
        ShowSelectionButtonsOnly();
        
        UpdateStatus("欢迎使用电力线可视化系统");
    }
    
    /// <summary>
    /// 登录按钮点击事件
    /// </summary>
    private void OnLoginButtonClicked()
    {
        if (authSystem == null) return;
        
        var usernameField = rootElement.Q<TextField>("login-username-field");
        var passwordField = rootElement.Q<TextField>("login-password-field");
        
        if (usernameField == null || passwordField == null) return;
        
        string username = usernameField.value;
        string password = passwordField.value;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("用户名和密码不能为空");
            return;
        }
        
        // 调用认证系统登录
        bool success = authSystem.LoginUser(username, password);
        
        if (success)
        {
            Debug.Log("登录成功");
        }
    }
    
    /// <summary>
    /// 注册按钮点击事件
    /// </summary>
    private void OnRegisterButtonClicked()
    {
        if (authSystem == null) return;
        
        var usernameField = rootElement.Q<TextField>("register-username-field");
        var passwordField = rootElement.Q<TextField>("register-password-field");
        var confirmPasswordField = rootElement.Q<TextField>("register-confirm-password-field");
        
        if (usernameField == null || passwordField == null || confirmPasswordField == null) return;
        
        string username = usernameField.value;
        string password = passwordField.value;
        string confirmPassword = confirmPasswordField.value;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("请填写用户名和密码");
            return;
        }
        
        if (password != confirmPassword)
        {
            Debug.LogWarning("两次输入的密码不一致");
            return;
        }
        
        // 调用认证系统注册
        bool success = authSystem.RegisterUser(username, password);
        
        if (success)
        {
            Debug.Log("注册成功，请登录");
            ShowLoginPanel();
        }
    }
    
    /// <summary>
    /// 在认证区域中创建登录表单
    /// </summary>
    void CreateLoginFormInAuthArea(VisualElement parent)
    {
        var loginForm = new VisualElement();
        loginForm.name = "login-form";
        loginForm.style.display = DisplayStyle.Flex;
        loginForm.style.flexDirection = FlexDirection.Column;
        loginForm.style.alignItems = Align.Center;
        loginForm.style.marginBottom = 25;
        
        // 用户名标签
        var usernameLabel = new Label("用户名");
        usernameLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        usernameLabel.style.fontSize = 14;
        usernameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        usernameLabel.style.marginBottom = 8;
        usernameLabel.style.alignSelf = Align.FlexStart;
        usernameLabel.style.marginLeft = 10;
        ApplyFont(usernameLabel, FontSize.Small);
        loginForm.Add(usernameLabel);
        
        // 用户名输入框
        var usernameField = new TextField();
        usernameField.name = "username-field";
        usernameField.style.width = 340;
        usernameField.style.height = 50;
        usernameField.style.marginBottom = 20;
        usernameField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        usernameField.style.borderTopLeftRadius = 12;
        usernameField.style.borderTopRightRadius = 12;
        usernameField.style.borderBottomLeftRadius = 12;
        usernameField.style.borderBottomRightRadius = 12;
        usernameField.style.borderLeftWidth = 2;
        usernameField.style.borderRightWidth = 2;
        usernameField.style.borderTopWidth = 2;
        usernameField.style.borderBottomWidth = 2;
        usernameField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.paddingLeft = 15;
        usernameField.style.paddingRight = 15;
        usernameField.style.paddingTop = 8;
        usernameField.style.paddingBottom = 8;
        usernameField.style.fontSize = 16;
        ApplyFont(usernameField, FontSize.Body);
        
        // 添加焦点效果
        usernameField.RegisterCallback<FocusInEvent>(evt => {
            usernameField.style.borderLeftColor = primaryColor;
            usernameField.style.borderRightColor = primaryColor;
            usernameField.style.borderTopColor = primaryColor;
            usernameField.style.borderBottomColor = primaryColor;
            usernameField.style.backgroundColor = Color.white;
        });
        
        usernameField.RegisterCallback<FocusOutEvent>(evt => {
            usernameField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        });
        
        loginForm.Add(usernameField);
        
        // 密码标签
        var passwordLabel = new Label("密码");
        passwordLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        passwordLabel.style.fontSize = 14;
        passwordLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        passwordLabel.style.marginBottom = 8;
        passwordLabel.style.alignSelf = Align.FlexStart;
        passwordLabel.style.marginLeft = 10;
        ApplyFont(passwordLabel, FontSize.Small);
        loginForm.Add(passwordLabel);
        
        // 密码输入框
        var passwordField = new TextField();
        passwordField.name = "password-field";
        passwordField.isPasswordField = true;
        passwordField.style.width = 340;
        passwordField.style.height = 50;
        passwordField.style.marginBottom = 25;
        passwordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        passwordField.style.borderTopLeftRadius = 12;
        passwordField.style.borderTopRightRadius = 12;
        passwordField.style.borderBottomLeftRadius = 12;
        passwordField.style.borderBottomRightRadius = 12;
        passwordField.style.borderLeftWidth = 2;
        passwordField.style.borderRightWidth = 2;
        passwordField.style.borderTopWidth = 2;
        passwordField.style.borderBottomWidth = 2;
        passwordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        passwordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.paddingLeft = 15;
        passwordField.style.paddingRight = 15;
        passwordField.style.paddingTop = 8;
        passwordField.style.paddingBottom = 8;
        passwordField.style.fontSize = 16;
        ApplyFont(passwordField, FontSize.Body);
        
        // 添加焦点效果
        passwordField.RegisterCallback<FocusInEvent>(evt => {
            passwordField.style.borderLeftColor = primaryColor;
            passwordField.style.borderRightColor = primaryColor;
            passwordField.style.borderTopColor = primaryColor;
            passwordField.style.borderBottomColor = primaryColor;
            passwordField.style.backgroundColor = Color.white;
        });
        
        passwordField.RegisterCallback<FocusOutEvent>(evt => {
            passwordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            passwordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            passwordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            passwordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            passwordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        });
        
        loginForm.Add(passwordField);
        
        // 登录按钮
        var loginButton = new Button(() => OnLoginButtonClickedInAuthArea()) { text = "登录" };
        loginButton.name = "login-button";
        loginButton.style.width = 340;
        loginButton.style.height = 50;
        loginButton.style.backgroundColor = primaryColor;
        loginButton.style.color = Color.white;
        loginButton.style.borderTopLeftRadius = 12;
        loginButton.style.borderTopRightRadius = 12;
        loginButton.style.borderBottomLeftRadius = 12;
        loginButton.style.borderBottomRightRadius = 12;
        loginButton.style.marginBottom = 20;
        loginButton.style.fontSize = 18;
        loginButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        loginButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(loginButton, FontSize.Title);
        
        // 添加悬停效果
        loginButton.RegisterCallback<MouseEnterEvent>(evt => {
            loginButton.style.backgroundColor = new Color(
                Mathf.Min(primaryColor.r + 0.1f, 1f),
                Mathf.Min(primaryColor.g + 0.1f, 1f),
                Mathf.Min(primaryColor.b + 0.1f, 1f),
                1f
            );
            loginButton.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
        });
        
        loginButton.RegisterCallback<MouseLeaveEvent>(evt => {
            loginButton.style.backgroundColor = primaryColor;
            loginButton.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        loginForm.Add(loginButton);
        
        // 切换到注册的按钮
        var switchToRegisterButton = new Button(() => SwitchToRegisterInAuthArea()) { text = "没有账户？点击注册" };
        switchToRegisterButton.name = "switch-to-register-button";
        switchToRegisterButton.style.width = 340;
        switchToRegisterButton.style.height = 40;
        switchToRegisterButton.style.backgroundColor = Color.clear;
        switchToRegisterButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        switchToRegisterButton.style.borderLeftWidth = 1;
        switchToRegisterButton.style.borderRightWidth = 1;
        switchToRegisterButton.style.borderTopWidth = 1;
        switchToRegisterButton.style.borderBottomWidth = 1;
        switchToRegisterButton.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToRegisterButton.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToRegisterButton.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToRegisterButton.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToRegisterButton.style.borderTopLeftRadius = 10;
        switchToRegisterButton.style.borderTopRightRadius = 10;
        switchToRegisterButton.style.borderBottomLeftRadius = 10;
        switchToRegisterButton.style.borderBottomRightRadius = 10;
        switchToRegisterButton.style.fontSize = 14;
        switchToRegisterButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 添加悬停效果
        switchToRegisterButton.RegisterCallback<MouseEnterEvent>(evt => {
            switchToRegisterButton.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            switchToRegisterButton.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            switchToRegisterButton.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            switchToRegisterButton.style.borderRightColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            switchToRegisterButton.style.borderTopColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            switchToRegisterButton.style.borderBottomColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        });
        
        switchToRegisterButton.RegisterCallback<MouseLeaveEvent>(evt => {
            switchToRegisterButton.style.backgroundColor = Color.clear;
            switchToRegisterButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            switchToRegisterButton.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            switchToRegisterButton.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            switchToRegisterButton.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            switchToRegisterButton.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        });
        
        loginForm.Add(switchToRegisterButton);
        
        parent.Add(loginForm);
        
        // 保存引用
        loginFormInAuthArea = loginForm;
    }
    
    /// <summary>
    /// 在认证区域中创建注册表单
    /// </summary>
    void CreateRegisterFormInAuthArea(VisualElement parent)
    {
        var registerForm = new VisualElement();
        registerForm.name = "register-form";
        registerForm.style.display = DisplayStyle.None;
        registerForm.style.flexDirection = FlexDirection.Column;
        registerForm.style.alignItems = Align.Center;
        registerForm.style.marginBottom = 30; // 增加底部边距
        
        // 用户名标签
        var usernameLabel = new Label("用户名");
        usernameLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        usernameLabel.style.fontSize = 16; // 增加字体大小
        usernameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        usernameLabel.style.marginBottom = 10; // 增加标签和输入框之间的间距
        usernameLabel.style.alignSelf = Align.FlexStart;
        usernameLabel.style.marginLeft = 10;
        ApplyFont(usernameLabel, FontSize.Body); // 改为Body大小
        registerForm.Add(usernameLabel);
        
        // 用户名输入框
        var usernameField = new TextField();
        usernameField.name = "register-username-field";
        usernameField.style.width = 340;
        usernameField.style.height = 55; // 增加输入框高度
        usernameField.style.marginBottom = 25; // 增加输入框之间的间距
        usernameField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 0.98f);
        usernameField.style.borderTopLeftRadius = 12;
        usernameField.style.borderTopRightRadius = 12;
        usernameField.style.borderBottomLeftRadius = 12;
        usernameField.style.borderBottomRightRadius = 12;
        usernameField.style.borderLeftWidth = 2;
        usernameField.style.borderRightWidth = 2;
        usernameField.style.borderTopWidth = 2;
        usernameField.style.borderBottomWidth = 2;
        usernameField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        usernameField.style.paddingLeft = 15;
        usernameField.style.paddingRight = 15;
        usernameField.style.paddingTop = 8;
        usernameField.style.paddingBottom = 8;
        usernameField.style.fontSize = 16;
        ApplyFont(usernameField, FontSize.Body);
        
        // 添加焦点效果
        usernameField.RegisterCallback<FocusInEvent>(evt => {
            usernameField.style.borderLeftColor = accentColor;
            usernameField.style.borderRightColor = accentColor;
            usernameField.style.borderTopColor = accentColor;
            usernameField.style.borderBottomColor = accentColor;
            usernameField.style.backgroundColor = Color.white;
        });
        
        usernameField.RegisterCallback<FocusOutEvent>(evt => {
            usernameField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            usernameField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        });
        
        registerForm.Add(usernameField);
        
        // 密码标签
        var passwordLabel = new Label("密码");
        passwordLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        passwordLabel.style.fontSize = 16; // 增加字体大小
        passwordLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        passwordLabel.style.marginBottom = 10; // 增加标签和输入框之间的间距
        passwordLabel.style.alignSelf = Align.FlexStart;
        passwordLabel.style.marginLeft = 10;
        ApplyFont(passwordLabel, FontSize.Body); // 改为Body大小
        registerForm.Add(passwordLabel);
        
        // 密码输入框
        var passwordField = new TextField();
        passwordField.name = "register-password-field";
        passwordField.isPasswordField = true;
        passwordField.style.width = 340;
        passwordField.style.height = 55; // 增加输入框高度
        passwordField.style.marginBottom = 25; // 增加输入框之间的间距
        passwordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        passwordField.style.borderTopLeftRadius = 12;
        passwordField.style.borderTopRightRadius = 12;
        passwordField.style.borderBottomLeftRadius = 12;
        passwordField.style.borderBottomRightRadius = 12;
        passwordField.style.borderLeftWidth = 2;
        passwordField.style.borderRightWidth = 2;
        passwordField.style.borderTopWidth = 2;
        passwordField.style.borderBottomWidth = 2;
        passwordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        passwordField.style.paddingLeft = 15;
        passwordField.style.paddingRight = 15;
        passwordField.style.paddingTop = 8;
        passwordField.style.paddingBottom = 8;
        passwordField.style.fontSize = 16;
        ApplyFont(passwordField, FontSize.Body);
        
        // 添加焦点效果
        passwordField.RegisterCallback<FocusInEvent>(evt => {
            passwordField.style.borderLeftColor = accentColor;
            passwordField.style.borderRightColor = accentColor;
            passwordField.style.borderTopColor = accentColor;
            passwordField.style.borderBottomColor = accentColor;
            passwordField.style.backgroundColor = Color.white;
        });
        
        passwordField.RegisterCallback<FocusOutEvent>(evt => {
            passwordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            passwordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            passwordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            passwordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            passwordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        });
        
        registerForm.Add(passwordField);
        
        // 确认密码标签
        var confirmPasswordLabel = new Label("确认密码");
        confirmPasswordLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        confirmPasswordLabel.style.fontSize = 16; // 增加字体大小
        confirmPasswordLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        confirmPasswordLabel.style.marginBottom = 10; // 增加标签和输入框之间的间距
        confirmPasswordLabel.style.alignSelf = Align.FlexStart;
        confirmPasswordLabel.style.marginLeft = 10;
        ApplyFont(confirmPasswordLabel, FontSize.Body); // 改为Body大小
        registerForm.Add(confirmPasswordLabel);
        
        // 确认密码输入框
        var confirmPasswordField = new TextField();
        confirmPasswordField.name = "confirm-password-field";
        confirmPasswordField.isPasswordField = true;
        confirmPasswordField.style.width = 340;
        confirmPasswordField.style.height = 55; // 增加输入框高度
        confirmPasswordField.style.marginBottom = 30; // 增加底部间距
        confirmPasswordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        confirmPasswordField.style.borderTopLeftRadius = 12;
        confirmPasswordField.style.borderTopRightRadius = 12;
        confirmPasswordField.style.borderBottomLeftRadius = 12;
        confirmPasswordField.style.borderBottomRightRadius = 12;
        confirmPasswordField.style.borderLeftWidth = 2;
        confirmPasswordField.style.borderRightWidth = 2;
        confirmPasswordField.style.borderTopWidth = 2;
        confirmPasswordField.style.borderBottomWidth = 2;
        confirmPasswordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        confirmPasswordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        confirmPasswordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        confirmPasswordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        confirmPasswordField.style.paddingLeft = 15;
        confirmPasswordField.style.paddingRight = 15;
        confirmPasswordField.style.paddingTop = 8;
        confirmPasswordField.style.paddingBottom = 8;
        confirmPasswordField.style.fontSize = 16;
        ApplyFont(confirmPasswordField, FontSize.Body);
        
        // 添加焦点效果
        confirmPasswordField.RegisterCallback<FocusInEvent>(evt => {
            confirmPasswordField.style.borderLeftColor = accentColor;
            confirmPasswordField.style.borderRightColor = accentColor;
            confirmPasswordField.style.borderTopColor = accentColor;
            confirmPasswordField.style.borderBottomColor = accentColor;
            confirmPasswordField.style.backgroundColor = Color.white;
        });
        
        confirmPasswordField.RegisterCallback<FocusOutEvent>(evt => {
            confirmPasswordField.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            confirmPasswordField.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            confirmPasswordField.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            confirmPasswordField.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            confirmPasswordField.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        });
        
        registerForm.Add(confirmPasswordField);
        
        // 注册按钮
        var registerButton = new Button(() => OnRegisterButtonClickedInAuthArea()) { text = "注册" };
        registerButton.name = "register-button";
        registerButton.style.width = 340;
        registerButton.style.height = 55; // 增加按钮高度
        registerButton.style.backgroundColor = accentColor;
        registerButton.style.color = Color.white;
        registerButton.style.borderTopLeftRadius = 12;
        registerButton.style.borderTopRightRadius = 12;
        registerButton.style.borderBottomLeftRadius = 12;
        registerButton.style.borderBottomRightRadius = 12;
        registerButton.style.marginBottom = 25; // 增加按钮间距
        registerButton.style.fontSize = 18;
        registerButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        registerButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(registerButton, FontSize.Title);
        
        // 添加悬停效果
        registerButton.RegisterCallback<MouseEnterEvent>(evt => {
            registerButton.style.backgroundColor = new Color(
                Mathf.Min(accentColor.r + 0.1f, 1f),
                Mathf.Min(accentColor.g + 0.1f, 1f),
                Mathf.Min(accentColor.b + 0.1f, 1f),
                1f
            );
            registerButton.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
        });
        
        registerButton.RegisterCallback<MouseLeaveEvent>(evt => {
            registerButton.style.backgroundColor = accentColor;
            registerButton.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        registerForm.Add(registerButton);
        
        // 切换到登录的按钮
        var switchToLoginButton = new Button(() => SwitchToLoginInAuthArea()) { text = "已有账户？点击登录" };
        switchToLoginButton.name = "switch-to-login-button";
        switchToLoginButton.style.width = 340;
        switchToLoginButton.style.height = 45; // 增加按钮高度
        switchToLoginButton.style.backgroundColor = Color.clear;
        switchToLoginButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        switchToLoginButton.style.borderLeftWidth = 1;
        switchToLoginButton.style.borderRightWidth = 1;
        switchToLoginButton.style.borderTopWidth = 1;
        switchToLoginButton.style.borderBottomWidth = 1;
        switchToLoginButton.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToLoginButton.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToLoginButton.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToLoginButton.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        switchToLoginButton.style.borderTopLeftRadius = 10;
        switchToLoginButton.style.borderTopRightRadius = 10;
        switchToLoginButton.style.borderBottomLeftRadius = 10;
        switchToLoginButton.style.borderBottomRightRadius = 10;
        switchToLoginButton.style.fontSize = 14;
        switchToLoginButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 添加悬停效果
        switchToLoginButton.RegisterCallback<MouseEnterEvent>(evt => {
            switchToLoginButton.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            switchToLoginButton.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            switchToLoginButton.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            switchToLoginButton.style.borderRightColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            switchToLoginButton.style.borderTopColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            switchToLoginButton.style.borderBottomColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        });
        
        switchToLoginButton.RegisterCallback<MouseLeaveEvent>(evt => {
            switchToLoginButton.style.backgroundColor = Color.clear;
            switchToLoginButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            switchToLoginButton.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            switchToLoginButton.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            switchToLoginButton.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            switchToLoginButton.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        });
        
        registerForm.Add(switchToLoginButton);
        
        parent.Add(registerForm);
        
        // 保存引用
        registerFormInAuthArea = registerForm;
    }
    
    /// <summary>
    /// 在认证区域中处理登录按钮点击
    /// </summary>
    void OnLoginButtonClickedInAuthArea()
    {
        Debug.Log("用户在认证区域中点击了登录按钮");
        
        // 获取输入框的值
        var usernameField = loginFormInAuthArea.Q<TextField>("username-field");
        var passwordField = loginFormInAuthArea.Q<TextField>("password-field");
        
        if (usernameField == null || passwordField == null)
        {
            Debug.LogError("无法找到用户名或密码输入框");
            UpdateStatus("错误：无法找到输入框");
            return;
        }
        
        string username = usernameField.value;
        string password = passwordField.value;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("请输入用户名和密码");
            return;
        }
        
        // 调用认证系统进行登录
        if (authSystem != null)
        {
            bool loginSuccess = authSystem.LoginUser(username, password);
            if (loginSuccess)
            {
                UpdateStatus("登录成功！");
                // 登录成功后会触发OnUserLoggedIn事件，然后调用BackToMainInterface
            }
            else
            {
                UpdateStatus("登录失败，请检查用户名和密码");
            }
        }
        else
        {
            Debug.LogError("认证系统未找到");
            UpdateStatus("错误：认证系统未找到");
        }
    }
    
    /// <summary>
    /// 在认证区域中处理注册按钮点击
    /// </summary>
    void OnRegisterButtonClickedInAuthArea()
    {
        Debug.Log("用户在认证区域中点击了注册按钮");
        
        // 获取输入框的值
        var usernameField = registerFormInAuthArea.Q<TextField>("register-username-field");
        var passwordField = registerFormInAuthArea.Q<TextField>("register-password-field");
        var confirmPasswordField = registerFormInAuthArea.Q<TextField>("confirm-password-field");
        
        if (usernameField == null || passwordField == null || confirmPasswordField == null)
        {
            Debug.LogError("无法找到注册表单的输入框");
            UpdateStatus("错误：无法找到输入框");
            return;
        }
        
        string username = usernameField.value;
        string password = passwordField.value;
        string confirmPassword = confirmPasswordField.value;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            UpdateStatus("请填写用户名、密码和确认密码");
            return;
        }
        
        if (password != confirmPassword)
        {
            UpdateStatus("两次输入的密码不一致");
            return;
        }
        
        // 调用认证系统进行注册
        if (authSystem != null)
        {
            bool registerSuccess = authSystem.RegisterUser(username, password);
            if (registerSuccess)
            {
                UpdateStatus("注册成功！请使用新账户登录");
                // 注册成功后切换到登录表单
                SwitchToLoginInAuthArea();
            }
            else
            {
                UpdateStatus("注册失败，用户名可能已存在");
            }
        }
        else
        {
            Debug.LogError("认证系统未找到");
            UpdateStatus("错误：认证系统未找到");
        }
    }
    
    /// <summary>
    /// 在认证区域中切换到注册表单
    /// </summary>
    void SwitchToRegisterInAuthArea()
    {
        Debug.Log("切换到注册表单");
        if (loginFormInAuthArea != null)
        {
            loginFormInAuthArea.style.display = DisplayStyle.None;
        }
        if (registerFormInAuthArea != null)
        {
            registerFormInAuthArea.style.display = DisplayStyle.Flex;
        }
        UpdateStatus("请填写注册信息");
    }
    
    /// <summary>
    /// 在认证区域中切换到登录表单
    /// </summary>
    void SwitchToLoginInAuthArea()
    {
        Debug.Log("切换到登录表单");
        if (registerFormInAuthArea != null)
        {
            registerFormInAuthArea.style.display = DisplayStyle.None;
        }
        if (loginFormInAuthArea != null)
        {
            loginFormInAuthArea.style.display = DisplayStyle.Flex;
        }
        UpdateStatus("请登录您的账户");
    }
} 