using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// URCM - Unity 资源引用计数管理系统
/// SDK入口类，提供简洁易用的资源管理API
/// </summary>
public static class URCM
{
    #region 初始化与配置

    /// <summary>
    /// 设置资源加载器
    /// </summary>
    /// <param name="operations">自定义的资源加载和释放操作实现</param>
    /// <example>
    /// <code>
    /// // 使用Resources系统
    /// URCM.SetAssetLoader(new ResourcesAssetOperations());
    /// 
    /// // 或使用Addressables系统
    /// URCM.SetAssetLoader(new AddressablesAssetOperations());
    /// </code>
    /// </example>
    public static void SetAssetLoader(IAssetOperations operations)
    {
        URCMCore.Instance.SetAssetOperations(operations);
    }

    /// <summary>
    /// 检查URCM是否已正确初始化
    /// </summary>
    /// <returns>是否已设置资源加载器</returns>
    public static bool IsInitialized()
    {
        // 这里我们假设URCMCore中会有IsAssetOperationsSet属性
        // 如果URCMCore中没有，可以添加一个
        return URCMCore.Instance != null && URCMCore.Instance.IsAssetOperationsSet;
    }

    /// <summary>
    /// 设置缓存容量
    /// </summary>
    /// <param name="capacity">新的缓存容量</param>
    public static void SetCacheCapacity(int capacity)
    {
        URCMCore.Instance.SetCacheCapacity(capacity);
    }

    /// <summary>
    /// 启用或禁用日志
    /// </summary>
    /// <param name="enabled">是否启用日志</param>
    public static void EnableLogging(bool enabled)
    {
        URCMCore.SetLogEnabled(enabled);
    }

    #endregion

    #region 基础 API

    /// <summary>
    /// 同步加载资源，自动管理引用计数
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径 (解释由具体的IAssetOperations实现决定)</param>
    /// <returns>加载的资源实例</returns>
    public static T Load<T>(string path) where T : Object
    {
        return URCMCore.Instance.LoadAsset<T>(path);
    }

    /// <summary>
    /// 异步加载资源，自动管理引用计数
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径 (解释由具体的IAssetOperations实现决定)</param>
    /// <returns>表示异步操作的任务</returns>
    public static Task<T> LoadAsync<T>(string path) where T : Object
    {
        return URCMCore.Instance.LoadAssetAsync<T>(path);
    }

    /// <summary>
    /// 释放资源，减少引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    public static void Release(string path)
    {
        URCMCore.Instance.ReleaseAsset(path);
    }

    /// <summary>
    /// 增加资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>新的引用计数</returns>
    public static int AddRef(string path)
    {
        return URCMCore.Instance.AddReference(path);
    }

    /// <summary>
    /// 减少资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>新的引用计数</returns>
    public static int RemoveRef(string path)
    {
        return URCMCore.Instance.RemoveReference(path);
    }

    /// <summary>
    /// 获取资源的当前引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>引用计数</returns>
    public static int GetRefCount(string path)
    {
        return URCMCore.Instance.GetReferenceCount(path);
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public static void ReleaseAll()
    {
        URCMCore.Instance.ReleaseAllResources();
    }

    #endregion

    #region 批量操作 API

    /// <summary>
    /// 批量加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="paths">资源路径列表</param>
    /// <returns>加载的资源字典</returns>
    public static Dictionary<string, T> LoadMultiple<T>(IEnumerable<string> paths) where T : Object
    {
        var result = new Dictionary<string, T>();
        foreach (var path in paths)
        {
            var asset = Load<T>(path);
            if (asset != null)
            {
                result[path] = asset;
            }
        }
        return result;
    }

    /// <summary>
    /// 异步批量加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="paths">资源路径列表</param>
    /// <returns>表示批量加载操作的任务</returns>
    public static async Task<Dictionary<string, T>> LoadMultipleAsync<T>(IEnumerable<string> paths) where T : Object
    {
        var tasks = new Dictionary<string, Task<T>>();
        var result = new Dictionary<string, T>();

        // 启动所有异步加载任务
        foreach (var path in paths)
        {
            tasks[path] = LoadAsync<T>(path);
        }

        // 等待所有任务完成
        foreach (var entry in tasks)
        {
            try
            {
                var asset = await entry.Value;
                if (asset != null)
                {
                    result[entry.Key] = asset;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading asset at {entry.Key}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 批量释放资源
    /// </summary>
    /// <param name="paths">资源路径列表</param>
    public static void ReleaseMultiple(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            Release(path);
        }
    }

    #endregion

    #region 预制体实例化 API

    /// <summary>
    /// 加载预制体并实例化
    /// </summary>
    /// <param name="path">预制体路径</param>
    /// <param name="parent">父对象</param>
    /// <returns>实例化的游戏对象</returns>
    public static GameObject Instantiate(string path, Transform parent = null)
    {
        var prefab = Load<GameObject>(path);
        if (prefab == null) return null;
        
        var instance = UnityEngine.Object.Instantiate(prefab, parent);
        
        // 可以在实例上附加组件，自动处理prefab的引用计数
        var refTracker = instance.AddComponent<URCMRefTracker>();
        refTracker.Initialize(path);
        
        return instance;
    }

    /// <summary>
    /// 异步加载预制体并实例化
    /// </summary>
    /// <param name="path">预制体路径</param>
    /// <param name="parent">父对象</param>
    /// <returns>表示实例化操作的任务</returns>
    public static async Task<GameObject> InstantiateAsync(string path, Transform parent = null)
    {
        var prefab = await LoadAsync<GameObject>(path);
        if (prefab == null) return null;
        
        var instance = UnityEngine.Object.Instantiate(prefab, parent);
        
        // 可以在实例上附加组件，自动处理prefab的引用计数
        var refTracker = instance.AddComponent<URCMRefTracker>();
        refTracker.Initialize(path);
        
        return instance;
    }

    #endregion

    #region 管理 API

    /// <summary>
    /// 清理未使用的资源
    /// </summary>
    public static void CleanUnused()
    {
        URCMCore.Instance.CleanUnusedResources();
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public static string GetCacheStats()
    {
        return URCMCore.Instance.GetCacheStatistics();
    }

    /// <summary>
    /// 重置缓存统计
    /// </summary>
    public static void ResetCacheStats()
    {
        URCMCore.Instance.ResetCacheStatistics();
    }

    #endregion

    #region 测试辅助方法

    /// <summary>
    /// 仅用于测试：获取指定路径的 LruObj 对象
    /// 注意：此方法仅用于测试目的，不应在生产代码中使用
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>缓存中的 LruObj 对象，如果不存在则返回 null</returns>
    public static LruObj GetLruObj(string path)
    {
        return URCMCore.Instance.GetLruObj(path);
    }

    #endregion
}

/// <summary>
/// 用于自动跟踪和管理实例化预制体的引用计数
/// </summary>
public class URCMRefTracker : MonoBehaviour
{
    private string _resourcePath;
    
    public void Initialize(string path)
    {
        _resourcePath = path;
    }
    
    private void OnDestroy()
    {
        // 当GameObject被销毁时，自动减少引用计数
        if (!string.IsNullOrEmpty(_resourcePath))
        {
            URCM.RemoveRef(_resourcePath);
        }
    }
}