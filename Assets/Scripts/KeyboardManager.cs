using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InputCode {
    mouseLeft, mouseRight, mouseWheel, tab, capsLock, shiftLeft, ctrlLeft, altLeft,
    W, A, S, D, Q, E, R, F, Z, X, C, V, P
}
public class KeyboardManager : Singleton<KeyboardManager> {
    float frame;
    int index = -1;
    const int saveNum = 30;
    const int maxKeyNum = (int)InputCode.P + 1;
    const float doubleTime = .3f;
    const float normalClickTime = .1f;
    const float doubleClickTime = .1f;

    class KeyInfo {
        public bool down;
        public bool stay;
        public bool up;
    }
    KeyInfo[,] info;

    InputCode[] mButtonInputCode;
    public void Open() {
        info = new KeyInfo[maxKeyNum, saveNum];
        for (int i = 0; i < maxKeyNum; i++)
            for (int j = 0; j < saveNum; j++)
                info[i, j] = new KeyInfo();

        // TODO : 나중에 변경할 수 있도록 수정
        mButtonInputCode = new InputCode[] {
            InputCode.A,    // 노멀버튼
            InputCode.S,
            InputCode.D,
            InputCode.F,
            InputCode.Z,    // fx버튼
            InputCode.X,
            InputCode.P     // 스타트 버튼
        };
    }

    public void Update() {
        index = (index + 1) % saveNum;
        frame = 1f / Time.deltaTime;
        if (frame > 60f) frame = 60f;   // 계산프레임 60고정

        for (int i = 0; i < maxKeyNum; i++)
            SaveInfo((InputCode)i);


        // 게임용 체크
        if (!IngameEngine.inst.m_playing)
            return;

        for (int i = 0; i < mButtonInputCode.Length; i++) {
            if (info[(int)mButtonInputCode[i], index].down) {
                Scoring.inst.OnButtonPressed(i);
            }
            if (info[(int)mButtonInputCode[i], index].up) {
                Scoring.inst.OnButtonReleased(i);
            }
        }
    }

    public bool CheckDouble(InputCode type) {
        int compareNum = Mathf.CeilToInt(frame * doubleTime); // 올림(넉넉한 판정)
        int num = 0, downNum = 0;
        for (int i = 0; i < compareNum; i++) {
            if (index - i < 0) num = saveNum + (index - i);
            else num = index - i;

            if (info[(int)type, num].down) downNum++;
            if (downNum > 1) return true;
        }

        return false;
    }

    // 단일클릭과 양클릭을 구분하기 위해 만든 함수, 정해진 프레임 동안 키를 누르고 있지 않으면 false 반환
    public bool CheckClick(InputCode type) {
        int compareNum = Mathf.CeilToInt(frame * doubleClickTime); // 올림(넉넉한 판정)
        int num = 0;
        for (int i = 0; i < compareNum; i++) {
            if (index - i < 0) num = saveNum + (index - i);
            else num = index - i;

            if (!info[(int)type, num].stay)
                return false;
        }
        return true;
    }

    public bool CheckDoubleClick() {
        int compareNum = Mathf.CeilToInt(frame * doubleClickTime); // 올림(넉넉한 판정)
        int num = 0, downNum = 0;
        for (int i = 0; i < compareNum; i++) {
            if (index - i < 0) num = saveNum + (index - i);
            else num = index - i;

            if (info[(int)InputCode.mouseLeft, num].down) downNum++;
            if (info[(int)InputCode.mouseRight, num].down) downNum++;
            if (downNum > 1) return true;
        }

        return false;
    }

    void SaveInfo(InputCode type) {
        switch (type) {
            case InputCode.mouseLeft:
            InputMouseInfo((int)type, 0);
            break;
            case InputCode.mouseRight:
            InputMouseInfo((int)type, 1);
            break;
            case InputCode.mouseWheel:
            InputMouseInfo((int)type, 2);
            break;
            case InputCode.tab:
            InputKeyInfo((int)type, KeyCode.Tab);
            break;
            case InputCode.capsLock:
            InputKeyInfo((int)type, KeyCode.CapsLock);
            break;
            case InputCode.shiftLeft:
            InputKeyInfo((int)type, KeyCode.LeftShift);
            break;
            case InputCode.ctrlLeft:
            InputKeyInfo((int)type, KeyCode.LeftControl);
            break;
            case InputCode.altLeft:
            InputKeyInfo((int)type, KeyCode.LeftAlt);
            break;
            case InputCode.W:
            InputKeyInfo((int)type, KeyCode.W);
            break;
            case InputCode.A:
            InputKeyInfo((int)type, KeyCode.A);
            break;
            case InputCode.S:
            InputKeyInfo((int)type, KeyCode.S);
            break;
            case InputCode.D:
            InputKeyInfo((int)type, KeyCode.D);
            break;
            case InputCode.Q:
            InputKeyInfo((int)type, KeyCode.Q);
            break;
            case InputCode.E:
            InputKeyInfo((int)type, KeyCode.E);
            break;
            case InputCode.R:
            InputKeyInfo((int)type, KeyCode.R);
            break;
            case InputCode.F:
            InputKeyInfo((int)type, KeyCode.F);
            break;
            case InputCode.Z:
            InputKeyInfo((int)type, KeyCode.Z);
            break;
            case InputCode.X:
            InputKeyInfo((int)type, KeyCode.X);
            break;
            case InputCode.C:
            InputKeyInfo((int)type, KeyCode.C);
            break;
            case InputCode.V:
            InputKeyInfo((int)type, KeyCode.V);
            break;
            case InputCode.P:
            InputKeyInfo((int)type, KeyCode.P);
            break;
        }
    }

    void InputKeyInfo(int type, KeyCode code) {
        info[type, index].down = Input.GetKeyDown(code);
        info[type, index].stay = Input.GetKey(code);
        info[type, index].up = Input.GetKeyUp(code);
    }
    void InputMouseInfo(int type, int num) {
        info[type, index].down = Input.GetMouseButtonDown(num);
        info[type, index].stay = Input.GetMouseButton(num);
        info[type, index].up = Input.GetMouseButtonUp(num);
    }
}
