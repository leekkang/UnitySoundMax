﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class MainPanel : PanelBase {
        Transform mCursorMain; //오브젝트를 사용하기 위한 변수
        Transform mStart;
        Transform mExit;
        int mCursorIndex;

        void Start() {
            mCursorMain = transform.Find("CursorMain"); //panel_main의 오브젝트를 받아옴
            mStart = transform.Find("StartBtn");
            mExit = transform.Find("ExitBtn");

            mCursorMain.position = mStart.position; //초기 커서의 위치를 start에 고정
            mCursorIndex = 0;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {
            //마우스가 위로 움직이면 커서가 start로 움직임
            if (positiveDirection) {
                mCursorMain.position = mStart.position;
                mCursorIndex = 0;
            } else {    //마우스가 아래로 움직이면 커서가 exit로 움직임
                mCursorMain.position = mExit.position;
                mCursorIndex = 1;
            }
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            if (mCursorIndex == 0)
                GuiManager.inst.ActivatePanel(PanelType.Select, true);
            else if (mCursorIndex == 1)
                Application.Quit();
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {

        }
    }
}