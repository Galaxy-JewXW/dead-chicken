using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 字体管理器 - 统一管理整个项目的字体
/// </summary>
public class FontManager : MonoBehaviour
{
    [Header("字体配置")]
    [Tooltip("主字体文件")]
    public Font mainFont;
    
    [Tooltip("备用字体文件")]
    public Font fallbackFont;
    
    [Header("字体大小配置")]
    [Tooltip("大标题字体大小")]
    public int largeTitleSize = 48;
    
    [Tooltip("标题字体大小")]
    public int titleSize = 24;
    
    [Tooltip("副标题字体大小")]
    public int subtitleSize = 18;
    
    [Tooltip("正文字体大小")]
    public int bodySize = 16;
    
    [Tooltip("小字体大小")]
    public int smallSize = 14;
    
    [Tooltip("极小字体大小")]
    public int tinySize = 12;
    
    [Header("字体样式")]
    [Tooltip("是否启用粗体")]
    public bool enableBold = true;
    
    [Tooltip("是否启用斜体")]
    public bool enableItalic = false;
    
    // 单例模式
    private static FontManager _instance;
    public static FontManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<FontManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("FontManager");
                    _instance = go.AddComponent<FontManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFonts();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化字体
    /// </summary>
    void InitializeFonts()
    {
        // 如果没有设置主字体，尝试从Resources加载
        if (mainFont == null)
        {
            mainFont = Resources.Load<Font>("Fonts/MainFont");
        }
        
        // 如果没有设置备用字体，使用Unity内置字体
        if (fallbackFont == null)
        {
            fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        Debug.Log("字体管理器初始化完成");
    }
    
    /// <summary>
    /// 获取当前字体
    /// </summary>
    public Font GetCurrentFont()
    {
        return mainFont != null ? mainFont : fallbackFont;
    }
    
    /// <summary>
    /// 应用字体到Label
    /// </summary>
    public void ApplyFont(Label label, FontSize size = FontSize.Body)
    {
        Font font = GetCurrentFont();
        if (font != null)
        {
            label.style.unityFont = font;
        }
        
        // 设置字体大小
        label.style.fontSize = GetFontSize(size);
        
        // 设置字体样式
        if (enableBold)
        {
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        else if (enableItalic)
        {
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
        }
        else
        {
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
    }
    
    /// <summary>
    /// 应用字体到Button
    /// </summary>
    public void ApplyFont(Button button, FontSize size = FontSize.Body)
    {
        Font font = GetCurrentFont();
        if (font != null)
        {
            button.style.unityFont = font;
        }
        
        // 设置字体大小
        button.style.fontSize = GetFontSize(size);
        
        // 设置字体样式
        if (enableBold)
        {
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        else if (enableItalic)
        {
            button.style.unityFontStyleAndWeight = FontStyle.Italic;
        }
        else
        {
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
    }
    
    /// <summary>
    /// 获取字体大小
    /// </summary>
    public int GetFontSize(FontSize size)
    {
        switch (size)
        {
            case FontSize.LargeTitle:
                return largeTitleSize;
            case FontSize.Title:
                return titleSize;
            case FontSize.Subtitle:
                return subtitleSize;
            case FontSize.Body:
                return bodySize;
            case FontSize.Small:
                return smallSize;
            case FontSize.Tiny:
                return tinySize;
            default:
                return bodySize;
        }
    }
    
    /// <summary>
    /// 更换字体
    /// </summary>
    public void ChangeFont(Font newFont)
    {
        mainFont = newFont;
        Debug.Log($"字体已更换为: {newFont.name}");
    }
    
    /// <summary>
    /// 从Resources加载字体
    /// </summary>
    public void LoadFontFromResources(string fontPath)
    {
        Font font = Resources.Load<Font>(fontPath);
        if (font != null)
        {
            ChangeFont(font);
        }
        else
        {
            Debug.LogError($"无法从Resources加载字体: {fontPath}");
        }
    }
}

/// <summary>
/// 字体大小枚举
/// </summary>
public enum FontSize
{
    LargeTitle,
    Title,
    Subtitle,
    Body,
    Small,
    Tiny
} 