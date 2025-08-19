using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System;

namespace UI
{
    /// <summary>
    /// 图表渲染器 - 使用Unity UI Toolkit绘制各种统计图表
    /// </summary>
    public class ChartRenderer : MonoBehaviour
    {
        [Header("图表样式")]
        public Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f);
        public Color secondaryColor = new Color(1f, 0.4f, 0.2f, 1f);
        public Color successColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        public Color warningColor = new Color(1f, 0.8f, 0.2f, 1f);
        public Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        // 日志系统
        private static string logFilePath;
        private static bool logInitialized = false;
        
        void Start()
        {
            InitializeLogSystem();
        }
        
        /// <summary>
        /// 初始化日志系统
        /// </summary>
        private void InitializeLogSystem()
        {
            if (!logInitialized)
            {
                try
                {
                    string logsFolder = Path.Combine(Application.dataPath, "..", "Logs");
                    if (!Directory.Exists(logsFolder))
                    {
                        Directory.CreateDirectory(logsFolder);
                    }
                    
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    logFilePath = Path.Combine(logsFolder, $"ChartRenderer_{timestamp}.log");
                    
                    string header = $"=== ChartRenderer调试日志 ===\n" +
                                  $"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"Unity版本: {Application.unityVersion}\n" +
                                  "================================\n\n";
                    
                    File.WriteAllText(logFilePath, header);
                    logInitialized = true;
                    
                    Debug.Log($"ChartRenderer日志系统已初始化，日志文件: {logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ChartRenderer初始化日志系统失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private void WriteLog(string message, string level = "INFO")
        {
            try
            {
                if (logInitialized && !string.IsNullOrEmpty(logFilePath))
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level}] {message}\n";
                    File.AppendAllText(logFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ChartRenderer写入日志失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 写入调试日志
        /// </summary>
        private void WriteDebugLog(string message)
        {
            WriteLog(message, "DEBUG");
            Debug.Log($"ChartRenderer: {message}");
        }
        
        /// <summary>
        /// 写入错误日志
        /// </summary>
        private void WriteErrorLog(string message)
        {
            WriteLog(message, "ERROR");
            Debug.LogError($"ChartRenderer: {message}");
        }
        
        /// <summary>
        /// 创建柱状图
        /// </summary>
        public void CreateBarChart(VisualElement container, List<float> data, List<string> labels)
        {
            WriteDebugLog($"ChartRenderer: 开始创建柱状图，数据: [{string.Join(", ", data)}], 标签: [{string.Join(", ", labels)}]");
            
            if (data == null || data.Count == 0 || labels == null || labels.Count == 0)
            {
                WriteErrorLog("ChartRenderer: 数据或标签为空，无法创建柱状图");
                return;
            }
            
            // 创建图表容器
            var chartContainer = new VisualElement();
            chartContainer.style.flexDirection = FlexDirection.Row;
            chartContainer.style.alignItems = Align.FlexEnd;
            chartContainer.style.height = 180;
            chartContainer.style.minHeight = 180;
            chartContainer.style.paddingTop = 10;
            chartContainer.style.paddingBottom = 20;
            chartContainer.style.paddingLeft = 20; // 为Y轴留出空间
            chartContainer.style.paddingRight = 20; // 为右侧留出空间
            // 移除背景色，让柱状图直接显示
            chartContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0f); // 完全透明
            chartContainer.style.justifyContent = Justify.SpaceAround;
            chartContainer.style.width = Length.Percent(100);
            
            WriteDebugLog("ChartRenderer: 图表容器已创建，高度: 180px, 最小高度: 180px，无背景");
            
            // 找到最大值用于归一化
            float maxValue = Mathf.Max(data.ToArray());
            if (maxValue <= 0) maxValue = 1f; // 避免除零错误
            
            WriteDebugLog($"ChartRenderer: 数据最大值: {maxValue}");
            
            // 创建Y轴标签容器
            var yAxisContainer = new VisualElement();
            yAxisContainer.style.flexDirection = FlexDirection.Column;
            yAxisContainer.style.justifyContent = Justify.SpaceBetween;
            yAxisContainer.style.width = 40;
            yAxisContainer.style.height = 140;
            yAxisContainer.style.marginTop = 10;
            yAxisContainer.style.marginBottom = 10;
            yAxisContainer.style.marginLeft = 20; // 增加左侧边距，将坐标系向右移动
            
            // 创建Y轴标签（从最大值到0）
            for (int i = 4; i >= 0; i--)
            {
                var yLabel = new Label($"{maxValue * i / 4:F0}");
                yLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                yLabel.style.fontSize = 10;
                yLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                yLabel.style.marginRight = 5;
                yAxisContainer.Add(yLabel);
            }
            
            // 创建主图表区域（简洁样式）
            var chartArea = new VisualElement();
            chartArea.style.flexDirection = FlexDirection.Row;
            chartArea.style.alignItems = Align.FlexEnd;
            chartArea.style.height = 140;
            chartArea.style.minHeight = 140;
            chartArea.style.flexGrow = 1;
            chartArea.style.justifyContent = Justify.SpaceBetween; // 使用SpaceBetween确保最大间距
            chartArea.style.position = Position.Relative;
            // 移除矩形背景，保持简洁
            chartArea.style.backgroundColor = new Color(0f, 0f, 0f, 0f); // 完全透明
            chartArea.style.paddingTop = 10;
            chartArea.style.paddingBottom = 25; // 增加底部空间，为X轴标签留出位置
            chartArea.style.paddingLeft = 0; // 移除左侧内边距，因为Y轴已经右移
            chartArea.style.paddingRight = 20; // 保持右侧内边距
            
            // 添加Y轴线条
            var yAxisLine = new VisualElement();
            yAxisLine.style.position = Position.Absolute;
            yAxisLine.style.left = 0;
            yAxisLine.style.top = 0;
            yAxisLine.style.bottom = 25; // 延伸到X轴位置
            yAxisLine.style.width = 1;
            yAxisLine.style.backgroundColor = new Color(0.5f, 0.6f, 0.8f, 0.8f);
            chartArea.Add(yAxisLine);
            
            // 添加X轴线条
            var xAxisLine = new VisualElement();
            xAxisLine.style.position = Position.Absolute;
            xAxisLine.style.left = 0;
            xAxisLine.style.right = 0;
            xAxisLine.style.bottom = 25; // 调整X轴位置，确保在容器内
            xAxisLine.style.height = 1;
            xAxisLine.style.backgroundColor = new Color(0.5f, 0.6f, 0.8f, 0.8f);
            chartArea.Add(xAxisLine);
            
            // 添加网格线
            for (int i = 1; i <= 4; i++)
            {
                var gridLine = new VisualElement();
                gridLine.style.position = Position.Absolute;
                gridLine.style.left = 0;
                gridLine.style.right = 0;
                gridLine.style.top = (140f * i / 4f) - 1f;
                gridLine.style.height = 1;
                gridLine.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                chartArea.Add(gridLine);
            }
            
            // 创建柱状图
            for (int i = 0; i < data.Count; i++)
            {
                var barContainer = new VisualElement();
                barContainer.style.flexDirection = FlexDirection.Column;
                barContainer.style.alignItems = Align.Center;
                barContainer.style.justifyContent = Justify.FlexEnd;
                barContainer.style.height = 140;
                barContainer.style.minHeight = 140;
                barContainer.style.width = 60; // 增加容器宽度，为标签留出更多空间
                barContainer.style.minWidth = 60;
                barContainer.style.marginLeft = 5; // 添加左侧边距
                barContainer.style.marginRight = 5; // 添加右侧边距
                barContainer.style.marginBottom = -10; // 添加底部边距，将整个柱状图+文字整体下移
                
                // 创建柱状图
                var bar = new VisualElement();
                float barHeight = (data[i] / maxValue) * 120f; // 最大高度120px
                bar.style.height = Mathf.Max(barHeight, 8f); // 最小高度8px
                bar.style.minHeight = 8f;
                bar.style.width = 30;
                bar.style.minWidth = 30;
                bar.style.backgroundColor = GetBarColor(i);
                // 使用正确的圆角属性
                bar.style.borderTopLeftRadius = 3;
                bar.style.borderTopRightRadius = 3;
                bar.style.borderBottomLeftRadius = 0; // 底部不圆角，与X轴相连
                bar.style.borderBottomRightRadius = 0;
                bar.style.marginBottom = 0; // 移除底部边距，确保与X轴相连
                
                // 创建数值标签
                var valueLabel = new Label(data[i].ToString());
                valueLabel.style.color = Color.white;
                valueLabel.style.fontSize = 11;
                valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                valueLabel.style.marginBottom = 3;
                
                // 创建X轴标签（直接放在柱状图容器内）
                var xLabel = new Label(labels[i]);
                xLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                xLabel.style.fontSize = 10;
                xLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                xLabel.style.marginTop = 2;
                
                // 组装柱状图（包含X轴标签）
                barContainer.Add(valueLabel);
                barContainer.Add(bar);
                barContainer.Add(xLabel);
                
                chartArea.Add(barContainer);
                
                WriteDebugLog($"ChartRenderer: 柱状图 {i} 已创建，高度: {barHeight:F1}px, 数值: {data[i]}");
            }
            
            // 移除独立的X轴标签容器，因为标签现在直接放在柱状图容器内
            // 组装图表
            chartContainer.Add(yAxisContainer);
            chartContainer.Add(chartArea);
            
            WriteDebugLog("ChartRenderer: 柱状图组装完成");
            
            // 添加到容器
            container.Add(chartContainer);
            
            WriteDebugLog("ChartRenderer: 柱状图已添加到容器");
        }
        
        /// <summary>
        /// 创建仪表图
        /// </summary>
        public void CreateGaugeChart(VisualElement container, float value, float maxValue, string label = "")
        {
            WriteDebugLog($"ChartRenderer: 开始创建仪表图，值: {value}, 最大值: {maxValue}, 标签: {label}");
            
            // 创建仪表图容器
            var gaugeContainer = new VisualElement();
            gaugeContainer.style.flexDirection = FlexDirection.Column;
            gaugeContainer.style.alignItems = Align.Center;
            gaugeContainer.style.justifyContent = Justify.Center;
            gaugeContainer.style.height = 140; // 减少高度，避免超出
            gaugeContainer.style.minHeight = 140;
            gaugeContainer.style.width = 140; // 减少宽度，避免超出
            gaugeContainer.style.minWidth = 140;
            // 移除背景色，让仪表图直接显示
            gaugeContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0f); // 完全透明
            gaugeContainer.style.paddingTop = 5; // 减少内边距
            gaugeContainer.style.paddingBottom = 5;
            
            WriteDebugLog("ChartRenderer: 仪表图容器已创建，尺寸: 140x140px，无背景");
            
            // 创建仪表图背景圆环
            var gaugeBackground = new VisualElement();
            gaugeBackground.style.height = 120; // 减少尺寸，避免超出
            gaugeBackground.style.minHeight = 120;
            gaugeBackground.style.width = 120;
            gaugeBackground.style.minWidth = 120;
            // 使用正确的圆角属性
            gaugeBackground.style.borderTopLeftRadius = 60;
            gaugeBackground.style.borderTopRightRadius = 60;
            gaugeBackground.style.borderBottomLeftRadius = 60;
            gaugeBackground.style.borderBottomRightRadius = 60;
            gaugeBackground.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f); // 更柔和的背景色
            // 使用正确的边框属性
            gaugeBackground.style.borderLeftWidth = 6; // 减少边框宽度
            gaugeBackground.style.borderRightWidth = 6;
            gaugeBackground.style.borderTopWidth = 6;
            gaugeBackground.style.borderBottomWidth = 6;
            gaugeBackground.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 0.8f); // 更柔和的边框色
            gaugeBackground.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            gaugeBackground.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            gaugeBackground.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            
            WriteDebugLog("ChartRenderer: 仪表图背景圆环已创建，尺寸: 120x120px");
            
            // 创建仪表图填充
            var gaugeFill = new VisualElement();
            gaugeFill.style.height = 110; // 减少尺寸，避免超出
            gaugeFill.style.minHeight = 110;
            gaugeFill.style.width = 110;
            gaugeFill.style.minWidth = 110;
            // 使用正确的圆角属性
            gaugeFill.style.borderTopLeftRadius = 55;
            gaugeFill.style.borderTopRightRadius = 55;
            gaugeFill.style.borderBottomLeftRadius = 55;
            gaugeFill.style.borderBottomRightRadius = 55;
            
            // 根据数值计算填充颜色，使用更美观的渐变色
            float fillPercentage = Mathf.Clamp01(value / maxValue);
            Color fillColor;
            if (fillPercentage >= 0.8f)
            {
                fillColor = new Color(0.2f, 0.8f, 0.4f, 1f); // 绿色 - 优秀
            }
            else if (fillPercentage >= 0.6f)
            {
                fillColor = new Color(0.4f, 0.7f, 0.9f, 1f); // 蓝色 - 良好
            }
            else if (fillPercentage >= 0.4f)
            {
                fillColor = new Color(1f, 0.7f, 0.2f, 1f); // 橙色 - 一般
            }
            else
            {
                fillColor = new Color(0.9f, 0.3f, 0.3f, 1f); // 红色 - 较差
            }
            
            gaugeFill.style.backgroundColor = fillColor;
            // 使用正确的边框属性
            gaugeFill.style.borderLeftWidth = 6;
            gaugeFill.style.borderRightWidth = 6;
            gaugeFill.style.borderTopWidth = 6;
            gaugeFill.style.borderBottomWidth = 6;
            gaugeFill.style.borderLeftColor = fillColor;
            gaugeFill.style.borderRightColor = fillColor;
            gaugeFill.style.borderTopColor = fillColor;
            gaugeFill.style.borderBottomColor = fillColor;
            
            WriteDebugLog($"ChartRenderer: 仪表图填充已创建，尺寸: 110x110px，填充百分比: {fillPercentage:F2}");
            
            // 创建数值标签容器（居中显示）
            var valueContainer = new VisualElement();
            valueContainer.style.position = Position.Absolute;
            valueContainer.style.left = 0;
            valueContainer.style.right = 0;
            valueContainer.style.top = 0;
            valueContainer.style.bottom = 0;
            valueContainer.style.alignItems = Align.Center;
            valueContainer.style.justifyContent = Justify.Center;
            // 移除zIndex属性，Unity UI Toolkit中不存在此属性
            
            // 创建主数值标签
            var valueLabel = new Label($"{value:F1}");
            valueLabel.style.color = Color.white;
            valueLabel.style.fontSize = 24; // 增大字体，更突出
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.marginBottom = 2; // 减少与最大值标签的间距
            
            // 创建最大值标签
            var maxValueLabel = new Label($"/ {maxValue:F1}");
            maxValueLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f); // 稍微亮一点的灰色
            maxValueLabel.style.fontSize = 16; // 适中的字体大小
            maxValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // 组装数值标签
            valueContainer.Add(valueLabel);
            valueContainer.Add(maxValueLabel);
            
            WriteDebugLog("ChartRenderer: 数值标签容器已创建，居中显示");
            
            // 组装仪表图
            gaugeBackground.Add(gaugeFill);
            gaugeBackground.Add(valueContainer); // 将数值标签添加到背景层，实现居中效果
            gaugeContainer.Add(gaugeBackground);
            
            WriteDebugLog("ChartRenderer: 仪表图组装完成");
            
            // 添加到容器
            container.Add(gaugeContainer);
            
            WriteDebugLog("ChartRenderer: 仪表图已添加到容器");
        }
        
        /// <summary>
        /// 获取柱状图颜色
        /// </summary>
        private Color GetBarColor(int index)
        {
            Color[] colors = { primaryColor, secondaryColor, successColor, warningColor, dangerColor };
            return colors[index % colors.Length];
        }
    }
}
