using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UI.SceneOverviewManager;

namespace UI
{
    /// <summary>
    /// 无人机巡检管理器
    /// 负责管理无人机全线路巡检功能
    /// </summary>
    public class DronePatrolManager : MonoBehaviour
    {
        [Header("无人机巡检配置")]
        public float droneSpeed = 2f; // 无人机移动速度
        public float droneHeight = 3.8f; // 无人机高度比例（相对于电塔高度，与第三人称跳转一致）
        public float droneDistance = 25f; // 无人机与电塔的侧边距离（增加距离）
        public float droneStayTime = 0f; // 在每个电塔停留时间（设为0实现匀速巡检）
        
        // 巡检状态
        private bool isDronePatrolling = false;
        private bool isDronePatrolPaused = false;
        private Coroutine dronePatrolCoroutine = null;
        private int currentTowerIndex = 0; // 当前巡检的电塔索引
        private List<TowerData> currentTowers = null; // 当前巡检的电塔列表
        
        // 暂停时的视角控制
        private bool enableCameraControlWhenPaused = true;
        private float mouseRotationSpeed = 2f;
        private float lastMouseX = 0f;
        private float lastMouseY = 0f;
        private bool isMouseControlActive = false;
        
        // 引用组件
        private SceneOverviewManager sceneOverviewManager;
        private CameraManager cameraManager;
        private SimpleUIToolkitManager uiManager;
        
        void Start()
        {
            // 获取必要的组件引用
            sceneOverviewManager = FindObjectOfType<SceneOverviewManager>();
            cameraManager = FindObjectOfType<CameraManager>();
            uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            
            if (sceneOverviewManager == null)
            {
                Debug.LogError("DronePatrolManager: 未找到SceneOverviewManager组件");
            }
            
            if (cameraManager == null)
            {
                Debug.LogError("DronePatrolManager: 未找到CameraManager组件");
            }
            
            if (uiManager == null)
            {
                Debug.LogError("DronePatrolManager: 未找到SimpleUIToolkitManager组件");
            }
        }
        
        /// <summary>
        /// 切换无人机巡检状态
        /// </summary>
        public void ToggleDronePatrol()
        {
            if (isDronePatrolling)
            {
                if (isDronePatrolPaused)
                {
                    ResumeDronePatrol();
                }
                else
                {
                    PauseDronePatrol();
                }
            }
            else
            {
                StartDronePatrol();
            }
        }
        
        /// <summary>
        /// 开始无人机巡检
        /// </summary>
        public void StartDronePatrol()
        {
            if (isDronePatrolling)
            {
                Debug.LogWarning("无人机巡检已在进行中");
                return;
            }
            
            var towers = GetTowerData();
            if (towers == null || towers.Count < 2)
            {
                Debug.LogWarning("电塔数据不足，无法进行巡检");
                if (uiManager != null)
                {
                    uiManager.UpdateStatusBar("电塔数据不足，无法进行巡检");
                }
                return;
            }
            
            // 按照电塔的X坐标排序，确保从第一个塔到最后一个塔
            currentTowers = towers.OrderBy(t => t.position.x).ToList();
            currentTowerIndex = 0; // 从第一个塔开始
            
            isDronePatrolling = true;
            dronePatrolCoroutine = StartCoroutine(DronePatrolCoroutine(currentTowers));
            
            // 更新按钮样式
            UpdateDronePatrolButton();
            
            // 更新状态栏
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar($"开始无人机巡检，共{currentTowers.Count}个电塔 - 按ESC停止，点击按钮暂停");
            }
            
            Debug.Log($"开始无人机巡检，共{currentTowers.Count}个电塔");
        }
        
        /// <summary>
        /// 暂停无人机巡检
        /// </summary>
        public void PauseDronePatrol()
        {
            if (!isDronePatrolling || isDronePatrolPaused)
            {
                return;
            }
            
            isDronePatrolPaused = true;
            UpdateDronePatrolButton();
            
            // 更新状态栏
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar("无人机巡检已暂停 - 按住右键拖拽旋转视角，点击按钮继续");
            }
            
            Debug.Log("暂停无人机巡检");
        }
        
        /// <summary>
        /// 继续无人机巡检
        /// </summary>
        public void ResumeDronePatrol()
        {
            if (!isDronePatrolling || !isDronePatrolPaused)
            {
                return;
            }
            
            isDronePatrolPaused = false;
            UpdateDronePatrolButton();
            
            // 更新状态栏
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar("无人机巡检已继续 - 按ESC停止，点击按钮暂停");
            }
            
            Debug.Log("继续无人机巡检");
        }
        
        /// <summary>
        /// 停止无人机巡检
        /// </summary>
        public void StopDronePatrol()
        {
            if (!isDronePatrolling)
            {
                return;
            }
            
            isDronePatrolling = false;
            isDronePatrolPaused = false;
            currentTowerIndex = 0;
            currentTowers = null;
            
            if (dronePatrolCoroutine != null)
            {
                StopCoroutine(dronePatrolCoroutine);
                dronePatrolCoroutine = null;
            }
            
            // 更新按钮样式
            UpdateDronePatrolButton();
            
            // 更新状态栏
            if (uiManager != null)
            {
                uiManager.UpdateStatusBar("无人机巡检已停止");
            }
            
            Debug.Log("停止无人机巡检");
        }
        
        /// <summary>
        /// 获取电塔数据
        /// </summary>
        private List<TowerData> GetTowerData()
        {
            if (sceneOverviewManager == null)
            {
                Debug.LogError("SceneOverviewManager未找到");
                return null;
            }
            
            // 直接调用SceneOverviewManager的公共方法
            return sceneOverviewManager.GetTowerData();
        }
        
        /// <summary>
        /// 更新无人机巡检按钮样式
        /// </summary>
        private void UpdateDronePatrolButton()
        {
            // 更新SimpleUIToolkitManager中的按钮
            if (uiManager != null)
            {
                uiManager.UpdateDronePatrolButtonStyle();
            }
        }
        
        /// <summary>
        /// 无人机巡检协程
        /// </summary>
        private IEnumerator DronePatrolCoroutine(List<TowerData> towers)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("未找到主摄像机，无法进行巡检");
                StopDronePatrol();
                yield break;
            }
            
            // 切换到飞行视角
            if (cameraManager != null)
            {
                cameraManager.SwitchView(2); // 切换到飞行视角
                yield return new WaitForSeconds(0.5f); // 等待视角切换完成
            }
            
            // 如果是从第一个塔开始，立即跳转到第一个塔的位置
            if (currentTowerIndex == 0 && towers.Count > 0)
            {
                TowerData firstTower = towers[0];
                Vector3 firstDronePosition = CalculateDronePosition(firstTower, 0);
                
                // 确定初始观察目标：如果有下一个塔，看向下一个塔；否则看向当前塔
                Vector3 initialLookTarget = towers.Count > 1 ? towers[1].position : firstTower.position;
                initialLookTarget.y += firstTower.height * droneHeight;
                
                // 立即设置到第一个塔的位置
                mainCamera.transform.position = firstDronePosition;
                Vector3 lookDirection = (initialLookTarget - firstDronePosition).normalized;
                mainCamera.transform.rotation = Quaternion.LookRotation(lookDirection);
                
                yield return new WaitForSeconds(0.2f); // 短暂停留让用户看到跳转效果
                
                // 设置下一个塔的索引
                currentTowerIndex = 1;
            }
            
            // 从当前索引开始巡检
            for (int i = currentTowerIndex; i < towers.Count && isDronePatrolling; i++)
            {
                currentTowerIndex = i; // 更新当前索引
                
                // 等待暂停解除
                yield return StartCoroutine(WaitForUnpause());
                
                if (!isDronePatrolling) break; // 检查是否已停止
                
                TowerData currentTower = towers[i];
                
                // 计算无人机位置：在电塔的3.8倍高度处，侧边观察
                Vector3 dronePosition = CalculateDronePosition(currentTower, i);
                
                // 计算观察目标：始终看向下一个塔的位置
                Vector3 lookAtTarget;
                if (i < towers.Count - 1)
                {
                    // 看向下一个塔
                    lookAtTarget = towers[i + 1].position;
                    lookAtTarget.y += towers[i + 1].height * droneHeight;
                }
                else
                {
                    // 最后一个塔，看向当前塔
                    lookAtTarget = currentTower.position;
                    lookAtTarget.y += currentTower.height * droneHeight;
                }
                
                // 计算移动时间 - 使用固定速度实现匀速巡检
                float distance = Vector3.Distance(mainCamera.transform.position, dronePosition);
                float moveTime = distance / droneSpeed;
                moveTime = Mathf.Clamp(moveTime, 0.5f, 8f); // 调整时间范围，允许更长的移动时间
                
                // 平滑移动到目标位置
                yield return StartCoroutine(SmoothMoveTo(mainCamera, dronePosition, lookAtTarget, moveTime));
                
                // 取消停留时间，实现匀速巡检
                if (droneStayTime > 0 && isDronePatrolling)
                {
                    yield return new WaitForSeconds(droneStayTime);
                }
            }
            
            // 巡检完成
            if (isDronePatrolling)
            {
                Debug.Log("无人机巡检完成");
                
                // 更新状态栏
                if (uiManager != null)
                {
                    uiManager.UpdateStatusBar("无人机巡检完成");
                }
                
                StopDronePatrol();
            }
        }
        
        /// <summary>
        /// 等待暂停解除的协程
        /// </summary>
        private IEnumerator WaitForUnpause()
        {
            while (isDronePatrolling && isDronePatrolPaused)
            {
                yield return null;
            }
        }
        
        /// <summary>
        /// 计算无人机在电塔侧边的位置
        /// </summary>
        private Vector3 CalculateDronePosition(TowerData tower, int towerIndex)
        {
            // 使用电塔索引来确定观察角度，但保持相对稳定的视角
            float angle = (towerIndex * 30f) % 360f; // 减少角度差，让视角更稳定
            float radianAngle = angle * Mathf.Deg2Rad;
            
            // 计算侧边位置
            Vector3 sideOffset = new Vector3(
                Mathf.Sin(radianAngle) * droneDistance,
                0f,
                Mathf.Cos(radianAngle) * droneDistance
            );
            
            // 计算最终位置：电塔位置 + 侧边偏移 + 高度
            Vector3 dronePosition = tower.position + sideOffset;
            dronePosition.y = tower.position.y + tower.height * droneHeight;
            
            // 确保无人机位置在地面之上
            float groundHeight = GetGroundHeight(dronePosition);
            dronePosition.y = Mathf.Max(dronePosition.y, groundHeight + 5f);
            
            return dronePosition;
        }
        
        /// <summary>
        /// 获取地面高度
        /// </summary>
        private float GetGroundHeight(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                return hit.point.y;
            }
            return 0f;
        }
        
        /// <summary>
        /// 平滑移动摄像机到目标位置
        /// </summary>
        private IEnumerator SmoothMoveTo(Camera camera, Vector3 targetPosition, Vector3 lookAtTarget, float duration)
        {
            Vector3 startPosition = camera.transform.position;
            Quaternion startRotation = camera.transform.rotation;
            
            // 计算目标旋转
            Vector3 lookDirection = (lookAtTarget - targetPosition).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration && isDronePatrolling)
            {
                // 暂停时停止移动
                if (isDronePatrolPaused)
                {
                    yield return null;
                    continue;
                }
                
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // 使用平滑曲线
                t = Mathf.SmoothStep(0f, 1f, t);
                
                // 更新摄像机位置和旋转
                camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                
                yield return null;
            }
            
            // 确保最终位置准确
            if (isDronePatrolling && !isDronePatrolPaused)
            {
                camera.transform.position = targetPosition;
                camera.transform.rotation = targetRotation;
            }
        }
        
        /// <summary>
        /// 获取巡检状态
        /// </summary>
        public bool IsPatrolling => isDronePatrolling;
        
        /// <summary>
        /// 获取暂停状态
        /// </summary>
        public bool IsPaused => isDronePatrolPaused;

        /// <summary>
        /// 在对象销毁时停止巡检
        /// </summary>
        void OnDestroy()
        {
            if (isDronePatrolling)
            {
                StopDronePatrol();
            }
        }

        void Update()
        {
            // ESC键停止巡检
            if (Input.GetKeyDown(KeyCode.Escape) && isDronePatrolling)
            {
                StopDronePatrol();
                return;
            }
            
            // 暂停时的视角控制
            if (isDronePatrolling && isDronePatrolPaused && enableCameraControlWhenPaused)
            {
                HandleCameraControlWhenPaused();
            }
        }
        
        /// <summary>
        /// 处理暂停时的摄像机控制
        /// </summary>
        private void HandleCameraControlWhenPaused()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            // 检测鼠标右键按下
            if (Input.GetMouseButtonDown(1))
            {
                isMouseControlActive = true;
                lastMouseX = Input.mousePosition.x;
                lastMouseY = Input.mousePosition.y;
            }
            
            // 检测鼠标右键释放
            if (Input.GetMouseButtonUp(1))
            {
                isMouseControlActive = false;
            }
            
            // 鼠标右键按住时进行视角旋转
            if (isMouseControlActive && Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseRotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * mouseRotationSpeed;
                
                // 水平旋转
                mainCamera.transform.Rotate(Vector3.up, mouseX, Space.World);
                
                // 垂直旋转
                mainCamera.transform.Rotate(Vector3.right, -mouseY, Space.Self);
                
                // 限制垂直旋转角度，避免翻转
                Vector3 eulerAngles = mainCamera.transform.eulerAngles;
                if (eulerAngles.x > 180f) eulerAngles.x -= 360f;
                eulerAngles.x = Mathf.Clamp(eulerAngles.x, -80f, 80f);
                mainCamera.transform.eulerAngles = eulerAngles;
            }
        }
    }
} 