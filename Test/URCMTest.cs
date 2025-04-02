using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class URCMTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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
            addedObjects.Add(obj);

            // 每添加10个对象，获取一次第一个对象
            if (i % 10 == 0 && i != 0)
            {
                var retrievedObj = cache.Get(0);
                Debug.Assert(retrievedObj == null, "Key为0的Obj应该已经被移除了！");
            }

            // 每添加20个对象，强制清理一次
            if (i % 20 == 0)
            {
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

    // 测试 URCM 初始化
    public void OnBtnTestURCMInitialization()
    {
        Debug.Log("========== 测试 URCM 初始化 ==========");
        
        // 检查初始状态
        Debug.Log("1. 检查初始化状态");
        bool initializedBefore = URCM.IsInitialized();
        Debug.Log($"URCM是否已初始化: {initializedBefore}");
        
        // 测试设置资源加载器
        Debug.Log("2. 设置资源加载器");
        URCM.SetAssetLoader(new ResourcesAssetOperations());
        
        // 再次检查初始化状态
        bool initializedAfter = URCM.IsInitialized();
        Debug.Log($"设置加载器后URCM是否已初始化: {initializedAfter}");
        
        // 设置缓存容量
        Debug.Log("3. 设置缓存容量");
        URCM.SetCacheCapacity(50);
        
        // 设置日志
        Debug.Log("4. 测试日志开关");
        URCM.EnableLogging(false);
        URCM.Load<Texture2D>("ShouldNotLogThis");
        URCM.EnableLogging(true);
        URCM.Load<Texture2D>("ShouldLogThis");
        
        Debug.Log("========== URCM 初始化测试完成 ==========");
    }

    // 测试资源加载器
    public void OnBtnTestAssetLoader()
    {
        Debug.Log("========== 测试不同资源加载器 ==========");
        
        // 清理当前状态
        URCM.ReleaseAll();
        
        // 使用Resources加载器
        Debug.Log("1. 使用Resources加载器");
        URCM.SetAssetLoader(new ResourcesAssetOperations());
        TestAssetLoader("Resources加载器");
        
        // 使用自定义加载器
        Debug.Log("2. 使用自定义加载器");
        URCM.SetAssetLoader(new MockAssetOperations());
        TestAssetLoader("自定义加载器");
        
        Debug.Log("========== 资源加载器测试完成 ==========");
    }
    
    private void TestAssetLoader(string loaderName)
    {
        Debug.Log($"测试 {loaderName} 同步加载");
        Texture2D texture = URCM.Load<Texture2D>("TestTexture");
        Debug.Log($"加载结果: {(texture != null ? "成功" : "失败")}");
        
        Debug.Log($"测试 {loaderName} 引用计数");
        int refCount = URCM.GetRefCount("TestTexture");
        Debug.Log($"引用计数: {refCount}");
        
        Debug.Log($"测试 {loaderName} 释放资源");
        URCM.Release("TestTexture");
        refCount = URCM.GetRefCount("TestTexture");
        Debug.Log($"释放后引用计数: {refCount}");
    }

    // 测试 URCM 的所有功能
    public void OnBtnTestURCM()
    {
        // 确保测试前已设置资源加载器
        if (!URCM.IsInitialized())
        {
            URCM.SetAssetLoader(new ResourcesAssetOperations());
        }
        
        StartCoroutine(TestURCMCoroutine());
    }

    private IEnumerator TestURCMCoroutine()
    {
        Debug.Log("========== 开始测试 URCM 功能 ==========");
        
        // 设置缓存容量
        Debug.Log("1. 测试设置缓存容量");
        URCM.SetCacheCapacity(20);
        
        // 假设你的项目中有这些资源可用于测试
        string texturePath = "Textures/TestTexture";
        string prefabPath = "Prefabs/TestPrefab";
        string materialPath = "Materials/TestMaterial";
        
        // 测试同步加载资源
        Debug.Log("2. 测试同步加载资源");
        Texture2D texture = URCM.Load<Texture2D>(texturePath);
        Debug.Log($"加载纹理结果: {(texture != null ? "成功" : "失败")}");
        
        if (texture != null)
        {
            // 测试引用计数增加
            Debug.Log("3. 测试引用计数增加");
            int count = URCM.GetRefCount(texturePath);
            Debug.Log($"纹理引用计数: {count}");
            Debug.Assert(count > 0, "引用计数应该大于0");
            
            // 测试引用计数再次增加
            URCM.AddRef(texturePath);
            int newCount = URCM.GetRefCount(texturePath);
            Debug.Log($"增加引用后的计数: {newCount}");
            Debug.Assert(newCount == count + 1, "引用计数应该增加1");
            
            // 测试引用计数减少
            Debug.Log("4. 测试引用计数减少");
            URCM.RemoveRef(texturePath);
            int decreasedCount = URCM.GetRefCount(texturePath);
            Debug.Log($"减少引用后的计数: {decreasedCount}");
            Debug.Assert(decreasedCount == count, "引用计数应该减少1");
        }
        
        // 测试缓存命中
        Debug.Log("5. 测试缓存命中");
        Texture2D cachedTexture = URCM.Load<Texture2D>(texturePath);
        Debug.Log($"从缓存加载纹理: {(cachedTexture != null ? "成功" : "失败")}");
        Debug.Assert(cachedTexture == texture, "应该返回相同的纹理实例");
        
        // 测试异步加载资源
        Debug.Log("6. 测试异步加载资源");
        Task<GameObject> prefabTask = URCM.LoadAsync<GameObject>(prefabPath);
        
        // 等待异步加载完成
        while (!prefabTask.IsCompleted)
        {
            yield return null;
        }
        
        GameObject prefab = prefabTask.Result;
        Debug.Log($"异步加载预制体结果: {(prefab != null ? "成功" : "失败")}");
        
        if (prefab != null)
        {
            // 测试实例化预制体
            Debug.Log("7. 测试实例化预制体");
            GameObject standardInstance = Instantiate(prefab);
            Debug.Log($"标准实例化: {(standardInstance != null ? "成功" : "失败")}");
            Destroy(standardInstance, 2f);
            
            // 测试URCM实例化方法
            GameObject urcmInstance = URCM.Instantiate(prefabPath);
            Debug.Log($"URCM实例化: {(urcmInstance != null ? "成功" : "失败")}");
            Destroy(urcmInstance, 2f);
        }
        
        // 测试批量加载资源
        Debug.Log("8. 测试批量加载资源");
        string[] texturePaths = new string[] { "Textures/Test1", "Textures/Test2", texturePath };
        var multipleResult = URCM.LoadMultiple<Texture2D>(texturePaths);
        Debug.Log($"批量加载结果: 成功加载 {multipleResult.Count} 个资源");
        
        // 测试异步批量加载
        Debug.Log("9. 测试异步批量加载");
        var multipleTask = URCM.LoadMultipleAsync<Texture2D>(texturePaths);
        yield return new WaitUntil(() => multipleTask.IsCompleted);
        var asyncMultipleResult = multipleTask.Result;
        Debug.Log($"异步批量加载结果: 成功加载 {asyncMultipleResult.Count} 个资源");
        
        // 测试同时加载多个资源
        Debug.Log("10. 测试同时加载多个不同类型资源");
        Task<Material> materialTask = URCM.LoadAsync<Material>(materialPath);
        
        // 等待异步加载完成
        while (!materialTask.IsCompleted)
        {
            yield return null;
        }
        
        Material material = materialTask.Result;
        Debug.Log($"异步加载材质结果: {(material != null ? "成功" : "失败")}");
        
        // 测试释放单个资源
        Debug.Log("11. 测试释放单个资源");
        if (material != null)
        {
            URCM.Release(materialPath);
            Debug.Log($"材质释放后的引用计数: {URCM.GetRefCount(materialPath)}");
        }
        
        // 测试批量释放资源
        Debug.Log("12. 测试批量释放资源");
        URCM.ReleaseMultiple(texturePaths);
        foreach (var path in texturePaths)
        {
            Debug.Log($"{path} 引用计数: {URCM.GetRefCount(path)}");
        }
        
        // 测试清理未使用的资源
        Debug.Log("13. 测试清理未使用的资源");
        URCM.CleanUnused();
        yield return new WaitForSeconds(0.5f);
        
        // 获取缓存统计
        Debug.Log("14. 获取缓存统计");
        Debug.Log(URCM.GetCacheStats());
        
        // 重置缓存统计
        Debug.Log("15. 重置缓存统计");
        URCM.ResetCacheStats();
        Debug.Log(URCM.GetCacheStats());
        
        // 验证资源状态
        Debug.Log("16. 验证资源状态");
        Debug.Log($"纹理引用计数: {URCM.GetRefCount(texturePath)}");
        Debug.Log($"预制体引用计数: {URCM.GetRefCount(prefabPath)}");
        Debug.Log($"材质引用计数: {URCM.GetRefCount(materialPath)}");
        
        // 测试强制释放所有资源
        Debug.Log("17. 测试强制释放所有资源");
        URCM.ReleaseAll();
        yield return new WaitForSeconds(0.5f);
        
        // 验证所有资源都已释放
        Debug.Log("18. 验证所有资源都已释放");
        Debug.Log($"纹理引用计数: {URCM.GetRefCount(texturePath)}");
        Debug.Log($"预制体引用计数: {URCM.GetRefCount(prefabPath)}");
        Debug.Log($"材质引用计数: {URCM.GetRefCount(materialPath)}");
        
        // 测试重新加载资源
        Debug.Log("19. 测试重新加载资源");
        Texture2D reloadedTexture = URCM.Load<Texture2D>(texturePath);
        Debug.Log($"重新加载纹理结果: {(reloadedTexture != null ? "成功" : "失败")}");
        
        Debug.Log("========== URCM 测试完成 ==========");
    }

    // 测试 URCM 错误处理
    public void OnBtnTestURCMErrors()
    {
        Debug.Log("========== 开始测试 URCM 错误处理 ==========");
        
        // 测试未初始化时的行为
        Debug.Log("1. 测试未初始化时的行为");
        // 保存当前加载器
        var currentLoader = new MockAssetOperations(); // 假设这里能获取当前加载器
        
        // 设置空加载器
        URCM.SetAssetLoader(null);
        
        // 尝试加载
        Texture2D nullLoaderTexture = URCM.Load<Texture2D>("TestTexture");
        Debug.Log($"未初始化时加载结果: {(nullLoaderTexture != null ? "成功" : "失败")}");
        
        // 恢复加载器
        URCM.SetAssetLoader(new ResourcesAssetOperations());
        
        // 测试加载不存在的资源
        Debug.Log("2. 测试加载不存在的资源");
        Texture2D nonExistentTexture = URCM.Load<Texture2D>("NonExistent/Texture");
        Debug.Log($"加载不存在纹理结果: {(nonExistentTexture != null ? "成功" : "失败")}");
        Debug.Assert(nonExistentTexture == null, "不存在的资源应该返回null");
        
        // 测试减少不存在资源的引用计数
        Debug.Log("3. 测试减少不存在资源的引用计数");
        int count = URCM.RemoveRef("NonExistent/Resource");
        Debug.Log($"不存在资源的引用计数: {count}");
        Debug.Assert(count == 0, "不存在资源的引用计数应该为0");
        
        // 测试设置无效的缓存容量
        Debug.Log("4. 测试设置无效的缓存容量");
        URCM.SetCacheCapacity(-1);
        URCM.SetCacheCapacity(0);
        
        // 测试释放未加载的资源
        Debug.Log("5. 测试释放未加载的资源");
        URCM.Release("NonExistent/Resource");
        
        // 测试禁用和启用日志
        Debug.Log("6. 测试日志控制");
        URCM.EnableLogging(false);
        URCM.Load<Texture2D>("TestLogDisabled/Texture"); // 应该没有日志输出
        URCM.EnableLogging(true);
        URCM.Load<Texture2D>("TestLogEnabled/Texture"); // 应该有日志输出
        
        Debug.Log("========== URCM 错误处理测试完成 ==========");
    }

    // 性能测试
    public void OnBtnTestURCMPerformance()
    {
        // 确保测试前已设置资源加载器
        if (!URCM.IsInitialized())
        {
            URCM.SetAssetLoader(new ResourcesAssetOperations());
        }
        
        StartCoroutine(TestURCMPerformanceCoroutine());
    }

    private IEnumerator TestURCMPerformanceCoroutine()
    {
        Debug.Log("========== 开始 URCM 性能测试 ==========");
        
        // 设置较大的缓存容量用于性能测试
        URCM.SetCacheCapacity(50);
        URCM.ResetCacheStats();
        
        // 假设有一系列测试资源
        string[] texturePaths = new string[] 
        {
            "Textures/Test1", "Textures/Test2", "Textures/Test3", 
            "Textures/Test4", "Textures/Test5", "Textures/Test6", 
            "Textures/Test7", "Textures/Test8", "Textures/Test9", "Textures/Test10"
        };
        
        // 批量加载资源计时
        Debug.Log("1. 测试批量加载资源性能");
        float startTime = Time.realtimeSinceStartup;
        
        var batchLoadTask = URCM.LoadMultipleAsync<Texture2D>(texturePaths);
        
        // 等待所有任务完成
        yield return new WaitUntil(() => batchLoadTask.IsCompleted);
        
        float loadTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"批量加载10个资源耗时: {loadTime:F4}秒");
        
        // 测试缓存命中性能
        Debug.Log("2. 测试缓存命中性能");
        startTime = Time.realtimeSinceStartup;
        
        for (int i = 0; i < 100; i++)
        {
            foreach (string path in texturePaths)
            {
                Texture2D tex = URCM.Load<Texture2D>(path);
            }
        }
        
        float cacheHitTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"从缓存中获取1000次资源耗时: {cacheHitTime:F4}秒");
        
        // 测试引用计数操作性能
        Debug.Log("3. 测试引用计数操作性能");
        startTime = Time.realtimeSinceStartup;
        
        for (int i = 0; i < 1000; i++)
        {
            string path = texturePaths[i % texturePaths.Length];
            URCM.AddRef(path);
            URCM.RemoveRef(path);
        }
        
        float refCountTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"2000次引用计数操作耗时: {refCountTime:F4}秒");
        
        // 测试预制体实例化性能
        Debug.Log("4. 测试预制体实例化性能");
        string prefabPath = "Prefabs/TestPrefab";
        GameObject prefab = URCM.Load<GameObject>(prefabPath);
        
        if (prefab != null)
        {
            startTime = Time.realtimeSinceStartup;
            List<GameObject> instances = new List<GameObject>();
            
            for (int i = 0; i < 50; i++)
            {
                instances.Add(URCM.Instantiate(prefabPath));
            }
            
            float instantiateTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"实例化50个预制体耗时: {instantiateTime:F4}秒");
            
            // 销毁所有实例
            foreach (var instance in instances)
            {
                Destroy(instance);
            }
        }
        
        // 测试释放资源性能
        Debug.Log("5. 测试释放资源性能");
        startTime = Time.realtimeSinceStartup;
        
        URCM.ReleaseAll();
        
        float releaseTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"释放所有资源耗时: {releaseTime:F4}秒");
        
        // 输出缓存统计
        Debug.Log("6. 缓存统计");
        Debug.Log(URCM.GetCacheStats());
        
        Debug.Log("========== URCM 性能测试完成 ==========");
        
        yield return null;
    }

    public void OnBtnTestDependency()
    {
        // 创建DependencyTest实例
        GameObject testObj = new GameObject("DependencyTestObject");
        DependencyTest dependencyTest = testObj.AddComponent<DependencyTest>();
        
        // 调用DependencyTest中的测试方法
        dependencyTest.Test();
        
        // 测试完成后销毁测试对象
        Destroy(testObj, 1f); // 延迟1秒销毁，确保日志输出完成
    }
}
