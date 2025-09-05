using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using U0UGames.ExcelDataParser;
using UnityEngine;

namespace U0UGames.Excel2SO
{
    /// <summary>
    /// Excel数据容器ScriptableObject基类
    /// 所有从Excel数据生成的ScriptableObject都应该继承此类
    /// 提供了从JSON和原始数据加载数据的基础功能
    /// </summary>
    public abstract class ExcelDataContainerSO : ScriptableObject
    {
        /// <summary>
        /// 从JSON字符串加载数据
        /// 子类需要实现此方法来处理具体的JSON反序列化逻辑
        /// </summary>
        /// <param name="json">包含数据的JSON字符串</param>
        public abstract void LoadJson(string json);
        
        /// <summary>
        /// 从原始数据字典加载数据
        /// 子类需要实现此方法来处理从Excel读取的原始数据
        /// </summary>
        /// <param name="rawData">包含Excel原始数据的字典</param>
        public abstract void LoadRawData(Dictionary<string, object> rawData);
        
        /// <summary>
        /// 尝试从Resources文件夹加载第一个匹配类型的ScriptableObject
        /// </summary>
        /// <typeparam name="T">要加载的ScriptableObject类型</typeparam>
        /// <param name="result">输出参数，如果找到则返回第一个匹配的对象</param>
        /// <returns>如果找到匹配的对象返回true，否则返回false</returns>
        public static bool TryLoadFirst<T>(out T result) where T:ExcelDataContainerSO
        {
            result = null;
            var soPath = Path.Join("ExcelSO", typeof(T).ToString());
            var soDataList = Resources.LoadAll(soPath);
            if (soDataList.Length <= 0) return false;
            foreach (var so in soDataList)
            {
                if (so is T targetSO)
                {
                    result = targetSO;
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 尝试从Resources文件夹加载所有匹配类型的ScriptableObject
        /// </summary>
        /// <typeparam name="T">要加载的ScriptableObject类型</typeparam>
        /// <param name="resultList">输出参数，包含所有找到的匹配对象列表</param>
        /// <returns>如果找到匹配的对象返回true，否则返回false</returns>
        public static bool TryLoad<T>(out List<T> resultList) where T:ExcelDataContainerSO
        {
            resultList = null;
            var findList = new List<T>();
            var soPath = Path.Join("ExcelSO", typeof(T).ToString());
            var soDataList = Resources.LoadAll(soPath);
            if (soDataList.Length <= 0) return false;
            foreach (var so in soDataList)
            {
                if (so is T targetSO)
                {
                    findList.Add(targetSO);
                }
            }

            if (findList.Count > 0)
            {
                resultList = findList;
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// 泛型Excel数据容器ScriptableObject基类
    /// 提供了类型安全的数据存储和访问功能
    /// 支持通过索引和键值对方式访问数据
    /// </summary>
    /// <typeparam name="T">数据项的类型</typeparam>
    public abstract class ExcelDataContainerSo<T> : ExcelDataContainerSO, IEnumerable<T>
    {
        /// <summary>
        /// 关键字段名称，用于数据查找
        /// 从Excel配置中读取，用于建立键值对映射
        /// </summary>
        [SerializeField] protected string keyName = null;
        
        /// <summary>
        /// 数据列表，存储所有从Excel导入的数据项
        /// </summary>
        [SerializeField] protected List<T> dataList = new List<T>();
        
        /// <summary>
        /// 数据字典，用于通过键值快速查找数据
        /// 键为关键字段的值，值为对应的数据项
        /// </summary>
        private Dictionary<string,T> dataDict;
        
        /// <summary>
        /// 获取泛型枚举器
        /// </summary>
        /// <returns>数据列表的泛型枚举器</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return dataList.GetEnumerator();
        }

        /// <summary>
        /// 获取非泛型枚举器
        /// </summary>
        /// <returns>数据列表的枚举器</returns>
        public IEnumerator GetEnumerator()
        {
            return dataList.GetEnumerator();
        }

        /// <summary>
        /// 获取数据项的数量
        /// </summary>
        public int Count => dataList.Count;
        
        /// <summary>
        /// 通过索引获取数据项
        /// </summary>
        /// <param name="index">数据项的索引</param>
        /// <returns>指定索引的数据项，如果索引无效则返回默认值</returns>
        public T GetDataByIndex(int index)
        {
            if (dataList != null && index >= 0 && index < dataList.Count())
            {
                return dataList[index];
            }
            return default;
        }

        /// <summary>
        /// 尝试创建数据字典
        /// 根据关键字段名称建立键值对映射，用于快速查找数据
        /// </summary>
        private void TryCreateDataDict()
        {
            if (keyName == null) 
            {
                dataDict = null;
                return;
            }
            
            // 如果字典已存在且大小匹配，不需要重建
            if (dataDict != null && dataDict.Count == dataList.Count) 
                return;
            
            // 使用反射缓存提高性能
            FieldInfo field = typeof(T).GetField(keyName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) 
            {
                Debug.LogWarning($"未找到字段: {keyName}，类型: {typeof(T).Name}");
                dataDict = null;
                return;
            }
            
            try
            {
                dataDict = new Dictionary<string, T>(dataList.Count); // 预分配容量
                int duplicateCount = 0;
                
                foreach (var data in dataList)
                {
                    if (data == null) continue;
                    
                    try
                    {
                        var fieldValue = field.GetValue(data);
                        if (fieldValue == null) continue;
                        
                        string value = fieldValue.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (dataDict.ContainsKey(value))
                            {
                                duplicateCount++;
                                Debug.LogWarning($"发现重复的键值: {value}，类型: {typeof(T).Name}");
                            }
                            else
                            {
                                dataDict[value] = data;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"获取字段值失败，字段: {keyName}, 错误: {ex.Message}");
                    }
                }
                
                if (duplicateCount > 0)
                {
                    Debug.LogWarning($"数据字典创建完成，发现 {duplicateCount} 个重复键值");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建数据字典失败: {ex.Message}");
                dataDict = null;
            }
        }

        /// <summary>
        /// 通过整数键获取数据项
        /// </summary>
        /// <param name="key">整数键值</param>
        /// <returns>匹配的数据项，如果未找到则返回默认值</returns>
        public virtual T GetDataByKey(int key) => GetDataByKey(key.ToString());
        
        /// <summary>
        /// 通过字符串键获取数据项
        /// </summary>
        /// <param name="key">字符串键值</param>
        /// <returns>匹配的数据项，如果未找到则返回默认值</returns>
        public virtual T GetDataByKey(string key)
        {
            if (keyName == null)
            {
                Debug.LogError("excel中没有指定关键词名称，可以通过#keyName指定");
                return default;
            }

            TryCreateDataDict();
            if (dataDict != null && dataDict.TryGetValue(key, out T data))
            {
                return data;
            }
            return default;
        }

        /// <summary>
        /// 从原始数据字典加载配置信息
        /// 主要提取关键字段名称等配置信息
        /// </summary>
        /// <param name="rawData">包含Excel原始数据的字典</param>
        public override void LoadRawData(Dictionary<string, object> rawData)
        {
            keyName = null;
            if (rawData.TryGetValue(ExcelReader.RawDataKey.keyName, out object name))
            {
                string nameStr = name as string;
                if (!string.IsNullOrEmpty(nameStr))
                {
                    keyName = nameStr;
                }
            }
        }
        
        /// <summary>
        /// 尝试从原始数据中加载指定类型的值
        /// </summary>
        /// <typeparam name="T1">要转换的目标类型</typeparam>
        /// <param name="rawData">原始数据字典</param>
        /// <param name="key">要获取的键</param>
        /// <param name="value">输出参数，转换后的值</param>
        /// <returns>如果转换成功返回true，否则返回false</returns>
        protected bool TryLoadValue<T1>(Dictionary<string, object> rawData, string key, out T1 value)
        {
            value = default;
            
            if (rawData == null)
            {
                Debug.LogError("原始数据字典为空");
                return false;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("键值不能为空");
                return false;
            }
            
            if (rawData.TryGetValue(key, out var obj))
            {
                try
                {
                    if (obj != null)
                    {
                        // 如果目标类型是字符串，直接转换
                        if (typeof(T1) == typeof(string))
                        {
                            value = (T1)(object)obj.ToString();
                            return true;
                        }
                        
                        // 如果对象已经是目标类型，直接转换
                        if (obj is T1 directValue)
                        {
                            value = directValue;
                            return true;
                        }
                        
                        // 使用JSON序列化进行转换
                        value = JsonConvert.DeserializeObject<T1>(obj.ToString());
                        return true;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"JSON反序列化失败，键: {key}, 值: {obj}, 目标类型: {typeof(T1)}, 错误: {jsonEx.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"类型转换失败，键: {key}, 值: {obj}, 目标类型: {typeof(T1)}, 错误: {ex.Message}");
                } 
            }
            
            return false;
        }
        
        /// <summary>
        /// 处理单个数据项
        /// 子类可以重写此方法来进行数据验证、转换或其他处理
        /// </summary>
        /// <param name="data">要处理的数据项</param>
        protected virtual void ProcessData(T data)
        {
            return;
        }
        
        /// <summary>
        /// 从JSON字符串加载数据列表
        /// 将JSON反序列化为数据项列表并存储
        /// </summary>
        /// <param name="json">包含数据列表的JSON字符串</param>
        public override void LoadJson(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("JSON字符串为空，清空数据列表");
                    dataList.Clear();
                    return;
                }

                List<Dictionary<string, object>> rawDataList;
                try
                {
                    rawDataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    if (rawDataList == null)
                    {
                        Debug.LogWarning("JSON反序列化结果为空，清空数据列表");
                        dataList.Clear();
                        return;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"JSON反序列化失败: {jsonEx.Message}\nJSON内容: {json}");
                    throw;
                }

                dataList.Clear();
                int successCount = 0;
                int totalCount = rawDataList.Count;

                foreach (var rawData in rawDataList)
                {
                    try
                    {
                        if (rawData == null)
                        {
                            Debug.LogWarning("跳过空的数据行");
                            continue;
                        }

                        string lineJson = JsonConvert.SerializeObject(rawData);
                        T data;
                        
                        try
                        {
                            data = JsonConvert.DeserializeObject<T>(lineJson);
                            if (data == null)
                            {
                                Debug.LogWarning($"反序列化结果为空，跳过数据行: {lineJson}");
                                continue;
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Debug.LogError($"反序列化数据行失败: {jsonEx.Message}\n数据行: {lineJson}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"解析数据行时发生错误: {ex.Message}\n数据行: {lineJson}");
                            continue;
                        }

                        try
                        {
                            ProcessData(data);
                            dataList.Add(data);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"处理数据项时发生错误: {ex.Message}\n数据: {lineJson}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"处理数据行时发生未预期错误: {ex.Message}");
                    }
                }

                Debug.Log($"数据加载完成: {successCount}/{totalCount} 个数据项成功加载");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadJson发生未预期错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                // 确保在出错时清空数据列表，避免部分加载的状态
                dataList.Clear();
            }
        }
    }


    // public abstract class ExcelDataLineSO: ScriptableObject
    // {
    //     public abstract void ProcessData();
    // }
}
