using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 运行时文件选择器 - 用于打包后的Unity应用
/// 使用Windows API提供原生文件对话框
/// </summary>
public static class RuntimeFileSelector
{
    // Windows API 结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OPENFILENAME
    {
        public int lStructSize = Marshal.SizeOf(typeof(OPENFILENAME));
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr hInstance = IntPtr.Zero;
        public string lpstrFilter = null;
        public string lpstrCustomFilter = null;
        public int nMaxCustFilter = 0;
        public int nFilterIndex = 0;
        public string lpstrFile = null;
        public int nMaxFile = 0;
        public string lpstrFileTitle = null;
        public int nMaxFileTitle = 0;
        public string lpstrInitialDir = null;
        public string lpstrTitle = null;
        public int Flags = 0;
        public short nFileOffset = 0;
        public short nFileExtension = 0;
        public string lpstrDefExt = null;
        public IntPtr lCustData = IntPtr.Zero;
        public IntPtr lpfnHook = IntPtr.Zero;
        public string lpTemplateName = null;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class SAVEFILENAME
    {
        public int lStructSize = Marshal.SizeOf(typeof(SAVEFILENAME));
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr hInstance = IntPtr.Zero;
        public string lpstrFilter = null;
        public string lpstrCustomFilter = null;
        public int nMaxCustFilter = 0;
        public int nFilterIndex = 0;
        public string lpstrFile = null;
        public int nMaxFile = 0;
        public string lpstrFileTitle = null;
        public int nMaxFileTitle = 0;
        public string lpstrInitialDir = null;
        public string lpstrTitle = null;
        public int Flags = 0;
        public short nFileOffset = 0;
        public short nFileExtension = 0;
        public string lpstrDefExt = null;
        public IntPtr lCustData = IntPtr.Zero;
        public IntPtr lpfnHook = IntPtr.Zero;
        public string lpTemplateName = null;
    }

    // Windows API 函数
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] OPENFILENAME ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In, Out] SAVEFILENAME ofn);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHBrowseForFolder([In, Out] BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class BROWSEINFO
    {
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr pidlRoot = IntPtr.Zero;
        public string pszDisplayName = null;
        public string lpszTitle = null;
        public uint ulFlags = 0;
        public IntPtr lpfn = IntPtr.Zero;
        public IntPtr lParam = IntPtr.Zero;
        public int iImage = 0;
    }

    // 常量
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_NOCHANGEDIR = 0x00000008;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;

    /// <summary>
    /// 打开文件对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="filter">文件过滤器，格式："描述|*.扩展名|所有文件|*.*"</param>
    /// <param name="initialDir">初始目录</param>
    /// <returns>选择的文件路径，如果取消则返回null</returns>
    public static string OpenFileDialog(string title, string filter, string initialDir = "")
    {
        try
        {
            OPENFILENAME ofn = new OPENFILENAME();
            ofn.lpstrTitle = title;
            ofn.lpstrFilter = filter;
            ofn.lpstrInitialDir = string.IsNullOrEmpty(initialDir) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : initialDir;
            ofn.lpstrFile = new string('\0', 260); // 最大路径长度
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            if (GetOpenFileName(ofn))
            {
                return ofn.lpstrFile.TrimEnd('\0');
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"文件对话框打开失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存文件对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="filter">文件过滤器</param>
    /// <param name="defaultName">默认文件名</param>
    /// <param name="initialDir">初始目录</param>
    /// <returns>保存的文件路径，如果取消则返回null</returns>
    public static string SaveFileDialog(string title, string filter, string defaultName = "", string initialDir = "")
    {
        try
        {
            SAVEFILENAME ofn = new SAVEFILENAME();
            ofn.lpstrTitle = title;
            ofn.lpstrFilter = filter;
            ofn.lpstrInitialDir = string.IsNullOrEmpty(initialDir) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : initialDir;
            ofn.lpstrFile = string.IsNullOrEmpty(defaultName) ? new string('\0', 260) : defaultName + new string('\0', 260 - defaultName.Length);
            ofn.nMaxFile = 260;
            ofn.Flags = OFN_OVERWRITEPROMPT | OFN_NOCHANGEDIR;

            if (GetSaveFileName(ofn))
            {
                return ofn.lpstrFile.TrimEnd('\0');
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存文件对话框打开失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 选择文件夹对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="initialDir">初始目录</param>
    /// <returns>选择的文件夹路径，如果取消则返回null</returns>
    public static string SelectFolderDialog(string title, string initialDir = "")
    {
        try
        {
            BROWSEINFO bi = new BROWSEINFO();
            bi.lpszTitle = title;
            bi.ulFlags = 0x00000001; // BIF_RETURNONLYFSDIRS

            IntPtr pidl = SHBrowseForFolder(bi);
            if (pidl != IntPtr.Zero)
            {
                StringBuilder path = new StringBuilder(260);
                if (SHGetPathFromIDList(pidl, path))
                {
                    return path.ToString();
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"文件夹选择对话框打开失败: {ex.Message}");
            return null;
        }
    }
} 