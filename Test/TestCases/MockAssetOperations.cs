using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 用于测试的模拟资源操作实现
/// </summary>
public class MockAssetOperations : IAssetOperations
{
    private Dictionary<string, Object> _mockAssets = new Dictionary<string, Object>();

    public T LoadAsset<T>(string path) where T : Object
    {
        Debug.Log($"[Mock] 加载资源: {path}");
        
        // 如果是Texture2D类型，返回一个临时创建的纹理
        if (typeof(T) == typeof(Texture2D))
        {
            var texture = new Texture2D(2, 2);
            texture.name = "MockTexture_" + path;
            _mockAssets[path] = texture;
            return texture as T;
        }
        
        // 如果是Material类型
        if (typeof(T) == typeof(Material))
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "MockMaterial_" + path;
            _mockAssets[path] = material;
            return material as T;
        }
        
        // 如果是GameObject类型
        if (typeof(T) == typeof(GameObject))
        {
            var go = new GameObject("MockGameObject_" + path);
            _mockAssets[path] = go;
            return go as T;
        }
        
        return null;
    }

    public async Task<T> LoadAssetAsync<T>(string path) where T : Object
    {
        Debug.Log($"[Mock] 异步加载资源: {path}");
        
        // 模拟异步加载延迟
        await Task.Delay(100);
        
        return LoadAsset<T>(path);
    }

    public void UnloadAsset(Object asset, string path)
    {
        Debug.Log($"[Mock] 卸载资源: {path}");
        
        if (_mockAssets.ContainsKey(path))
        {
            _mockAssets.Remove(path);
        }
        
        // 如果是GameObject，销毁它
        if (asset is GameObject go)
        {
            Object.Destroy(go);
        }
    }

    public string GetDisplayName(string path)
    {
        return $"[Mock] {path}";
    }
}