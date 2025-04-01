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
        
    }

    public void OnBtnPopTest()
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

    public void OnBtnPressureTest()
    {
        var cache = new LruCache<int>(10); // 设置容量为 10
        int totalItems = 100; // 总共要添加的对象数量
        List<LruObj> addedObjects = new List<LruObj>();

        // 添加对象
        for (int i = 0; i < totalItems; i++)
        {
            var obj = new LruObj { Guid = i.ToString() };
            cache.Put(i, obj);
            Debug.Log($"Put {JsonUtility.ToJson(obj)}");
            addedObjects.Add(obj);

            // 每添加10个对象，获取一次第一个对象
            if (i % 10 == 0 && i != 0)
            {
                Debug.Log($"=============={i}");
                var retrievedObj = cache.Get(0);
                Debug.Assert(retrievedObj == null, "Key为0的Obj应该已经被移除了！");
            }

            // 每添加20个对象，强制清理一次
            if (i % 20 == 0)
            {
                Debug.Log($"========================================================{i}");
                cache.ForceClean();
                Debug.Log($"Count after force cleaning: {cache.Count}"); // 应该是 0
                Debug.Assert(cache.Count == 0, "Count should be 0 after force cleaning");
            }
        }

        // 确保缓存的行为符合预期
        cache.ForceClean();
        Debug.Log($"Final Count: {cache.Count}"); // 应该是 0
        Debug.Assert(cache.Count == 0, "Final Count should be 0");
    }
}
