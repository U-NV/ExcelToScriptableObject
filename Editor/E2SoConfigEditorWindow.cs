using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Excel2SO.Editor
{
    public class E2SoConfigEditorWindow
    {
        private Excel2SoConfig _config;
        
        public void Init()
        {
            try
            {
                if (_config == null)
                {
                    _config = Excel2SoConfig.GetOrCreateConfig();
                    if (_config == null)
                    {
                        Debug.LogError("无法创建或获取Excel2SoConfig配置");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"初始化配置编辑器窗口失败: {ex.Message}");
            }
        }


        private string SelectPathBtn(string oldPath, string defaultText, bool isFolderPath, bool relativeToRoot = true)
        {
            try
            {
                string buttonText = null;
                string openPath = null;
                string selectPath = oldPath;

                if (string.IsNullOrEmpty(oldPath))
                {
                    buttonText = defaultText;
                    openPath = relativeToRoot ? UnityPathUtility.RootFolderPath : Application.dataPath;
                }
                else
                {
                    buttonText = oldPath;
                    openPath = relativeToRoot ?
                        UnityPathUtility.RootFolderPathToFullPath(oldPath) :
                        UnityPathUtility.AssetPathToFullPath(oldPath);
                }
                
                if (GUILayout.Button(buttonText))
                {
                    try
                    {
                        if (isFolderPath)
                        {
                            selectPath = EditorUtility.OpenFolderPanel("选择数据文件夹", openPath, null);
                        }
                        else
                        {
                            string directory = Path.GetDirectoryName(openPath);
                            if (string.IsNullOrEmpty(directory))
                            {
                                directory = Application.dataPath;
                            }
                            selectPath = EditorUtility.OpenFilePanel("选择数据文件", directory, "xlsx");
                        }
                        
                        if (!string.IsNullOrEmpty(selectPath))
                        {
                            selectPath = relativeToRoot ?
                                UnityPathUtility.FullPathToRootFolderPath(selectPath) :
                                UnityPathUtility.FullPathToAssetPath(selectPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"选择路径时发生错误: {ex.Message}");
                    }
                }

                return selectPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SelectPathBtn发生未预期错误: {ex.Message}");
                return oldPath;
            }
        }
        
        private void DrawConfigLine(Excel2SoConfig.SoGenerateConfig generateConfig)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Excel数据文件：", GUILayout.Width(120));
                generateConfig.excelFileRootPath = 
                    SelectPathBtn(generateConfig.excelFileRootPath, "选择数据文件", false,true);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("导出文件夹：", GUILayout.Width(120));
                generateConfig.resultFolderRootPath = 
                    SelectPathBtn(generateConfig.resultFolderRootPath, "选择导出路径", true, false);
                EditorGUILayout.EndHorizontal();

                generateConfig.soClassName = EditorGUILayout.TextField("SO类名:", generateConfig.soClassName);
                if (generateConfig.isSheetToMultiFile)
                {
                    generateConfig.keyName = EditorGUILayout.TextField("文件名为此属性的值:", generateConfig.keyName);
                }
                else
                {
                    generateConfig.keyName = EditorGUILayout.TextField("关键词属性名称:", generateConfig.keyName);
                }
            }
            {
                EditorGUILayout.BeginHorizontal();
                generateConfig.isSheetToMultiFile =
                    EditorGUILayout.ToggleLeft("每行数据生成为单独的SO文件", generateConfig.isSheetToMultiFile);

                if (generateConfig.isSheetToMultiFile)
                {
                    generateConfig.isMultiFileToChildFolder =
                        EditorGUILayout.ToggleLeft("每个Sheet生成到单独的文件夹", generateConfig.isMultiFileToChildFolder);
                }
                else
                {
                    generateConfig.isMultiFileToChildFolder = false;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private Vector2 _scrollPos;
        public void OnGUI()
        {
            try
            {
                if (_config == null)
                {
                    EditorGUILayout.HelpBox("配置对象为空，请重新初始化", MessageType.Error);
                    if (GUILayout.Button("重新初始化"))
                    {
                        Init();
                    }
                    return;
                }

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var configList = _config.generateConfigs;
                if (configList == null)
                {
                    EditorGUILayout.HelpBox("配置列表为空", MessageType.Warning);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                    return;
                }

                // 使用倒序遍历避免删除元素时的索引问题
                for (var index = configList.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        if (index >= configList.Count) continue; // 防止索引越界
                        
                        var generateConfig = configList[index];
                        if (generateConfig == null)
                        {
                            Debug.LogWarning($"配置项 {index} 为空，跳过");
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        DrawConfigLine(generateConfig);
                        if (GUILayout.Button("-", GUILayout.Width(20))) 
                        {
                            try
                            {
                                configList.RemoveAt(index);
                                Debug.Log($"删除配置项 {index}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"删除配置项失败: {ex.Message}");
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"绘制配置项 {index} 时发生错误: {ex.Message}");
                    }
                }

                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("+"))
                    {
                        try
                        {
                            var newConfig = new Excel2SoConfig.SoGenerateConfig();
                            configList.Add(newConfig);
                            Debug.Log("添加新的配置项");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"添加配置项失败: {ex.Message}");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();

                if (GUI.changed)
                {
                    try
                    {
                        // 标记对象为已修改
                        EditorUtility.SetDirty(_config);
                        // 保存已修改的 Asset
                        AssetDatabase.SaveAssetIfDirty(_config);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"保存配置失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"OnGUI发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                EditorGUILayout.HelpBox($"发生错误: {ex.Message}", MessageType.Error);
            }
        }
    }
}
