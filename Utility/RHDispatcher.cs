using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ResourcefulHands;

public class RHDispatcher : MonoBehaviour
{
    private static RHDispatcher? _instance;
    private static readonly Queue<System.Action> ExecutionQueue = new();

    public static void Initialize()
    {
        if (_instance) return;
        var go = new GameObject("RH_Dispatcher");
        _instance = go.AddComponent<RHDispatcher>();
        DontDestroyOnLoad(go);
    }

    public static void RunOnMainThread(System.Action action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                ExecutionQueue.Dequeue().Invoke();
            }
        }
    }

    public static Coroutine? StartStaticCoroutine(IEnumerator routine) => _instance?.StartCoroutine(routine);
}