using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 电力线标记信息数据结构
/// </summary>
[System.Serializable]
public class PowerlineMark
{
    public string powerlineId; // 电力线唯一标识
    public string markText; // 标记文本
    public DateTime createTime; // 创建时间
    public Vector3 position; // 标记位置
    public string powerlineType; // 电力线类型
    public string voltage; // 电压等级
    
    public PowerlineMark(string id, string text, Vector3 pos, string type, string volt)
    {
        powerlineId = id;
        markText = text;
        createTime = DateTime.Now;
        position = pos;
        powerlineType = type;
        voltage = volt;
    }
}

/// <summary>
/// 电力线标记管理系统
/// 负责管理所有电力线的标记信息
/// </summary>
public class PowerlineMarkingSystem : MonoBehaviour
{
    [Header("标记设置")]
    public bool enableMarking = true;
    public int maxMarksPerPowerline = 10; // 每条电力线最大标记数量
    
    // 标记数据存储
    private Dictionary<string, List<PowerlineMark>> powerlineMarks = new Dictionary<string, List<PowerlineMark>>();
    
    // 单例模式
    private static PowerlineMarkingSystem instance;
    public static PowerlineMarkingSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PowerlineMarkingSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PowerlineMarkingSystem");
                    instance = go.AddComponent<PowerlineMarkingSystem>();
                }
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
        }
    }
    
    /// <summary>
    /// 为指定电力线添加标记
    /// </summary>
    public bool AddMark(PowerlineInteraction powerline, string markText)
    {
        if (!enableMarking || powerline == null || string.IsNullOrEmpty(markText))
            return false;
            
        string powerlineId = GetPowerlineId(powerline);
        
        // 检查标记数量限制
        if (powerlineMarks.ContainsKey(powerlineId) && 
            powerlineMarks[powerlineId].Count >= maxMarksPerPowerline)
        {
            Debug.LogWarning($"电力线 {powerlineId} 的标记数量已达到上限 ({maxMarksPerPowerline})");
            return false;
        }
        
        // 获取电力线信息
        var info = powerline.GetDetailedInfo();
        Vector3 markPosition = powerline.transform.position;
        string powerlineType = info.basicInfo?.wireType ?? "未知";
        string voltage = info.voltage ?? "未知";
        
        // 创建新标记
        var newMark = new PowerlineMark(powerlineId, markText, markPosition, powerlineType, voltage);
        
        // 添加到存储
        if (!powerlineMarks.ContainsKey(powerlineId))
        {
            powerlineMarks[powerlineId] = new List<PowerlineMark>();
        }
        
        powerlineMarks[powerlineId].Add(newMark);
        
        Debug.Log($"已为电力线 {powerlineId} 添加标记: {markText}");
        return true;
    }
    
    /// <summary>
    /// 获取指定电力线的所有标记
    /// </summary>
    public List<PowerlineMark> GetPowerlineMarks(PowerlineInteraction powerline)
    {
        if (powerline == null) return new List<PowerlineMark>();
        
        string powerlineId = GetPowerlineId(powerline);
        if (powerlineMarks.ContainsKey(powerlineId))
        {
            return new List<PowerlineMark>(powerlineMarks[powerlineId]);
        }
        
        return new List<PowerlineMark>();
    }
    
    /// <summary>
    /// 获取所有电力线的标记
    /// </summary>
    public List<PowerlineMark> GetAllMarks()
    {
        var allMarks = new List<PowerlineMark>();
        foreach (var marks in powerlineMarks.Values)
        {
            allMarks.AddRange(marks);
        }
        
        // 按创建时间排序（最新的在前）
        allMarks.Sort((a, b) => b.createTime.CompareTo(a.createTime));
        return allMarks;
    }
    
    /// <summary>
    /// 删除指定标记
    /// </summary>
    public bool RemoveMark(string powerlineId, int markIndex)
    {
        if (!powerlineMarks.ContainsKey(powerlineId) || markIndex < 0 || markIndex >= powerlineMarks[powerlineId].Count)
            return false;
            
        powerlineMarks[powerlineId].RemoveAt(markIndex);
        Debug.Log($"已删除电力线 {powerlineId} 的标记 #{markIndex}");
        return true;
    }
    
    /// <summary>
    /// 清空指定电力线的所有标记
    /// </summary>
    public bool ClearPowerlineMarks(PowerlineInteraction powerline)
    {
        if (powerline == null) return false;
        
        string powerlineId = GetPowerlineId(powerline);
        if (powerlineMarks.ContainsKey(powerlineId))
        {
            powerlineMarks[powerlineId].Clear();
            Debug.Log($"已清空电力线 {powerlineId} 的所有标记");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 清空所有标记
    /// </summary>
    public void ClearAllMarks()
    {
        powerlineMarks.Clear();
        Debug.Log("已清空所有电力线标记");
    }
    
    /// <summary>
    /// 获取电力线唯一标识
    /// </summary>
    private string GetPowerlineId(PowerlineInteraction powerline)
    {
        if (powerline?.powerlineInfo == null)
            return "unknown";
            
        var info = powerline.powerlineInfo;
        return $"powerline_{info.index}_{info.wireType}";
    }
    
    /// <summary>
    /// 获取标记统计信息
    /// </summary>
    public Dictionary<string, int> GetMarkStatistics()
    {
        var stats = new Dictionary<string, int>();
        foreach (var kvp in powerlineMarks)
        {
            stats[kvp.Key] = kvp.Value.Count;
        }
        return stats;
    }
    
    /// <summary>
    /// 获取总标记数量
    /// </summary>
    public int GetTotalMarkCount()
    {
        int total = 0;
        foreach (var marks in powerlineMarks.Values)
        {
            total += marks.Count;
        }
        return total;
    }
} 