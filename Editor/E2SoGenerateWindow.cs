using System;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Excel2SO.Editor
{
    public class E2SoGenerateWindow
    {
        private Excel2SoConfig _config;
        private Vector2 _scrollPos;
        private bool _onlyKeepNewGeneratedFile;
        
        private class EditorPrefsKey
        {
            public const string OnlyKeepNewGeneratedFile = "E2SoGenerateWindow.ClearFolderBeforeGenerate";
        }
        
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

                _onlyKeepNewGeneratedFile = EditorPrefs.GetBool(EditorPrefsKey.OnlyKeepNewGeneratedFile, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"初始化生成窗口失败: {ex.Message}");
            }
        }
        private void DrawCurrConfig(Excel2SoConfig.SoGenerateConfig generateConfig)
        {
            try
            {
                if (generateConfig == null)
                {
                    EditorGUILayout.HelpBox("配置项为空", MessageType.Warning);
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("生成", GUILayout.Width(40)))
                    {
                        try
                        {
                            SoGenerateUtils.ExcelToScriptableObject(generateConfig, _onlyKeepNewGeneratedFile);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"生成ScriptableObject失败: {ex.Message}");
                        }
                    }
                    
                    // 显示Excel文件路径按钮
                    string excelPath = generateConfig.excelFileRootPath ?? "未设置";
                    if (GUILayout.Button(excelPath))
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(generateConfig.excelFileRootPath))
                            {
                                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(generateConfig.excelFileRootPath);
                                if (obj != null)
                                {
                                    EditorGUIUtility.PingObject(obj);
                                }
                                else
                                {
                                    Debug.LogWarning($"无法找到Excel文件: {generateConfig.excelFileRootPath}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"定位Excel文件失败: {ex.Message}");
                        }
                    }
                    
                    // 显示输出文件夹路径按钮
                    string outputPath = generateConfig.resultFolderRootPath ?? "未设置";
                    if (GUILayout.Button(outputPath))
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(generateConfig.resultFolderRootPath))
                            {
                                UnityEngine.Object folderObject = AssetDatabase.LoadMainAssetAtPath(generateConfig.resultFolderRootPath);
                                if (folderObject != null)
                                {
                                    EditorGUIUtility.PingObject(folderObject);
                                }
                                else
                                {
                                    Debug.LogWarning($"无法找到输出文件夹: {generateConfig.resultFolderRootPath}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"定位输出文件夹失败: {ex.Message}");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                Debug.LogError($"绘制配置项时发生错误: {ex.Message}");
                EditorGUILayout.HelpBox($"绘制配置项时发生错误: {ex.Message}", MessageType.Error);
            }
        }

        private void ShowCurrConfig()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("当前所有配置文件：");
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (var generateConfig in _config.generateConfigs)
                {
                    DrawCurrConfig(generateConfig);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }
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

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _onlyKeepNewGeneratedFile = EditorGUILayout.ToggleLeft("只保留新生成的文件", _onlyKeepNewGeneratedFile);
                try
                {
                    EditorPrefs.SetBool(EditorPrefsKey.OnlyKeepNewGeneratedFile, _onlyKeepNewGeneratedFile);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"保存编辑器偏好设置失败: {ex.Message}");
                }

                ShowCurrConfig();

                if (GUILayout.Button("重新生成所有文件"))
                {
                    try
                    {
                        if (_config.generateConfigs == null || _config.generateConfigs.Count == 0)
                        {
                            Debug.LogWarning("没有配置项可以生成");
                            return;
                        }

                        int successCount = 0;
                        int totalCount = _config.generateConfigs.Count;
                        
                        foreach (var generateConfig in _config.generateConfigs)
                        {
                            try
                            {
                                if (generateConfig != null)
                                {
                                    SoGenerateUtils.ExcelToScriptableObject(generateConfig, _onlyKeepNewGeneratedFile);
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"生成配置项失败: {ex.Message}");
                            }
                        }
                        
                        Debug.Log($"批量生成完成: {successCount}/{totalCount} 个配置项成功处理");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"批量生成过程中发生错误: {ex.Message}");
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            catch (Exception ex)
            {
                Debug.LogError($"OnGUI发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                EditorGUILayout.HelpBox($"发生错误: {ex.Message}", MessageType.Error);
            }
        }
    }
}