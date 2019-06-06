﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoundMax;

public enum InputCode {
    mouseLeft, mouseRight, mouseWheel, tab, capsLock, shiftLeft, ctrlLeft, altLeft,
    W, A, S, D, Q, E, R, F, Z, X, C, V, B, P
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

    /// <summary> 
    /// 레이저 노트 처리용으로 마우스를 사용할지 키보드를 사용할지 결정하는 변수 
    /// 마우스 사용 시 true, 키보드 사용시 false로 해야한다.
    /// 키보드는 왼쪽 레이저가 Q, W, 오른쪽 레이저가 E, R을 사용한다.
    /// </summary>
    public bool mIsLaserUseMouse;
    /// <summary> 레이저 노트 키보드 값 </summary>
    float[] mLaserKeyValue = new float[2];

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
        };
    }

    public void Update() {
        index = (index + 1) % saveNum;
        frame = 1f / Time.deltaTime;
        if (frame > 60f) frame = 60f;   // 계산프레임 60고정

        for (int i = 0; i < maxKeyNum; i++)
            SaveInfo((InputCode)i);

        // 메인 UI용 체크
        if (!IngameEngine.inst.mPlaying) {
            for (int i = 0; i < mButtonInputCode.Length; i++) {
                if (info[(int)mButtonInputCode[i], index].down) {
                    if (i < 4) GuiManager.inst.OnClickBtnNormal();
                    else GuiManager.inst.OnClickBtnFX();

                    return;
                }
            }

            if (info[(int)InputCode.V, index].down)
                GuiManager.inst.OnClickBtnStart();

            // 오토플레이 전환
            if (info[(int)InputCode.C, index].down) {
                Scoring.inst.autoplay = !Scoring.inst.autoplay;
                Debug.Log("autoplay : " + Scoring.inst.autoplay);
            }

            return;
        }

        // 피버 게이지 증가
        if (info[(int)InputCode.B, index].stay) {
            IngameEngine.inst.UpdateMaximizeGuage(IngameEngine.STAY_MAXIMIZE_PERCENT * Time.deltaTime);
        }
        // 피버 클릭
        if (info[(int)InputCode.B, index].down) {
            IngameEngine.inst.StartMaximizeTime();
        }

        // 게임용 체크
        for (int i = 0; i < mButtonInputCode.Length; i++) {
            if (info[(int)mButtonInputCode[i], index].down) {
                Scoring.inst.OnButtonPressed(i);
                // 퍼즈상태일때는 Gui가 반응하도록 구현
                if (IngameEngine.inst.m_paused) {
                    if (i < 4) GuiManager.inst.OnClickBtnNormal();
                    else GuiManager.inst.OnClickBtnFX();
                    return;
                }
            }
            if (info[(int)mButtonInputCode[i], index].up) {
                Scoring.inst.OnButtonReleased(i);
            }
        }

        if (info[(int)InputCode.V, index].down) {
            if (IngameEngine.inst.m_paused)
                GuiManager.inst.OnClickBtnStart();
            else 
                IngameEngine.inst.OnClickPauseButton(false);
        }

        // 에디터 디버그용. 마우스 대신 사용
        if (!mIsLaserUseMouse) {
            if (info[(int)InputCode.Q, index].stay)
                mLaserKeyValue[0] = -0.1f;
            else if (info[(int)InputCode.W, index].stay)
                mLaserKeyValue[0] = 0.1f;
            else
                mLaserKeyValue[0] = 0;

            if (info[(int)InputCode.E, index].stay)
                mLaserKeyValue[1] = -0.1f;
            else if (info[(int)InputCode.R, index].stay)
                mLaserKeyValue[1] = 0.1f;
            else
                mLaserKeyValue[1] = 0;
        }

    }

    public bool CheckHold(int num) {
        return info[(int)mButtonInputCode[num], index].stay;
    }

    public float GetLaserAxisValue(int num) {
        // 0 : left, 1 : right
        // TODO : 기본 속도로 돌릴경우 어느정도로 갈지 결정해야함
        if (mIsLaserUseMouse)
            return (num == 0 ? Input.GetAxis("Mouse X") : -Input.GetAxis("Mouse Y")) * 0.3f;
        else
            return mLaserKeyValue[num];
    }

    public int GetLaserDirection(int num) {
        // 0 : left, 1 : right
        if (mIsLaserUseMouse)
            return System.Math.Sign(num == 0 ? Input.GetAxis("Mouse X") : -Input.GetAxis("Mouse Y"));
        else
            return System.Math.Sign(mLaserKeyValue[num]);
    }

    KeyCode GetKeyCode(InputCode code) {
        if (code == InputCode.tab)
            return KeyCode.Tab;
        if (code == InputCode.capsLock)
            return KeyCode.CapsLock;
        if (code == InputCode.shiftLeft)
            return KeyCode.LeftShift;
        if (code == InputCode.ctrlLeft)
            return KeyCode.LeftControl;
        if (code == InputCode.altLeft)
            return KeyCode.LeftAlt;

        return (KeyCode)System.Enum.Parse(typeof(KeyCode), code.ToString());
    }

    void SaveInfo(InputCode type) {
        if (type == InputCode.mouseLeft)
            InputMouseInfo((int)type, 0);
        else if (type == InputCode.mouseRight)
            InputMouseInfo((int)type, 1);
        else if (type == InputCode.mouseWheel)
            InputMouseInfo((int)type, 2);
        else
            InputKeyInfo((int)type, GetKeyCode(type));
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

    #region Not Used In SoundMax

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

    #endregion
}
