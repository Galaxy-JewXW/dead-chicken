# las2off 工具说明

本工具用于将LAS点云文件转换为OFF格式，便于Unity等三维可视化系统加载。

## 安装与依赖

- Python 3.11
- pip install numpy>=1.19.0 laspy>=2.0.0 open3d scipy scikit-learn tqdm

## 使用说明

1. 确保Python环境和依赖安装完成
2. 在Unity中选择.las文件上传，系统自动调用转换脚本
3. 转换后自动加载OFF点云

## 常见问题

- Python未添加到PATH：请检查环境变量
- pip权限问题：加--user参数
- 大文件内存不足：建议分批处理

## 技术支持

如遇问题请联系开发团队或查阅控制台日志。

---

（原INSTALL_GUIDE.md内容已合并） 