using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class CursorSelect : MonoBehaviour {
        readonly int MOUSE_MOVE_POINT = 5;
        readonly int MAX_MUSIC_NUM = 14;

        private GameObject mCursorSelect;

        private GameObject[] mBackPanel;

        public float mMouseX; //마우스 축의 움직임을 받기 위한 변수
        public float mMouseY;
        public float mMouseXSpeed = 2.0f; //마우스 움직임 민감도
        public float mMouseYSpeed = 2.0f;

        int mCurIndex;

        void Start() {
            mCursorSelect = GameObject.Find("CursorSelect");
            mBackPanel = new GameObject[14];
            for (int i = 0; i < MAX_MUSIC_NUM; i++) {
                mBackPanel[i] = GameObject.Find("BackPanel" + i);
            }

            mCurIndex = 0;
            mCursorSelect.transform.SetParent(mBackPanel[mCurIndex].transform, false);
        }

        bool CheckDirectionChange(float a, float b) {
            return (a > 0 && b < 0) || (b > 0 && a < 0);
        }

        void Update() {
            ControlXAxis();
            mMouseY = mMouseYSpeed * Input.GetAxis("Mouse Y");
        }

        void ControlXAxis() {
            float xMove = Input.GetAxis("Mouse X");
            if (xMove == 0)
                return;

            if (CheckDirectionChange(xMove, mMouseX))
                mMouseX = 0f;

            mMouseX += mMouseXSpeed * xMove; //마우스의 움직임을 받음
            mMouseY = mMouseYSpeed * Input.GetAxis("Mouse Y");

            CursorMoveSelect();

            //Debug.Log("mMouseX : " + mMouseX); //debug로 console창에서 값이 제대로 들어오는지 체크
            //Debug.Log("mCursorSelect.transform.parent : " + mCursorSelect.transform.parent);
        }

        void CursorMoveSelect() {
            if (mMouseX > MOUSE_MOVE_POINT) {
                mCurIndex = ++mCurIndex % MAX_MUSIC_NUM;
                mCursorSelect.transform.SetParent(mBackPanel[mCurIndex].transform, false);
                mMouseX = 0f;
            } else if (mMouseX < -MOUSE_MOVE_POINT) {
                mCurIndex = (--mCurIndex + MAX_MUSIC_NUM) % MAX_MUSIC_NUM; // 음수처리
                mCursorSelect.transform.SetParent(mBackPanel[mCurIndex].transform, false);
                mMouseX = 0f;
            }
        }
    }
}