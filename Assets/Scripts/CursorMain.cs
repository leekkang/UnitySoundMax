using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class CursorMain : MonoBehaviour {

        private GameObject mCursorMain; //오브젝트를 사용하기 위한 변수
        private GameObject mStart;
        private GameObject mExit;
        public float mMouseX; //마우스 축의 움직임을 받기 위한 변수
        public float mMouseY;
        public float mMouseXSpeed = 2.0f; //마우스 움직임 민감도
        public float mMouseYSpeed = 2.0f;

        void Start() {
            mCursorMain = GameObject.Find("CursorMain"); //panel_main의 오브젝트를 받아옴
            mStart = GameObject.Find("Start");
            mExit = GameObject.Find("Exit");

            mCursorMain.transform.position = mStart.transform.position; //초기 커서의 위치를 start에 고정
        }

        void Update() {

            mMouseX = mMouseXSpeed * Input.GetAxis("Mouse X"); //마우스의 움직임을 받음
            mMouseY = mMouseYSpeed * Input.GetAxis("Mouse Y");

            Debug.Log("mMouseX " + mMouseX); //debug로 console창에서 값이 제대로 들어오는지 체크
            Debug.Log("mMouseY " + mMouseY);
            Debug.Log("Cursor Local Position" + mCursorMain.transform.position);

            CusorMoveMain(); //커서무빙함수
        }

        void CusorMoveMain() {
            if (mMouseX < 0) { //마우스가 아래로 움직이면 커서가 exit로 움직임
                mCursorMain.transform.position = mExit.transform.position;
            } else if (mMouseX > 0) { //마우스가 위로 움직이면 커서가 start로 움직임
                mCursorMain.transform.position = mStart.transform.position;
            }
        }
    }
}