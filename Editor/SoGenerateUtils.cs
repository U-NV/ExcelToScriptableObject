using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using U0UGames.Excel;
using UnityEditor;
using UnityEngine;

// using DG.Tweening.Plugins.Core.PathCore;

namespace U0UGames.Excel2SO.Editor
{
    /// <summary>
    /// ScriptableObject生成工具类
    /// 提供将Excel数据转换为ScriptableObject的核心功能
    /// 支持单文件模式和多文件模式的生成
    /// </summary>
    public static class SoGenerateUtils
    {
        /// <summary>
        /// 日志前缀，用于标识ExcelAutoSO相关的日志信息
        /// </summary>
        private const string LogPrefix = "<color=#00ff00>[ExcelAutoSO]</color> ";
        
        /// <summary>
        /// 运行时程序集缓存
        /// </summary>
        private static Assembly _runtimeAssembly;
        
        /// <summary>
        /// 获取运行时程序集
        /// 用于反射查找ScriptableObject类型
        /// </summary>
        public static Assembly RuntimeAssembly
        {
            get
            {
                if (_runtimeAssembly == null)
                {
                    _runtimeAssembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                }
                return _runtimeAssembly;
            }
        }
        /// <summary>
        /// Excel转ScriptableObject配置结构体
        /// 包含单个Sheet转换所需的所有配置信息
        /// </summary>
        private struct ExcelToScriptableObjectConfig
        {
            /// <summary>
            /// 是否删除旧文件
            /// 在多文件模式下，是否删除不再存在的文件
            /// </summary>
            public readonly bool removeOldFile;
            
            /// <summary>
            /// Sheet名称
            /// </summary>
            public readonly string sheetName;
            
            // public readonly string className;
            
            /// <summary>
            /// ScriptableObject类型
            /// </summary>
            public readonly Type soClass;
            
            /// <summary>
            /// 是否将Sheet数据生成为多个文件
            /// true: 每行数据生成一个文件
            /// false: 整个Sheet生成一个文件
            /// </summary>
            public readonly bool isSheetDataToMultiFile;
            
            /// <summary>
            /// 关键字段名称
            /// 用于数据查找和文件命名
            /// </summary>
            public readonly string keyName;
            
            /// <summary>
            /// 保存文件夹的完整路径
            /// </summary>
            public readonly string saveFolderFullPath;

            /// <summary>
            /// 构造函数
            /// 根据生成配置和当前Sheet名称创建配置对象
            /// </summary>
            /// <param name="generateConfig">生成配置</param>
            /// <param name="currSheetName">当前Sheet名称</param>
            /// <param name="removeOldFile">是否删除旧文件</param>
            public ExcelToScriptableObjectConfig(Excel2SoConfig.SoGenerateConfig generateConfig, string currSheetName, bool removeOldFile = false)
            {
                this.removeOldFile = removeOldFile;
                // keyName
                keyName = generateConfig.keyName;
                // sheetName
                sheetName = currSheetName;
                isSheetDataToMultiFile = generateConfig.isSheetToMultiFile;

                var folderAssetPath = generateConfig.resultFolderRootPath;
                if (generateConfig.isSheetToMultiFile && generateConfig.isMultiFileToChildFolder)
                {
                    folderAssetPath = Path.Combine(folderAssetPath, currSheetName);
                }
                saveFolderFullPath = UnityPathUtility.AssetPathToFullPath(folderAssetPath);
                
                // soClassType
                var className = generateConfig.soClassName;
                soClass = null;
                if (string.IsNullOrEmpty(className))
                {
                    Debug.LogError($"{LogPrefix} excel 文件没有指定 so 类型，可能是没有添加关键词 #{ExcelReader.RawDataKey.className}");
                }
                else
                {
                    foreach (var classType in RuntimeAssembly.GetTypes())
                    {
                        if (classType.FullName == null )continue;
                        var namePath = classType.FullName.Split(".");
                        var lastClassName = namePath[namePath.Length - 1];
                        if (lastClassName == className)
                        {
                            soClass = classType;
                            break;
                        }
                    }
                    if (soClass == null)
                    {
                        Debug.LogError($"{LogPrefix} 找不到类型：{className}");
                    }
                }
            }
            
            /// <summary>
            /// 验证配置是否有效
            /// </summary>
            /// <returns>如果配置有效返回true，否则返回false</returns>
            public bool IsValid()
            {
                return soClass != null
                       && sheetName != null
                       && saveFolderFullPath != null;
            }
        }
        
        
        
        /// <summary>
        /// 将Excel文件转换为ScriptableObject
        /// 这是主要的转换方法，处理整个Excel文件的所有Sheet
        /// </summary>
        /// <param name="generateConfig">生成配置</param>
        /// <param name="onlyKeepNewGeneratedFile">是否只保留新生成的文件（删除旧文件）</param>
        public static void ExcelToScriptableObject(Excel2SoConfig.SoGenerateConfig generateConfig, bool onlyKeepNewGeneratedFile = true)
        {
            try
            {
                // 参数验证
                if (generateConfig == null)
                {
                    Debug.LogError($"{LogPrefix} 生成配置不能为空");
                    return;
                }

                if (string.IsNullOrEmpty(generateConfig.excelFileRootPath))
                {
                    Debug.LogError($"{LogPrefix} Excel文件路径不能为空");
                    return;
                }

                if (string.IsNullOrEmpty(generateConfig.resultFolderRootPath))
                {
                    Debug.LogError($"{LogPrefix} 输出文件夹路径不能为空");
                    return;
                }

                var excelFullPath = UnityPathUtility.AssetPathToFullPath(generateConfig.excelFileRootPath);
                var exportFolderFullPath = UnityPathUtility.AssetPathToFullPath(generateConfig.resultFolderRootPath);
                
                if (!File.Exists(excelFullPath))
                {
                    Debug.LogError($"{LogPrefix} Excel文件不存在: {excelFullPath}");
                    return;
                }
                
                if (string.IsNullOrEmpty(exportFolderFullPath))
                {
                    Debug.LogError($"{LogPrefix} 输出文件夹路径无效: {generateConfig.resultFolderRootPath}");
                    return;
                }

                // 读取Excel数据
                List<Dictionary<string, object>> dataList;
                try
                {
                    dataList = ExcelReader.GetRawData(excelFullPath);
                    if (dataList == null || dataList.Count == 0)
                    {
                        Debug.LogWarning($"{LogPrefix} Excel文件中没有找到有效数据: {excelFullPath}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{LogPrefix} 读取Excel文件失败: {excelFullPath}\n错误: {ex.Message}");
                    return;
                }

                int successCount = 0;
                int totalSheets = dataList.Count;
                
                foreach (var sheetRawData in dataList)
                {
                    try
                    {
                        if (!sheetRawData.TryGetValue(ExcelReader.RawDataKey.sheetName, out object sheetNameObj) || sheetNameObj == null)
                        {
                            Debug.LogWarning($"{LogPrefix} 跳过无效的Sheet数据（缺少Sheet名称）");
                            continue;
                        }
                        
                        string sheetName = sheetNameObj as string;
                        if (string.IsNullOrEmpty(sheetName))
                        {
                            Debug.LogWarning($"{LogPrefix} 跳过无效的Sheet数据（Sheet名称为空）");
                            continue;
                        }
                        
                        ExcelToScriptableObjectConfig config = new ExcelToScriptableObjectConfig(generateConfig, sheetName, onlyKeepNewGeneratedFile);
                        if (!config.IsValid())
                        {
                            Debug.LogError($"{LogPrefix} Sheet '{sheetName}' 的配置无效，跳过处理");
                            continue;
                        }
                        
                        UpdateSoFile(config, sheetRawData);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LogPrefix} 处理Sheet时发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"{LogPrefix} 转换完成: {successCount}/{totalSheets} 个Sheet成功处理\n" +
                          $"SO类名: {generateConfig.soClassName}\n" +
                          $"源文件: {generateConfig.excelFileRootPath}\n" +
                          $"输出目录: {generateConfig.resultFolderRootPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Excel转ScriptableObject过程中发生未预期的错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        
        



        /// <summary>
        /// 尝试创建文件夹
        /// 如果文件夹不存在则创建它
        /// </summary>
        /// <param name="folderFullPath">要创建的文件夹完整路径</param>
        /// <returns>如果创建成功或文件夹已存在返回true，否则返回false</returns>
        private static bool TryCreateFolder(string folderFullPath)
        {
            if (string.IsNullOrEmpty(folderFullPath))
            {
                Debug.LogError($"{LogPrefix} 文件夹路径为空");
                return false;
            }
            
            try
            {
                if (!Directory.Exists(folderFullPath))
                {
                    Directory.CreateDirectory(folderFullPath);
                    Debug.Log($"{LogPrefix} 创建文件夹: {folderFullPath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} 创建文件夹失败: {folderFullPath}\n错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将Sheet数据生成为单个ScriptableObject文件
        /// 整个Sheet的所有数据都存储在一个文件中
        /// </summary>
        /// <param name="config">转换配置</param>
        /// <param name="rawData">原始数据字典</param>
        private static void SheetToSingleSoFile(ExcelToScriptableObjectConfig config, Dictionary<string, object> rawData)
        {
            try
            {
                if (rawData == null)
                {
                    Debug.LogError($"{LogPrefix} 原始数据为空");
                    return;
                }

                if (!rawData.TryGetValue(ExcelReader.RawDataKey.dataList, out object dataListObj))
                {
                    Debug.LogWarning($"{LogPrefix} Sheet '{config.sheetName}' 中没有找到数据列表");
                    return;
                }
                
                if (dataListObj is not List<Dictionary<string, object>> rawDataList)
                {
                    Debug.LogError($"{LogPrefix} Sheet '{config.sheetName}' 的数据格式不正确");
                    return;
                }

                if (rawDataList.Count == 0)
                {
                    Debug.LogWarning($"{LogPrefix} Sheet '{config.sheetName}' 中没有数据行");
                    return;
                }
                
                // 创建文件夹
                if (!TryCreateFolder(config.saveFolderFullPath))
                {
                    Debug.LogError($"{LogPrefix} 无法创建输出文件夹: {config.saveFolderFullPath}");
                    return;
                }

                string assetFullPath = Path.Combine(config.saveFolderFullPath, $"{config.sheetName}.asset");
                string assetPath = UnityPathUtility.FullPathToAssetPath(assetFullPath);
                
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError($"{LogPrefix} 无法转换资源路径: {assetFullPath}");
                    return;
                }

                string json;
                try
                {
                    json = JsonConvert.SerializeObject(rawDataList);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{LogPrefix} 序列化数据失败: {ex.Message}");
                    return;
                }

                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        ExcelDataContainerSO _target = null;
                        
                        // 检查文件是否已存在
                        if (File.Exists(assetFullPath))
                        {
                            _target = AssetDatabase.LoadAssetAtPath<ExcelDataContainerSO>(assetPath);
                            if (_target == null)
                            {
                                Debug.LogWarning($"{LogPrefix} 无法加载现有资源: {assetPath}");
                            }
                        }
                        
                        // 如果文件不存在或加载失败，创建新文件
                        if (_target == null)
                        {
                            try
                            {
                                _target = ScriptableObject.CreateInstance(config.soClass) as ExcelDataContainerSO;
                                if (_target == null)
                                {
                                    Debug.LogError($"{LogPrefix} 无法创建ScriptableObject实例，类型: {config.soClass?.Name}");
                                    return;
                                }
                                
                                AssetDatabase.CreateAsset(_target, assetPath);
                                Debug.Log($"{LogPrefix} 创建新的ScriptableObject文件: {assetPath}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"{LogPrefix} 创建ScriptableObject失败: {ex.Message}");
                                return;
                            }
                        }

                        // 加载数据
                        try
                        {
                            _target.LoadRawData(rawData);
                            _target.LoadJson(json);
                            EditorUtility.SetDirty(_target);
                            Debug.Log($"{LogPrefix} 成功更新ScriptableObject: {assetPath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"{LogPrefix} 加载数据到ScriptableObject失败: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LogPrefix} 处理ScriptableObject时发生错误: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} SheetToSingleSoFile发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 尝试从嵌套字典中获取字符串值
        /// 支持通过点分隔的路径访问嵌套数据
        /// </summary>
        /// <param name="namePath">字段路径，使用点分隔（如"user.name"）</param>
        /// <param name="data">数据字典</param>
        /// <returns>找到的字符串值，如果未找到则返回null</returns>
        private static string TryGetNameValue(string namePath,Dictionary<string,object> data)
        {
            if(string.IsNullOrEmpty(namePath))return null;
            if(data == null)return null;
            
            string[] names = namePath.Split('.');
            Dictionary<string, object> currDir = data;
            foreach (var name in names)
            {
                if (!currDir.TryGetValue(name, out var value))
                {
                    return null;
                }
                if (value is Dictionary<string, object> nextData)
                {
                    currDir = nextData;
                    continue;
                }
                else if(value is string targetValue)
                {
                    return targetValue;
                }
            }

            return null;
        }
        
        
        /// <summary>
        /// 将Sheet数据生成为多个ScriptableObject文件
        /// 每行数据生成一个独立的文件
        /// </summary>
        /// <param name="config">转换配置</param>
        /// <param name="rawData">原始数据字典</param>
        private static void SheetToMultiSoFile(ExcelToScriptableObjectConfig config, Dictionary<string, object> rawData)
        {
            try
            {
                if (rawData == null)
                {
                    Debug.LogError($"{LogPrefix} 原始数据为空");
                    return;
                }

                if (!rawData.TryGetValue(ExcelReader.RawDataKey.dataList, out object dataListObj))
                {
                    Debug.LogWarning($"{LogPrefix} Sheet '{config.sheetName}' 中没有找到数据列表");
                    return;
                }
                
                if (dataListObj is not List<Dictionary<string, object>> rawDataList)
                {
                    Debug.LogError($"{LogPrefix} Sheet '{config.sheetName}' 的数据格式不正确");
                    return;
                }

                if (rawDataList.Count == 0)
                {
                    Debug.LogWarning($"{LogPrefix} Sheet '{config.sheetName}' 中没有数据行");
                    return;
                }

                var soClass = config.soClass;
                if (soClass == null)
                {
                    Debug.LogError($"{LogPrefix} ScriptableObject类型为空");
                    return;
                }

                var classInterfaces = soClass.GetInterfaces();
                if (!classInterfaces.Contains(typeof(IExcelLineData)))
                {
                    Debug.LogError($"{LogPrefix} 目标SoClass {soClass.Name} 没有实现接口 IExcelLineData");
                    return;
                }
                
                if (string.IsNullOrEmpty(config.keyName))
                {
                    Debug.LogError($"{LogPrefix} 关键字段名称为空，无法生成文件名");
                    return;
                }
                
                // 创建文件夹
                if (!TryCreateFolder(config.saveFolderFullPath))
                {
                    Debug.LogError($"{LogPrefix} 无法创建输出文件夹: {config.saveFolderFullPath}");
                    return;
                }
                
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        HashSet<string> newFileNameSet = new HashSet<string>();
                        int successCount = 0;
                        int totalRows = rawDataList.Count;
                        
                        foreach (var excelRawDataLine in rawDataList)
                        {
                            try
                            {
                                if (excelRawDataLine == null)
                                {
                                    Debug.LogWarning($"{LogPrefix} 跳过空的数据行");
                                    continue;
                                }

                                string assetName = TryGetNameValue(config.keyName, excelRawDataLine);
                                
                                if (string.IsNullOrEmpty(assetName))
                                {
                                    Debug.LogWarning($"{LogPrefix} 跳过没有关键字段值的数据行，字段: {config.keyName}");
                                    continue;
                                }
                                
                                // 清理文件名，移除非法字符
                                assetName = SanitizeFileName(assetName);
                                if (string.IsNullOrEmpty(assetName))
                                {
                                    Debug.LogWarning($"{LogPrefix} 跳过无效的文件名");
                                    continue;
                                }
                                
                                string assetFullPath = Path.Combine(config.saveFolderFullPath, $"{assetName}.asset");
                                string assetPath = UnityPathUtility.FullPathToAssetPath(assetFullPath);
                                
                                if (string.IsNullOrEmpty(assetPath))
                                {
                                    Debug.LogError($"{LogPrefix} 无法转换资源路径: {assetFullPath}");
                                    continue;
                                }

                                string lineJson;
                                try
                                {
                                    lineJson = JsonConvert.SerializeObject(excelRawDataLine);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"{LogPrefix} 序列化数据行失败: {ex.Message}");
                                    continue;
                                }

                                ScriptableObject _target = null;
                                
                                // 检查文件是否已存在
                                if (File.Exists(assetFullPath))
                                {
                                    try
                                    {
                                        var existingFile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                                        if (existingFile != null && existingFile.GetType() == soClass)
                                        {
                                            _target = existingFile;
                                        }
                                        else
                                        {
                                            // 类型不匹配，删除旧文件
                                            File.Delete(assetFullPath);
                                            Debug.Log($"{LogPrefix} 删除类型不匹配的旧文件: {assetPath}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"{LogPrefix} 处理现有文件时出错: {ex.Message}");
                                    }
                                }
                                
                                // 如果文件不存在或类型不匹配，创建新文件
                                if (_target == null)
                                {
                                    try
                                    {
                                        _target = ScriptableObject.CreateInstance(soClass);
                                        if (_target == null)
                                        {
                                            Debug.LogError($"{LogPrefix} 无法创建ScriptableObject实例，类型: {soClass.Name}");
                                            continue;
                                        }
                                        
                                        AssetDatabase.CreateAsset(_target, assetPath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"{LogPrefix} 创建ScriptableObject失败: {ex.Message}");
                                        continue;
                                    }
                                }
                                
                                // 加载数据
                                try
                                {
                                    JsonUtility.FromJsonOverwrite(lineJson, _target);
                                    
                                    if (_target is IExcelLineData lineData)
                                    {
                                        lineData.ProcessData();
                                        EditorUtility.SetDirty(_target);
                                        newFileNameSet.Add(assetName);
                                        successCount++;
                                    }
                                    else
                                    {
                                        Debug.LogError($"{LogPrefix} ScriptableObject未实现IExcelLineData接口: {assetPath}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"{LogPrefix} 加载数据到ScriptableObject失败: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"{LogPrefix} 处理数据行时发生错误: {ex.Message}");
                            }
                        }

                        Debug.Log($"{LogPrefix} 多文件模式处理完成: {successCount}/{totalRows} 个文件成功生成");

                        // 删除旧文件
                        if (config.removeOldFile)
                        {
                            try
                            {
                                CleanupOldFiles(config.saveFolderFullPath, newFileNameSet);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"{LogPrefix} 清理旧文件时发生错误: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LogPrefix} 多文件模式处理过程中发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} SheetToMultiSoFile发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 清理文件名，移除非法字符
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <returns>清理后的文件名</returns>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;
                
            // 移除或替换Windows文件系统不允许的字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // 移除前后空格
            fileName = fileName.Trim();
            
            // 确保文件名不为空且不以点开头
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("."))
            {
                fileName = "unnamed_" + DateTime.Now.Ticks;
            }
            
            return fileName;
        }
        
        /// <summary>
        /// 清理旧文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="newFileNameSet">新文件名集合</param>
        private static void CleanupOldFiles(string folderPath, HashSet<string> newFileNameSet)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return;
                    
                var allFiles = Directory.GetFiles(folderPath);
                int deletedCount = 0;
                
                foreach (var filePath in allFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var extension = Path.GetExtension(fileName);
                        
                        // 跳过.meta文件
                        if (extension == ".meta")
                            continue;
                            
                        // 移除扩展名获取文件名
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        
                        if (!newFileNameSet.Contains(nameWithoutExtension))
                        {
                            File.Delete(filePath);
                            deletedCount++;
                            
                            // 同时删除对应的.meta文件
                            var metaFilePath = filePath + ".meta";
                            if (File.Exists(metaFilePath))
                            {
                                File.Delete(metaFilePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LogPrefix} 删除文件失败: {filePath}\n错误: {ex.Message}");
                    }
                }
                
                if (deletedCount > 0)
                {
                    Debug.Log($"{LogPrefix} 清理了 {deletedCount} 个旧文件");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} 清理旧文件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新ScriptableObject文件
        /// 根据配置选择单文件或多文件模式
        /// </summary>
        /// <param name="config">转换配置</param>
        /// <param name="rawData">原始数据字典</param>
        private static void UpdateSoFile(ExcelToScriptableObjectConfig config, Dictionary<string, object> rawData)
        {
            try
            {
                if (!config.IsValid())
                {
                    Debug.LogError($"{LogPrefix} Excel导出配置错误");
                    return;
                }
                
                if (rawData == null)
                {
                    Debug.LogError($"{LogPrefix} 原始数据为空");
                    return;
                }
                
                if (!rawData.ContainsKey(ExcelReader.RawDataKey.dataList))
                {
                    Debug.LogWarning($"{LogPrefix} 原始数据中缺少数据列表");
                    return;
                }

                if (config.isSheetDataToMultiFile)
                {
                    SheetToMultiSoFile(config, rawData);
                }
                else
                {
                    SheetToSingleSoFile(config, rawData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} UpdateSoFile发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}