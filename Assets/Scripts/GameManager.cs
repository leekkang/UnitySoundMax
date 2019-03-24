using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    void Start() {
        GuiManager.inst.Open();
        SoundManager.inst.Open();
        KeyboardManager.inst.Open();
        IngameEngine.inst.Open();
        DataBase.inst.Open();
    }
    
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    Rect buttonRect = new Rect(Screen.width * 0.25f, Screen.height * 0.1f, Screen.width * 0.5f, Screen.height * 0.1f);
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
            MusicData data = new MusicData();
            data.Load("colorfulsky", Difficulty.Challenge);
        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.1f, buttonRect.width, buttonRect.height), "Logout") && !buttonDragging) {

        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.2f, buttonRect.width, buttonRect.height), "StoryPanel") && !buttonDragging) {

        }
    }
#endif
}
