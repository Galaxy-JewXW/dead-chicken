using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 文件选择器测试脚本
/// 用于测试运行时文件选择功能
/// </summary>
public class FileSelectorTest : MonoBehaviour
{
    [Header("测试配置")]
    public bool testOnStart = false;
    
    void Start()
    {
        if (testOnStart)
        {
            TestFileSelector();
        }
    }
    
    [ContextMenu("测试文件选择器")]
    public void TestFileSelector()
    {
        Debug.Log("开始测试文件选择器...");
        
        // 测试LAS文件选择
        string lasPath = RuntimeFileSelector.OpenFileDialog("测试选择LAS文件", "LAS文件|*.las|所有文件|*.*");
        if (!string.IsNullOrEmpty(lasPath))
        {
            Debug.Log($"选择的LAS文件: {lasPath}");
        }
        else
        {
            Debug.Log("未选择LAS文件或用户取消");
        }
        
        // 测试点云文件选择
        string pointCloudPath = RuntimeFileSelector.OpenFileDialog("测试选择点云文件", "点云文件|*.las;*.off|LAS文件|*.las|OFF文件|*.off|所有文件|*.*");
        if (!string.IsNullOrEmpty(pointCloudPath))
        {
            Debug.Log($"选择的点云文件: {pointCloudPath}");
        }
        else
        {
            Debug.Log("未选择点云文件或用户取消");
        }
        
        // 测试保存文件对话框
        string savePath = RuntimeFileSelector.SaveFileDialog("测试保存文件", "文本文件|*.txt|所有文件|*.*", "test.txt");
        if (!string.IsNullOrEmpty(savePath))
        {
            Debug.Log($"保存文件路径: {savePath}");
        }
        else
        {
            Debug.Log("未选择保存路径或用户取消");
        }
        
        // 测试文件夹选择
        string folderPath = RuntimeFileSelector.SelectFolderDialog("测试选择文件夹");
        if (!string.IsNullOrEmpty(folderPath))
        {
            Debug.Log($"选择的文件夹: {folderPath}");
        }
        else
        {
            Debug.Log("未选择文件夹或用户取消");
        }
        
        Debug.Log("文件选择器测试完成");
    }
    
    [ContextMenu("测试LAS文件选择")]
    public void TestLasFileSelection()
    {
        string path = RuntimeFileSelector.OpenFileDialog("选择LAS文件", "LAS文件|*.las|所有文件|*.*");
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log($"成功选择LAS文件: {path}");
        }
        else
        {
            Debug.Log("LAS文件选择失败或用户取消");
        }
    }
    
    [ContextMenu("测试点云文件选择")]
    public void TestPointCloudFileSelection()
    {
        string path = RuntimeFileSelector.OpenFileDialog("选择点云文件", "点云文件|*.las;*.off|LAS文件|*.las|OFF文件|*.off|所有文件|*.*");
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log($"成功选择点云文件: {path}");
        }
        else
        {
            Debug.Log("点云文件选择失败或用户取消");
        }
    }
} 