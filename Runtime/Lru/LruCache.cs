using System;
using System.Collections.Generic;
using UnityEngine;

public class LruCache<TKey>
{
    public class DoublyLinkedNode
    {
        public TKey Key;
        public LruObj Value;
        public DoublyLinkedNode Prev;
        public DoublyLinkedNode Next;

        public DoublyLinkedNode(TKey key, LruObj value)
        {
            Key = key;
            Value = value;
        }
    }

    public int Capacity{get; private set;}
    public int Count => cacheMap.Count;
    public readonly Dictionary<TKey, DoublyLinkedNode> cacheMap;
    private readonly LinkedList<DoublyLinkedNode> linkedList = new LinkedList<DoublyLinkedNode>();
    
    public Func<LruObj, bool> onRemove { get; set; }

    public LruCache(int capacity)
    {
        this.Capacity = capacity > 0 ? capacity : throw new System.ArgumentException("Capacity must be greater than zero", nameof(capacity));
        this.cacheMap = new Dictionary<TKey, DoublyLinkedNode>();
    }

    public LruObj Get(TKey key)
    {
        if (cacheMap.TryGetValue(key, out var node))
        {
            MoveToHead(node);
            return node.Value;
        }
        return default;
    }

    public void Put(TKey key, LruObj value)
    {
        if (cacheMap.TryGetValue(key, out var node))
        {
            node.Value = value;
            MoveToHead(node);
        }
        else
        {
            var newNode = new DoublyLinkedNode(key, value);
            cacheMap[key] = newNode;
            AddNode(newNode);

            if (Count > Capacity)
            {
                var tryNode = PeekTail();
                Debug.Log($"Lru尝试弹出 --> {tryNode?.Key.ToString()}");
                if (tryNode == null || tryNode.Value.Guid == value.Guid)
                {
                    URCM.LogWarning($"LruCache过小({Capacity})，自动扩容1，为了{key}");
                    Capacity++;
                }
                else
                {
                    if (onRemove?.Invoke(tryNode.Value) == false)
                    {
                        URCM.LogWarning($"LruCache遭遇无法销毁的节点{key}，当前容量为({Capacity})，自动扩容1");
                        Capacity++;
                    }
                    else
                    {
                        Debug.Log($"Lru尝试弹出成功 --> {tryNode?.Key.ToString()}");
                        var tailNode = PopTail();
                        cacheMap.Remove(tailNode.Key);
                    }
                }
            }
        }

        CheckConsistency();
    }
    
    public void ClearAll()
    {
        linkedList.Clear();
        cacheMap.Clear();
    }

    /// <summary>
    /// 强制
    /// </summary>
    public void ForceClean()
    {
        // 创建一个列表来存储要移除的键
        List<TKey> keysToRemove = new List<TKey>();

        // 遍历 cacheMap，设置 KeepInCache 为 false，并记录要移除的键
        foreach (KeyValuePair<TKey, DoublyLinkedNode> keyValuePair in cacheMap)
        {
            keyValuePair.Value.Value.KeepInCache = false;
            keysToRemove.Add(keyValuePair.Key);
        }

        // 移除所有记录的键
        foreach (var key in keysToRemove)
        {
            cacheMap.Remove(key);
        }

        // 清空链表
        linkedList.Clear();
    }
    






    
    private void AddNode(DoublyLinkedNode node)
    {
        URCM.Log($"AddNode: Adding node with key: {node.Key}");
        linkedList.AddFirst(node);
        CheckConsistency();
    }

    private void RemoveNode(DoublyLinkedNode node)
    {
        if (node == null) return;
        linkedList.Remove(node);
        CheckConsistency();
    }

    private void MoveToHead(DoublyLinkedNode node)
    {
        URCM.Log($"MoveToHead: Moving node to head with key: {node.Key}");
        RemoveNode(node);
        AddNode(node);
    }

    private DoublyLinkedNode PopTail()
    {
        if (linkedList.Count == 0) return null;
        var node = linkedList.Last.Value;
        linkedList.RemoveLast();
        return node;
    }

    // 新方法 PeekTail
    public DoublyLinkedNode PeekTail()
    {
        var node = linkedList.Last;
        while (node != null)
        {
            var lru = node.Value;
            if (!lru.Value.KeepInCache)
            {
                // 找到即将被 pop 的节点, 返回该节点, 但不移除它
                return node.Value;
            }
            node = node.Previous;
        }
        // 没有找到合适的节点返回 null
        return null;
    }

    private void CheckConsistency()
    {
        try
        {
            // Verify head <-> nodes <-> tail connections
            DoublyLinkedNode current = null;
            foreach (var node in linkedList)
            {
                current = node;
                if (current.Prev != null)
                {
                    URCM.Log($"Inconsistency found: Node with key {current.Key} has incorrect Prev reference.");
                    return;
                }
            }

            if (current != null && current.Next != null)
            {
                URCM.Log("Inconsistency found: Tail node not correctly connected.");
                return;
            }

            // Verify tail <-> nodes <-> head connections
            current = null;
            foreach (var node in linkedList)
            {
                current = node;
                if (current.Next != null)
                {
                    URCM.Log($"Inconsistency found: Node with key {current.Key} has incorrect Next reference.");
                    return;
                }
            }

            if (current != null && current.Prev != null)
            {
                URCM.Log("Inconsistency found: Head node not correctly connected.");
                return;
            }

            URCM.Log("Consistency check passed.");
        }
        catch (Exception ex)
        {
            URCM.Log($"Exception during consistency check: {ex.Message}");
        }
    }
}