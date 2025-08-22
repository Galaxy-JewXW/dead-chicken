using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI助手配置文件 - 管理AI助手的设置和知识库
/// </summary>
[CreateAssetMenu(fileName = "AIAssistantConfig", menuName = "AI Assistant/Configuration")]
public class AIAssistantConfig : ScriptableObject
{
    [Header("基础设置")]
    public string assistantName = "电力线助手";
    public string assistantVersion = "1.0.0";
    public Color primaryColor = new Color(0.2f, 0.8f, 0.4f, 1f);
    public Color secondaryColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    
    [Header("聊天设置")]
    public int maxChatHistory = 50;
    public float typingSpeed = 0.05f;
    public bool enableTypingEffect = true;
    public bool enableSoundEffects = false;
    [Tooltip("是否在启动时自动显示聊天面板")]
    public bool showChatPanelOnStart = false;
    
    [Header("知识库设置")]
    public List<KnowledgeEntry> knowledgeBase = new List<KnowledgeEntry>();
    public List<QuickResponse> quickResponses = new List<QuickResponse>();
    
    [Header("系统集成")]
    public bool integrateWithPowerlineSystem = true;
    public bool integrateWithCameraSystem = true;
    public bool integrateWithTerrainSystem = true;
    public bool integrateWithDangerSystem = true;
    
    /// <summary>
    /// 知识库条目
    /// </summary>
    [System.Serializable]
    public class KnowledgeEntry
    {
        public string category;
        public string keyword;
        public string response;
        public string[] relatedKeywords;
        public int priority = 1;
    }
    
    /// <summary>
    /// 快速回复
    /// </summary>
    [System.Serializable]
    public class QuickResponse
    {
        public string trigger;
        public string response;
        public string[] variations;
    }
    
    void OnEnable()
    {
        InitializeDefaultKnowledge();
    }
    
    /// <summary>
    /// 初始化默认知识库
    /// </summary>
    private void InitializeDefaultKnowledge()
    {
        if (knowledgeBase.Count > 0) return;
        
        // 系统功能知识
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "系统功能",
            keyword = "电力线",
            response = "电力线系统是核心功能，包括：\n• 电塔管理和可视化\n• 线路路径规划\n• 危险监测和预警\n• 无人机巡检管理\n• 地形适配和优化",
            relatedKeywords = new string[] { "电塔", "线路", "监测", "巡检" },
            priority = 5
        });
        
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "系统功能",
            keyword = "相机控制",
            response = "相机系统提供多种视角：\n• 第一人称视角：沉浸式体验\n• 俯视视角：全局观察\n• 自由飞行：灵活导航\n• 鼠标控制：滚轮缩放，右键旋转",
            relatedKeywords = new string[] { "视角", "导航", "缩放", "旋转" },
            priority = 4
        });
        
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "系统功能",
            keyword = "危险监测",
            response = "危险监测系统功能：\n• 树木危险评估\n• 距离测量工具\n• 风险等级分类\n• 预警通知系统\n• 历史记录追踪",
            relatedKeywords = new string[] { "树木", "距离", "风险", "预警" },
            priority = 4
        });
        
        // 操作指导知识
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "操作指导",
            keyword = "如何操作",
            response = "基本操作步骤：\n1. 使用WASD键移动相机\n2. 鼠标滚轮缩放场景\n3. 右键拖拽旋转视角\n4. 点击电塔查看信息\n5. 使用工具栏切换功能",
            relatedKeywords = new string[] { "移动", "缩放", "旋转", "点击" },
            priority = 3
        });
        
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "操作指导",
            keyword = "电塔设置",
            response = "电塔设置步骤：\n1. 选择电塔预制体\n2. 配置CSV数据文件\n3. 设置缩放参数\n4. 调整连接点位置\n5. 验证连接效果",
            relatedKeywords = new string[] { "预制体", "数据", "缩放", "连接" },
            priority = 3
        });
        
        // 问题解答知识
        knowledgeBase.Add(new KnowledgeEntry
        {
            category = "问题解答",
            keyword = "常见问题",
            response = "常见问题及解决方案：\n• 电塔位置不准确：使用位置修正功能\n• 连接线显示异常：检查数据格式\n• 性能问题：启用LOD优化\n• 材质问题：检查材质设置",
            relatedKeywords = new string[] { "位置", "连接", "性能", "材质" },
            priority = 2
        });
        
        // 快速回复
        quickResponses.Add(new QuickResponse
        {
            trigger = "你好",
            response = "你好！我是电力线可视化系统的AI助手，很高兴为您服务！",
            variations = new string[] { "您好", "hi", "hello" }
        });
        
        quickResponses.Add(new QuickResponse
        {
            trigger = "帮助",
            response = "我可以帮助您了解系统功能、提供操作指导、解答技术问题。请告诉我您需要什么帮助？",
            variations = new string[] { "help", "帮助", "支持" }
        });
        
        quickResponses.Add(new QuickResponse
        {
            trigger = "谢谢",
            response = "不客气！如果还有其他问题，随时可以问我。",
            variations = new string[] { "感谢", "thank", "thanks" }
        });
    }
    
    /// <summary>
    /// 根据关键词查找知识库条目
    /// </summary>
    public KnowledgeEntry FindKnowledgeEntry(string query)
    {
        string lowerQuery = query.ToLower();
        
        // 按优先级排序查找
        var sortedEntries = new List<KnowledgeEntry>(knowledgeBase);
        sortedEntries.Sort((a, b) => b.priority.CompareTo(a.priority));
        
        foreach (var entry in sortedEntries)
        {
            if (lowerQuery.Contains(entry.keyword.ToLower()))
                return entry;
                
            if (entry.relatedKeywords != null)
            {
                foreach (var keyword in entry.relatedKeywords)
                {
                    if (lowerQuery.Contains(keyword.ToLower()))
                        return entry;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取快速回复
    /// </summary>
    public string GetQuickResponse(string query)
    {
        string lowerQuery = query.ToLower();
        
        foreach (var response in quickResponses)
        {
            if (lowerQuery.Contains(response.trigger.ToLower()))
                return response.response;
                
            if (response.variations != null)
            {
                foreach (var variation in response.variations)
                {
                    if (lowerQuery.Contains(variation.ToLower()))
                        return response.response;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 添加新的知识库条目
    /// </summary>
    public void AddKnowledgeEntry(string category, string keyword, string response, string[] relatedKeywords = null, int priority = 1)
    {
        var entry = new KnowledgeEntry
        {
            category = category,
            keyword = keyword,
            response = response,
            relatedKeywords = relatedKeywords,
            priority = priority
        };
        
        knowledgeBase.Add(entry);
    }
    
    /// <summary>
    /// 移除知识库条目
    /// </summary>
    public void RemoveKnowledgeEntry(string keyword)
    {
        knowledgeBase.RemoveAll(entry => entry.keyword.ToLower() == keyword.ToLower());
    }
}
