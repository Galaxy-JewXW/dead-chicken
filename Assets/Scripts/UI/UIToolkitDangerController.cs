using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// UI Toolkit危险物控制器 - 独立危险物管理系统
/// </summary>
public class UIToolkitDangerController : MonoBehaviour
{
    [Header("危险物系统")]
    public LayerMask groundLayerMask = -1;
    public float markerHeightOffset = 0.05f;
    
    private SimpleUIToolkitManager uiManager;
    private VisualElement dangerPanel;
    private bool isCreatingMarker = false;
    
    // 危险物管理
    private List<DangerMarker> dangerMarkers = new List<DangerMarker>();
    private GameObject markersParent;
    
    // 当前选择的危险物属性
    private DangerType selectedDangerType = DangerType.Other;
    private DangerLevel selectedDangerLevel = DangerLevel.Medium;
    private string selectedDescription = "危险物";
    
    // 鼠标交互相关
    private bool isMouseDown = false;
    private float doubleClickTime = 0.3f;
    private float lastClickTime = 0f;
    
    // UI元素引用
    private TextField descriptionField;
    private VisualElement createButtonContainer;
    private Label createButtonLabel;
    private Label statusLabel;
    private VisualElement markerListContainer;
    
    void Start()
    {
        uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (uiManager == null)
        {
            Debug.LogError("未找到SimpleUIToolkitManager，UIToolkitDangerController无法工作");
            return;
        }
        
        // 创建标记父对象
        if (markersParent == null)
        {
            markersParent = new GameObject("DangerMarkers");
        }
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (uiManager == null) return;
        Debug.Log("UIToolkitDangerController已初始化");
    }
    
    
    /// <summary>
    /// 创建危险物面板UI
    /// </summary>
    public VisualElement CreateDangerPanel()
    {
        dangerPanel = new VisualElement();
        dangerPanel.style.width = Length.Percent(100);
        dangerPanel.style.height = Length.Percent(100);
        dangerPanel.style.flexDirection = FlexDirection.Column;
        
        // 创建控制区域（固定高度，不被挤压）
        CreateControlSection();
        
        // 创建危险物列表区域（可伸缩）
        CreateMarkerListSection();
        
        return dangerPanel;
    }
    
    void CreateControlSection()
    {
        var controlContainer = new VisualElement();
        controlContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        controlContainer.style.marginBottom = 10;
        controlContainer.style.paddingTop = 15;
        controlContainer.style.paddingBottom = 15;
        controlContainer.style.paddingLeft = 15;
        controlContainer.style.paddingRight = 15;
        controlContainer.style.borderTopLeftRadius = 8;
        controlContainer.style.borderTopRightRadius = 8;
        controlContainer.style.borderBottomLeftRadius = 8;
        controlContainer.style.borderBottomRightRadius = 8;
        controlContainer.style.borderLeftWidth = 2;
        controlContainer.style.borderRightWidth = 2;
        controlContainer.style.borderTopWidth = 2;
        controlContainer.style.borderBottomWidth = 2;
        controlContainer.style.borderLeftColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        controlContainer.style.borderRightColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        controlContainer.style.borderTopColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        controlContainer.style.borderBottomColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        // 设置固定高度，防止被挤压
        controlContainer.style.flexShrink = 0;
        controlContainer.style.flexGrow = 0;
        // 确保控制容器可以传递输入事件给子元素
        controlContainer.focusable = false;
        controlContainer.pickingMode = PickingMode.Position;
        
        // 危险类型选择 - 改用按钮选择器
        var typeLabel = new Label("危险类型:");
        typeLabel.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        typeLabel.style.fontSize = 14;
        typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        typeLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(typeLabel);
        controlContainer.Add(typeLabel);
        
        // 创建类型选择按钮组
        var typeButtonContainer = new VisualElement();
        typeButtonContainer.style.flexDirection = FlexDirection.Row;
        typeButtonContainer.style.flexWrap = Wrap.Wrap;
        typeButtonContainer.style.marginBottom = 10;
        
        var typeOptions = new string[] { "建筑危险", "植被危险", "设备危险", "其他危险" };
        var typeButtons = new VisualElement[4];
        
        for (int i = 0; i < typeOptions.Length; i++)
        {
            var index = i; // 捕获循环变量
            var button = new VisualElement();
            button.style.height = 28;
            button.style.width = 75;
            button.style.marginRight = 5;
            button.style.marginBottom = 5;
            button.style.backgroundColor = i == 3 ? new Color(0.2f, 0.7f, 0.2f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f); // 默认选中"其他危险"
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            var label = new Label(typeOptions[i]);
            label.style.color = i == 3 ? Color.white : Color.black; // 默认选中"其他危险"
            label.style.fontSize = 11;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            uiManager?.ApplyFont(label);
            button.Add(label);
            
            button.RegisterCallback<ClickEvent>(evt => {
                // 更新所有按钮状态
                for (int j = 0; j < typeButtons.Length; j++)
                {
                    var btn = typeButtons[j];
                    var lbl = btn.Q<Label>();
                    if (j == index)
                    {
                        btn.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
                        lbl.style.color = Color.white;
                    }
                    else
                    {
                        btn.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                        lbl.style.color = Color.black;
                    }
                }
                selectedDangerType = (DangerType)index;
                Debug.Log($"选择危险类型: {selectedDangerType}");
            });
            
            typeButtons[i] = button;
            typeButtonContainer.Add(button);
        }
        controlContainer.Add(typeButtonContainer);
        
        // 危险等级选择 - 改用按钮选择器
        var levelLabel = new Label("危险等级:");
        levelLabel.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        levelLabel.style.fontSize = 14;
        levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        levelLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(levelLabel);
        controlContainer.Add(levelLabel);
        
        // 创建等级选择按钮组
        var levelButtonContainer = new VisualElement();
        levelButtonContainer.style.flexDirection = FlexDirection.Row;
        levelButtonContainer.style.marginBottom = 15;
        
        var levelOptions = new string[] { "低危险", "中等危险", "高危险" };
        var levelColors = new Color[] {
            new Color(1f, 0.8f, 0f, 1f),     // 金黄色
            new Color(1f, 0.4f, 0f, 1f),     // 橙红色
            new Color(0.9f, 0.1f, 0.1f, 1f)  // 深红色
        };
        var levelButtons = new VisualElement[3];
        
        for (int i = 0; i < levelOptions.Length; i++)
        {
            var index = i; // 捕获循环变量
            var button = new VisualElement();
            button.style.height = 32;
            // 根据文字长度调整按钮宽度，确保"中等危险"能完整显示
            float buttonWidth = (levelOptions[i].Length > 3) ? 95 : 80; // "中等危险"需要更宽的按钮
            button.style.width = buttonWidth;
            button.style.marginRight = 5;
            button.style.backgroundColor = i == 1 ? levelColors[i] : new Color(0.9f, 0.9f, 0.9f, 1f); // 默认选中"中等危险"
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftColor = levelColors[i];
            button.style.borderRightColor = levelColors[i];
            button.style.borderTopColor = levelColors[i];
            button.style.borderBottomColor = levelColors[i];
            
            var label = new Label(levelOptions[i]);
            label.style.color = i == 1 ? Color.white : Color.black; // 默认选中"中等危险"
            // 根据文字长度调整字体大小
            float fontSize = (levelOptions[i].Length > 3) ? 11 : 12; // "中等危险"使用稍小的字体
            label.style.fontSize = fontSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            // 防止文字换行 - 使用overflow处理
            label.style.overflow = Overflow.Hidden;
            uiManager?.ApplyFont(label);
            button.Add(label);
            
            button.RegisterCallback<ClickEvent>(evt => {
                // 更新所有按钮状态
                for (int j = 0; j < levelButtons.Length; j++)
                {
                    var btn = levelButtons[j];
                    var lbl = btn.Q<Label>();
                    if (j == index)
                    {
                        btn.style.backgroundColor = levelColors[j];
                        lbl.style.color = Color.white;
                    }
                    else
                    {
                        btn.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                        lbl.style.color = Color.black;
                    }
                }
                selectedDangerLevel = (DangerLevel)index;
                Debug.Log($"选择危险等级: {selectedDangerLevel}");
            });
            
            levelButtons[i] = button;
            levelButtonContainer.Add(button);
        }
        controlContainer.Add(levelButtonContainer);
        
        // 危险描述输入
        var descLabel = new Label("危险描述:");
        descLabel.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        descLabel.style.fontSize = 14;
        descLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        descLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(descLabel);
        controlContainer.Add(descLabel);
        
        descriptionField = new TextField();
        descriptionField.value = "危险物";
        descriptionField.style.marginBottom = 15;
        descriptionField.style.height = 28;
        descriptionField.style.backgroundColor = Color.white;
        descriptionField.style.color = Color.black;
        descriptionField.style.fontSize = 12;
        descriptionField.style.borderLeftWidth = 1;
        descriptionField.style.borderRightWidth = 1;
        descriptionField.style.borderTopWidth = 1;
        descriptionField.style.borderBottomWidth = 1;
        descriptionField.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        descriptionField.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        descriptionField.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        descriptionField.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        descriptionField.style.borderTopLeftRadius = 4;
        descriptionField.style.borderTopRightRadius = 4;
        descriptionField.style.borderBottomLeftRadius = 4;
        descriptionField.style.borderBottomRightRadius = 4;
        descriptionField.style.paddingLeft = 8;
        descriptionField.style.paddingRight = 8;
        descriptionField.style.paddingTop = 4;
        descriptionField.style.paddingBottom = 4;
        uiManager?.ApplyFont(descriptionField);
        selectedDescription = "危险物";
        
        // 注册事件回调
        descriptionField.RegisterValueChangedCallback(evt => {
            selectedDescription = evt.newValue;
            Debug.Log($"输入危险描述: {selectedDescription}");
        });
        controlContainer.Add(descriptionField);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;
        
        // 开始创建按钮
        createButtonContainer = new VisualElement();
        createButtonContainer.style.width = 120;
        createButtonContainer.style.height = 35;
        createButtonContainer.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        createButtonContainer.style.justifyContent = Justify.Center;
        createButtonContainer.style.alignItems = Align.Center;
        createButtonContainer.style.borderTopLeftRadius = 5;
        createButtonContainer.style.borderTopRightRadius = 5;
        createButtonContainer.style.borderBottomLeftRadius = 5;
        createButtonContainer.style.borderBottomRightRadius = 5;
        
        createButtonLabel = new Label("开始创建");
        createButtonLabel.style.color = Color.white;
        createButtonLabel.style.fontSize = 14;
        createButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(createButtonLabel);
        createButtonContainer.Add(createButtonLabel);
        createButtonContainer.RegisterCallback<ClickEvent>(evt => ToggleCreateMode());
        buttonContainer.Add(createButtonContainer);
        
        // 清除按钮
        var clearButtonContainer = new VisualElement();
        clearButtonContainer.style.width = 120;
        clearButtonContainer.style.height = 35;
        clearButtonContainer.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        clearButtonContainer.style.justifyContent = Justify.Center;
        clearButtonContainer.style.alignItems = Align.Center;
        clearButtonContainer.style.borderTopLeftRadius = 5;
        clearButtonContainer.style.borderTopRightRadius = 5;
        clearButtonContainer.style.borderBottomLeftRadius = 5;
        clearButtonContainer.style.borderBottomRightRadius = 5;
        
        var clearButtonLabel = new Label("清除所有");
        clearButtonLabel.style.color = Color.white;
        clearButtonLabel.style.fontSize = 14;
        clearButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(clearButtonLabel);
        clearButtonContainer.Add(clearButtonLabel);
        clearButtonContainer.RegisterCallback<ClickEvent>(evt => ClearAllMarkers());
        buttonContainer.Add(clearButtonContainer);
        
        controlContainer.Add(buttonContainer);
        
        // 状态标签
        statusLabel = new Label("点击\"开始创建\"进入创建模式");
        statusLabel.style.color = new Color(0.7f, 0.4f, 0.1f, 1f);
        statusLabel.style.fontSize = 12;
        statusLabel.style.marginTop = 10;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
        statusLabel.style.paddingTop = 5;
        statusLabel.style.paddingBottom = 5;
        uiManager?.ApplyFont(statusLabel);
        controlContainer.Add(statusLabel);
        
        dangerPanel.Add(controlContainer);
    }
    
    void CreateMarkerListSection()
    {
        var listContainer = new VisualElement();
        listContainer.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        listContainer.style.paddingTop = 15;
        listContainer.style.paddingBottom = 15;
        listContainer.style.paddingLeft = 15;
        listContainer.style.paddingRight = 15;
        listContainer.style.borderTopLeftRadius = 8;
        listContainer.style.borderTopRightRadius = 8;
        listContainer.style.borderBottomLeftRadius = 8;
        listContainer.style.borderBottomRightRadius = 8;
        listContainer.style.borderLeftWidth = 2;
        listContainer.style.borderRightWidth = 2;
        listContainer.style.borderTopWidth = 2;
        listContainer.style.borderBottomWidth = 2;
        listContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        // 设置可伸缩属性，占用剩余空间
        listContainer.style.flexGrow = 1;
        listContainer.style.flexShrink = 1;
        
        var listTitle = new Label("已创建的危险标记");
        listTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        listTitle.style.fontSize = 16;
        listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitle.style.marginBottom = 8;
        uiManager?.ApplyFont(listTitle);
        listContainer.Add(listTitle);
        
        // 创建滚动视图，参考测距列表的实现
        var scrollView = new ScrollView();
        scrollView.style.minHeight = 250;
        scrollView.style.maxHeight = 1000;
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        scrollView.style.overflow = Overflow.Hidden;
        scrollView.scrollDecelerationRate = 0.9f;

        // 添加滚轮事件处理，提升滚动体验
        scrollView.RegisterCallback<WheelEvent>(evt =>
        {
            scrollView.scrollOffset += new Vector2(0, evt.delta.y * 200f); // 放大滚动量
            evt.StopPropagation();
        });

        // 标记列表容器
        markerListContainer = new VisualElement();
        markerListContainer.style.flexDirection = FlexDirection.Column;
        markerListContainer.style.flexShrink = 0; // 防止收缩
        
        scrollView.Add(markerListContainer);
        listContainer.Add(scrollView);
        
        dangerPanel.Add(listContainer);
    }
    
    void ToggleCreateMode()
    {
        if (isCreatingMarker)
        {
            ExitCreateMode();
        }
        else
        {
            EnterCreateMode();
        }
    }
    
    void EnterCreateMode()
    {
        isCreatingMarker = true;
        if (createButtonLabel != null)
        {
            createButtonLabel.text = "退出创建";
        }
        if (createButtonContainer != null)
        {
            createButtonContainer.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        }
        if (statusLabel != null)
        {
            statusLabel.text = "创建模式：双击地面创建危险标记，ESC退出";
            statusLabel.style.backgroundColor = new Color(0.8f, 1f, 0.8f, 1f);
        }
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("危险物创建模式：双击地面创建标记");
        }
    }
    
    void ExitCreateMode()
    {
        isCreatingMarker = false;
        if (createButtonLabel != null)
        {
            createButtonLabel.text = "开始创建";
        }
        if (createButtonContainer != null)
        {
            createButtonContainer.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        }
        if (statusLabel != null)
        {
            statusLabel.text = "点击\"开始创建\"进入创建模式";
            statusLabel.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
        }
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("已退出危险物创建模式");
        }
    }
    
    void ClearAllMarkers()
    {
        foreach (Transform child in markersParent.transform)
        {
            Destroy(child.gameObject);
        }
        dangerMarkers.Clear();
        UpdateMarkerList();
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("已清除所有危险标记");
        }
    }
    
    /// <summary>
    /// 更新危险标记列表显示
    /// </summary>
    public void UpdateMarkerList()
    {
        if (markerListContainer == null) return;
        
        markerListContainer.Clear();
        
        if (dangerMarkers == null || dangerMarkers.Count == 0)
        {
            var noMarkersLabel = new Label("暂无危险标记");
            noMarkersLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            noMarkersLabel.style.fontSize = 12;
            noMarkersLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noMarkersLabel.style.paddingTop = 20;
            noMarkersLabel.style.paddingBottom = 20;
            uiManager?.ApplyFont(noMarkersLabel);
            markerListContainer.Add(noMarkersLabel);
            return;
        }
        
        for (int i = 0; i < dangerMarkers.Count; i++)
        {
            var marker = dangerMarkers[i];
            if (marker == null) continue;
            
            CreateMarkerListItem(marker, i + 1);
        }
    }
    
    void CreateMarkerListItem(DangerMarker marker, int index)
    {
        var itemContainer = new VisualElement();
        itemContainer.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
        itemContainer.style.marginBottom = 8;
        itemContainer.style.paddingTop = 12;
        itemContainer.style.paddingBottom = 12;
        itemContainer.style.paddingLeft = 12;
        itemContainer.style.paddingRight = 12;
        itemContainer.style.borderTopLeftRadius = 6;
        itemContainer.style.borderTopRightRadius = 6;
        itemContainer.style.borderBottomLeftRadius = 6;
        itemContainer.style.borderBottomRightRadius = 6;
        itemContainer.style.borderLeftWidth = 3;
        itemContainer.style.borderRightWidth = 1;
        itemContainer.style.borderTopWidth = 1;
        itemContainer.style.borderBottomWidth = 1;
        
        // 根据危险等级设置边框颜色 - 左边框更粗突出危险等级
        Color borderColor = GetDangerLevelColor(marker.dangerLevel);
        itemContainer.style.borderLeftColor = borderColor;
        itemContainer.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        itemContainer.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        itemContainer.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // 标题行
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.justifyContent = Justify.SpaceBetween;
        titleRow.style.alignItems = Align.Center;
        titleRow.style.marginBottom = 8;
        
        var titleLabel = new Label($"危险标记 #{index}");
        titleLabel.style.color = new Color(0.2f, 0.5f, 0.8f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(titleLabel);
        titleRow.Add(titleLabel);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        
        // 跳转按钮
        var jumpButtonContainer = new VisualElement();
        jumpButtonContainer.style.width = 50;
        jumpButtonContainer.style.height = 24;
        jumpButtonContainer.style.marginRight = 5;
        jumpButtonContainer.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        jumpButtonContainer.style.justifyContent = Justify.Center;
        jumpButtonContainer.style.alignItems = Align.Center;
        jumpButtonContainer.style.borderTopLeftRadius = 3;
        jumpButtonContainer.style.borderTopRightRadius = 3;
        jumpButtonContainer.style.borderBottomLeftRadius = 3;
        jumpButtonContainer.style.borderBottomRightRadius = 3;
        
        var jumpButtonLabel = new Label("跳转");
        jumpButtonLabel.style.color = Color.white;
        jumpButtonLabel.style.fontSize = 11;
        jumpButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(jumpButtonLabel);
        jumpButtonContainer.Add(jumpButtonLabel);
        
        jumpButtonContainer.RegisterCallback<ClickEvent>(evt => JumpToMarker(marker));
        buttonContainer.Add(jumpButtonContainer);
        
        // 删除按钮
        var deleteButtonContainer = new VisualElement();
        deleteButtonContainer.style.width = 50;
        deleteButtonContainer.style.height = 24;
        deleteButtonContainer.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        deleteButtonContainer.style.justifyContent = Justify.Center;
        deleteButtonContainer.style.alignItems = Align.Center;
        deleteButtonContainer.style.borderTopLeftRadius = 3;
        deleteButtonContainer.style.borderTopRightRadius = 3;
        deleteButtonContainer.style.borderBottomLeftRadius = 3;
        deleteButtonContainer.style.borderBottomRightRadius = 3;
        
        var deleteButtonLabel = new Label("删除");
        deleteButtonLabel.style.color = Color.white;
        deleteButtonLabel.style.fontSize = 11;
        deleteButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(deleteButtonLabel);
        deleteButtonContainer.Add(deleteButtonLabel);
        
        deleteButtonContainer.RegisterCallback<ClickEvent>(evt => RemoveMarker(marker));
        buttonContainer.Add(deleteButtonContainer);
        
        titleRow.Add(buttonContainer);
        
        itemContainer.Add(titleRow);
        
        // 信息网格
        var infoGrid = new VisualElement();
        infoGrid.style.flexDirection = FlexDirection.Column;
        
        // 类型和等级行
        var typeRow = new VisualElement();
        typeRow.style.flexDirection = FlexDirection.Row;
        typeRow.style.justifyContent = Justify.SpaceBetween;
        typeRow.style.marginBottom = 4;
        
        var typeInfo = new Label($"类型: {GetDangerTypeString(marker.dangerType)}");
        typeInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        typeInfo.style.fontSize = 12;
        uiManager?.ApplyFont(typeInfo);
        typeRow.Add(typeInfo);
        
        var levelInfo = new Label($"等级: {GetDangerLevelString(marker.dangerLevel)}");
        levelInfo.style.color = borderColor;
        levelInfo.style.fontSize = 12;
        levelInfo.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(levelInfo);
        typeRow.Add(levelInfo);
        
        infoGrid.Add(typeRow);
        
        // 描述
        var descInfo = new Label($"描述: {marker.description}");
        descInfo.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        descInfo.style.fontSize = 12;
        descInfo.style.whiteSpace = WhiteSpace.Normal;
        descInfo.style.marginBottom = 4;
        uiManager?.ApplyFont(descInfo);
        infoGrid.Add(descInfo);
        
        // 位置信息
        var posInfo = new Label($"位置: ({marker.transform.position.x:F1}, {marker.transform.position.y:F1}, {marker.transform.position.z:F1})");
        posInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        posInfo.style.fontSize = 11;
        uiManager?.ApplyFont(posInfo);
        infoGrid.Add(posInfo);
        
        // 创建时间
        var timeInfo = new Label($"创建时间: {marker.createTime:HH:mm:ss}");
        timeInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        timeInfo.style.fontSize = 10;
        uiManager?.ApplyFont(timeInfo);
        infoGrid.Add(timeInfo);
        
        itemContainer.Add(infoGrid);
        markerListContainer.Add(itemContainer);
    }
    
    void RemoveMarker(DangerMarker marker)
    {
        if (marker != null)
        {
            Destroy(marker.gameObject);
            dangerMarkers.Remove(marker);
            UpdateMarkerList();
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar("已删除危险标记");
            }
        }
    }
    
    /// <summary>
    /// 跳转到指定危险物标记，参考电塔跳转功能实现
    /// </summary>
    void JumpToMarker(DangerMarker marker)
    {
        if (marker == null)
        {
            Debug.LogWarning("跳转失败：危险物标记为空");
            return;
        }
        
        Vector3 markerPosition = marker.transform.position;
        
        // 查找CameraManager组件
        var cameraManager = FindObjectOfType<CameraManager>();
        
        // 计算观察偏移量
        Vector3 cameraOffset = CalculateMarkerViewOffset(marker);
        
        // 如果没有CameraManager，尝试直接操作摄像机
        if (cameraManager == null)
        {
            Debug.LogWarning("CameraManager未找到，尝试直接操作主摄像机");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraPos = markerPosition + cameraOffset;
                
                // 确保摄像机位置在地面之上
                float groundLevel = GetGroundHeight(cameraPos);
                cameraPos.y = Mathf.Max(cameraPos.y, groundLevel + 2f);
                
                Vector3 fallbackLookAtTarget = markerPosition + Vector3.up * 0.5f; // 稍微向上看
                
                StartCoroutine(SmoothJumpToMarkerPosition(cameraPos, fallbackLookAtTarget, marker));
                return;
            }
            else
            {
                Debug.LogError("跳转失败：未找到主摄像机");
                return;
            }
        }
        
        // 计算摄像机目标位置
        Vector3 finalCameraPos = markerPosition + cameraOffset;
        
        // 确保摄像机位置在地面之上
        float finalGroundLevel = GetGroundHeight(finalCameraPos);
        finalCameraPos.y = Mathf.Max(finalCameraPos.y, finalGroundLevel + 2f);
        
        // 根据视角调整观察目标点
        Vector3 lookAtTarget = markerPosition;
        if (cameraManager != null && cameraManager.GetCurrentView() == 2) // 飞行视角
        {
            lookAtTarget.y += 0.5f; // 飞行视角下与危险物持平，稍微向上看一点点
        }
        else
        {
            lookAtTarget.y += 1f; // 其他视角稍微向上看
        }
        
        StartCoroutine(SmoothJumpToMarkerPosition(finalCameraPos, lookAtTarget, marker));
    }
    
    /// <summary>
    /// 计算危险物标记的视角偏移量
    /// </summary>
    Vector3 CalculateMarkerViewOffset(DangerMarker marker)
    {
        // 使用危险物的哈希码计算角度，确保每个危险物都有不同的视角
        float angle = (marker.GetHashCode() % 360) * Mathf.Deg2Rad;
        
        float baseDistance = 15f; // 基础距离
        float baseHeight = 5f;    // 基础高度
        
        // 根据当前视角调整位置
        var cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            int currentView = cameraManager.GetCurrentView();
            switch (currentView)
            {
                case 1: // 上帝视角 - 在标记的上方俯视
                    baseHeight = 30f;
                    baseDistance = 3f;
                    break;
                    
                case 2: // 飞行视角 - 与危险物高度持平，近距离观察
                    baseHeight = 0f; // 与危险物同高度
                    baseDistance = 8f; // 较近的距离
                    break;
                    
                default: // 第一人称视角 - 地面高度
                    baseHeight = 3f;
                    baseDistance = 10f;
                    break;
            }
        }
        
        // 计算最终偏移量
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * baseDistance,
            baseHeight,
            Mathf.Sin(angle) * baseDistance
        );
        
        return offset;
    }
    
    /// <summary>
    /// 平滑跳转到危险物标记位置
    /// </summary>
    System.Collections.IEnumerator SmoothJumpToMarkerPosition(Vector3 targetPos, Vector3 lookAtPos, DangerMarker marker)
    {
        Camera mainCamera = null;
        var cameraManager = FindObjectOfType<CameraManager>();
        
        // 优先使用CameraManager的摄像机
        if (cameraManager != null && cameraManager.mainCamera != null)
        {
            mainCamera = cameraManager.mainCamera;
        }
        else
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("跳转失败：未找到可用的摄像机");
            yield break;
        }
        
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        
        // 根据当前视角计算目标旋转
        Quaternion targetRot;
        if (cameraManager != null)
        {
            int currentView = cameraManager.GetCurrentView();
            switch (currentView)
            {
                case 1: // 上帝视角 - 向下俯视
                    Vector3 godViewDirection = (lookAtPos - targetPos).normalized;
                    godViewDirection.y = Mathf.Min(godViewDirection.y, -0.5f);
                    targetRot = Quaternion.LookRotation(godViewDirection);
                    break;
                    
                case 2: // 飞行视角 - 自然看向目标点
                    Vector3 flyViewDirection = (lookAtPos - targetPos).normalized;
                    targetRot = Quaternion.LookRotation(flyViewDirection);
                    break;
                    
                default: // 第一人称视角 - 正常看向危险物
                    Vector3 fpViewDirection = (lookAtPos - targetPos).normalized;
                    targetRot = Quaternion.LookRotation(fpViewDirection);
                    break;
            }
        }
        else
        {
            Vector3 fallbackDirection = (lookAtPos - targetPos).normalized;
            targetRot = Quaternion.LookRotation(fallbackDirection);
        }
        
        float elapsedTime = 0f;
        float duration = 1f; // 跳转动画持续时间
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 使用平滑曲线
            t = Mathf.SmoothStep(0f, 1f, t);
            
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        // 确保最终位置准确
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
        
        // 更新状态栏信息
        if (uiManager != null)
        {
            string markerInfo = $"已跳转到 {GetDangerTypeString(marker.dangerType)} - {marker.description}";
            uiManager.UpdateStatusBar(markerInfo);
        }
    }
    
    /// <summary>
    /// 获取地面高度（简化版本）
    /// </summary>
    float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        return 0f; // 默认地面高度
    }
    
    Color GetDangerLevelColor(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low:
                return new Color(1f, 0.8f, 0f, 1f); // 金黄色
            case DangerLevel.Medium:
                return new Color(1f, 0.4f, 0f, 1f); // 橙红色
            case DangerLevel.High:
                return new Color(0.9f, 0.1f, 0.1f, 1f); // 深红色
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
    
    string GetDangerTypeString(DangerType type)
    {
        switch (type)
        {
            case DangerType.Building: return "建筑危险";
            case DangerType.Vegetation: return "植被危险";
            case DangerType.Equipment: return "设备危险";
            case DangerType.Other: return "其他危险";
            default: return "未知";
        }
    }
    
    string GetDangerLevelString(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low: return "低危险";
            case DangerLevel.Medium: return "中等危险";
            case DangerLevel.High: return "高危险";
            default: return "未知";
        }
    }
    
    void Update()
    {
        // 处理ESC键退出创建模式
        if (Input.GetKeyDown(KeyCode.Escape) && isCreatingMarker)
        {
            ExitCreateMode();
        }
        
        // 处理危险物创建
        if (isCreatingMarker)
        {
            UpdateMouseInput();
        }
    }
    
    void UpdateMouseInput()
    {
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
        }
        
        if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            isMouseDown = false;
            
            // 检查鼠标是否在UI上，如果是则忽略此次点击
            if (uiManager != null && uiManager.IsMouseOverUI())
            {
                return; // 鼠标在UI上，不处理点击
            }
            
            // 检测双击
            float currentTime = Time.time;
            if (currentTime - lastClickTime < doubleClickTime)
            {
                // 双击创建危险物
                CreateDangerMarkerAtMousePosition();
            }
            lastClickTime = currentTime;
        }
    }
    
    void CreateDangerMarkerAtMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask))
        {
            Vector3 position = hit.point + Vector3.up * markerHeightOffset;
            CreateDangerMarker(position);
        }
    }
    
    void CreateDangerMarker(Vector3 position)
    {
        // 创建标记游戏对象
        GameObject markerObj = new GameObject("DangerMarker");
        markerObj.transform.position = position;
        markerObj.transform.SetParent(markersParent.transform);
        
        // 添加DangerMarker组件
        DangerMarker marker = markerObj.AddComponent<DangerMarker>();
        marker.SetDangerInfo(selectedDangerType, selectedDangerLevel, selectedDescription, "用户");
        
        // 添加到列表
        dangerMarkers.Add(marker);
        
        // 更新UI
        UpdateMarkerList();
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar($"已创建{GetDangerLevelString(selectedDangerLevel)}{GetDangerTypeString(selectedDangerType)}标记");
        }
        
        Debug.Log($"创建危险标记 - 类型:{selectedDangerType}, 等级:{selectedDangerLevel}, 位置:{position}");
    }
    
    void OnDestroy()
    {
        foreach (Transform child in markersParent.transform)
        {
            Destroy(child.gameObject);
        }
        dangerMarkers.Clear();
    }
    
    /// <summary>
    /// 隐藏危险物控制器
    /// </summary>
    public void Hide()
    {
        ExitCreateMode();
        this.enabled = false;
    }
    
    /// <summary>
    /// 显示危险物控制器（
    /// </summary>
    public void Show()
    {
        this.enabled = true;
        // 更新标记列表
        UpdateMarkerList();
    }

    /// <summary>
    /// 获取危险物标记列表（供其他组件访问）
    /// </summary>
    public List<DangerMarker> GetDangerMarkers()
    {
        return dangerMarkers ?? new List<DangerMarker>();
    }

    /// <summary>
    /// 跳转到指定危险物标记（供其他组件调用）
    /// </summary>
    public void JumpToSpecificMarker(DangerMarker marker)
    {
        if (marker != null)
        {
            JumpToMarker(marker);
        }
    }
} 