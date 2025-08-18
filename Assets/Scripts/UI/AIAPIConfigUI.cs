using UnityEngine;
using UnityEngine.UIElements;

public class AIAPIConfigUI : MonoBehaviour
{
    private VisualElement configPanel;
    private TextField apiKeyField;
    private Label statusLabel;
    
    private void Start()
    {
        CreateConfigUI();
    }
    
    private void CreateConfigUI()
    {
        // 创建配置面板
        configPanel = new VisualElement();
        configPanel.name = "aiApiConfigPanel";
        configPanel.style.position = Position.Absolute;
        configPanel.style.top = Length.Percent(10);
        configPanel.style.left = Length.Percent(10);
        configPanel.style.width = Length.Percent(80);
        configPanel.style.height = Length.Percent(80);
        configPanel.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 0.98f);
        configPanel.style.borderTopLeftRadius = 20;
        configPanel.style.borderTopRightRadius = 20;
        configPanel.style.borderBottomLeftRadius = 20;
        configPanel.style.borderBottomRightRadius = 20;
        configPanel.style.paddingTop = 30;
        configPanel.style.paddingBottom = 30;
        configPanel.style.paddingLeft = 40;
        configPanel.style.paddingRight = 40;
        configPanel.style.flexDirection = FlexDirection.Column;
        configPanel.style.display = DisplayStyle.None;
        
        // 标题
        var titleLabel = new Label("AI API 配置");
        titleLabel.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        titleLabel.style.fontSize = 24;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 30;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        configPanel.Add(titleLabel);
        
        // API密钥输入框
        var apiKeyLabel = new Label("API密钥:");
        apiKeyLabel.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        apiKeyLabel.style.fontSize = 16;
        apiKeyLabel.style.marginBottom = 8;
        configPanel.Add(apiKeyLabel);
        
        apiKeyField = new TextField();
        apiKeyField.style.height = 40;
        apiKeyField.style.fontSize = 14;
        apiKeyField.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        apiKeyField.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        apiKeyField.style.marginBottom = 20;
        configPanel.Add(apiKeyField);
        
        // 状态标签
        statusLabel = new Label("");
        statusLabel.style.color = new Color(0.9f, 0.9f, 1f, 1f);
        statusLabel.style.fontSize = 14;
        statusLabel.style.marginBottom = 20;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        configPanel.Add(statusLabel);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.Center;
        
        // 保存按钮
        var saveButton = new Button(() => SaveConfig());
        saveButton.text = "保存配置";
        saveButton.style.width = 120;
        saveButton.style.height = 45;
        saveButton.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 0.9f);
        saveButton.style.color = Color.white;
        saveButton.style.marginRight = 20;
        buttonContainer.Add(saveButton);
        
        // 关闭按钮
        var closeButton = new Button(() => HideConfig());
        closeButton.text = "关闭";
        closeButton.style.width = 120;
        closeButton.style.height = 45;
        closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        closeButton.style.color = Color.white;
        buttonContainer.Add(closeButton);
        
        configPanel.Add(buttonContainer);
        
        // 添加到根元素
        var rootElement = FindObjectOfType<UIDocument>()?.rootVisualElement;
        if (rootElement != null)
        {
            rootElement.Add(configPanel);
        }
    }
    
    private void SaveConfig()
    {
        if (AIAPIManager.Instance != null)
        {
            AIAPIManager.Instance.SetAPIKey(apiKeyField.value);
            statusLabel.text = "配置已保存！";
            statusLabel.style.color = new Color(0.2f, 0.8f, 0.4f, 1f);
        }
        else
        {
            statusLabel.text = "错误：AI API管理器未找到";
            statusLabel.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        }
    }
    
    public void ShowConfig()
    {
        if (configPanel != null)
        {
            configPanel.style.display = DisplayStyle.Flex;
        }
    }
    
    public void HideConfig()
    {
        if (configPanel != null)
        {
            configPanel.style.display = DisplayStyle.None;
        }
    }
}
