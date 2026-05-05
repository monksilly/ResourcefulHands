using UnityEngine;

namespace ResourcefulHands.Utility;

public static class TransformExtensions
{
    public static Transform? FindParentWithName(this Transform current, string name)
    {
        while (current != null && current.name != name)
        {
            current = current.parent;
        }
        return current;
    }

    public static Transform? FindTopmostParent(this Transform current)
    {
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current;
    }
    
    public static T? FindAt<T>(this Transform t, string path) where T : Component
    {
        string[] objectNames = path.Split('/');
        foreach (var name in objectNames)
        {
            t = t.Find(name);
            if (t == null) return null;
        }
        return t.GetComponentInChildren<T>();
    }
}