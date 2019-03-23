using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
    void Start() {

    }
    
    void Update() {

    }


#if DEVELOPMENT_BUILD || UNITY_EDITOR
    Rect buttonRect = new Rect(0, Screen.height * 0.6f, Screen.width * 0.15f, Screen.height * 0.1f);
    bool buttonPressed = false;
    bool buttonDragging = false;

    private void OnGUI() {
        if (buttonRect.Contains(Event.current.mousePosition)) {
            if (Event.current.type == EventType.MouseDown) {
                buttonPressed = true;
            }
            if (Event.current.type == EventType.MouseUp) {
                buttonPressed = false;
            }
        }

        if (buttonPressed && Event.current.type == EventType.MouseDrag) {
            buttonDragging = true;
            buttonRect.x = Event.current.mousePosition.x - buttonRect.width / 2f;
            buttonRect.y = Event.current.mousePosition.y - buttonRect.height / 2f;
        }

        if (buttonDragging && !buttonPressed) {
            buttonDragging = false;
            return;
        }

        if (GUI.Button(buttonRect, "PracticeBattle") && !buttonDragging) {
        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.1f, buttonRect.width, buttonRect.height), "Logout") && !buttonDragging) {

        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.2f, buttonRect.width, buttonRect.height), "StoryPanel") && !buttonDragging) {

        }
    }
#endif
}
