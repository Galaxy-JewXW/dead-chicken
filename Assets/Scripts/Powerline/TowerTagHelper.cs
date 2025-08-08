using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 电塔标签辅助工具
/// 用于自动创建和管理Tower标签
/// </summary>
public class TowerTagHelper : MonoBehaviour
{
    [ContextMenu("创建Tower标签")]
    public void CreateTowerTag()
    {
#if UNITY_EDITOR
        // 检查标签是否已存在
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        bool tagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue.Equals("Tower"))
            {
                tagExists = true;
                break;
            }
        }
        
        if (!tagExists)
        {
            // 添加新标签
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(0);
            newTag.stringValue = "Tower";
            tagManager.ApplyModifiedProperties();
            
            Debug.Log("成功添加Tower标签!");
        }
        else
        {
            Debug.Log("Tower标签已存在");
        }
#else
        Debug.LogWarning("此功能仅在编辑器模式下可用");
#endif
    }
    
    [ContextMenu("为现有电塔设置标签")]
    public void SetTagForExistingTowers()
    {
        // 查找所有名称包含Tower的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Tower") || obj.name.Contains("GoodTower"))
            {
                try
                {
                    obj.tag = "Tower";
                    count++;
                }
                catch (UnityException ex)
                {
                    Debug.LogWarning($"无法为{obj.name}设置Tower标签: {ex.Message}");
                }
            }
        }
        
        Debug.Log($"为{count}个电塔对象设置了Tower标签");
    }
} 