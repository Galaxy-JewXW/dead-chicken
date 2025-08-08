using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 电塔总览管理器
/// 负责管理电塔数据和提供总览功能
/// </summary>
public class TowerOverviewManager : MonoBehaviour
{
    [Header("电塔总览配置")]
    public SceneInitializer sceneInitializer;
    public CameraManager cameraManager;
    
    [Header("跳转设置")]
    public float jumpDistance = 20f; // 跳转到电塔时的距离
    public float jumpHeight = 10f;   // 跳转到电塔时的高度
    public float jumpSpeed = 2f;     // 跳转动画速度
    public float angleVariation = 45f; // 每个电塔的角度变化范围
    
    [System.Serializable]
    public class TowerOverviewInfo
    {
        public int id;
        public string name;
        public Vector3 position;
        public float height;
        public GameObject towerObject;
        public string status;
        public float voltage;
        public int connectedWires;
        
        public TowerOverviewInfo(int id, string name, Vector3 pos, float h, GameObject obj = null)
        {
            this.id = id;
            this.name = name;
            this.position = pos;
            this.height = h;
            this.towerObject = obj;
            this.status = "正常";
            this.voltage = 220f;
            this.connectedWires = 8;
        }
    }
    
    private List<TowerOverviewInfo> allTowers = new List<TowerOverviewInfo>();
    private bool isJumping = false;
    
    void Start()
    {
        // 自动查找组件
        if (sceneInitializer == null)
            sceneInitializer = FindObjectOfType<SceneInitializer>();
        
        if (cameraManager == null)
            cameraManager = FindObjectOfType<CameraManager>();
        
        // 延迟初始化，确保场景初始化完成
        Invoke("InitializeTowerData", 1f);
    }
    
    /// <summary>
    /// 初始化电塔数据
    /// </summary>
    public void InitializeTowerData()
    {
        allTowers.Clear();
        
        // 方法1：从SceneInitializer获取电塔数据
        if (sceneInitializer != null)
        {
            LoadTowersFromSceneInitializer();
        }
        
        // 方法2：从场景中查找电塔对象
        LoadTowersFromScene();
        

    }
    
    /// <summary>
    /// 从SceneInitializer加载电塔数据
    /// </summary>
    void LoadTowersFromSceneInitializer()
    {
        var towerData = sceneInitializer.LoadSimpleTowerData();
        
        for (int i = 0; i < towerData.Count; i++)
        {
            var tower = towerData[i];
            string towerName = $"电塔-{i + 1:D2}";
            
            var towerInfo = new TowerOverviewInfo(
                i + 1,
                towerName,
                tower.position,
                tower.height
            );
            
            allTowers.Add(towerInfo);
        }
    }
    
    /// <summary>
    /// 从场景中查找电塔对象
    /// </summary>
    void LoadTowersFromScene()
    {
        // 查找场景中所有电塔对象
        GameObject[] towerObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name.Contains("Tower") && obj.activeInHierarchy)
            .ToArray();
        
        // 更新已有电塔信息，添加GameObject引用
        foreach (var towerObj in towerObjects)
        {
            Vector3 pos = towerObj.transform.position;
            
            // 查找匹配的电塔信息
            var matchingTower = allTowers.FirstOrDefault(t => 
                Vector3.Distance(t.position, pos) < 5f);
            
            if (matchingTower != null)
            {
                matchingTower.towerObject = towerObj;
                matchingTower.name = towerObj.name;
            }
            else
            {
                // 如果没有找到匹配的，创建新的电塔信息
                var newTower = new TowerOverviewInfo(
                    allTowers.Count + 1,
                    towerObj.name,
                    pos,
                    GetTowerHeight(towerObj),
                    towerObj
                );
                allTowers.Add(newTower);
            }
        }
        
        // 按ID排序
        allTowers = allTowers.OrderBy(t => t.id).ToList();
    }
    
    /// <summary>
    /// 获取电塔高度
    /// </summary>
    float GetTowerHeight(GameObject towerObj)
    {
        if (towerObj == null) return 10f;
        
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            
            foreach (var renderer in renderers)
            {
                minY = Mathf.Min(minY, renderer.bounds.min.y);
                maxY = Mathf.Max(maxY, renderer.bounds.max.y);
            }
            
            return maxY - minY;
        }
        
        return 10f; // 默认高度
    }
    
    /// <summary>
    /// 获取所有电塔信息
    /// </summary>
    public List<TowerOverviewInfo> GetAllTowers()
    {
        return allTowers;
    }
    
    /// <summary>
    /// 根据ID获取电塔信息
    /// </summary>
    public TowerOverviewInfo GetTowerById(int id)
    {
        return allTowers.FirstOrDefault(t => t.id == id);
    }
    
    /// <summary>
    /// 跳转到指定电塔
    /// </summary>
    public void JumpToTower(int towerId)
    {
        var tower = GetTowerById(towerId);
        if (tower != null)
        {
            JumpToTower(tower);
        }
    }
    
    /// <summary>
    /// 跳转到指定电塔
    /// </summary>
    public void JumpToTower(TowerOverviewInfo tower)
    {
        if (tower == null)
        {
            Debug.LogWarning("跳转失败：电塔信息为空");
            return;
        }
        
        if (isJumping)
        {
            Debug.LogWarning("跳转失败：正在跳转中，请稍后再试");
            return;
        }
        
        Vector3 towerPosition = tower.position;
        
        // 根据电塔ID计算不同的观察角度，传递电塔高度信息
        Vector3 cameraOffset = CalculateUniqueViewOffset(tower.id, tower.height);
        
        // 如果没有CameraManager，尝试直接操作摄像机
        if (cameraManager == null)
        {
            Debug.LogWarning("CameraManager未找到，尝试直接操作主摄像机");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraPos = towerPosition + cameraOffset;
                
                // 确保摄像机位置在地面之上
                float groundLevel = GetGroundHeight(cameraPos);
                cameraPos.y = Mathf.Max(cameraPos.y, groundLevel + 5f);
                
                // 调整观察目标点（与下面的逻辑保持一致）
                Vector3 fallbackLookAtTarget = towerPosition;
                // 假设飞行视角（如果没有CameraManager，通过高度判断可能是飞行视角）
                if (cameraPos.y > towerPosition.y + tower.height * 0.5f)
                {
                    fallbackLookAtTarget.y += tower.height * 3.8f;
                }
                
                StartCoroutine(SmoothJumpToPosition(cameraPos, fallbackLookAtTarget));
        
                return;
            }
            else
            {
                Debug.LogError("跳转失败：未找到主摄像机");
                return;
            }
        }
        
        // 计算摄像机目标位置（在电塔旁边）
        Vector3 finalCameraPos = towerPosition + cameraOffset;
        
        // 确保摄像机位置在地面之上
        float finalGroundLevel = GetGroundHeight(finalCameraPos);
        finalCameraPos.y = Mathf.Max(finalCameraPos.y, finalGroundLevel + 5f);
        
        // 根据视角调整观察目标点
        Vector3 lookAtTarget = towerPosition;
        if (cameraManager != null && cameraManager.GetCurrentView() == 2) // 飞行视角
        {
            // 在飞行视角下，观察目标点应该是电塔高度的80%处，与摄像机平视
            lookAtTarget.y += tower.height * 3.8f;
        }
        
        StartCoroutine(SmoothJumpToPosition(finalCameraPos, lookAtTarget));
        

    }
    
    /// <summary>
    /// 平滑跳转到指定位置
    /// </summary>
    System.Collections.IEnumerator SmoothJumpToPosition(Vector3 targetPos, Vector3 lookAtPos)
    {
        isJumping = true;
        
        Camera mainCamera = null;
        
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
            isJumping = false;
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
                    Vector3 downDirection = (lookAtPos - targetPos).normalized;
                    // 确保是向下看的角度
                    downDirection.y = Mathf.Min(downDirection.y, -0.7f); // 强制向下
                    targetRot = Quaternion.LookRotation(downDirection);
                    break;
                    
                case 2: // 飞行视角 - 自然看向目标点
                    Vector3 flyDirection = (lookAtPos - targetPos).normalized;
                    targetRot = Quaternion.LookRotation(flyDirection);
                    break;
                    
                default: // 第一人称视角 - 正常看向电塔
                    Vector3 defaultDirection = (lookAtPos - targetPos).normalized;
                    targetRot = Quaternion.LookRotation(defaultDirection);
                    break;
            }
        }
        else
        {
            // 默认情况：看向电塔
            Vector3 fallbackDirection = (lookAtPos - targetPos).normalized;
            targetRot = Quaternion.LookRotation(fallbackDirection);
        }
        
        float elapsedTime = 0f;
        float duration = 1f / jumpSpeed;
        

        
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
        

        isJumping = false;
    }
    
    /// <summary>
    /// 根据电塔ID计算独特的视角偏移
    /// </summary>
    Vector3 CalculateUniqueViewOffset(int towerId, float towerHeight = 0f)
    {
        // 使用电塔ID作为种子，确保每个电塔都有固定但不同的视角
        float angle = (towerId * angleVariation) % 360f;
        float radianAngle = angle * Mathf.Deg2Rad;
        
        // 计算基础偏移
        float baseDistance = jumpDistance;
        float baseHeight = jumpHeight;
        
        // 根据当前视角调整位置
        if (cameraManager != null)
        {
            int currentView = cameraManager.GetCurrentView();
            switch (currentView)
            {
                case 1: // 上帝视角 - 在塔的顶部俯视
                    baseHeight = 80f; // 设置很高的高度
                    baseDistance = 5f; // 距离很近，几乎在塔的正上方
                    break;
                    
                case 2: // 飞行视角 - 基于电塔高度的80%处平视
                    if (towerHeight > 0f)
                    {
                        baseHeight = towerHeight * 3.8f; // 电塔高度的80%
                    }
                    else
                    {
                        baseHeight *= 3.5f; // 如果没有高度信息，使用原来的逻辑
                    }
                    break;
                    
                default: // 第一人称视角 - 保持原有设置
                    break;
            }
        }
        
        // 根据电塔ID添加一些变化
        float distanceMultiplier = 1f + (towerId % 3) * 0.2f; // 1.0, 1.2, 1.4的变化
        float heightMultiplier = 1f + (towerId % 4) * 0.15f;  // 1.0, 1.15, 1.3, 1.45的变化
        
        // 计算最终偏移
        Vector3 offset = new Vector3(
            Mathf.Sin(radianAngle) * baseDistance * distanceMultiplier,
            baseHeight * heightMultiplier,
            Mathf.Cos(radianAngle) * baseDistance * distanceMultiplier
        );
        
        return offset;
    }
    
    /// <summary>
    /// 获取地面高度
    /// </summary>
    float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        return 0f;
    }
    
    /// <summary>
    /// 获取电塔统计信息
    /// </summary>
    public string GetTowerStatistics()
    {
        int totalTowers = allTowers.Count;
        int normalTowers = allTowers.Count(t => t.status == "正常");
        int warningTowers = allTowers.Count(t => t.status == "警告");
        int errorTowers = allTowers.Count(t => t.status == "异常");
        
        float avgHeight = allTowers.Average(t => t.height);
        float minHeight = allTowers.Min(t => t.height);
        float maxHeight = allTowers.Max(t => t.height);
        
        return $"电塔总数: {totalTowers}\n" +
               $"正常: {normalTowers} | 警告: {warningTowers} | 异常: {errorTowers}\n" +
               $"平均高度: {avgHeight:F1}m\n" +
               $"高度范围: {minHeight:F1}m - {maxHeight:F1}m";
    }
    
    /// <summary>
    /// 搜索电塔
    /// </summary>
    public List<TowerOverviewInfo> SearchTowers(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return allTowers;
        
        return allTowers.Where(t => 
            t.name.Contains(keyword) || 
            t.id.ToString().Contains(keyword) ||
            t.position.ToString().Contains(keyword)
        ).ToList();
    }
    
    /// <summary>
    /// 按距离排序电塔
    /// </summary>
    public List<TowerOverviewInfo> GetTowersByDistance(Vector3 referencePoint)
    {
        return allTowers.OrderBy(t => Vector3.Distance(t.position, referencePoint)).ToList();
    }
    
    /// <summary>
    /// 按高度排序电塔
    /// </summary>
    public List<TowerOverviewInfo> GetTowersByHeight(bool ascending = true)
    {
        return ascending ? 
            allTowers.OrderBy(t => t.height).ToList() : 
            allTowers.OrderByDescending(t => t.height).ToList();
    }
} 