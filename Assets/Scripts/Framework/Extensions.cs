using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
