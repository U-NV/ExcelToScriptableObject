using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
# endif

namespace U0UGames.Excel2SO
{
    /// <summary>
    /// Excel转ScriptableObject配置文件
    /// 用于存储和管理Excel文件转换为ScriptableObject的配置信息
    /// </summary>
    // [CreateAssetMenu(fileName = "Excel2SoConfig", menuName = "SO/Excel2SoConfig", order = 0)]
    public class Excel2SoConfig : ScriptableObject
    {
        /// <summary>
        /// ScriptableObject生成配置类
        /// 定义了单个Excel文件转换为ScriptableObject的详细配置
        /// </summary>
        [System.Serializable]
        public class SoGenerateConfig
        {
            /// <summary>
            /// Excel文件的根路径（相对于项目根目录）
            /// </summary>
            [FormerlySerializedAs("excelFileAssetPath")] public string excelFileRootPath;
            
            /// <summary>
            /// 生成的ScriptableObject文件的保存路径（相对于Assets目录）
            /// </summary>
            [FormerlySerializedAs("resultFolderAssetPath")] public string resultFolderRootPath;
            
            /// <summary>
            /// 要生成的ScriptableObject类名
            /// 该类必须继承自ExcelDataContainerSO或实现IExcelLineData接口
            /// </summary>
            public string soClassName;
            
            /// <summary>
            /// 关键字段名称，用于数据查找和文件命名
            /// 在单文件模式下用作数据查找的键
            /// 在多文件模式下用作生成文件名的字段名
            /// </summary>
            public string keyName;
            
            /// <summary>
            /// 是否将每个Sheet行数据生成为单独的ScriptableObject文件
            /// true: 每行数据生成一个文件（多文件模式）
            /// false: 整个Sheet生成一个文件（单文件模式）
            /// </summary>
            public bool isSheetToMultiFile;
            
            /// <summary>
            /// 在多文件模式下，是否将每个Sheet的数据生成到单独的子文件夹中
            /// 仅在isSheetToMultiFile为true时有效
            /// </summary>
            public bool isMultiFileToChildFolder;
        }

        /// <summary>
        /// 生成配置列表
        /// 可以配置多个Excel文件到ScriptableObject的转换规则
        /// </summary>
        public List<SoGenerateConfig> generateConfigs = new List<SoGenerateConfig>();
        
#if UNITY_EDITOR
        /// <summary>
        /// 配置文件的默认保存路径
        /// </summary>
        private const string Excel2SoConfigAssetPath = @"Assets\Settings\Excel2SoConfig.asset";
        
        /// <summary>
        /// 获取或创建Excel2So配置文件
        /// 如果配置文件不存在，会自动创建一个新的配置文件
        /// </summary>
        /// <returns>Excel2SoConfig配置对象</returns>
        public static Excel2SoConfig GetOrCreateConfig()
        {
            // 先确保文件夹存在
            var folderFullPath = UnityPathUtility.AssetPathToFullPath(Path.GetDirectoryName(Excel2SoConfigAssetPath));
            if (!Directory.Exists(folderFullPath))
            {
                Directory.CreateDirectory(folderFullPath);
            }

            var config = AssetDatabase.LoadAssetAtPath<Excel2SoConfig>(Excel2SoConfigAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<Excel2SoConfig>();
                AssetDatabase.CreateAsset(config, Excel2SoConfigAssetPath);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            return config;
        }
#endif
    }

}
