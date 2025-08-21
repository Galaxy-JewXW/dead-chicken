using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraManager : MonoBehaviour
{
    [Header("相机组件")]
    public Camera mainCamera;
    
    [Header("视角控制组件")]
    public FirstPersonCamera firstPersonCamera;
    public GodViewCamera godViewCamera;
    public FlyCamera flyCamera;
    
    [Header("视角位置")]
    public Transform firstPersonView;
    public Transform godView;
    public Transform flyView;
    
    [Header("电塔查找设置")]
    [Tooltip("是否在切换视角时自动寻找最近的电塔")]
    public bool autoFindNearestTower = true;
    
    private int currentView = 0;
    private Transform[] views;
    private string[] viewNames = { "第一人称视角", "上帝视角", "飞行视角" };

    void Start()
    {
        // 自动初始化视角位置，避免重合
        if (firstPersonView != null && firstPersonView.position == Vector3.zero)
            firstPersonView.position = new Vector3(0, 2, -10);
        if (godView != null && godView.position == Vector3.zero)
            godView.position = new Vector3(0, 200, -100); // 调整上帝视角初始位置
        if (flyView != null && flyView.position == Vector3.zero)
            flyView.position = new Vector3(30, 20, 30);
            
        views = new Transform[] { firstPersonView, godView, flyView };
        
        // 自动获取组件
        if (firstPersonCamera == null)
            firstPersonCamera = GetComponent<FirstPersonCamera>();
        if (godViewCamera == null)
            godViewCamera = GetComponent<GodViewCamera>();
        if (flyCamera == null)
            flyCamera = GetComponent<FlyCamera>();
        
        // 确保主相机不包含点云层级（只在弹窗中显示）
        if (mainCamera != null)
        {
            int pointCloudLayer = 31; // 点云层级
            int pointCloudLayerMask = 1 << pointCloudLayer;
            
            // 确保culling mask不包含点云层级
            if ((mainCamera.cullingMask & pointCloudLayerMask) != 0)
            {
                mainCamera.cullingMask &= ~pointCloudLayerMask;
                Debug.Log($"主相机初始化时已设置culling mask，排除点云层级 {pointCloudLayer}");
            }
        }
            
        SwitchView(2); // 改为飞行视角（第三人称）
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SwitchView(0); // 第一人称
        if (Input.GetKeyDown(KeyCode.F2)) SwitchView(1); // 上帝视角
        if (Input.GetKeyDown(KeyCode.F3)) SwitchView(2); // 飞行视角
    }

    public void SwitchView(int idx)
    {
        if (idx < 0 || idx >= views.Length || views[idx] == null) return;
        
        // 停用所有相机控制组件
        if (firstPersonCamera != null) firstPersonCamera.enabled = false;
        if (godViewCamera != null) godViewCamera.enabled = false;
        if (flyCamera != null) flyCamera.enabled = false;
        
        // 重置鼠标状态
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 获取目标位置：优先使用最近的电塔位置，如果没有则使用默认视角位置
        Vector3 targetPosition = views[idx].position;
        Vector3 targetRotation = views[idx].rotation.eulerAngles;
        
        if (autoFindNearestTower)
        {
            Vector3 nearestTowerPosition = FindNearestTowerPosition();
            if (nearestTowerPosition != Vector3.zero)
            {
                targetPosition = CalculateViewPosition(nearestTowerPosition, idx);
                Debug.Log($"切换到 {viewNames[idx]}，使用最近电塔位置: {nearestTowerPosition}");
            }
            else
            {
                Debug.Log($"切换到 {viewNames[idx]}，未找到电塔，使用默认位置");
            }
        }
        
        // 设置相机位置和旋转
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = Quaternion.Euler(targetRotation);
        
        // 根据视角类型启用对应的控制组件
        switch (idx)
        {
            case 0: // 第一人称视角
                if (firstPersonCamera != null)
                {
                    firstPersonCamera.enabled = true;
                    // 设置第一人称相机到合适的地面位置
                    Vector3 fpPosition = targetPosition;
                    fpPosition.y = GetGroundHeight(fpPosition) + 3.0f; // 人眼高度，进一步调高
                    firstPersonCamera.SetPlayerPosition(fpPosition);
                }
                break;
                
            case 1: // 上帝视角
                if (godViewCamera != null)
                {
                    godViewCamera.enabled = true;
                    // 设置上帝视角到合适的俯视位置
                    Vector3 godPosition = targetPosition;
                    godPosition.y = Mathf.Max(godPosition.y, 50f); // 降低最小高度
                    mainCamera.transform.position = godPosition;
                    // 设置俯视角度
                    Vector3 rotation = mainCamera.transform.eulerAngles;
                    rotation.x = 45f; // 45度俯视角，更适合观察电力线
                    mainCamera.transform.eulerAngles = rotation;
                }
                break;
                
            case 2: // 飞行视角
                if (flyCamera != null)
                {
                    flyCamera.enabled = true;
                }
                break;
        }
        
        // 确保主相机不包含点云层级（只在弹窗中显示）
        if (mainCamera != null)
        {
            int pointCloudLayer = 31; // 点云层级
            int pointCloudLayerMask = 1 << pointCloudLayer;
            
            // 确保culling mask不包含点云层级
            if ((mainCamera.cullingMask & pointCloudLayerMask) != 0)
            {
                mainCamera.cullingMask &= ~pointCloudLayerMask;
                Debug.Log($"主相机culling mask已更新，排除点云层级 {pointCloudLayer}");
            }
        }
        
        currentView = idx;
        Debug.Log($"切换到 {viewNames[idx]} (F{idx + 1})");
    }
    
    /// <summary>
    /// 查找最近的电塔位置
    /// </summary>
    private Vector3 FindNearestTowerPosition()
    {
        // 通过TowerOverviewManager查找电塔
        var towerManager = FindObjectOfType<TowerOverviewManager>();
        if (towerManager != null)
        {
            var allTowers = towerManager.GetAllTowers();
            if (allTowers != null && allTowers.Count > 0)
            {
                // 找到最近的电塔
                var nearestTower = allTowers.OrderBy(t => Vector3.Distance(t.position, mainCamera.transform.position)).First();
                return nearestTower.position;
            }
        }
        
        return Vector3.zero; // 没有找到电塔
    }
    
    /// <summary>
    /// 根据电塔位置计算不同视角的相机位置
    /// </summary>
    private Vector3 CalculateViewPosition(Vector3 towerPosition, int viewIndex)
    {
        Vector3 viewPosition = towerPosition;
        
        switch (viewIndex)
        {
            case 0: // 第一人称视角：在电塔附近的地面
                viewPosition.y = GetGroundHeight(towerPosition) + 3.0f;
                viewPosition += new Vector3(5f, 0, -5f); // 稍微偏移，避免卡在电塔里
                break;
                
            case 1: // 上帝视角：在电塔上方的高处
                viewPosition.y += 100f; // 在电塔上方100米
                viewPosition += new Vector3(0, 0, -50f); // 稍微后退，获得更好的俯视角度
                break;
                
            case 2: // 飞行视角：在电塔附近的空中
                viewPosition.y += 20f; // 在电塔上方20米
                viewPosition += new Vector3(15f, 0, -15f); // 斜向偏移
                break;
        }
        
        return viewPosition;
    }
    
    // 获取地面高度的辅助方法
    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        return 0f; // 默认地面高度
    }
    
    // 公共方法：获取当前视角
    public int GetCurrentView()
    {
        return currentView;
    }
    
    // 公共方法：获取当前视角名称
    public string GetCurrentViewName()
    {
        return viewNames[currentView];
    }
    
    /// <summary>
    /// 获取是否自动寻找最近电塔
    /// </summary>
    public bool GetAutoFindNearestTower()
    {
        return autoFindNearestTower;
    }
    
    /// <summary>
    /// 手动设置是否自动寻找最近电塔
    /// </summary>
    public void SetAutoFindNearestTower(bool enabled)
    {
        autoFindNearestTower = enabled;
        Debug.Log($"自动寻找最近电塔功能已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 强制刷新当前视角位置（重新计算到最近电塔的位置）
    /// </summary>
    public void RefreshCurrentViewPosition()
    {
        if (autoFindNearestTower)
        {
            SwitchView(currentView); // 重新切换当前视角
        }
    }
} 