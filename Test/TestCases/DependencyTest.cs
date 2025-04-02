using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class DependencyTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void Test()
    {
        Debug.Log("========== 测试资源依赖关系 ==========");

        // 确保测试前已设置资源加载器
        if (!URCM.IsInitialized())
        {
            URCM.SetAssetLoader(new MockAssetOperations());
        }

        // 清理所有现有资源
        URCM.ReleaseAll();
        
        // 确保所有资源初始引用计数为0
        string[] allPaths = { 
            "Textures/ResA", "Textures/ResB", "Textures/ResC", 
            "Textures/ResD", "Textures/ResE", "Textures/ResF" 
        };
        foreach (var path in allPaths)
        {
            Debug.Assert(URCM.GetRefCount(path) == 0, $"初始状态下 {path} 的引用计数应为0");
        }

        // 创建一个复杂的依赖关系图
        // A -> B -> D
        // A -> C -> D
        // A -> E
        // B -> F
        // C -> F
        // E -> F -> D -> A
        // 这样D和F都被多个资源依赖

        // 加载基础资源D和F (被依赖最多的资源)
        Debug.Log("1. 加载基础资源D和F");
        var resD = URCM.Load<Texture2D>("Textures/ResD");
        var resF = URCM.Load<Texture2D>("Textures/ResF");
        
        // 验证D和F的引用计数为1
        Debug.Assert(URCM.GetRefCount("Textures/ResD") == 1, "D的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResF") == 1, "F的引用计数应为1");
        
        // 手动添加F对D的依赖 (不通过URCM，直接修改LruObj)
        var cacheF = URCM.GetLruObj("Textures/ResF");
        var cacheD = URCM.GetLruObj("Textures/ResD");
        if (cacheF != null && cacheD != null)
        {
            cacheF.ChildReses.Add(cacheD);
            Debug.Log("添加依赖: F -> D");
            // 依赖关系建立后，D的引用计数不应变化（仅通过对象引用，不增加引用计数）
            int dRefCount = URCM.GetRefCount("Textures/ResD");
            Debug.Assert(dRefCount == 1, $"添加依赖后D的引用计数应仍为1，现在为{dRefCount}");
        }

        // 加载中间层资源B、C和E，并设置依赖关系
        Debug.Log("2. 加载中间层资源B、C和E");
        var resB = URCM.Load<Texture2D>("Textures/ResB");
        var resC = URCM.Load<Texture2D>("Textures/ResC");
        var resE = URCM.Load<Texture2D>("Textures/ResE");
        
        // 验证B、C、E的引用计数为1
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 1, "B的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResC") == 1, "C的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResE") == 1, "E的引用计数应为1");
        
        // 设置B的依赖
        var cacheB = URCM.GetLruObj("Textures/ResB");
        if (cacheB != null)
        {
            cacheB.ChildReses.Add(cacheD);
            cacheB.ChildReses.Add(cacheF);
            Debug.Log("添加依赖: B -> D, B -> F");
        }
        
        // 设置C的依赖
        var cacheC = URCM.GetLruObj("Textures/ResC");
        if (cacheC != null)
        {
            cacheC.ChildReses.Add(cacheD);
            cacheC.ChildReses.Add(cacheF);
            Debug.Log("添加依赖: C -> D, C -> F");
        }
        
        // 设置E的依赖
        var cacheE = URCM.GetLruObj("Textures/ResE");
        if (cacheE != null)
        {
            cacheE.ChildReses.Add(cacheF);
            Debug.Log("添加依赖: E -> F");
        }

        // 最后加载顶层资源A，并设置依赖关系
        Debug.Log("3. 加载顶层资源A");
        var resA = URCM.Load<Texture2D>("Textures/ResA");
        
        // 验证A的引用计数为1
        Debug.Assert(URCM.GetRefCount("Textures/ResA") == 1, "A的引用计数应为1");
        
        var cacheA = URCM.GetLruObj("Textures/ResA");
        if (cacheA != null)
        {
            cacheA.ChildReses.Add(cacheB);
            cacheA.ChildReses.Add(cacheC);
            cacheA.ChildReses.Add(cacheE);
            Debug.Log("添加依赖: A -> B, A -> C, A -> E");
        }

        // 添加D对A的依赖，形成循环依赖
        Debug.Log("3.1 添加D对A的依赖，形成循环：E -> F -> D -> A -> ...");
        if (cacheD != null && cacheA != null) 
        {
            cacheD.ChildReses.Add(cacheA);
            Debug.Log("添加依赖: D -> A（形成循环依赖）");
        }

        // 打印复杂的依赖关系图
        Debug.Log("完整的依赖关系图现在是:");
        Debug.Log("A -> B -> D -> A (循环)");
        Debug.Log("A -> C -> D -> A (循环)");
        Debug.Log("A -> E -> F -> D -> A (循环)");

        // 打印当前依赖状态
        Debug.Log("4. 依赖关系建立完成，当前引用计数:");
        PrintRefCounts(new string[] { 
            "Textures/ResA", "Textures/ResB", "Textures/ResC", 
            "Textures/ResD", "Textures/ResE", "Textures/ResF" 
        });
        
        // 验证所有资源的引用计数仍然为1（因为LruObj.ChildReses不增加引用计数）
        Debug.Assert(URCM.GetRefCount("Textures/ResA") == 1, "依赖关系建立后A的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 1, "依赖关系建立后B的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResC") == 1, "依赖关系建立后C的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResD") == 1, "依赖关系建立后D的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResE") == 1, "依赖关系建立后E的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResF") == 1, "依赖关系建立后F的引用计数应为1");

        // 测试场景1: 释放A，观察B、C、E是否被释放，而D、F因为引用共享是否保留
        Debug.Log("5. 测试场景1: 释放A");
        URCM.Release("Textures/ResA");
        Debug.Log("释放A后的引用计数:");
        PrintRefCounts(new string[] { 
            "Textures/ResA", "Textures/ResB", "Textures/ResC", 
            "Textures/ResD", "Textures/ResE", "Textures/ResF" 
        });
        
        // 验证释放A后引用计数的变化
        Debug.Assert(URCM.GetRefCount("Textures/ResA") == 0, "释放后A的引用计数应为0");

        // 由于现在存在循环依赖，资源管理器应该能够妥善处理，这些资源应该都被释放
        // 但取决于URCM的实现机制，可能会有不同的处理方式
        Debug.Log("注意：由于存在循环依赖 E->F->D->A，资源处理逻辑可能有所不同");
        // 检查B、C、E是否被释放
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 0, "A释放后B的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResC") == 0, "A释放后C的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResE") == 0, "A释放后E的引用计数应为0");

        // D和F因为循环依赖到A，如果循环依赖被正确处理，它们也应该被释放
        Debug.Assert(URCM.GetRefCount("Textures/ResD") == 0, "循环依赖正确处理后，释放A应导致D被释放");
        Debug.Assert(URCM.GetRefCount("Textures/ResF") == 0, "循环依赖正确处理后，释放A应导致F被释放");

        // 测试场景2: 释放中间层资源B，观察D和F的引用计数变化
        Debug.Log("6. 测试场景2: 释放B");
        URCM.Release("Textures/ResB");
        Debug.Log("释放B后的引用计数:");
        PrintRefCounts(new string[] { 
            "Textures/ResB", "Textures/ResD", "Textures/ResF" 
        });
        
        // 验证B已被完全释放
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 0, "释放后B的引用计数应为0");
        // D和F应该仍有引用，因为它们被其他资源共享
        Debug.Assert(URCM.GetRefCount("Textures/ResD") == 1, "B释放后D的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResF") == 1, "B释放后F的引用计数应为1");

        // 测试场景3: 强制释放所有资源
        Debug.Log("7. 测试场景3: 释放所有资源");
        URCM.ReleaseAll();
        Debug.Log("释放所有资源后的引用计数:");
        PrintRefCounts(new string[] { 
            "Textures/ResA", "Textures/ResB", "Textures/ResC", 
            "Textures/ResD", "Textures/ResE", "Textures/ResF" 
        });
        
        // 验证所有资源都已释放
        foreach (var path in allPaths)
        {
            Debug.Assert(URCM.GetRefCount(path) == 0, $"释放所有资源后 {path} 的引用计数应为0");
        }

        // 测试场景4: 循环依赖 (A->B->C->A)
        Debug.Log("8. 测试场景4: 循环依赖");
        resA = URCM.Load<Texture2D>("Textures/ResA");
        resB = URCM.Load<Texture2D>("Textures/ResB");
        resC = URCM.Load<Texture2D>("Textures/ResC");
        
        // 验证初始引用计数
        Debug.Assert(URCM.GetRefCount("Textures/ResA") == 1, "A的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 1, "B的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResC") == 1, "C的引用计数应为1");
        
        cacheA = URCM.GetLruObj("Textures/ResA");
        cacheB = URCM.GetLruObj("Textures/ResB");
        cacheC = URCM.GetLruObj("Textures/ResC");
        
        if (cacheA != null && cacheB != null && cacheC != null)
        {
            cacheA.ChildReses.Add(cacheB);
            cacheB.ChildReses.Add(cacheC);
            cacheC.ChildReses.Add(cacheA); // 创建循环依赖
            Debug.Log("创建循环依赖: A -> B -> C -> A");
        }

        // 尝试释放A，看看是否会导致无限循环
        Debug.Log("尝试释放循环依赖中的A:");
        URCM.Release("Textures/ResA");
        PrintRefCounts(new string[] { 
            "Textures/ResA", "Textures/ResB", "Textures/ResC"
        });
        
        // 验证释放是否成功，以及防止了无限循环
        // 正确实现应该能检测到循环依赖并适当处理
        Debug.Assert(URCM.GetRefCount("Textures/ResA") == 0, "循环依赖中释放A后，A的引用计数应为0");
        
        // B和C的引用计数期望值取决于资源管理器的实现方式
        // 可能值为0（如果循环依赖被正确处理并释放）或1（如果管理器保守处理）
        // 我们假设管理器能够正确处理循环依赖
        Debug.Assert(URCM.GetRefCount("Textures/ResB") == 0, "循环依赖中释放A后，B的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResC") == 0, "循环依赖中释放A后，C的引用计数应为0");

        // 测试场景5: 多重共享依赖
        Debug.Log("9. 测试场景5: 多重共享依赖");
        // 清理所有资源
        URCM.ReleaseAll();
        
        // 创建结构: X是共享依赖
        // P -> X
        // Q -> X
        // R -> X
        var resX = URCM.Load<Texture2D>("Textures/ResX");
        var resP = URCM.Load<Texture2D>("Textures/ResP");
        var resQ = URCM.Load<Texture2D>("Textures/ResQ");
        var resR = URCM.Load<Texture2D>("Textures/ResR");
        
        // 验证初始引用计数
        Debug.Assert(URCM.GetRefCount("Textures/ResX") == 1, "初始X的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResP") == 1, "初始P的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResQ") == 1, "初始Q的引用计数应为1");
        Debug.Assert(URCM.GetRefCount("Textures/ResR") == 1, "初始R的引用计数应为1");
        
        var cacheX = URCM.GetLruObj("Textures/ResX");
        var cacheP = URCM.GetLruObj("Textures/ResP");
        var cacheQ = URCM.GetLruObj("Textures/ResQ");
        var cacheR = URCM.GetLruObj("Textures/ResR");
        
        if (cacheP != null && cacheQ != null && cacheR != null && cacheX != null)
        {
            cacheP.ChildReses.Add(cacheX);
            cacheQ.ChildReses.Add(cacheX);
            cacheR.ChildReses.Add(cacheX);
            Debug.Log("创建共享依赖: P -> X, Q -> X, R -> X");
        }
        
        Debug.Log("初始引用计数:");
        PrintRefCounts(new string[] { "Textures/ResP", "Textures/ResQ", "Textures/ResR", "Textures/ResX" });
        
        // 逐个释放依赖资源
        URCM.Release("Textures/ResP");
        Debug.Log("释放P后的引用计数:");
        PrintRefCounts(new string[] { "Textures/ResP", "Textures/ResQ", "Textures/ResR", "Textures/ResX" });
        
        // 验证P被释放，X仍然存在
        Debug.Assert(URCM.GetRefCount("Textures/ResP") == 0, "释放后P的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResX") == 1, "释放P后X的引用计数应为1");
        
        URCM.Release("Textures/ResQ");
        Debug.Log("释放Q后的引用计数:");
        PrintRefCounts(new string[] { "Textures/ResP", "Textures/ResQ", "Textures/ResR", "Textures/ResX" });
        
        // 验证Q被释放，X仍然存在
        Debug.Assert(URCM.GetRefCount("Textures/ResQ") == 0, "释放后Q的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResX") == 1, "释放Q后X的引用计数应为1");
        
        URCM.Release("Textures/ResR");
        Debug.Log("释放R后的引用计数:");
        PrintRefCounts(new string[] { "Textures/ResP", "Textures/ResQ", "Textures/ResR", "Textures/ResX" });
        
        // 验证R被释放，X也应该被释放（因为没有其他引用了）
        Debug.Assert(URCM.GetRefCount("Textures/ResR") == 0, "释放后R的引用计数应为0");
        Debug.Assert(URCM.GetRefCount("Textures/ResX") == 0, "释放所有引用后X的引用计数应为0");

        Debug.Log("========== 资源依赖关系测试完成 ==========");
    }

    // 辅助方法：打印多个资源的引用计数
    private void PrintRefCounts(string[] paths)
    {
        foreach (var path in paths)
        {
            Debug.Log($"{path}: {URCM.GetRefCount(path)}");
        }
    }
}
