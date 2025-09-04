using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace U0UGames.Excel2SO
{
    /// <summary>
    /// Excel行数据接口
    /// 实现此接口的类可以处理从Excel导入的单行数据
    /// 主要用于多文件生成模式，每个Excel行对应一个ScriptableObject文件
    /// </summary>
    public interface IExcelLineData
    {
        #if UNITY_EDITOR
        /// <summary>
        /// 处理数据的方法
        /// 在从Excel数据反序列化到ScriptableObject后调用
        /// 可以在这里进行数据验证、转换或其他后处理操作
        /// </summary>
        public void ProcessData();
        #endif
    }
    

    /// <summary>
    /// 资源更新工具类
    /// 提供从指定路径加载和更新Unity资源的功能
    /// </summary>
    public static class UpdateAssetUtils
    {
        /// <summary>
        /// 从指定文件夹和文件名获取Unity资源
        /// </summary>
        /// <typeparam name="T">资源类型，必须继承自UnityEngine.Object</typeparam>
        /// <param name="assetFolder">资源文件夹路径</param>
        /// <param name="assetName">资源文件名</param>
        /// <returns>找到的资源对象，如果未找到则返回null</returns>
        public static T GetAsset<T>(string assetFolder, string assetName)where T:UnityEngine.Object
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetFolder))return null;
            if (string.IsNullOrEmpty(assetName))return null;
            
            var assetPath =  Path.Combine(assetFolder, assetName);
            var assetFile = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return assetFile;
#else
            return null;
#endif
        }
        
        /// <summary>
        /// 尝试更新目标资源对象
        /// 如果找到新的资源文件且与当前目标对象不同，则更新目标对象
        /// </summary>
        /// <typeparam name="T">资源类型，必须继承自UnityEngine.Object</typeparam>
        /// <param name="assetFolder">资源文件夹路径</param>
        /// <param name="assetName">资源文件名</param>
        /// <param name="targetObject">要更新的目标对象引用</param>
        public static void TryUpdateAsset<T>(
            string assetFolder, string assetName, ref T targetObject)where T:UnityEngine.Object
        {
#if UNITY_EDITOR
            var assetFile = GetAsset<T>(assetFolder,assetName);
            if (assetFile != null && assetFile != targetObject)
            {
                targetObject = assetFile;
            }
#else
            targetObject = null;
#endif
        }
    }

}