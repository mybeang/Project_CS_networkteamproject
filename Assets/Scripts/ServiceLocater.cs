using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator 
{
    private static readonly Dictionary<Type, object> _services = new ();

    // 서비스 등록
    public static void Register<T>(T service)
    {
        var type = typeof(T);
        _services[type] = service;
    }

    // 서비스 해제
    public static void Unregister<T>()
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
        }
    }

    // 서비스 획득
    public static T Get<T>()
    {
        var type = typeof(T);
        if (_services.TryGetValue(type, out var service))
        {
            return (T)service;
        }
        return default;
    }

    public static void PrintServices()
    {
        Debug.Log("------- `Service Locator` Service List ------");
        foreach (var (key, value) in _services)
        {
            Debug.Log($"{key} : {value}");
        }
        Debug.Log("----------------------------------------------");
    }
}
