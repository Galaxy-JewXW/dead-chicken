using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UI
{
    /// <summary>
    /// 日志查看器 - 用于在Unity编辑器中查看统计大屏的日志文件
    /// </summary>
    public class LogViewer : EditorWindow
    {
        private Vector2 scrollPosition;
        private string[] logFiles;
        private int selectedLogIndex = 0;
        private string logContent = "";
        private bool autoRefresh = true;
        private double lastRefreshTime;
        private const float REFRESH_INTERVAL = 2f; // 2秒自动刷新一次
        
        [MenuItem("工具/统计大屏日志查看器")]
        public static void ShowWindow()
        {
            GetWindow<LogViewer>("统计大屏日志查看器");
        }
        
        void OnEnable()
        {
            RefreshLogFiles();
            if (logFiles.Length > 0)
            {
                selectedLogIndex = 0;
                LoadLogContent();
            }
        }
        
        void Update()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
            {
                if (logFiles.Length > 0 && selectedLogIndex >= 0 && selectedLogIndex < logFiles.Length)
                {
                    LoadLogContent();
                }
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }
        
        void OnGUI()
        {
            GUILayout.Label("统计大屏日志查看器", EditorStyles.boldLabel);
            
            // 日志文件选择
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("选择日志文件:", GUILayout.Width(100));
            
            if (GUILayout.Button("刷新文件列表", GUILayout.Width(100)))
            {
                RefreshLogFiles();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (logFiles.Length > 0)
            {
                selectedLogIndex = EditorGUILayout.Popup("日志文件:", 
                    selectedLogIndex, 
                    logFiles.Select(f => Path.GetFileName(f)).ToArray());
                
                if (GUILayout.Button("加载日志内容", GUILayout.Width(100)))
                {
                    LoadLogContent();
                }
            }
            else
            {
                GUILayout.Label("未找到日志文件");
            }
            
            // 自动刷新选项
            autoRefresh = EditorGUILayout.Toggle("自动刷新", autoRefresh);
            
            EditorGUILayout.Space();
            
            // 日志内容显示
            if (!string.IsNullOrEmpty(logContent) && logFiles.Length > 0 && selectedLogIndex >= 0 && selectedLogIndex < logFiles.Length)
            {
                GUILayout.Label($"日志内容 (文件: {Path.GetFileName(logFiles[selectedLogIndex])})", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                // 使用TextArea显示日志内容，支持滚动
                logContent = EditorGUILayout.TextArea(logContent, GUILayout.ExpandHeight(true));
                
                EditorGUILayout.EndScrollView();
                
                // 操作按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("复制到剪贴板", GUILayout.Width(120)))
                {
                    EditorGUIUtility.systemCopyBuffer = logContent;
                    Debug.Log("日志内容已复制到剪贴板");
                }
                
                if (GUILayout.Button("在文件管理器中显示", GUILayout.Width(150)))
                {
                    string logsFolder = Path.GetDirectoryName(logFiles[selectedLogIndex]);
                    if (Directory.Exists(logsFolder))
                    {
                        EditorUtility.RevealInFinder(logsFolder);
                    }
                }
                
                if (GUILayout.Button("清空日志", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空这个日志文件吗？", "确定", "取消"))
                    {
                        File.WriteAllText(logFiles[selectedLogIndex], "");
                        LoadLogContent();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void RefreshLogFiles()
        {
            try
            {
                string logsFolder = Path.Combine(Application.dataPath, "..", "Logs");
                if (Directory.Exists(logsFolder))
                {
                    logFiles = Directory.GetFiles(logsFolder, "StatisticsDashboard_*.log")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToArray();
                }
                else
                {
                    logFiles = new string[0];
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"刷新日志文件失败: {ex.Message}");
                logFiles = new string[0];
            }
        }
        
        private void LoadLogContent()
        {
            try
            {
                if (logFiles.Length > 0 && selectedLogIndex >= 0 && selectedLogIndex < logFiles.Length)
                {
                    string selectedLogFile = logFiles[selectedLogIndex];
                    if (File.Exists(selectedLogFile))
                    {
                        logContent = File.ReadAllText(selectedLogFile);
                        lastRefreshTime = EditorApplication.timeSinceStartup;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载日志内容失败: {ex.Message}");
                logContent = $"加载日志失败: {ex.Message}";
            }
        }
    }
}
