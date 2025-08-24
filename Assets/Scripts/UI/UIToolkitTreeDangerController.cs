using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
// 简化功能，删除LINQ引用

/// <summary>
/// UI Toolkit树木危险监测控制器 - 简化版本
/// 专注于核心功能：参数设置、监测控制、统计显示和危险树木列表
/// </summary>
public class UIToolkitTreeDangerController : MonoBehaviour
{
         [Header("监测系统设置")]
     public bool enableAutoMonitoring = true;
     public float monitoringInterval = 1f;
     public float maxDetectionDistance = 100f;
    
    [Header("危险评估参数")]
    public float criticalDistance = 5f;   // 危险距离 - 降低到5米
    public float warningDistance = 15f;   // 警告距离 - 降低到15米
    public float safeDistance = 30f;      // 安全距离 - 降低到30米
    
    // 简化参数，删除生长率参数
    
    // 简化参数，删除高树检测相关参数
    
    // UI管理器引用
    private SimpleUIToolkitManager uiManager;
    
    // 监测系统引用
    private TreeDangerMonitor treeDangerMonitor;
    
                   // UI元素引用
      private VisualElement treeDangerPanel;
      private VisualElement controlSection;
     
     // 显示元素
     // 删除不再需要的变量
     // private Label statisticsLabel;
     private VisualElement treeListContainer;
    
    // 监测状态
    private bool isMonitoring = false;
    
    void Start()
    {
        // 查找UI管理器
        uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (uiManager == null)
        {
            Debug.LogError("未找到SimpleUIToolkitManager，UIToolkitTreeDangerController无法工作");
            return;
        }
        
        // 查找或创建监测系统
        treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        if (treeDangerMonitor == null)
        {
            var monitorObj = new GameObject("TreeDangerMonitor");
            treeDangerMonitor = monitorObj.AddComponent<TreeDangerMonitor>();
            Debug.Log("已创建TreeDangerMonitor组件");
        }
        
        // 确保参数值已设置
        Debug.Log($"参数初始化 - 危险距离: {criticalDistance}, 警告距离: {warningDistance}, 安全距离: {safeDistance}");
        
        // 同步参数
        SyncMonitoringParameters();
        
        Initialize();
        
        // 启动自动刷新协程
        StartCoroutine(AutoRefreshCoroutine());
        
        // 启动延迟刷新UI协程，确保参数值显示
        StartCoroutine(DelayedRefreshUI());
        
        // 启动场景检查协程
        StartCoroutine(CheckSceneObjects());
    }
    
    /// <summary>
    /// 延迟刷新UI的协程，确保参数值正确显示
    /// </summary>
    IEnumerator DelayedRefreshUI()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 强制刷新显示
        if (treeDangerPanel != null)
        {
            RefreshDisplay();
            Debug.Log("延迟刷新UI完成");
        }
    }
    
    /// <summary>
    /// 检查场景中的对象情况
    /// </summary>
    IEnumerator CheckSceneObjects()
    {
        yield return new WaitForSeconds(1f);
        
        // 检查场景中的树木
        var trees = FindObjectsOfType<GameObject>().Where(obj => obj.name.ToLower().Contains("tree") || obj.name.ToLower().Contains("树")).ToArray();
        Debug.Log($"场景中找到 {trees.Length} 个树木对象");
        
        // 检查场景中的电力线
        var powerlines = FindObjectsOfType<PowerlineInteraction>();
        Debug.Log($"场景中找到 {powerlines.Length} 个电力线对象");
        
        // 检查距离情况
        if (trees.Length > 0 && powerlines.Length > 0)
        {
            var nearestTree = trees[0];
            var nearestPowerline = powerlines[0];
            var distance = Vector3.Distance(nearestTree.transform.position, nearestPowerline.transform.position);
            Debug.Log($"示例：树木 '{nearestTree.name}' 与电力线 '{nearestPowerline.name}' 的距离为 {distance:F2}m");
            
            if (distance <= criticalDistance)
            {
                Debug.Log($"⚠️ 发现危险情况！距离 {distance:F2}m <= 危险阈值 {criticalDistance}m");
            }
            else if (distance <= warningDistance)
            {
                Debug.Log($"⚠️ 发现警告情况！距离 {distance:F2}m <= 警告阈值 {warningDistance}m");
            }
            else if (distance <= safeDistance)
            {
                Debug.Log($"⚠️ 发现安全边界情况！距离 {distance:F2}m <= 安全阈值 {safeDistance}m");
            }
            else
            {
                Debug.Log($"✅ 距离安全，距离 {distance:F2}m > 安全阈值 {safeDistance}m");
            }
        }
    }
    
    public void Initialize()
    {
        if (uiManager == null) return;
        Debug.Log("UIToolkitTreeDangerController已初始化");
    }
    
         /// <summary>
     /// 创建树木危险监测面板UI
     /// </summary>
     public VisualElement CreateTreeDangerPanel()
     {
         treeDangerPanel = new VisualElement();
         treeDangerPanel.style.width = Length.Percent(100);
         treeDangerPanel.style.height = Length.Percent(100);
         treeDangerPanel.style.flexDirection = FlexDirection.Column;
         
         // 创建控制区域
         CreateControlSection();
         
         // 创建合并的统计和危险树木区域
         CreateMergedStatisticsAndTreeListSection();
         
         return treeDangerPanel;
     }
    
    void CreateControlSection()
    {
        controlSection = new VisualElement();
        controlSection.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        controlSection.style.marginBottom = 10;
        controlSection.style.paddingTop = 12;
        controlSection.style.paddingBottom = 12;
         controlSection.style.paddingLeft = 10;
         controlSection.style.paddingRight = 10;
        controlSection.style.borderTopLeftRadius = 8;
        controlSection.style.borderTopRightRadius = 8;
        controlSection.style.borderBottomLeftRadius = 8;
        controlSection.style.borderBottomRightRadius = 8;
        controlSection.style.borderLeftWidth = 2;
        controlSection.style.borderLeftColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlSection.style.flexShrink = 0;
         controlSection.style.width = Length.Percent(100);
         controlSection.style.maxWidth = Length.Percent(100);
        
        // 标题
         var titleLabel = new Label("树木危险监测");
        titleLabel.style.color = new Color(0.1f, 0.5f, 0.1f, 1f);
         titleLabel.style.fontSize = 16;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
         titleLabel.style.marginBottom = 15;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        controlSection.Add(titleLabel);
        
        // 参数控制区域
        CreateParameterControls();
        
        treeDangerPanel.Add(controlSection);
    }
    
    void CreateParameterControls()
    {
        // 创建参数控制容器
        var paramContainer = new VisualElement();
        paramContainer.style.flexDirection = FlexDirection.Column;
        paramContainer.style.marginBottom = 15;
        
         // 只保留核心参数
        CreateSimplifiedParameterRow("危险距离:", criticalDistance, (value) => {
            criticalDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.criticalDistance = value;
        }, paramContainer);
        
        CreateSimplifiedParameterRow("警告距离:", warningDistance, (value) => {
            warningDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.warningDistance = value;
        }, paramContainer);
        
        CreateSimplifiedParameterRow("安全距离:", safeDistance, (value) => {
            safeDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.safeDistance = value;
        }, paramContainer);
        
        controlSection.Add(paramContainer);
        
         // 创建控制按钮
        CreateControlButtons();
        
        // 强制刷新参数值显示
        StartCoroutine(ForceRefreshParameterValues(paramContainer));
    }
    
    /// <summary>
    /// 强制刷新参数值显示的协程
    /// </summary>
    IEnumerator ForceRefreshParameterValues(VisualElement paramContainer)
    {
        yield return new WaitForSeconds(0.1f);
        
        // 查找所有TextField并强制设置值
        var textFields = paramContainer.Query<TextField>().ToList();
        Debug.Log($"找到 {textFields.Count} 个TextField");
        
        foreach (var textField in textFields)
        {
            if (textField != null)
            {
                // 根据TextField的父级标签来确定应该设置什么值
                var parent = textField.parent;
                if (parent != null)
                {
                    var label = parent.Q<Label>();
                    if (label != null)
                    {
                        string labelText = label.text;
                        float value = 0f;
                        
                        if (labelText.Contains("危险距离"))
                            value = criticalDistance;
                        else if (labelText.Contains("警告距离"))
                            value = warningDistance;
                        else if (labelText.Contains("安全距离"))
                            value = safeDistance;
            // 简化参数，删除基础生长率
            // 简化参数，删除高树检测相关参数
                        
                        if (value > 0f)
                        {
                            textField.value = value.ToString("F1");
                            Debug.Log($"强制设置 {labelText}: {value}, TextField值: {textField.value}");
                        }
                    }
                }
            }
        }
        
        // 再次验证所有TextField的值
        yield return new WaitForSeconds(0.1f);
        foreach (var textField in textFields)
        {
            if (textField != null)
            {
                Debug.Log($"TextField最终值: {textField.value}");
            }
        }
    }
    
    void CreateSimplifiedParameterRow(string labelText, float defaultValue, Action<float> onValueChanged, VisualElement container)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;
        
        // 标签
        var label = new Label(labelText);
        label.style.fontSize = 11;
        label.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        label.style.minWidth = 120;
        uiManager?.ApplyFont(label);
        row.Add(label);
        
        // 数值输入框 - 参考其他UI的实现方式
        var textField = new TextField();
        
        // 确保默认值不为0或无效值
        float displayValue = defaultValue;
        if (displayValue <= 0f)
        {
            // 如果默认值无效，设置合理的默认值
            if (labelText.Contains("危险距离"))
                displayValue = 5f;  // 降低到5米
            else if (labelText.Contains("警告距离"))
                displayValue = 15f; // 降低到15米
            else if (labelText.Contains("安全距离"))
                displayValue = 30f; // 降低到30米
            // 简化参数，删除基础生长率
        }
        
        // 直接设置初始值 - 参考其他UI的实现
        textField.value = displayValue.ToString("F1");
        
        // 设置样式 - 参考其他UI的样式设置
        textField.style.width = 80;
        textField.style.height = 25;
        textField.style.fontSize = 12;
        textField.style.color = Color.black;
        textField.style.backgroundColor = Color.white;
        textField.style.borderTopWidth = 1;
        textField.style.borderBottomWidth = 1;
        textField.style.borderLeftWidth = 1;
        textField.style.borderRightWidth = 1;
        textField.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.paddingTop = 4;
        textField.style.paddingBottom = 4;
        textField.style.paddingLeft = 6;
        textField.style.paddingRight = 6;
        textField.style.borderTopLeftRadius = 3;
        textField.style.borderTopRightRadius = 3;
        textField.style.borderBottomLeftRadius = 3;
        textField.style.borderBottomRightRadius = 3;
        
        // 应用字体
        uiManager?.ApplyFont(textField);
        
        // 添加调试信息
        Debug.Log($"创建参数行: {labelText}, 默认值: {defaultValue}, 显示值: {displayValue}, TextField值: {textField.value}");
        
        // 注册值改变回调
        textField.RegisterValueChangedCallback(evt => {
            Debug.Log($"参数值改变: {labelText} 从 {evt.previousValue} 到 {evt.newValue}");
            if (float.TryParse(evt.newValue, out float newValue))
            {
                onValueChanged(newValue);
                Debug.Log($"参数已更新: {labelText} = {newValue}");
            }
            else
            {
                Debug.LogWarning($"无法解析数值: {evt.newValue}");
            }
        });
        
        // 添加到行容器
        row.Add(textField);
        container.Add(row);
        
        // 验证TextField是否正确设置
        Debug.Log($"参数行创建完成: {labelText}, TextField最终值: {textField.value}");
    }
    
                   void CreateControlButtons()
      {
          // 创建按钮容器
          var buttonContainer = new VisualElement();
          buttonContainer.style.flexDirection = FlexDirection.Column;
          buttonContainer.style.marginBottom = 15;
          
                                          // 开始危险树木监测按钮
             var dangerMonitoringButton = new Button(() => GenerateRandomDangerTrees());
             dangerMonitoringButton.text = "开始危险树木监测";
            dangerMonitoringButton.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            dangerMonitoringButton.style.color = Color.white;
            dangerMonitoringButton.style.fontSize = 14;
            dangerMonitoringButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            dangerMonitoringButton.style.paddingTop = 10;
            dangerMonitoringButton.style.paddingBottom = 10;
            dangerMonitoringButton.style.paddingLeft = 15;
            dangerMonitoringButton.style.paddingRight = 15;
            dangerMonitoringButton.style.borderTopLeftRadius = 8;
            dangerMonitoringButton.style.borderTopRightRadius = 8;
            dangerMonitoringButton.style.borderBottomLeftRadius = 8;
            dangerMonitoringButton.style.borderBottomRightRadius = 8;
            dangerMonitoringButton.style.width = Length.Percent(100);
            dangerMonitoringButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            uiManager?.ApplyFont(dangerMonitoringButton);
           buttonContainer.Add(dangerMonitoringButton);
 
           
                     // 简化功能，删除自动识别高树危险按钮
          
          controlSection.Add(buttonContainer);
          
          // 删除状态显示
          // CreateStatusDisplay();
      }
    
         // 简化功能，删除测试方法
     
     // 简化功能，删除高树危险检测方法
     
     // 简化功能，删除高度计算方法
     
     // 简化功能，删除电力线高度计算方法
     
     // 简化功能，删除高树危险判断方法
     
          // 简化功能，删除标记树木危险方法
     
          // 简化功能，删除恢复材质方法
     
          // 删除状态显示方法
     // void CreateStatusDisplay()
     
     /// <summary>
     /// 创建合并的统计和危险树木区域
     /// </summary>
     void CreateMergedStatisticsAndTreeListSection()
     {
         // 创建合并区域
         var mergedSection = new VisualElement();
         mergedSection.style.backgroundColor = new Color(0.95f, 0.97f, 1f, 1f);
         mergedSection.style.marginBottom = 10;
         mergedSection.style.paddingTop = 15;
         mergedSection.style.paddingBottom = 15;
         mergedSection.style.paddingLeft = 12;
         mergedSection.style.paddingRight = 12;
         mergedSection.style.borderTopLeftRadius = 8;
         mergedSection.style.borderTopRightRadius = 8;
         mergedSection.style.borderBottomLeftRadius = 8;
         mergedSection.style.borderBottomRightRadius = 8;
         mergedSection.style.borderLeftWidth = 3;
         mergedSection.style.borderLeftColor = new Color(0.3f, 0.6f, 1f, 1f);
         mergedSection.style.flexShrink = 0;
         mergedSection.style.width = Length.Percent(100);
         mergedSection.style.maxWidth = Length.Percent(100);
         
         // 合并标题
         var titleLabel = new Label("监测统计与危险树木");
         titleLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
         titleLabel.style.fontSize = 16;
         titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
         titleLabel.style.marginBottom = 15;
         titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
         uiManager?.ApplyFont(titleLabel);
         mergedSection.Add(titleLabel);
         
         // 创建统计信息显示
         CreateMergedStatisticsDisplay(mergedSection);
         
         // 创建危险树木列表
         CreateMergedTreeListDisplay(mergedSection);
         
         treeDangerPanel.Add(mergedSection);
     }
     
           /// <summary>
      /// 创建合并后的统计信息显示
      /// </summary>
      void CreateMergedStatisticsDisplay(VisualElement parent)
      {
          // 创建统计信息容器
          var statsContainer = new VisualElement();
          statsContainer.style.flexDirection = FlexDirection.Column;
          statsContainer.style.marginBottom = 15;
          statsContainer.style.paddingTop = 10;
          statsContainer.style.paddingBottom = 10;
          statsContainer.style.paddingLeft = 8;
          statsContainer.style.paddingRight = 8;
          statsContainer.style.backgroundColor = new Color(1f, 0.98f, 0.95f, 1f);
          statsContainer.style.borderTopLeftRadius = 6;
          statsContainer.style.borderTopRightRadius = 6;
          statsContainer.style.borderBottomLeftRadius = 6;
          statsContainer.style.borderBottomRightRadius = 6;
          statsContainer.style.borderLeftWidth = 2;
          statsContainer.style.borderLeftColor = new Color(1f, 0.6f, 0.2f, 1f);
          
          // 获取监测统计信息 - 使用与统计大屏相同的数据源
          var treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
          int totalTrees = 0;
          int safeTrees = 0;
          int warningTrees = 0;
          int criticalTrees = 0;
          int emergencyTrees = 0;
          
          if (treeDangerMonitor != null)
          {
              // 使用与统计大屏相同的方法获取数据
              var dangerStats = treeDangerMonitor.GetDangerStatistics();
              
              // 统计总树木数
              totalTrees = 0;
              foreach (var kvp in dangerStats)
              {
                  totalTrees += kvp.Value;
              }
              
              // 设置各状态树木数量
              safeTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Safe) ? 
                  dangerStats[TreeDangerMonitor.TreeDangerLevel.Safe] : 0;
              warningTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Warning) ? 
                  dangerStats[TreeDangerMonitor.TreeDangerLevel.Warning] : 0;
              criticalTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Critical) ? 
                  dangerStats[TreeDangerMonitor.TreeDangerLevel.Critical] : 0;
              emergencyTrees = dangerStats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Emergency) ? 
                  dangerStats[TreeDangerMonitor.TreeDangerLevel.Emergency] : 0;
              
              Debug.Log($"树木检测界面统计: 总树木数={totalTrees}, 安全={safeTrees}, 警告={warningTrees}, 危险={criticalTrees}, 紧急={emergencyTrees}");
          }
                     else
           {
               // 如果找不到TreeDangerMonitor，使用原来的方法作为备选
               var allTrees = FindObjectsOfType<GameObject>().Where(obj => 
                   obj.name.ToLower().Contains("tree") || 
                   obj.name.ToLower().Contains("植物") || 
                   obj.name.ToLower().Contains("树")).ToArray();
               
                               // 过滤掉系统组件
                var realTrees = allTrees.Where(obj => 
                    !obj.name.Contains("TreeDangerMonitor") && 
                    !obj.name.Contains("TreeDanger") &&
                    !obj.name.Contains("Monitor") &&
                    !obj.name.Contains("Controller") &&
                    !obj.name.Contains("System") &&
                    !obj.name.Contains("Manager") &&
                    !obj.name.Contains("UI") &&
                    !obj.name.Contains("Panel")).ToArray();
               
               totalTrees = realTrees.Length;
               safeTrees = totalTrees; // 默认为安全状态
               Debug.LogWarning($"未找到TreeDangerMonitor，使用备选统计方法。过滤前: {allTrees.Length}，过滤后: {realTrees.Length}");
           }
          
          // 显示统计信息
          var statsLabel = new Label($"树木总数: {totalTrees}棵 (安全:{safeTrees} 警告:{warningTrees} 危险:{criticalTrees} 紧急:{emergencyTrees})");
          statsLabel.style.fontSize = 13;
          statsLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
          statsLabel.style.marginBottom = 8;
          statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
          uiManager?.ApplyFont(statsLabel);
          statsContainer.Add(statsLabel);
          
          parent.Add(statsContainer);
      }
     
     /// <summary>
     /// 创建合并后的危险树木列表显示
     /// </summary>
     void CreateMergedTreeListDisplay(VisualElement parent)
     {
                   // 创建滚动视图
          var scrollView = new ScrollView();
          scrollView.style.minHeight = 250;
          scrollView.style.maxHeight = 500;
          scrollView.style.flexGrow = 1;
          scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
          scrollView.style.overflow = Overflow.Hidden;
          scrollView.scrollDecelerationRate = 0.9f;
 
         // 添加滚轮事件处理
         scrollView.RegisterCallback<WheelEvent>(evt =>
         {
             scrollView.scrollOffset += new Vector2(0, evt.delta.y * 200f);
             evt.StopPropagation();
         });
 
         // 危险树木列表容器
         treeListContainer = new VisualElement();
         treeListContainer.style.flexDirection = FlexDirection.Column;
         treeListContainer.style.flexShrink = 0;
         
         // 初始化危险树木列表
         CreateSimplifiedTreeListContainer();
         
         scrollView.Add(treeListContainer);
         parent.Add(scrollView);
     }
    
         void CreateSimplifiedTreeListContainer()
     {
         // 检查是否有危险树木（包括随机标记的和监测发现的）
         var dangerousTrees = GetAllDangerousTrees();
         
         if (dangerousTrees.Count > 0)
         {
             // 显示危险树木列表
             DisplayAllDangerousTrees(dangerousTrees);
         }
         else
         {
             // 显示无危险树木信息
             CreateNoDangerTreesDisplay();
         }
     }
    
    /// <summary>
    /// 获取所有危险树木（包括随机标记的和监测发现的）
    /// </summary>
    List<GameObject> GetAllDangerousTrees()
    {
        var dangerousTrees = new List<GameObject>();
        
        // 优先使用TreeDangerMonitor的监测数据
        var treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        if (treeDangerMonitor != null)
        {
            // 从TreeDangerMonitor获取危险树木列表
            var monitoredDangerousTrees = treeDangerMonitor.GetDangerousTreesList();
            if (monitoredDangerousTrees.Count > 0)
            {
                dangerousTrees.AddRange(monitoredDangerousTrees);
                Debug.Log($"从TreeDangerMonitor获取到 {monitoredDangerousTrees.Count} 棵危险树木");
            }
        }
        
        // 备选方法：查找所有带有DangerMarker组件的树木（用于兼容性）
        var allTrees = FindObjectsOfType<GameObject>().Where(obj => 
            obj.name.ToLower().Contains("tree") || 
            obj.name.ToLower().Contains("植物") || 
            obj.name.ToLower().Contains("树")).ToArray();
            
        foreach (var tree in allTrees)
        {
            // 过滤掉系统组件和监测器组件
            if (tree.name.Contains("TreeDangerMonitor") || 
                tree.name.Contains("TreeDanger") ||
                tree.name.Contains("Monitor") ||
                tree.name.Contains("Controller") ||
                tree.name.Contains("System") ||
                tree.name.Contains("Manager") ||
                tree.name.Contains("UI") ||
                tree.name.Contains("Panel"))
            {
                continue; // 跳过系统组件
            }
            
            // 检查树木本身或其子对象是否有DangerMarker
            if (tree.GetComponent<DangerMarker>() != null || 
                tree.GetComponentInChildren<DangerMarker>() != null)
            {
                // 避免重复添加
                if (!dangerousTrees.Contains(tree))
                {
                    dangerousTrees.Add(tree);
                }
            }
        }
        
        Debug.Log($"总共找到 {dangerousTrees.Count} 棵危险树木");
        return dangerousTrees;
    }
    
         void CreateNoDangerTreesDisplay()
     {
         // 创建简化的无危险树木信息
         var noDangerPanel = new VisualElement();
         noDangerPanel.style.backgroundColor = new Color(0.95f, 0.98f, 0.95f, 1f);
         noDangerPanel.style.marginBottom = 10;
         noDangerPanel.style.paddingTop = 10;
         noDangerPanel.style.paddingBottom = 10;
         noDangerPanel.style.paddingLeft = 10;
         noDangerPanel.style.paddingRight = 10;
         noDangerPanel.style.borderTopLeftRadius = 6;
         noDangerPanel.style.borderTopRightRadius = 6;
         noDangerPanel.style.borderBottomLeftRadius = 6;
         noDangerPanel.style.borderBottomRightRadius = 6;
         noDangerPanel.style.borderLeftWidth = 1;
         noDangerPanel.style.borderLeftColor = new Color(0.2f, 0.7f, 0.2f, 1f);
         
         // 简化的信息
         var infoLabel = new Label("暂无危险树木");
         infoLabel.style.fontSize = 12;
         infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
         infoLabel.style.color = new Color(0.4f, 0.6f, 0.4f, 1f);
         uiManager?.ApplyFont(infoLabel);
         noDangerPanel.Add(infoLabel);
         
         treeListContainer.Add(noDangerPanel);
     }
    
    
    
    /// <summary>
    /// 延迟刷新显示的协程
    /// </summary>
    IEnumerator DelayedRefreshDisplay()
    {
        yield return new WaitForSeconds(0.5f);
        RefreshDisplay();
    }
    

    
         void GenerateRandomDangerTrees()
     {
         // 启动协程来避免卡死
         StartCoroutine(GenerateRandomDangerTreesCoroutine());
     }
     
     IEnumerator GenerateRandomDangerTreesCoroutine()
    {
                 if (treeDangerMonitor == null)
         {
             UpdateStatus("TreeDangerMonitor未找到");
             yield break;
         }
        
        // 获取场景中的所有树木
        var allSceneTrees = FindObjectsOfType<GameObject>().Where(obj => 
            obj.name.ToLower().Contains("tree") || 
            obj.name.ToLower().Contains("植物") || 
            obj.name.ToLower().Contains("树")).ToArray();
            
        // 过滤掉系统组件
        var sceneTrees = allSceneTrees.Where(obj => 
            !obj.name.Contains("TreeDangerMonitor") && 
            !obj.name.Contains("TreeDanger") &&
            !obj.name.Contains("Monitor") &&
            !obj.name.Contains("Controller") &&
            !obj.name.Contains("System") &&
            !obj.name.Contains("Manager") &&
            !obj.name.Contains("UI") &&
            !obj.name.Contains("Panel")).ToArray();
            
                 if (sceneTrees.Length == 0)
         {
             UpdateStatus("场景中没有找到树木对象");
             yield break;
         }
        
        // 获取场景中的电塔
        var powerTowers = FindObjectsOfType<GameObject>().Where(obj => 
            obj.name.ToLower().Contains("tower") || 
            obj.name.ToLower().Contains("电塔") ||
            obj.name.ToLower().Contains("pole") ||
            obj.name.ToLower().Contains("杆")).ToArray();
        
        if (powerTowers.Length == 0)
        {
            UpdateStatus("场景中没有找到电塔对象，将使用随机选择");
        }
        
                 // 减少标记数量：选择更少的树木作为危险树木（最多5-10棵）
         int totalTrees = sceneTrees.Length;
         int maxDangerTrees = Mathf.Min(10, Mathf.Max(3, totalTrees / 50)); // 最多10棵，最少3棵
         int dangerTreeCount = UnityEngine.Random.Range(3, maxDangerTrees + 1);
        
                 // 先清除所有现有标记
         treeDangerMonitor.ClearAllDangerMarkers();
         
         // 显示进度信息
         UpdateStatus("正在分析树木与电力线路的距离关系...");
        
                 // 优化：预计算电力线路段，使用更智能的连接方式
         var powerlineSegments = new List<(Vector3 start, Vector3 end)>();
         if (powerTowers.Length >= 2)
         {
             // 计算所有电塔之间的可能连接，但优先选择距离较近的连接
             var allConnections = new List<(Vector3 start, Vector3 end, float distance)>();
             
             for (int i = 0; i < powerTowers.Length; i++)
             {
                 for (int j = i + 1; j < powerTowers.Length; j++)
                 {
                     float distance = Vector3.Distance(powerTowers[i].transform.position, powerTowers[j].transform.position);
                     // 只考虑距离合理的连接（比如100米以内）
                     if (distance <= 100f)
                     {
                         allConnections.Add((powerTowers[i].transform.position, powerTowers[j].transform.position, distance));
                     }
                 }
             }
             
             // 按距离排序，选择最近的连接作为电力线路
             var sortedConnections = allConnections.OrderBy(x => x.distance).ToList();
             
             // 选择前15个最近的连接作为主要电力线路（增加覆盖范围）
             int maxLines = Mathf.Min(15, sortedConnections.Count);
             for (int i = 0; i < maxLines; i++)
             {
                 powerlineSegments.Add((sortedConnections[i].start, sortedConnections[i].end));
             }
             
             Debug.Log($"创建了 {powerlineSegments.Count} 条电力线路，覆盖 {powerTowers.Length} 个电塔");
         }
         
         // 计算每棵树的危险评分（必须比电塔高且距离电力线路很近）
         var treeScores = new List<(GameObject tree, float score)>();
         
         // 优化：分批处理树木，避免一次性处理太多
         int batchSize = 50; // 减少批次大小，提高响应性
         int processedCount = 0;
         
         for (int batchStart = 0; batchStart < sceneTrees.Length; batchStart += batchSize)
         {
             int batchEnd = Mathf.Min(batchStart + batchSize, sceneTrees.Length);
             
             for (int i = batchStart; i < batchEnd; i++)
             {
                 var tree = sceneTrees[i];
                 float score = 0f;
                 
                                   // 计算树木高度
                  float treeHeight = CalculateTreeHeight(tree);
                  
                  // 计算到电力线路的最短距离和对应的电塔高度
                  float minDistanceToPowerline = float.MaxValue;
                  float nearestTowerHeight = 0f;
                  Vector3 nearestTowerPos = Vector3.zero;
                  
                  if (powerlineSegments.Count > 0)
                  {
                      Vector3 treePos = tree.transform.position;
                      
                      // 检查每条电力线路，找到最近的线路和对应的电塔
                      foreach (var segment in powerlineSegments)
                      {
                          float distanceToLine = DistanceToLineSegment(treePos, segment.start, segment.end);
                          
                          if (distanceToLine < minDistanceToPowerline)
                          {
                              minDistanceToPowerline = distanceToLine;
                              
                              // 找到最近的线路后，计算对应电塔的高度
                              // 选择距离树木更近的电塔作为参考
                              float distToStart = Vector3.Distance(treePos, segment.start);
                              float distToEnd = Vector3.Distance(treePos, segment.end);
                              
                              if (distToStart < distToEnd)
                              {
                                  nearestTowerPos = segment.start;
                              }
                              else
                              {
                                  nearestTowerPos = segment.end;
                              }
                              
                              // 找到对应的电塔对象并获取其高度
                              foreach (var tower in powerTowers)
                              {
                                  if (Vector3.Distance(tower.transform.position, nearestTowerPos) < 1f)
                                  {
                                      nearestTowerHeight = CalculateTowerHeight(tower);
                                      break;
                                  }
                              }
                          }
                      }
                  }
                  else if (powerTowers.Length == 1)
                  {
                      // 如果只有一个电塔，使用到电塔的距离
                      minDistanceToPowerline = Vector3.Distance(tree.transform.position, powerTowers[0].transform.position);
                      nearestTowerHeight = CalculateTowerHeight(powerTowers[0]);
                  }
                  else
                  {
                      // 如果没有电塔，跳过这棵树（不标记为危险）
                      continue;
                  }
                  
                                     // 关键条件1：树木达到电塔30%高度即可视为危险（降低要求）
                   if (treeHeight < nearestTowerHeight * 0.3f)
                   {
                       continue; // 树木高度不足电塔的30%，跳过
                   }
                  
                                     // 关键条件2：考虑距离电力线路较近的树木（20米以内，放宽要求）
                   if (minDistanceToPowerline > 20f)
                   {
                       continue; // 距离太远，跳过
                   }
                  
                                   // 如果已经找到了足够多的候选树木，可以提前退出
                 if (treeScores.Count >= dangerTreeCount * 5) // 增加候选数量，提高选择范围
                 {
                     break;
                 }
                  
                  // 计算综合评分：基于高度差和距离
                  // 高度差权重：树木比电塔高得越多，越危险
                  float heightDifference = treeHeight - nearestTowerHeight;
                  float heightScore = heightDifference * 20f; // 高度差越大分数越高
                  
                  // 距离权重：距离越近分数越高（使用倒数关系）
                  float distanceScore = minDistanceToPowerline > 0 ? 50f / minDistanceToPowerline : 50f;
                  
                  // 综合评分：高度差 + 距离分数
                  score = heightScore + distanceScore;
                  
                  // 调试信息
                  Debug.Log($"树木 {tree.name}: 高度={treeHeight:F1}m, 电塔高度={nearestTowerHeight:F1}m, 高度差={heightDifference:F1}m, 距离={minDistanceToPowerline:F1}m, 评分={score:F1}");
                  
                  treeScores.Add((tree, score));
             }
             
             processedCount += (batchEnd - batchStart);
             
             // 每处理完一批，让出控制权，避免卡死
             if (processedCount % 100 == 0) // 更频繁地让出控制权
             {
                 yield return null;
             }
             
             // 如果已经找到了足够的候选树木，提前退出
             if (treeScores.Count >= dangerTreeCount * 5) // 增加候选数量，提高选择范围
             {
                 break;
             }
         }
        
                 // 显示分析完成信息
         UpdateStatus($"分析完成，找到 {treeScores.Count} 棵候选树木，正在标记...");
         
         // 按评分排序，选择评分最高的树木
         var selectedTrees = treeScores
             .OrderByDescending(x => x.score)
             .Take(dangerTreeCount)
             .Select(x => x.tree);
        
        int markedCount = 0;
        foreach (var tree in selectedTrees)
        {
            // 根据评分分配危险等级（评分越高，危险等级越高）
            var treeScore = treeScores.First(x => x.tree == tree).score;
            
            TreeDangerMonitor.TreeDangerLevel level;
            if (treeScore > 150f) // 高评分：高危险
            {
                level = TreeDangerMonitor.TreeDangerLevel.Emergency;
            }
            else if (treeScore > 100f) // 中等评分：危险
            {
                level = TreeDangerMonitor.TreeDangerLevel.Critical;
            }
            else // 低评分：警告
            {
                level = TreeDangerMonitor.TreeDangerLevel.Warning;
            }
            
            // 创建危险标记
            MarkTreeAsDangerous(tree, level);
            markedCount++;
        }
        
                 UpdateStatus($"已智能标记 {markedCount} 棵危险树木（达到电塔30%高度且距离电力线路20米内的树木）");
        UpdateDisplay();
        
        // 自动刷新统计大屏
        var statisticsDashboard = FindObjectOfType<MonoBehaviour>();
        if (statisticsDashboard != null && statisticsDashboard.GetType().Name == "StatisticsDashboardController")
        {
            // 使用反射调用ManualRefresh方法
            var manualRefreshMethod = statisticsDashboard.GetType().GetMethod("ManualRefresh");
            if (manualRefreshMethod != null)
            {
                manualRefreshMethod.Invoke(statisticsDashboard, null);
                Debug.Log("已自动刷新统计大屏");
            }
            else
            {
                Debug.LogWarning("StatisticsDashboardController没有ManualRefresh方法");
            }
        }
        else
        {
            Debug.LogWarning("未找到StatisticsDashboardController，无法自动刷新统计大屏");
        }
    }
    
    void MarkTreeAsDangerous(GameObject tree, TreeDangerMonitor.TreeDangerLevel level)
    {
        // 检查是否已经有危险标记
        if (tree.GetComponent<DangerMarker>() != null)
        {
            return; // 已经有标记，跳过
        }
        
        // 创建标记对象
        var marker = new GameObject($"TreeDangerMarker_{tree.name}");
        marker.transform.SetParent(tree.transform);
        marker.transform.localPosition = Vector3.up * 2f; // 在树的上方
        
        // 添加DangerMarker组件
        DangerMarker dangerMarker = marker.AddComponent<DangerMarker>();
        
        // 转换危险等级
        DangerType dangerType = DangerType.Vegetation; // 树木属于植被类型
        DangerLevel dangerLevel = ConvertTreeDangerLevel(level);
        
                 // 设置危险信息
         string description = $"危险树木 - {GetDangerLevelText(level)}";
         dangerMarker.SetDangerInfo(dangerType, dangerLevel, description, "监测系统");
        
        // 高亮显示树木（如果有Renderer）
        var renderer = tree.GetComponent<Renderer>();
        if (renderer != null)
        {
            try
            {
                var material = renderer.material;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", GetDangerLevelColor(level) * 0.3f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法设置材质发光效果: {e.Message}");
            }
        }
    }
    
         /// <summary>
     /// 计算点到线段的最短距离
     /// </summary>
     float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
     {
         Vector3 line = lineEnd - lineStart;
         Vector3 pointToStart = point - lineStart;
         
         float lineLength = line.magnitude;
         if (lineLength == 0f) return Vector3.Distance(point, lineStart);
         
         Vector3 lineDirection = line / lineLength;
         
         // 计算投影点在线段上的参数t
         float t = Vector3.Dot(pointToStart, lineDirection);
         
         // 如果t < 0，最近点是lineStart
         if (t <= 0f)
         {
             return Vector3.Distance(point, lineStart);
         }
         
         // 如果t > lineLength，最近点是lineEnd
         if (t >= lineLength)
         {
             return Vector3.Distance(point, lineEnd);
         }
         
         // 否则，最近点在线段上
         Vector3 projection = lineStart + t * lineDirection;
         return Vector3.Distance(point, projection);
     }
     
     /// <summary>
     /// 计算电塔高度
     /// </summary>
     float CalculateTowerHeight(GameObject tower)
     {
         // 获取电塔的Renderer组件
         var renderer = tower.GetComponent<Renderer>();
         if (renderer != null)
         {
             // 使用Renderer的bounds来计算高度
             return renderer.bounds.size.y;
         }
         
         // 如果没有Renderer，尝试获取Collider
         var collider = tower.GetComponent<Collider>();
         if (collider != null)
         {
             return collider.bounds.size.y;
         }
         
         // 如果都没有，使用Transform的scale作为估算
         return tower.transform.localScale.y * 3f; // 假设电塔高度约为scale的3倍
     }
     
     /// <summary>
     /// 计算树木高度
     /// </summary>
     float CalculateTreeHeight(GameObject tree)
    {
        // 获取树木的Renderer组件
        var renderer = tree.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 使用Renderer的bounds来计算高度
            return renderer.bounds.size.y;
        }
        
        // 如果没有Renderer，尝试获取Collider
        var collider = tree.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.size.y;
        }
        
        // 如果都没有，使用Transform的scale作为估算
        return tree.transform.localScale.y * 2f; // 假设树木高度约为scale的2倍
    }
    
    /// <summary>
    /// 转换TreeDangerLevel到DangerLevel
    /// </summary>
    DangerLevel ConvertTreeDangerLevel(TreeDangerMonitor.TreeDangerLevel treeLevel)
    {
        switch (treeLevel)
        {
            case TreeDangerMonitor.TreeDangerLevel.Safe:
                return DangerLevel.Low;
            case TreeDangerMonitor.TreeDangerLevel.Warning:
                return DangerLevel.Medium;
            case TreeDangerMonitor.TreeDangerLevel.Critical:
                return DangerLevel.High;
            case TreeDangerMonitor.TreeDangerLevel.Emergency:
                return DangerLevel.High;
            default:
                return DangerLevel.Medium;
        }
    }
    
         // 更新方法
     void UpdateStatus(string message)
     {
         // 删除状态标签更新，只保留日志
         Debug.Log($"[TreeDangerController] {message}");
     }
    
         void UpdateStatistics()
     {
                 // 获取基本统计信息
        var allTrees = FindObjectsOfType<GameObject>().Where(obj => 
            obj.name.ToLower().Contains("tree") || 
            obj.name.ToLower().Contains("植物") || 
            obj.name.ToLower().Contains("树")).ToArray();
            
        // 过滤掉系统组件
        var realTrees = allTrees.Where(obj => 
            !obj.name.Contains("TreeDangerMonitor") && 
            !obj.name.Contains("TreeDanger") &&
            !obj.name.Contains("Monitor") &&
            !obj.name.Contains("Controller") &&
            !obj.name.Contains("System") &&
            !obj.name.Contains("Manager") &&
            !obj.name.Contains("UI") &&
            !obj.name.Contains("Panel")).ToArray();
         var dangerousTrees = GetAllDangerousTrees();
         
                 // 由于现在使用合并区域，统计信息会在CreateMergedStatisticsDisplay中实时更新
        // 这里只保留方法以保持兼容性
        Debug.Log($"统计更新 - 过滤前总树木: {allTrees.Length}, 过滤后总树木: {realTrees.Length}, 危险树木: {dangerousTrees.Count}");
     }
    
         void UpdateTreeList()
     {
         if (treeListContainer == null) return;
         
         // 清除旧的列表内容
         treeListContainer.Clear();
         
         // 获取所有危险树木（包括随机标记的和监测发现的）
         var dangerousTrees = GetAllDangerousTrees();
         
         if (dangerousTrees.Count == 0)
         {
             // 没有危险树木，显示无危险信息
             CreateNoDangerTreesDisplay();
         }
         else
         {
             // 有危险树木，显示列表
             DisplayAllDangerousTrees(dangerousTrees);
         }
     }
    
    void DisplayAllDangerousTrees(List<GameObject> dangerousTrees)
    {
        if (dangerousTrees == null || dangerousTrees.Count == 0) return;
        
        // 显示危险树木统计
        var statsLabel = new Label($"发现 {dangerousTrees.Count} 棵危险树木");
        statsLabel.style.fontSize = 12;
        statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsLabel.style.color = new Color(0.8f, 0.3f, 0.3f, 1f);
        statsLabel.style.marginBottom = 10;
        statsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(statsLabel);
        treeListContainer.Add(statsLabel);
        
        // 显示每棵危险树木的信息
        foreach (var tree in dangerousTrees)
        {
            CreateDangerousTreeListItem(tree);
        }
    }
    
         void CreateDangerousTreeListItem(GameObject tree)
     {
         // 创建危险树木列表项
         var itemContainer = new VisualElement();
         itemContainer.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
         itemContainer.style.marginBottom = 8;
         itemContainer.style.paddingTop = 8;
         itemContainer.style.paddingBottom = 8;
         itemContainer.style.paddingLeft = 8;
         itemContainer.style.paddingRight = 8;
         itemContainer.style.borderTopLeftRadius = 6;
         itemContainer.style.borderTopRightRadius = 6;
         itemContainer.style.borderBottomLeftRadius = 6;
         itemContainer.style.borderBottomRightRadius = 6;
         itemContainer.style.borderLeftWidth = 2;
         
         // 获取危险标记信息
         var dangerMarker = tree.GetComponent<DangerMarker>() ?? tree.GetComponentInChildren<DangerMarker>();
         if (dangerMarker != null)
         {
             // 根据危险等级设置边框颜色
             Color borderColor = GetDangerLevelColorFromDangerMarker(dangerMarker.dangerLevel);
             itemContainer.style.borderLeftColor = borderColor;
             
             // 危险等级标签
             var levelLabel = new Label($"危险等级: {GetDangerLevelStringFromDangerMarker(dangerMarker.dangerLevel)}");
             levelLabel.style.fontSize = 12;
             levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
             levelLabel.style.color = borderColor;
             levelLabel.style.marginBottom = 5;
             uiManager?.ApplyFont(levelLabel);
             itemContainer.Add(levelLabel);
             
             // 危险类型标签
             var typeLabel = new Label($"危险类型: {GetDangerTypeString(dangerMarker.dangerType)}");
             typeLabel.style.fontSize = 11;
             typeLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
             typeLabel.style.marginBottom = 3;
             uiManager?.ApplyFont(typeLabel);
             itemContainer.Add(typeLabel);
             
             // 删除描述信息显示
             
             // 创建时间
             var timeLabel = new Label($"标记时间: {dangerMarker.createTime:HH:mm:ss}");
             timeLabel.style.fontSize = 10;
             timeLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
             uiManager?.ApplyFont(timeLabel);
             itemContainer.Add(timeLabel);
             
             // 添加跳转按钮
             CreateJumpButton(itemContainer, tree, dangerMarker);
         }
         else
         {
             // 如果没有DangerMarker，显示基本信息
             itemContainer.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
             
             var nameLabel = new Label($"树木名称: {tree.name}");
             nameLabel.style.fontSize = 12;
             nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
             nameLabel.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
             nameLabel.style.marginBottom = 5;
             uiManager?.ApplyFont(nameLabel);
             itemContainer.Add(nameLabel);
             
             var posLabel = new Label($"位置: ({tree.transform.position.x:F1}, {tree.transform.position.y:F1}, {tree.transform.position.z:F1})");
             posLabel.style.fontSize = 11;
             posLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
             uiManager?.ApplyFont(posLabel);
             itemContainer.Add(posLabel);
             
             // 添加跳转按钮
             CreateJumpButton(itemContainer, tree, null);
         }
         
         treeListContainer.Add(itemContainer);
     }
    
         /// <summary>
     /// 创建跳转按钮
     /// </summary>
     void CreateJumpButton(VisualElement container, GameObject tree, DangerMarker dangerMarker)
     {
         // 创建跳转按钮容器
         var jumpContainer = new VisualElement();
         jumpContainer.style.flexDirection = FlexDirection.Row;
         jumpContainer.style.justifyContent = Justify.FlexEnd;
         jumpContainer.style.marginTop = 8;
         
                             // 跳转按钮
           var jumpButton = new Button(() => JumpToTree(tree, dangerMarker));
           jumpButton.text = "跳转";
           jumpButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
           jumpButton.style.color = Color.white;
           jumpButton.style.fontSize = 11;
           jumpButton.style.unityFontStyleAndWeight = FontStyle.Bold;
           jumpButton.style.paddingTop = 4;
           jumpButton.style.paddingBottom = 4;
           jumpButton.style.paddingLeft = 8;
           jumpButton.style.paddingRight = 8;
           jumpButton.style.borderTopLeftRadius = 4;
           jumpButton.style.borderTopRightRadius = 4;
           jumpButton.style.borderBottomLeftRadius = 4;
           jumpButton.style.borderBottomRightRadius = 4;
           jumpButton.style.minWidth = 50;
           jumpButton.style.minHeight = 24;
           jumpButton.style.unityTextAlign = TextAnchor.MiddleCenter;
          
          // 应用字体，确保文字正确显示
          uiManager?.ApplyFont(jumpButton);
         
         // 悬停效果
         jumpButton.RegisterCallback<MouseEnterEvent>(evt => 
         {
             jumpButton.style.backgroundColor = new Color(0.3f, 0.7f, 0.9f, 1f);
         });
         
         jumpButton.RegisterCallback<MouseLeaveEvent>(evt => 
         {
             jumpButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
         });
         
         jumpContainer.Add(jumpButton);
         container.Add(jumpContainer);
     }
     
     /// <summary>
     /// 跳转到指定树木位置
     /// </summary>
     void JumpToTree(GameObject tree, DangerMarker dangerMarker)
     {
         if (tree == null)
         {
             Debug.LogWarning("跳转失败：树木对象为空");
             return;
         }
         
         Vector3 treePosition = tree.transform.position;
         
         // 查找CameraManager组件
         var cameraManager = FindObjectOfType<CameraManager>();
         
         // 计算观察偏移量
         Vector3 cameraOffset = CalculateTreeViewOffset(tree, dangerMarker);
         
         // 如果没有CameraManager，尝试直接操作摄像机
         if (cameraManager == null)
         {
             Debug.LogWarning("CameraManager未找到，尝试直接操作主摄像机");
             Camera mainCamera = Camera.main;
             if (mainCamera != null)
             {
                 Vector3 cameraPos = treePosition + cameraOffset;
                 
                 // 确保摄像机位置在地面之上
                 float groundLevel = GetGroundHeight(cameraPos);
                 cameraPos.y = Mathf.Max(cameraPos.y, groundLevel + 2f);
                 
                 Vector3 fallbackLookAtTarget = treePosition + Vector3.up * 0.5f; // 稍微向上看
                 
                 StartCoroutine(SmoothJumpToTreePosition(cameraPos, fallbackLookAtTarget, tree));
                 return;
             }
             else
             {
                 Debug.LogError("跳转失败：未找到主摄像机");
                 return;
             }
         }
         
         // 计算摄像机目标位置
         Vector3 finalCameraPos = treePosition + cameraOffset;
         
         // 确保摄像机位置在地面之上
         float finalGroundLevel = GetGroundHeight(finalCameraPos);
         finalCameraPos.y = Mathf.Max(finalCameraPos.y, finalGroundLevel + 2f);
         
         // 根据视角调整观察目标点
         Vector3 lookAtTarget = treePosition;
         if (cameraManager != null && cameraManager.GetCurrentView() == 2) // 飞行视角
         {
             lookAtTarget.y += 0.5f; // 飞行视角下与树木持平，稍微向上看一点点
         }
         
         // 执行跳转
         StartCoroutine(SmoothJumpToTreePosition(finalCameraPos, lookAtTarget, tree));
         
         Debug.Log($"跳转到树木位置: {treePosition}");
     }
     
     /// <summary>
     /// 计算树木观察偏移量
     /// </summary>
     Vector3 CalculateTreeViewOffset(GameObject tree, DangerMarker dangerMarker)
     {
         Vector3 offset = Vector3.zero;
         
         // 获取树木高度
         float treeHeight = CalculateTreeHeight(tree);
         
         // 根据危险等级调整观察距离
         float baseDistance = 8f; // 基础观察距离
         if (dangerMarker != null)
         {
             switch (dangerMarker.dangerLevel)
             {
                 case DangerLevel.High:
                     baseDistance = 6f; // 高危险等级，近距离观察
                     break;
                 case DangerLevel.Medium:
                     baseDistance = 8f; // 中等危险等级，中等距离观察
                     break;
                 case DangerLevel.Low:
                     baseDistance = 10f; // 低危险等级，远距离观察
                     break;
             }
         }
         
         // 计算偏移量：在树木后方上方观察
         offset = new Vector3(0, treeHeight * 0.8f, -baseDistance);
         
         return offset;
     }
     
     /// <summary>
     /// 获取地面高度
     /// </summary>
     float GetGroundHeight(Vector3 position)
     {
         // 使用射线检测获取地面高度
         RaycastHit hit;
         if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
         {
             return hit.point.y;
         }
         
         // 如果没有检测到地面，返回默认高度
         return 0f;
     }
     
     /// <summary>
     /// 平滑跳转到树木位置
     /// </summary>
     System.Collections.IEnumerator SmoothJumpToTreePosition(Vector3 targetPos, Vector3 lookAtPos, GameObject tree)
     {
         Camera mainCamera = null;
         var cameraManager = FindObjectOfType<CameraManager>();
         
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
                     Vector3 godViewDirection = (lookAtPos - targetPos).normalized;
                     godViewDirection.y = Mathf.Min(godViewDirection.y, -0.5f);
                     targetRot = Quaternion.LookRotation(godViewDirection);
                     break;
                     
                 case 2: // 飞行视角 - 自然看向目标点
                     Vector3 flyViewDirection = (lookAtPos - targetPos).normalized;
                     targetRot = Quaternion.LookRotation(flyViewDirection);
                     break;
                     
                 default: // 第一人称视角 - 正常看向树木
                     Vector3 fpViewDirection = (lookAtPos - targetPos).normalized;
                     targetRot = Quaternion.LookRotation(fpViewDirection);
                     break;
             }
         }
         else
         {
             Vector3 fallbackDirection = (lookAtPos - targetPos).normalized;
             targetRot = Quaternion.LookRotation(fallbackDirection);
         }
         
         float elapsedTime = 0f;
         float duration = 1f; // 跳转动画持续时间
         
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
         
         Debug.Log($"相机已跳转到树木位置: {tree.transform.position}");
     }
     
     // 辅助方法
     Color GetDangerLevelColor(TreeDangerMonitor.TreeDangerLevel level)
    {
        switch (level)
        {
            case TreeDangerMonitor.TreeDangerLevel.Safe:
                return new Color(0.2f, 0.7f, 0.2f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Warning:
                return new Color(1f, 0.6f, 0f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Critical:
                return new Color(1f, 0.4f, 0f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Emergency:
                return new Color(0.9f, 0.1f, 0.1f, 1f);
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
    
    /// <summary>
    /// 从DangerMarker获取危险等级颜色
    /// </summary>
    Color GetDangerLevelColorFromDangerMarker(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low:
                return new Color(1f, 0.8f, 0f, 1f);      // 金黄色
            case DangerLevel.Medium:
                return new Color(1f, 0.4f, 0f, 1f);      // 橙红色
            case DangerLevel.High:
                return new Color(0.9f, 0.1f, 0.1f, 1f); // 深红色
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
    
    /// <summary>
    /// 从DangerMarker获取危险等级字符串
    /// </summary>
    string GetDangerLevelStringFromDangerMarker(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low:
                return "低危险";
            case DangerLevel.Medium:
                return "中等危险";
            case DangerLevel.High:
                return "高危险";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 获取危险类型字符串
    /// </summary>
    string GetDangerTypeString(DangerType type)
    {
        switch (type)
        {
            case DangerType.Building:
                return "建筑危险";
            case DangerType.Vegetation:
                return "植被危险";
            case DangerType.Equipment:
                return "设备危险";
            case DangerType.Other:
                return "其他危险";
            default:
                return "未知类型";
        }
    }
    
    string GetDangerLevelText(TreeDangerMonitor.TreeDangerLevel level)
    {
        switch (level)
        {
            case TreeDangerMonitor.TreeDangerLevel.Safe:
                return "安全";
            case TreeDangerMonitor.TreeDangerLevel.Warning:
                return "警告";
            case TreeDangerMonitor.TreeDangerLevel.Critical:
                return "危险";
            case TreeDangerMonitor.TreeDangerLevel.Emergency:
                return "紧急";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 获取当前FPS
    /// </summary>
    // float GetFPS()
    // {
    //     return 1.0f / Time.deltaTime;
    // }
    
    /// <summary>
    /// 获取内存使用量（MB）
    /// </summary>
    // float GetMemoryUsage()
    // {
    //     return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    // }
    
    /// <summary>
    /// 获取场景中的对象数量
    /// </summary>
    // int GetSceneObjectCount()
    // {
    //     return FindObjectsOfType<GameObject>().Length;
    // }
    

    
    void SyncMonitoringParameters()
    {
        if (treeDangerMonitor == null) return;
        
        // 同步距离参数
        treeDangerMonitor.criticalDistance = criticalDistance;
        treeDangerMonitor.warningDistance = warningDistance;
        treeDangerMonitor.safeDistance = safeDistance;
        
        // 简化参数，删除生长率同步
    }
    
    // 公共接口方法
    public void RefreshDisplay()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogWarning("TreeDangerMonitor未找到，无法刷新显示");
            return;
        }
        
        // 更新显示
        UpdateDisplay();
        
        Debug.Log("显示刷新完成");
    }
    
    public void Hide()
    {
        this.enabled = false;
    }
    
    public void Show()
    {
        this.enabled = true;
        RefreshDisplay();
    }
    
         public void UpdateDisplay()
     {
         // 更新统计信息
         UpdateStatistics();
         
         // 更新树木列表
         UpdateTreeList();
         
         // 简化功能，删除距离信息更新
         
         // 删除状态显示更新
     }
    
    // 简化功能，删除更新距离信息显示方法
    
         /// <summary>
     /// 自动刷新协程
     /// </summary>
     IEnumerator AutoRefreshCoroutine()
     {
         while (true)
         {
             yield return new WaitForSeconds(0.5f);
             
             if (isMonitoring)
             {
                 UpdateDisplay();
             }
         }
     }
    
    // 简化显示，删除跳转功能
}



