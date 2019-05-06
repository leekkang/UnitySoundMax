using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class GameManager : MonoBehaviour {
        void Start() {
            GuiManager.inst.Open();
            SoundManager.inst.Open();
            KeyboardManager.inst.Open();
            IngameEngine.inst.Open();
            DataBase.inst.Open();

            StartCoroutine(CoLoadMain());

            Scoring.inst.autoplay = true;
            KeyboardManager.inst.mIsLaserUseMouse = true;
        }

        IEnumerator CoLoadMain() {
            Debug.Log("Loading for Initialize ...");
            yield return new WaitUntil(() => DataBase.inst.mOpenComplete);
            Debug.Log("Initialize Done!");
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Rect buttonRect = new Rect(0, 0, Screen.width * 0.5f, Screen.height * 0.1f);
        Rect scoreRect = new Rect(Screen.width * 0.5f, Screen.height * 0.1f, Screen.width * 0.5f, Screen.height * 0.1f);
        bool buttonPressed = false;
        bool buttonDragging = false;

        string m_score_text;
        private void OnGUI() {
            // 막음. 필요할 때 리턴 지우고 사용
            return;

            if (!DataBase.inst.mOpenComplete)
                return;

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
                if (IngameEngine.inst.mPlaying)
                    IngameEngine.inst.mForceEnd = true;
                MusicData data = DataBase.inst.mDicMusic["colorfulsky"].Find((x) => x.mDifficulty == Difficulty.Advanced);
                Scoring.inst.autoplayButtons = true;
                KeyboardManager.inst.mIsLaserUseMouse = true;
                IngameEngine.inst.StartGame(data, 2.0f);
            }

            if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.1f, buttonRect.width, buttonRect.height), "max burning play") && !buttonDragging) {
                if (IngameEngine.inst.mPlaying)
                    IngameEngine.inst.mForceEnd = true;
                MusicData data = DataBase.inst.mDicMusic["max_burning"].Find((x) => x.mDifficulty == Difficulty.Exhausted);
                //Scoring.inst.autoplayButtons = true;
                Scoring.inst.autoplay = true;
                KeyboardManager.inst.mIsLaserUseMouse = true;
                IngameEngine.inst.StartGame(data, 5f);
            }

            if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.2f, buttonRect.width, buttonRect.height), "xross infection play") && !buttonDragging) {
                if (IngameEngine.inst.mPlaying)
                    IngameEngine.inst.mForceEnd = true;
                MusicData data = DataBase.inst.mDicMusic["xross_infection"].Find((x) => x.mDifficulty == Difficulty.Exhausted);
                //Scoring.inst.autoplayButtons = true;
                //Scoring.inst.autoplay = true;
                KeyboardManager.inst.mIsLaserUseMouse = true;
                IngameEngine.inst.StartGame(data, 5f);
            }

            if (GUI.Button(new Rect(buttonRect.x, buttonRect.y + Screen.height * 0.3f, buttonRect.width, buttonRect.height), "auto play bool") && !buttonDragging) {

                IngameEngine.inst.mForceEnd = true;
            }
        }
#endif
    }

}