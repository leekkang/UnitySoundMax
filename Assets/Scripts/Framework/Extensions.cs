using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.CompilerServices;

public static class Extensions {
    public static Transform FindRecursive(this Transform t, string str) {
        for (int i = 0; i < t.childCount; i++) {
            Transform child = t.GetChild(i);
            if (child.name == str)
                return child;
            else {
                child = FindRecursive(child, str);
                if (child != null)
                    return child;
            }
        }
        return null;
    }

    public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp) {
        return new UnityWebRequestAwaiter(asyncOp);
    }
}

public class UnityWebRequestAwaiter : INotifyCompletion {
    private UnityWebRequestAsyncOperation asyncOp;
    private System.Action continuation;

    public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp) {
        this.asyncOp = asyncOp;
        asyncOp.completed += OnRequestCompleted;
    }

    public bool IsCompleted { get { return asyncOp.isDone; } }

    public void GetResult() { }

    public void OnCompleted(System.Action continuation) {
        this.continuation = continuation;
    }

    private void OnRequestCompleted(AsyncOperation obj) {
        continuation();
    }
}
