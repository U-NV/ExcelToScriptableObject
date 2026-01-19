using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace U0UGames.Excel2SO
{
    public static class UnityPathUtility
    {
        public static string FullPathToAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            
            // 统一使用正斜杠
            fullPath = fullPath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            
            // 只替换开头的 Application.dataPath，避免替换路径中间出现的相同字符串
            if (fullPath.StartsWith(dataPath))
            {
                string relativePath = fullPath.Substring(dataPath.Length).TrimStart('/', '\\');
                return string.IsNullOrEmpty(relativePath) ? "Assets" : "Assets/" + relativePath;
            }
            
            // 兜底：如果以上都不匹配，使用原始替换方法
            return fullPath.Replace(dataPath, "Assets");
        }
        public static string AssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            
            // 如果已经是完整路径（包含驱动器符或绝对路径），直接返回
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }
            
            // 统一使用正斜杠
            assetPath = assetPath.Replace('\\', '/');
            
            // 确保路径以 "Assets" 开头
            if (!assetPath.StartsWith("Assets"))
            {
                // 如果不是以 Assets 开头，添加 Assets 前缀
                assetPath = "Assets/" + assetPath.TrimStart('/');
            }
            
            // 只替换开头的 "Assets"，避免替换路径中间出现的 "Assets"（如 StreamingAssets）
            if (assetPath.StartsWith("Assets/") || assetPath == "Assets")
            {
                string relativePath = assetPath == "Assets" ? "" : assetPath.Substring("Assets".Length).TrimStart('/');
                return Path.Combine(Application.dataPath, relativePath).Replace('\\', '/');
            }
            
            // 兜底：如果以上都不匹配，使用原始替换方法
            return assetPath.Replace("Assets", Application.dataPath);
        }

        public static string RootFolderPath = Application.dataPath.Replace("Assets","");
        public static string FullPathToRootFolderPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            return fullPath.Replace(RootFolderPath,"");
        }
        public static string RootFolderPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return Path.Join(RootFolderPath, assetPath);
        }

        public static void DeleteAllFile(string folderPath, bool skipMetaFile)
        {
            if (!Directory.Exists(folderPath))return;
            
            string[] files = Directory.GetFiles(folderPath);
            foreach (var filePath in files)
            {
                if (skipMetaFile)
                {
                    var extension = Path.GetExtension(filePath);
                    if (extension == ".meta")
                    {
                        continue;
                    }
                }
                
                try
                {
                    File.Delete(filePath);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
        }
    }
}
