using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

/// <summary>
/// Unity 资源引用计数管理器核心实现
/// 负责管理资源的引用计数、缓存和生命周期
/// </summary>
public class URCMCore : MonoBehaviour
{
    #region 单例实现
    private static URCMCore _instance;
    public static URCMCore Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("URCMCore");
                _instance = go.AddComponent<URCMCore>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    #endregion

    #region 配置参数
    private static bool _showLog = true;
    private const int DEFAULT_CACHE_CAPACITY = 100;
    private const float CLEAN_INTERVAL = 60f; // 自动清理的时间间隔（秒）
    #endregion

    #region 资源缓存
    // 使用 LruCache 来管理资源缓存
    private LruCache<string> _resourceCache;
    
    // 资源引用计数字典
    private Dictionary<string, int> _referenceCountMap = new Dictionary<string, int>();
    
    // 正在加载的资源任务
    private Dictionary<string, TaskCompletionSource<Object>> _loadingTasks = new Dictionary<string, TaskCompletionSource<Object>>();
    
    // 资源操作接口
    private IAssetOperations _assetOperations;
    #endregion

    #region Unity 生命周期
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeCache();
    }

    private void Start()
    {
        // 启动自动清理协程
        StartCoroutine(AutoCleanCoroutine());
    }

    private void OnDestroy()
    {
        // 清理所有资源
        ReleaseAllResources();
    }
    #endregion

    #region 初始化
    private void InitializeCache()
    {
        // 使用优化后的 LruCache，启用调试日志
        _resourceCache = new LruCache<string>(DEFAULT_CACHE_CAPACITY, _showLog);
        _resourceCache.OnRemove = OnResourceRemoved;
        Log("URCMCore initialized with cache capacity: " + DEFAULT_CACHE_CAPACITY);
    }
    
    /// <summary>
    /// 设置资源操作接口
    /// </summary>
    /// <param name="operations">资源操作接口实现</param>
    public void SetAssetOperations(IAssetOperations operations)
    {
        if (operations == null)
        {
            LogWarning("Asset operations cannot be null");
            return;
        }
        
        _assetOperations = operations;
        Log("Asset operations set: " + operations.GetType().Name);
    }

    /// <summary>
    /// 检查是否已设置资源操作接口
    /// </summary>
    /// <returns>是否已设置资源操作接口</returns>
    public bool IsAssetOperationsSet => _assetOperations != null;
    #endregion

    #region 公共 API
    /// <summary>
    /// 同步加载资源并增加引用计数
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源</returns>
    public T LoadAsset<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            LogWarning("Cannot load asset with empty path");
            return null;
        }
        
        if (_assetOperations == null)
        {
            LogWarning("Asset operations not set. Please call SetAssetOperations first.");
            return null;
        }

        // 增加引用计数
        AddReference(path);

        // 尝试从缓存获取
        var lruObj = _resourceCache.Get(path);
        if (lruObj != null && lruObj.Asset != null)
        {
            Log($"Hit cache for {_assetOperations.GetDisplayName(path)}");
            return lruObj.Asset as T;
        }

        // 如果缓存中不存在，使用注入的资源操作接口加载
        T asset = _assetOperations.LoadAsset<T>(path);
        if (asset == null)
        {
            LogWarning($"Failed to load asset at path: {_assetOperations.GetDisplayName(path)}");
            RemoveReference(path);
            return null;
        }

        // 添加到缓存
        AddToCache(path, asset);
        
        return asset;
    }

    /// <summary>
    /// 异步加载资源并增加引用计数
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>异步加载的资源任务</returns>
    public async Task<T> LoadAssetAsync<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            LogWarning("Cannot load asset with empty path");
            return null;
        }
        
        if (_assetOperations == null)
        {
            LogWarning("Asset operations not set. Please call SetAssetOperations first.");
            return null;
        }

        // 增加引用计数
        AddReference(path);

        // 尝试从缓存获取
        var lruObj = _resourceCache.Get(path);
        if (lruObj != null && lruObj.Asset != null)
        {
            Log($"Hit cache for {_assetOperations.GetDisplayName(path)}");
            return lruObj.Asset as T;
        }

        // 如果已经有相同路径的加载任务在进行中，等待该任务完成
        if (_loadingTasks.TryGetValue(path, out var existingTask))
        {
            try
            {
                var result = await existingTask.Task;
                return result as T;
            }
            catch
            {
                LogWarning($"Failed to await existing loading task for {_assetOperations.GetDisplayName(path)}");
                RemoveReference(path);
                return null;
            }
        }

        // 创建新的加载任务
        var tcs = new TaskCompletionSource<Object>();
        _loadingTasks[path] = tcs;

        try
        {
            // 使用注入的资源操作接口异步加载
            T asset = await _assetOperations.LoadAssetAsync<T>(path);
            
            if (asset == null)
            {
                throw new Exception($"Failed to load asset at path: {_assetOperations.GetDisplayName(path)}");
            }

            // 添加到缓存
            AddToCache(path, asset);
            
            // 完成任务
            tcs.SetResult(asset);
            _loadingTasks.Remove(path);
            
            return asset;
        }
        catch (Exception e)
        {
            LogWarning($"Error loading asset at path {_assetOperations.GetDisplayName(path)}: {e.Message}");
            tcs.SetException(e);
            _loadingTasks.Remove(path);
            RemoveReference(path);
            return null;
        }
    }

    /// <summary>
    /// 获取资源的当前引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>引用计数</returns>
    public int GetReferenceCount(string path)
    {
        return _referenceCountMap.TryGetValue(path, out int count) ? count : 0;
    }

    /// <summary>
    /// 增加资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>新的引用计数</returns>
    public int AddReference(string path)
    {
        if (!_referenceCountMap.TryGetValue(path, out int count))
        {
            count = 0;
        }
        
        count++;
        _referenceCountMap[path] = count;
        Log($"Added reference to {path}, new count: {count}");
        
        return count;
    }

    /// <summary>
    /// 减少资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>新的引用计数</returns>
    public int RemoveReference(string path)
    {
        if (!_referenceCountMap.TryGetValue(path, out int count))
        {
            return 0;
        }
        
        count--;
        
        if (count <= 0)
        {
            count = 0;
            _referenceCountMap.Remove(path);
            Log($"Reference count for {path} reached zero, marked for potential unload");
            
            // 获取缓存对象并标记为可释放
            var lruObj = _resourceCache.Get(path);
            if (lruObj != null)
            {
                lruObj.KeepInCache = false;
            }
        }
        else
        {
            _referenceCountMap[path] = count;
        }
        
        Log($"Removed reference from {path}, new count: {count}");
        return count;
    }

    /// <summary>
    /// 释放指定资源
    /// </summary>
    /// <param name="path">资源路径</param>
    public void ReleaseAsset(string path)
    {
        RemoveReference(path);
        
        // 如果引用计数为0，尝试立即从缓存中移除
        if (GetReferenceCount(path) <= 0)
        {
            var lruObj = _resourceCache.Get(path);
            if (lruObj != null)
            {
                lruObj.KeepInCache = false;
                // 执行资源清理
                OnResourceRemoved(lruObj);
                
                // 使用优化后的 Remove 方法直接从缓存中移除
                _resourceCache.Remove(path);
            }
        }
    }

    /// <summary>
    /// 设置缓存容量
    /// </summary>
    /// <param name="capacity">新的缓存容量</param>
    public void SetCacheCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            LogWarning("Cache capacity must be greater than zero");
            return;
        }
        
        _resourceCache.Capacity = capacity;
        Log($"Cache capacity set to {capacity}");
    }

    /// <summary>
    /// 清理缓存中未使用的资源
    /// </summary>
    public void CleanUnusedResources()
    {
        Log("Starting cleanup of unused resources");
        
        // 标记所有引用计数为0的资源为可释放
        var cachedKeys = new List<string>();
        
        // 使用 Contains 方法检查键是否存在于缓存中
        foreach (var path in _referenceCountMap.Keys)
        {
            if (_resourceCache.Contains(path))
            {
                cachedKeys.Add(path);
            }
        }
        
        foreach (var path in cachedKeys)
        {
            if (GetReferenceCount(path) <= 0)
            {
                var lruObj = _resourceCache.Get(path);
                if (lruObj != null)
                {
                    lruObj.KeepInCache = false;
                }
            }
        }
        
        // 执行清理
        _resourceCache.Clean();
        
        // 输出缓存统计信息
        Log(_resourceCache.GetStatistics());
    }
    
    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void ReleaseAllResources()
    {
        Log("Releasing all resources");
        _resourceCache.ForceClean();
        _referenceCountMap.Clear();
    }
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public string GetCacheStatistics()
    {
        return _resourceCache.GetStatistics();
    }
    
    /// <summary>
    /// 重置缓存统计数据
    /// </summary>
    public void ResetCacheStatistics()
    {
        _resourceCache.ResetStatistics();
        Log("Cache statistics reset");
    }

    /// <summary>
    /// 仅用于测试：获取指定路径的 LruObj 对象
    /// 注意：此方法仅用于测试目的，不应在生产代码中使用
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>缓存中的 LruObj 对象，如果不存在则返回 null</returns>
    public LruObj GetLruObj(string path)
    {
        if (string.IsNullOrEmpty(path) || _resourceCache == null)
        {
            return null;
        }

        // 直接从缓存中获取 LruObj
        return _resourceCache.Get(path);
    }
    #endregion

    #region 内部方法
    private void AddToCache(string path, Object asset)
    {
        var lruObj = new LruObj 
        { 
            FullPath = path,
            Guid = Guid.NewGuid().ToString(),
            KeepInCache = true,
            Asset = asset
        };
        
        _resourceCache.Put(path, lruObj);
        Log($"Added to cache: {_assetOperations?.GetDisplayName(path) ?? path}");
    }

    private bool OnResourceRemoved(LruObj obj)
    {
        if (obj == null)
        {
            return true;
        }
        
        // 如果引用计数大于0，不应该被移除
        if (GetReferenceCount(obj.FullPath) > 0)
        {
            Log($"Prevented unloading of {_assetOperations?.GetDisplayName(obj.FullPath) ?? obj.FullPath} because reference count > 0");
            return false;
        }
        
        // 执行卸载
        if (obj.Asset != null && _assetOperations != null)
        {
            Log($"Unloading asset: {_assetOperations.GetDisplayName(obj.FullPath)}");
            
            // 处理子资源
            foreach (var childObj in obj.ChildReses)
            {
                OnResourceRemoved(childObj);
            }
            
            // 调用OnDestroy回调
            if (obj.OnDestroy != null)
            {
                bool canDestroy = obj.OnDestroy.Invoke(obj);
                if (!canDestroy)
                {
                    return false;
                }
            }
            
            // 使用注入的资源操作接口卸载资源
            _assetOperations.UnloadAsset(obj.Asset, obj.FullPath);
            obj.Asset = null;
            
            // 标记为已释放
            obj.HasReleased = true;
        }
        
        return true;
    }
    
    private IEnumerator AutoCleanCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(CLEAN_INTERVAL);
            CleanUnusedResources();
        }
    }
    #endregion

    #region 日志方法
    public static void Log(string message)
    {
        if(!_showLog) return;
        Debug.Log($"URCM: {message}");
    }

    public static void LogWarning(string message)
    {
        if(!_showLog) return;
        Debug.LogWarning($"URCM: {message}");
    }

    public static void SetLogEnabled(bool enabled)
    {
        _showLog = enabled;
    }
    #endregion
}
