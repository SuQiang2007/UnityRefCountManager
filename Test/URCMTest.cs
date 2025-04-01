using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
// using NUnit.Framework; // 移除 NUnit 测试框架
// using UnityEngine.Assertions; // 移除 Assertions

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

    // 添加此方法来测试 URCM 的所有功能
    public void OnBtnTestURCM()
    {
        StartCoroutine(TestURCMCoroutine());
    }

    private IEnumerator TestURCMCoroutine()
    {
        Debug.Log("========== 开始测试 URCM 功能 ==========");
        
        // 测试初始化
        Debug.Log("1. 测试初始化和单例");
        Debug.Assert(URCM.Instance != null, "URCM 实例应该存在");
        
        // 设置缓存容量
        Debug.Log("2. 测试设置缓存容量");
        URCM.Instance.SetCacheCapacity(20);
        
        // 假设你的项目中有这些资源可用于测试
        string texturePath = "Textures/TestTexture";
        string prefabPath = "Prefabs/TestPrefab";
        string materialPath = "Materials/TestMaterial";
        
        // 测试同步加载资源
        Debug.Log("3. 测试同步加载资源");
        Texture2D texture = URCM.Instance.LoadAsset<Texture2D>(texturePath);
        Debug.Log($"加载纹理结果: {(texture != null ? "成功" : "失败")}");
        
        if (texture != null)
        {
            // 测试引用计数增加
            Debug.Log("4. 测试引用计数增加");
            int count = URCM.Instance.GetReferenceCount(texturePath);
            Debug.Log($"纹理引用计数: {count}");
            Debug.Assert(count > 0, "引用计数应该大于0");
            
            // 测试引用计数再次增加
            URCM.Instance.AddReference(texturePath);
            int newCount = URCM.Instance.GetReferenceCount(texturePath);
            Debug.Log($"增加引用后的计数: {newCount}");
            Debug.Assert(newCount == count + 1, "引用计数应该增加1");
            
            // 测试引用计数减少
            Debug.Log("5. 测试引用计数减少");
            URCM.Instance.RemoveReference(texturePath);
            int decreasedCount = URCM.Instance.GetReferenceCount(texturePath);
            Debug.Log($"减少引用后的计数: {decreasedCount}");
            Debug.Assert(decreasedCount == count, "引用计数应该减少1");
        }
        
        // 测试缓存命中
        Debug.Log("6. 测试缓存命中");
        Texture2D cachedTexture = URCM.Instance.LoadAsset<Texture2D>(texturePath);
        Debug.Log($"从缓存加载纹理: {(cachedTexture != null ? "成功" : "失败")}");
        Debug.Assert(cachedTexture == texture, "应该返回相同的纹理实例");
        
        // 测试异步加载资源
        Debug.Log("7. 测试异步加载资源");
        Task<GameObject> prefabTask = URCM.Instance.LoadAssetAsync<GameObject>(prefabPath);
        
        // 等待异步加载完成
        while (!prefabTask.IsCompleted)
        {
            yield return null;
        }
        
        GameObject prefab = prefabTask.Result;
        Debug.Log($"异步加载预制体结果: {(prefab != null ? "成功" : "失败")}");
        
        if (prefab != null)
        {
            // 测试实例化加载的预制体
            Debug.Log("8. 测试实例化加载的预制体");
            GameObject instance = Instantiate(prefab);
            Debug.Log($"预制体实例化: {(instance != null ? "成功" : "失败")}");
            
            // 延迟销毁实例
            Destroy(instance, 2f);
        }
        
        // 测试同时加载多个资源
        Debug.Log("9. 测试同时加载多个资源");
        Task<Material> materialTask = URCM.Instance.LoadAssetAsync<Material>(materialPath);
        
        // 等待异步加载完成
        while (!materialTask.IsCompleted)
        {
            yield return null;
        }
        
        Material material = materialTask.Result;
        Debug.Log($"异步加载材质结果: {(material != null ? "成功" : "失败")}");
        
        // 测试释放单个资源
        Debug.Log("10. 测试释放单个资源");
        if (material != null)
        {
            URCM.Instance.ReleaseAsset(materialPath);
            Debug.Log($"材质释放后的引用计数: {URCM.Instance.GetReferenceCount(materialPath)}");
        }
        
        // 测试清理未使用的资源
        Debug.Log("11. 测试清理未使用的资源");
        URCM.Instance.CleanUnusedResources();
        yield return new WaitForSeconds(0.5f);
        
        // 验证资源状态
        Debug.Log("12. 验证资源状态");
        Debug.Log($"纹理引用计数: {URCM.Instance.GetReferenceCount(texturePath)}");
        Debug.Log($"预制体引用计数: {URCM.Instance.GetReferenceCount(prefabPath)}");
        Debug.Log($"材质引用计数: {URCM.Instance.GetReferenceCount(materialPath)}");
        
        // 测试强制释放所有资源
        Debug.Log("13. 测试强制释放所有资源");
        URCM.Instance.ReleaseAllResources();
        yield return new WaitForSeconds(0.5f);
        
        // 验证所有资源都已释放
        Debug.Log("14. 验证所有资源都已释放");
        Debug.Log($"纹理引用计数: {URCM.Instance.GetReferenceCount(texturePath)}");
        Debug.Log($"预制体引用计数: {URCM.Instance.GetReferenceCount(prefabPath)}");
        Debug.Log($"材质引用计数: {URCM.Instance.GetReferenceCount(materialPath)}");
        
        // 测试重新加载资源
        Debug.Log("15. 测试重新加载资源");
        Texture2D reloadedTexture = URCM.Instance.LoadAsset<Texture2D>(texturePath);
        Debug.Log($"重新加载纹理结果: {(reloadedTexture != null ? "成功" : "失败")}");
        
        Debug.Log("========== URCM 测试完成 ==========");
    }

    // 添加错误处理测试方法
    public void OnBtnTestURCMErrors()
    {
        Debug.Log("========== 开始测试 URCM 错误处理 ==========");
        
        // 测试加载不存在的资源
        Debug.Log("1. 测试加载不存在的资源");
        Texture2D nonExistentTexture = URCM.Instance.LoadAsset<Texture2D>("NonExistent/Texture");
        Debug.Log($"加载不存在纹理结果: {(nonExistentTexture != null ? "成功" : "失败")}");
        Debug.Assert(nonExistentTexture == null, "不存在的资源应该返回null");
        
        // 测试减少不存在资源的引用计数
        Debug.Log("2. 测试减少不存在资源的引用计数");
        int count = URCM.Instance.RemoveReference("NonExistent/Resource");
        Debug.Log($"不存在资源的引用计数: {count}");
        Debug.Assert(count == 0, "不存在资源的引用计数应该为0");
        
        // 测试设置无效的缓存容量
        Debug.Log("3. 测试设置无效的缓存容量");
        URCM.Instance.SetCacheCapacity(-1);
        URCM.Instance.SetCacheCapacity(0);
        
        // 测试释放未加载的资源
        Debug.Log("4. 测试释放未加载的资源");
        URCM.Instance.ReleaseAsset("NonExistent/Resource");
        
        Debug.Log("========== URCM 错误处理测试完成 ==========");
    }

    // 添加性能测试方法
    public void OnBtnTestURCMPerformance()
    {
        StartCoroutine(TestURCMPerformanceCoroutine());
    }

    private IEnumerator TestURCMPerformanceCoroutine()
    {
        Debug.Log("========== 开始 URCM 性能测试 ==========");
        
        // 设置较大的缓存容量用于性能测试
        URCM.Instance.SetCacheCapacity(50);
        
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
        
        List<Task<Texture2D>> loadTasks = new List<Task<Texture2D>>();
        foreach (string path in texturePaths)
        {
            loadTasks.Add(URCM.Instance.LoadAssetAsync<Texture2D>(path));
        }
        
        // 等待所有任务完成
        while (loadTasks.Any(t => !t.IsCompleted))
        {
            yield return null;
        }
        
        float loadTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"批量加载10个资源耗时: {loadTime:F4}秒");
        
        // 测试缓存命中性能
        Debug.Log("2. 测试缓存命中性能");
        startTime = Time.realtimeSinceStartup;
        
        for (int i = 0; i < 100; i++)
        {
            foreach (string path in texturePaths)
            {
                Texture2D tex = URCM.Instance.LoadAsset<Texture2D>(path);
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
            URCM.Instance.AddReference(path);
            URCM.Instance.RemoveReference(path);
        }
        
        float refCountTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"2000次引用计数操作耗时: {refCountTime:F4}秒");
        
        // 测试释放资源性能
        Debug.Log("4. 测试释放资源性能");
        startTime = Time.realtimeSinceStartup;
        
        URCM.Instance.ReleaseAllResources();
        
        float releaseTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"释放所有资源耗时: {releaseTime:F4}秒");
        
        Debug.Log("========== URCM 性能测试完成 ==========");
        
        yield return null;
    }
}
