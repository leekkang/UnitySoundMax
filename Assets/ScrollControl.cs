using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollControl : MonoBehaviour {
    UIProgressBar hScrollbar;
    UIProgressBar vScrollbar;
    public float keyboardSensitivity = 2;
    void Awake() {
        //Assign both scrollbars on Awake
        hScrollbar =
        GetComponent<UIScrollView>().horizontalScrollBar;
        vScrollbar = GetComponent<UIScrollView>().verticalScrollBar;
    }

    void Update() {
        //Get keyboard input axes values
        Vector2 keyDelta = Vector2.zero;
        keyDelta.Set(Input.GetAxis("Horizontal"),
        Input.GetAxis("Vertical"));
        //If no keyboard arrow is pressed, leave
        if (keyDelta == Vector2.zero) return;
        //Make it framerate independent and multiply by sensitivity
        keyDelta *= Time.deltaTime * keyboardSensitivity;
        //Scroll by adjusting scrollbars' values
        hScrollbar.value += keyDelta.x;
        vScrollbar.value -= keyDelta.y;
    }
}
