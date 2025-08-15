using UnityEngine;
using System.Collections.Generic;

namespace PowerlineSystem
{
    /// <summary>
    /// 树木大小调试器
    /// 用于监控和调试树木的缩放设置
    /// </summary>
    public class TreeSizeDebugger : MonoBehaviour
    {
        [Header("调试设置")]
        [Tooltip("是否在控制台输出调试信息")]
        public bool enableConsoleOutput = true;
        
        [Tooltip("是否在场景中显示树木信息")]
        public bool enableSceneDisplay = true;
        
        [Tooltip("刷新树木列表")]
        public bool refreshTreeList = false;
        
        [Header("树木信息")]
        [SerializeField]
        private List<GameObject> treesInScene = new List<GameObject>();
        
        [SerializeField]
        private int totalTreeCount = 0;
        
        [SerializeField]
        private float averageScale = 0f;
        
        [SerializeField]
        private float minScale = 0f;
        
        [SerializeField]
        private float maxScale = 0f;
        
        void Start()
        {
            RefreshTreeList();
        }
        
        void Update()
        {
            if (refreshTreeList)
            {
                RefreshTreeList();
                refreshTreeList = false;
            }
        }
        
        /// <summary>
        /// 刷新场景中的树木列表
        /// </summary>
        [ContextMenu("刷新树木列表")]
        public void RefreshTreeList()
        {
            treesInScene.Clear();
            
            // 查找所有树木对象
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Tree") || obj.name.Contains("tree"))
                {
                    treesInScene.Add(obj);
                }
            }
            
            totalTreeCount = treesInScene.Count;
            
            if (enableConsoleOutput)
            {
                Debug.Log($"[TreeSizeDebugger] 找到 {totalTreeCount} 棵树");
            }
            
            CalculateScaleStatistics();
        }
        
        /// <summary>
        /// 计算缩放统计信息
        /// </summary>
        private void CalculateScaleStatistics()
        {
            if (treesInScene.Count == 0) return;
            
            float totalScale = 0f;
            minScale = float.MaxValue;
            maxScale = float.MinValue;
            
            foreach (GameObject tree in treesInScene)
            {
                if (tree != null)
                {
                    float scale = tree.transform.localScale.x; // 假设X、Y、Z缩放相同
                    totalScale += scale;
                    minScale = Mathf.Min(minScale, scale);
                    maxScale = Mathf.Max(maxScale, scale);
                }
            }
            
            averageScale = totalScale / treesInScene.Count;
            
            if (enableConsoleOutput)
            {
                Debug.Log($"[TreeSizeDebugger] 缩放统计: 平均={averageScale:F2}, 最小={minScale:F2}, 最大={maxScale:F2}");
            }
        }
        
        /// <summary>
        /// 输出所有树木的详细信息
        /// </summary>
        [ContextMenu("输出树木详细信息")]
        public void OutputTreeDetails()
        {
            if (treesInScene.Count == 0)
            {
                Debug.LogWarning("[TreeSizeDebugger] 没有找到树木，请先刷新树木列表");
                return;
            }
            
            Debug.Log($"[TreeSizeDebugger] === 树木详细信息 ===");
            Debug.Log($"[TreeSizeDebugger] 总共找到 {totalTreeCount} 棵树");
            
            for (int i = 0; i < Mathf.Min(treesInScene.Count, 10); i++) // 只显示前10棵
            {
                GameObject tree = treesInScene[i];
                if (tree != null)
                {
                    Vector3 scale = tree.transform.localScale;
                    Vector3 position = tree.transform.position;
                    Debug.Log($"[TreeSizeDebugger] 树木 {i+1}: {tree.name}");
                    Debug.Log($"[TreeSizeDebugger]   位置: {position}");
                    Debug.Log($"[TreeSizeDebugger]   缩放: {scale}");
                    Debug.Log($"[TreeSizeDebugger]   缩放倍数: {scale.x:F2}");
                }
            }
            
            if (treesInScene.Count > 10)
            {
                Debug.Log($"[TreeSizeDebugger] ... 还有 {treesInScene.Count - 10} 棵树");
            }
            
            Debug.Log($"[TreeSizeDebugger] === 统计信息 ===");
            Debug.Log($"[TreeSizeDebugger] 平均缩放: {averageScale:F2}");
            Debug.Log($"[TreeSizeDebugger] 最小缩放: {minScale:F2}");
            Debug.Log($"[TreeSizeDebugger] 最大缩放: {maxScale:F2}");
        }
        
        /// <summary>
        /// 测试树木缩放
        /// </summary>
        [ContextMenu("测试树木缩放")]
        public void TestTreeScaling()
        {
            if (treesInScene.Count == 0)
            {
                Debug.LogWarning("[TreeSizeDebugger] 没有找到树木，请先刷新树木列表");
                return;
            }
            
            Debug.Log($"[TreeSizeDebugger] === 测试树木缩放 ===");
            
            // 测试第一棵树的缩放
            GameObject testTree = treesInScene[0];
            if (testTree != null)
            {
                Vector3 originalScale = testTree.transform.localScale;
                Debug.Log($"[TreeSizeDebugger] 测试树木: {testTree.name}");
                Debug.Log($"[TreeSizeDebugger] 原始缩放: {originalScale}");
                
                // 临时放大10倍
                testTree.transform.localScale = originalScale * 10f;
                Debug.Log($"[TreeSizeDebugger] 放大10倍后: {testTree.transform.localScale}");
                
                // 恢复原始缩放
                testTree.transform.localScale = originalScale;
                Debug.Log($"[TreeSizeDebugger] 恢复原始缩放: {testTree.transform.localScale}");
            }
        }
        
        /// <summary>
        /// 在场景中显示树木信息
        /// </summary>
        void OnDrawGizmos()
        {
            if (!enableSceneDisplay || treesInScene.Count == 0) return;
            
            Gizmos.color = Color.green;
            
            foreach (GameObject tree in treesInScene)
            {
                if (tree != null)
                {
                    // 绘制树木位置
                    Gizmos.DrawWireSphere(tree.transform.position, 1f);
                    
                    // 绘制缩放信息
                    Vector3 scale = tree.transform.localScale;
                    float maxScale = Mathf.Max(scale.x, scale.y, scale.z);
                    Gizmos.DrawWireCube(tree.transform.position, Vector3.one * maxScale);
                }
            }
        }
        
        /// <summary>
        /// 在Inspector中显示统计信息
        /// </summary>
        void OnValidate()
        {
            if (refreshTreeList)
            {
                RefreshTreeList();
                refreshTreeList = false;
            }
        }
    }
}
