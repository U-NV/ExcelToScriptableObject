# Excel to ScriptableObject

Unity编辑器插件，将Excel数据自动转换为ScriptableObject资源。支持单文件和多文件生成模式，具备完善的异常处理、数据验证和性能优化。

## ✨ 功能特性

### 🔄 数据转换
- **Excel转SO**: 将Excel表格数据自动转换为Unity ScriptableObject资源
- **双模式支持**: 支持单文件模式（整个Sheet生成一个SO）和多文件模式（每行数据生成一个SO）
- **智能类型识别**: 自动识别Excel数据类型并转换为对应的C#类型
- **批量处理**: 支持批量转换多个Excel文件

### 🛠️ 编辑器工具
- **可视化配置**: 提供友好的编辑器界面进行配置管理
- **实时预览**: 支持配置预览和路径选择
- **一键生成**: 支持单个配置或批量生成ScriptableObject
- **路径管理**: 智能的输入输出路径管理

### 🔒 数据安全
- **异常处理**: 完善的异常处理机制，确保转换过程稳定可靠
- **数据验证**: 严格的数据验证和格式检查
- **错误恢复**: 部分失败时的数据恢复机制
- **日志记录**: 详细的操作日志和错误信息

### ⚡ 性能优化
- **内存优化**: 优化的内存使用，支持大文件处理
- **缓存机制**: 智能缓存机制，避免重复处理
- **批量操作**: 高效的批量文件操作
- **进度跟踪**: 实时显示处理进度

## 📦 安装

### 方法一：Package Manager安装（推荐）
1. 在Unity编辑器中打开 `Window > Package Manager`
2. 点击左上角的 `+` 按钮，选择 `Add package from git URL`
3. 输入：`https://github.com/U-NV/ExcelToScriptableObject.git`
4. 点击 `Add` 完成安装

### 方法二：手动安装
1. 下载最新版本的插件包
2. 解压到Unity项目的 `Assets/` 目录下
3. 重新打开Unity编辑器

## 🚀 快速开始

### 1. 打开工具窗口
在Unity编辑器中，点击菜单 `工具 > Excel2So` 打开工具窗口。

### 2. 配置转换规则
在"配置"标签页中：
- 选择Excel文件路径
- 设置输出文件夹路径
- 指定ScriptableObject类名
- 配置关键字段名称
- 选择生成模式（单文件/多文件）

### 3. 生成ScriptableObject
在"生成"标签页中：
- 点击"生成"按钮开始转换
- 或使用"重新生成所有文件"进行批量处理

## �� 详细使用指南

### 配置说明

#### 基本配置
- **Excel数据文件**: 选择要转换的Excel文件路径
- **导出文件夹**: 设置ScriptableObject文件的输出目录
- **SO类名**: 指定要生成的ScriptableObject类名
- **关键词属性名称**: 用于数据查找的关键字段名

#### 生成模式
- **单文件模式**: 整个Excel Sheet生成一个ScriptableObject文件
- **多文件模式**: 每行数据生成一个独立的ScriptableObject文件
- **子文件夹**: 在多文件模式下，可选择为每个Sheet创建单独的子文件夹

### 创建ScriptableObject类

#### 单文件模式示例
```csharp
using U0UGames.Excel2SO;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/GameConfig")]
public class GameConfigSO : ExcelDataContainerSo<GameConfigData>
{
    // 可以添加自定义方法
    public GameConfigData GetConfigById(int id)
    {
        return GetDataByKey(id);
    }
}

[System.Serializable]
public class GameConfigData
{
    public int id;
    public string name;
    public float value;
    public bool isActive;
}
```

#### 多文件模式示例
```csharp
using U0UGames.Excel2SO;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemConfig", menuName = "Config/ItemConfig")]
public class ItemConfigSO : ScriptableObject, IExcelLineData
{
    public int itemId;
    public string itemName;
    public string description;
    public int price;
    public ItemType type;

    public void ProcessData()
    {
        // 数据后处理逻辑
        // 例如：验证数据、计算派生值等
        if (price < 0) price = 0;
    }
}

public enum ItemType
{
    Weapon,
    Armor,
    Consumable
}
```

### 运行时使用

```csharp
using U0UGames.Excel2SO;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    [SerializeField] private GameConfigSO gameConfig;
    
    void Start()
    {
        // 通过索引获取数据
        var config1 = gameConfig.GetDataByIndex(0);
        
        // 通过键值获取数据
        var config2 = gameConfig.GetDataByKey(1001);
        
        // 遍历所有数据
        foreach (var config in gameConfig)
        {
            Debug.Log($"Config: {config.name}, Value: {config.value}");
        }
    }
}
```

## �� 使用场景

### 游戏配置管理
- 将游戏配置数据存储在Excel中，自动转换为ScriptableObject
- 支持热更新和版本控制
- 便于策划人员维护配置数据

### 本地化数据
- 多语言文本数据管理
- 支持动态语言切换
- 便于翻译人员维护

### 关卡数据
- 关卡配置、敌人数据、道具数据等
- 支持复杂的嵌套数据结构
- 便于关卡设计师调整参数

### 数值平衡
- 角色属性、技能数据、装备属性等
- 支持数值调整和版本对比
- 便于数值策划平衡游戏

## ⚙️ 高级功能

### 自定义数据处理
```csharp
public class CustomConfigSO : ExcelDataContainerSo<CustomData>
{
    protected override void ProcessData(CustomData data)
    {
        // 自定义数据处理逻辑
        data.processedValue = data.rawValue * 2;
        data.isValid = data.rawValue > 0;
    }
}
```

### 数据验证
```csharp
public class ValidatedConfigSO : ScriptableObject, IExcelLineData
{
    public int id;
    public string name;
    public float value;
    
    public void ProcessData()
    {
        // 数据验证
        if (id <= 0)
            Debug.LogError($"Invalid ID: {id}");
            
        if (string.IsNullOrEmpty(name))
            Debug.LogError($"Empty name for ID: {id}");
            
        if (value < 0)
            value = 0; // 自动修正
    }
}
```

## 🔧 配置选项

### 生成选项
- **只保留新生成的文件**: 生成时删除不再存在的旧文件
- **自动重新生成**: Excel文件变化时自动重新生成（需要文件监听器）

### 路径配置
- **相对路径**: 支持相对于项目根目录的路径
- **绝对路径**: 支持绝对路径配置
- **路径验证**: 自动验证路径有效性

## �� 故障排除

### 常见问题

**Q: 生成失败，提示"找不到类型"？**
A: 确保ScriptableObject类已正确创建，并且类名与配置中的"SO类名"一致。

**Q: 多文件模式下文件名重复？**
A: 检查Excel中的关键字段是否有重复值，确保每行数据的关键字段值唯一。

**Q: 数据转换失败？**
A: 检查Excel数据格式，确保数据类型与C#类定义匹配。

**Q: 内存不足错误？**
A: 对于大文件，建议使用单文件模式，或分批处理数据。

### 调试技巧

```csharp
// 启用详细日志
Debug.Log("开始处理Excel数据...");

// 检查配置有效性
if (config.IsValid())
{
    // 执行转换
}

// 验证生成结果
var so = AssetDatabase.LoadAssetAtPath<YourConfigSO>(assetPath);
if (so != null)
{
    Debug.Log($"成功生成: {so.name}");
}
```

## 📋 系统要求

- **Unity版本**: Unity 2019.4.25f1 或更高版本
- **.NET Framework**: 4.7.1 或更高版本
- **依赖包**: ExcelReader（已包含在依赖中）
- **支持格式**: .xlsx, .xls

## �� 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件

## �� 贡献

欢迎提交Issue和Pull Request来改进这个工具包：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## �� 技术支持

如有问题，请通过以下方式联系：

- �� 邮箱: haowei1117@foxmail.com
- �� GitHub Issues: [提交问题](https://github.com/U-NV/ExcelToScriptableObject/issues)
- �� GitHub仓库: [ExcelToScriptableObject](https://github.com/U-NV/ExcelToScriptableObject)
- 📖 文档: [在线文档](https://github.com/U-NV/ExcelToScriptableObject/wiki)

## 📝 更新日志

查看 [CHANGELOG.md](CHANGELOG.md) 了解版本更新历史。

---

⭐ 如果这个工具对您有帮助，请给个Star支持一下！