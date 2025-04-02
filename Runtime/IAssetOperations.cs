using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 资源操作接口，定义资源加载和释放的抽象操作
/// </summary>
public interface IAssetOperations
{
    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源</returns>
    T LoadAsset<T>(string path) where T : Object;
    
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>表示异步加载操作的任务</returns>
    Task<T> LoadAssetAsync<T>(string path) where T : Object;
    
    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="asset">要释放的资源</param>
    /// <param name="path">资源路径</param>
    void UnloadAsset(Object asset, string path);
    
    /// <summary>
    /// 获取资源路径的显示名称（用于日志等）
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>显示名称</returns>
    string GetDisplayName(string path);
}