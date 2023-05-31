using UnityEngine;

namespace CarbonCompatLoader;

public static class Logger
{
    public static void Info(object obj)
    {
        Debug.Log($"[CCL] {obj}");
    }
    public static void Warn(object obj)
    {
        Debug.LogWarning($"[CCL] {obj}");
    }
    public static void Error(object obj)
    {
        Debug.LogError($"[CCL] {obj}");
    }
}