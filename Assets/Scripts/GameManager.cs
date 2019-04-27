using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoundMax;

public class GameManager : MonoBehaviour {
    void Start() {
        GuiManager.inst.Open();
        SoundManager.inst.Open();
        KeyboardManager.inst.Open();
        IngameEngine.inst.Open();
        DataBase.inst.Open();
    }
    
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    Rect buttonRect = new Rect(0, 0, Screen.width * 0.5f, Screen.height * 0.1f);
    Rect scoreRect = new Rect(Screen.width * 0.5f, Screen.height * 0.1f, Screen.width * 0.5f, Screen.height * 0.1f);
    bool buttonPressed = false;
    bool buttonDragging = false;

    string m_score_text;
    private void OnGUI() {
        GUI.skin.label.fontSize = 20;
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

        if (GUI.Button(buttonRect, "colorfulsky play") && !buttonDragging) {
            MusicData data = new MusicData();
            Scoring.inst.autoplayButtons = true;
            data.Load("colorfulsky", Difficulty.Challenge);
            IngameEngine.inst.StartGame(data, 1.0f);
        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.1f, buttonRect.width, buttonRect.height), "max burning play") && !buttonDragging) {
            MusicData data = new MusicData();
            //Scoring.inst.autoplayButtons = true;
            Scoring.inst.autoplay = true;
            //Scoring.inst.autoplayButtons = false;
            data.Load("max_burning", Difficulty.Infinite);
            IngameEngine.inst.StartGame(data, 3f);
        }

        if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.2f, buttonRect.width, buttonRect.height), "audio play") && !buttonDragging) {
            AudioSource source = GameObject.Find("FXSound").GetComponent<AudioSource>();
            DataBase.inst.LoadAudio("colorfulsky", AudioType.OGGVORBIS, (audio) => {
                Debug.Log("audio sample : " + audio.samples);
                Debug.Log("audio length : " + audio.length);
                source.clip = audio;
                source.Play();
            });
        }
    }
#endif
}
