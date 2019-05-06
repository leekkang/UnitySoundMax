using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class IngamePanel : PanelBase {
        Transform mCursor;
        List<Transform> mListButton = new List<Transform>();
        int mCursorIndex;
        
        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            mCursor = transform.Find("CursorIngame");
            mListButton.Add(transform.Find("Continue"));
            mListButton.Add(transform.Find("Restart"));
            mListButton.Add(transform.Find("ForceEnd"));

            mCursorIndex = 0;
            mCursor.position = mListButton[mCursorIndex].position;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {
            if (positiveDirection)
                mCursorIndex = Mathf.Max(0, mCursorIndex - 1);
            else
                mCursorIndex = Mathf.Min(mListButton.Count - 1, mCursorIndex + 1);

            mCursor.position = mListButton[mCursorIndex].position;
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {

        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {
            if (mCursorIndex == 0) {
                IngameEngine.inst.OnClickPauseButton(true);
            } else {
                StartCoroutine(CoEndIngame(mCursorIndex == 1));
            }
        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
            if (mCursorIndex == 0) {
                IngameEngine.inst.OnClickPauseButton(true);
            } else {
                StartCoroutine(CoEndIngame(mCursorIndex == 1));
            }
        }

        IEnumerator CoEndIngame(bool restart) {
            IngameEngine.inst.mForceEnd = true;
            yield return new WaitUntil(() => IngameEngine.inst.m_ended);
            if (restart) {
                IngameEngine.inst.Restart();
            } else {
                GuiManager.inst.ActivatePanel(PanelType.Select, true);
            }
        }
    }
}