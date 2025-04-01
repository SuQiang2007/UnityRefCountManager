using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 高性能 LRU (Least Recently Used) 缓存实现
/// </summary>
public class LruCache<TKey> where TKey : notnull
{
    #region 数据结构

    /// <summary>
    /// 双向链表节点
    /// </summary>
    private class CacheNode
    {
        public TKey Key;
        public LruObj Value;
        public CacheNode Next;
        public CacheNode Prev;

        public CacheNode(TKey key, LruObj value)
        {
            Key = key;
            Value = value;
        }
    }

    #endregion

    #region 字段和属性

    // 头尾指针（直接使用指针而不是LinkedList，减少内存开销和间接调用）
    private CacheNode _head;
    private CacheNode _tail;

    // 存储键和节点的映射关系
    private readonly Dictionary<TKey, CacheNode> _cacheMap;

    /// <summary>
    /// 缓存容量
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// 当前缓存的元素数量
    /// </summary>
    public int Count => _cacheMap.Count;

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public long Hits { get; private set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public long Misses { get; private set; }

    /// <summary>
    /// 资源被移除时的回调函数
    /// </summary>
    public Func<LruObj, bool> OnRemove { get; set; }

    // 调试标志
    private readonly bool _enableDebugLogging = false;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建指定容量的 LRU 缓存
    /// </summary>
    /// <param name="capacity">缓存容量</param>
    /// <param name="enableDebugLogging">是否启用调试日志</param>
    public LruCache(int capacity, bool enableDebugLogging = false)
    {
        if (capacity <= 0)
            throw new ArgumentException("缓存容量必须大于零", nameof(capacity));

        Capacity = capacity;
        _cacheMap = new Dictionary<TKey, CacheNode>(capacity);
        _enableDebugLogging = enableDebugLogging;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取指定键的缓存项，并将其移至最近使用位置
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>缓存项，如不存在则返回 null</returns>
    public LruObj Get(TKey key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            Hits++;
            LogDebug($"Cache hit for key: {key}");
            MoveToHead(node);
            return node.Value;
        }

        Misses++;
        LogDebug($"Cache miss for key: {key}");
        return default;
    }

    /// <summary>
    /// 将项放入缓存，如存在则更新值并移至最近使用位置
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void Put(TKey key, LruObj value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (_cacheMap.TryGetValue(key, out var existingNode))
        {
            LogDebug($"Updating existing cache entry: {key}");
            existingNode.Value = value;
            MoveToHead(existingNode);
            return;
        }

        LogDebug($"Adding new cache entry: {key}");
        var newNode = new CacheNode(key, value);
        _cacheMap[key] = newNode;
        AddToHead(newNode);

        // 如果超出容量，移除最久未使用的项
        if (Count > Capacity)
        {
            RemoveLeastRecentlyUsed();
        }
    }

    /// <summary>
    /// 检查缓存中是否包含指定键
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>是否包含</returns>
    public bool Contains(TKey key)
    {
        return _cacheMap.ContainsKey(key);
    }

    /// <summary>
    /// 从缓存中移除指定键
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>是否成功移除</returns>
    public bool Remove(TKey key)
    {
        if (!_cacheMap.TryGetValue(key, out var node))
            return false;

        RemoveNode(node);
        _cacheMap.Remove(key);
        return true;
    }

    /// <summary>
    /// 清除所有缓存项
    /// </summary>
    public void Clear()
    {
        LogDebug("Clearing all cache entries");
        _cacheMap.Clear();
        _head = _tail = null;
    }

    /// <summary>
    /// 清理不需要保留在缓存中的资源
    /// </summary>
    public void Clean()
    {
        LogDebug("Cleaning unused resources in cache");
        var nodesToRemove = new List<CacheNode>();
        
        // 收集所有需要清理的节点
        var currentNode = _tail;
        while (currentNode != null)
        {
            var nextNode = currentNode.Prev; // 保存下一个节点，因为当前节点可能会被移除
            
            if (!currentNode.Value.KeepInCache)
            {
                bool canRemove = true;
                if (OnRemove != null)
                {
                    canRemove = OnRemove(currentNode.Value);
                }
                
                if (canRemove)
                {
                    nodesToRemove.Add(currentNode);
                }
                else
                {
                    // 如果不能移除，重新标记为保留
                    currentNode.Value.KeepInCache = true;
                    LogDebug($"Cannot remove node with key {currentNode.Key}, marked to keep in cache");
                }
            }
            
            currentNode = nextNode;
        }
        
        // 移除节点
        foreach (var node in nodesToRemove)
        {
            _cacheMap.Remove(node.Key);
            RemoveNode(node);
            LogDebug($"Removed node with key {node.Key}");
        }
        
        LogDebug($"Cleaned {nodesToRemove.Count} nodes, remaining: {Count}");
    }

    /// <summary>
    /// 强制清理所有缓存项，无论是否标记为保留
    /// </summary>
    public void ForceClean()
    {
        LogDebug("Force cleaning all cache entries");
        
        // 标记所有项为可清理
        foreach (var node in _cacheMap.Values)
        {
            node.Value.KeepInCache = false;
        }
        
        // 清理所有项
        var keysToRemove = new List<TKey>(_cacheMap.Keys);
        foreach (var key in keysToRemove)
        {
            var node = _cacheMap[key];
            
            // 尝试调用回调
            bool canRemove = true;
            if (OnRemove != null)
            {
                canRemove = OnRemove(node.Value);
            }
            
            if (canRemove)
            {
                _cacheMap.Remove(key);
                RemoveNode(node);
            }
        }
        
        LogDebug($"Force cleaned cache, remaining entries: {Count}");
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>包含命中率等信息的字符串</returns>
    public string GetStatistics()
    {
        long totalRequests = Hits + Misses;
        double hitRatio = totalRequests > 0 ? (double)Hits / totalRequests : 0;
        
        return $"Cache Statistics: Size={Count}/{Capacity}, Hits={Hits}, " +
               $"Misses={Misses}, Hit Ratio={hitRatio:P2}";
    }

    /// <summary>
    /// 重置统计数据
    /// </summary>
    public void ResetStatistics()
    {
        Hits = 0;
        Misses = 0;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 将节点移动到链表头部（最近使用）
    /// </summary>
    private void MoveToHead(CacheNode node)
    {
        if (node == _head)
            return;

        RemoveNode(node);
        AddToHead(node);
    }

    /// <summary>
    /// 将节点添加到链表头部
    /// </summary>
    private void AddToHead(CacheNode node)
    {
        node.Next = _head;
        node.Prev = null;

        if (_head != null)
            _head.Prev = node;

        _head = node;

        if (_tail == null)
            _tail = node;
    }

    /// <summary>
    /// 从链表中移除节点
    /// </summary>
    private void RemoveNode(CacheNode node)
    {
        if (node.Prev != null)
            node.Prev.Next = node.Next;
        else
            _head = node.Next;

        if (node.Next != null)
            node.Next.Prev = node.Prev;
        else
            _tail = node.Prev;
    }

    /// <summary>
    /// 移除最久未使用的项（链表尾部）
    /// </summary>
    private void RemoveLeastRecentlyUsed()
    {
        if (_tail == null)
            return;

        var nodeToRemove = _tail;
        bool canRemove = true;

        // 尝试移除，如果不能移除则扩容
        if (OnRemove != null)
        {
            canRemove = OnRemove(nodeToRemove.Value);
        }

        if (canRemove)
        {
            LogDebug($"Removing least recently used entry: {nodeToRemove.Key}");
            _cacheMap.Remove(nodeToRemove.Key);
            RemoveNode(nodeToRemove);
        }
        else
        {
            LogDebug($"Cannot remove entry {nodeToRemove.Key}, increasing capacity");
            Capacity++;
        }
    }

    /// <summary>
    /// 输出调试日志
    /// </summary>
    [Conditional("DEBUG")]
    private void LogDebug(string message)
    {
        if (_enableDebugLogging)
        {
            UnityEngine.Debug.Log($"[LruCache] {message}");
        }
    }

    #endregion
}