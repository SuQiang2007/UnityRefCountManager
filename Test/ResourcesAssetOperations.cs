using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 基于Unity Resources系统的资源操作实现
/// </summary>
public class ResourcesAssetOperations : IAssetOperations
{
    public T LoadAsset<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public async Task<T> LoadAssetAsync<T>(string path) where T : Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);
        
        while (!request.isDone)
        {
            await Task.Yield();
        }
        
        return request.asset as T;
    }

    public void UnloadAsset(Object asset, string path)
    {
        if (asset != null)
        {
            Resources.UnloadAsset(asset);
        }
    }

    public string GetDisplayName(string path)
    {
        return path;
    }
}