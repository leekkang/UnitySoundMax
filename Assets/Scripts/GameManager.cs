using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class GameManager : MonoBehaviour {
        void Start() {
#if UNITY_STANDALONE
            Screen.SetResolution(607, 1080, false);
            //Screen.SetResolution(1080, 1920, false);
            Screen.fullScreen = false;
#endif

            GuiManager.inst.Open();
            MainPanel mainPanel = (MainPanel)GuiManager.inst.GetPanel(PanelType.Main);
            DataBase.inst.Open(mainPanel.SetLoadMusic);

            KeyboardManager.inst.Open();
            IngameEngine.inst.Open();
            SoundManager.inst.Open();

            StartCoroutine(CoLoadMain(mainPanel));

            KeyboardManager.inst.mIsLaserUseMouse = true;
        }

        IEnumerator CoLoadMain(MainPanel mainPanel) {
            mainPanel.Loading();
            Debug.Log("Loading for Initialize ...");
            yield return new WaitUntil(() => DataBase.inst.mOpenComplete);
            Debug.Log("Initialize Done!");
            mainPanel.ActivateButton();
        }

        public void OnApplicationQuit() {
            if (DataBase.inst.mOpenComplete)
                SaveDataAdapter.SaveData(DataBase.inst.mUserData, "user");
        }


        public static void guiLog(string text) {
            mLogText1 = mLogText2;
            mLogText2 = mLogText3;
            mLogText3 = text;
        }

        static string mLogText1;
        static string mLogText2;
        static string mLogText3;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Rect textRect = new Rect(0, 0, Screen.width * 0.5f, Screen.height * 0.1f);
        private void OnGUI() {
            // 막음. 필요할 때 리턴 지우고 사용
            //return;

            //if (!DataBase.inst.mOpenComplete)
            //    return;

            GUI.skin.label.fontSize = 20;
            //if (textRect.Contains(Event.current.mousePosition)) {
            //    if (Event.current.type == EventType.MouseDown) {
            //        buttonPressed = true;
            //    }
            //    if (Event.current.type == EventType.MouseUp) {
            //        buttonPressed = false;
            //    }
            //}

            //if (buttonPressed && Event.current.type == EventType.MouseDrag) {
            //    buttonDragging = true;
            //    textRect.x = Event.current.mousePosition.x - textRect.width / 2f;
            //    textRect.y = Event.current.mousePosition.y - textRect.height / 2f;
            //}

            //if (buttonDragging && !buttonPressed) {
            //    buttonDragging = false;
            //    return;
            //}

            GUIStyle myStyle = new GUIStyle();
            myStyle.fontSize = 15;
            if (GUI.Button(textRect, "test")) {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Music");
                if (!System.IO.File.Exists(System.IO.Path.Combine(path, "dignity/nofx.ogg"))) {
                    GameManager.guiLog("Cannot find file!");
                } else {
                    GameManager.guiLog("find file!");
                }
            }
            if (GUI.Button(new Rect(textRect.x, textRect.y + Screen.height * 0.1f, textRect.width, textRect.height), "test2")) {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Music");
                path = System.IO.Path.Combine(path, "dignity/nofx.ogg");
                DataBase.inst.LoadAudio(path, (audio) => {
                    if (audio == null) {
                        GameManager.guiLog("Cannot find file!");
                    } else {
                        GameManager.guiLog("find file!");
                    }
                });
            }
            if (GUI.Button(new Rect(textRect.x, textRect.y + Screen.height * 0.2f, textRect.width, textRect.height), "test3")) {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Music");
                if (!System.IO.File.Exists(System.IO.Path.Combine(path, "dignity\\nofx.ogg"))) {
                    GameManager.guiLog("Cannot find file!");
                } else {
                    GameManager.guiLog("find file!");
                }
            }
            GUI.Label(new Rect(textRect.x, textRect.y + Screen.height * 0.3f, textRect.width, textRect.height), mLogText1, myStyle);
            GUI.Label(new Rect(textRect.x, textRect.y + Screen.height * 0.4f, textRect.width, textRect.height), mLogText2, myStyle);
            GUI.Label(new Rect(textRect.x, textRect.y + Screen.height * 0.5f, textRect.width, textRect.height), mLogText3, myStyle);
            //GUI.TextArea(new Rect(textRect.x, textRect.y + Screen.height * 0.1f, textRect.width, textRect.height), mLogText1);
            //GUI.TextArea(new Rect(textRect.x, textRect.y + Screen.height * 0.2f, textRect.width, textRect.height), mLogText2);
            //GUI.TextArea(new Rect(textRect.x, textRect.y + Screen.height * 0.3f, textRect.width, textRect.height), mLogText3);
        }
#endif
    }

}