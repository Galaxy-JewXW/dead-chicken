using UnityEngine;

/// <summary>
/// 电力线3D查看器自动初始化器
/// 确保Powerline3DViewer在场景中自动创建
/// </summary>
public class Powerline3DViewerInitializer : MonoBehaviour
{
    void Start()
    {
        // 确保Powerline3DViewer存在
        if (Powerline3DViewer.Instance == null)
        {
            Debug.Log("自动创建Powerline3DViewer实例");
        }
        else
        {
            Debug.Log("Powerline3DViewer已存在");
        }
    }
} 