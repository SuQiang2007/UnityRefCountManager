using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using NUnit.Framework; // 移除 NUnit 测试框架
// using UnityEngine.Assertions; // 移除 Assertions

public class URCMTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestLruCache(); // 在 Start 方法中调用测试
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 测试 LruCache 的功能
    public void TestLruCache()
    {
        var cache = new LruCache<int>(3); // 设置容量为 3

        // 测试添加元素
        cache.Put(1, new LruObj { Guid = "1" });
        Debug.Log("Count after adding 1: " + cache.Count); // 应该是 1
        Debug.Assert(cache.Count == 1, "Count should be 1");

        // 测试获取元素
        var obj = cache.Get(1);
        Debug.Assert(obj != null, "Object should not be null");
        Debug.Assert(obj.Guid == "1", "Object Guid should be 1");

        // 测试超出容量时的行为
        cache.Put(2, new LruObj { Guid = "2" });
        cache.Put(3, new LruObj { Guid = "3" });
        Debug.Log("Count after adding 2 and 3: " + cache.Count); // 应该是 3
        Debug.Assert(cache.Count == 3, "Count should be 3");

        // 测试 LRU 行为
        cache.Put(4, new LruObj { Guid = "4" });
        Debug.Log("Count after adding 4: " + cache.Count); // 应该是 3
        Debug.Assert(cache.Count == 3, "Count should still be 3");
        Debug.Assert(cache.Get(1) == null, "Object 1 should have been removed");

        // 测试强制清理
        cache.ForceClean();
        Debug.Log("Count after force cleaning: " + cache.Count); // 应该是 0
        Debug.Assert(cache.Count == 0, "Count should be 0");
    }
}
