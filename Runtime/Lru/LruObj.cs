using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class LruObj
{
    public string FullPath;
    public bool KeepInCache;
    public string Guid = System.Guid.NewGuid().ToString();
    public bool HasReleased = false;
    public bool HasAdded = false;
    
    // 添加 Asset 字段来存储实际的资源引用
    public Object Asset;

    public Func<LruObj, bool> OnDestroy;
    
    //不是子界面，而是依附于本Obj逻辑的碎图之类的资源
    public List<LruObj> ChildReses = new();
}