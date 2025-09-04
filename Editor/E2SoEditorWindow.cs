using System;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Excel2SO.Editor
{
    public class E2SoEditorWindow : EditorWindow
    {
        private class EditorPrefsKey
        {
            public const string ToolBarModeIndex = "E2SoEditorWindow.ToolBarModeIndex";
        }
        private readonly E2SoConfigEditorWindow _configEditorWindow = new E2SoConfigEditorWindow();
        private readonly E2SoGenerateWindow _generateWindow = new E2SoGenerateWindow();
        
        [MenuItem("工具/Excel2So")]
        static void Open()
        {
            E2SoEditorWindow window = EditorWindow.GetWindow<E2SoEditorWindow>("ExcelToSo");
            window.minSize = new Vector2(100, 100);
        }
        
        private void OnEnable()
        {
            _mode = EditorPrefs.GetInt(EditorPrefsKey.ToolBarModeIndex, 0);
            _configEditorWindow.Init();
            _generateWindow.Init();
        }

        private int _mode = 0;
        private string[] toolBarOption = new string[]
        {
            "配置","生成"
        };
        private void OnGUI()
        {
            _mode = GUILayout.Toolbar(_mode,toolBarOption);
            EditorPrefs.SetInt(EditorPrefsKey.ToolBarModeIndex, _mode);
            
            EditorGUILayout.BeginVertical();
            switch (_mode)
            {
                case 0:
                    _configEditorWindow.OnGUI();
                    break;
                case 1:
                    _generateWindow.OnGUI();
                    break;
            }
            
            EditorGUILayout.EndVertical();

        }
    }
}
