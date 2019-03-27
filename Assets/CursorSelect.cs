using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorSelect : MonoBehaviour {

    private GameObject mCursorSelect;

    private GameObject[] mBackPanel = new GameObject[14];

    public float mMouseX; //마우스 축의 움직임을 받기 위한 변수
    public float mMouseY;
    public float mMouseXSpeed = 2.0f; //마우스 움직임 민감도
    public float mMouseYSpeed = 2.0f;

    void Start() {
        mCursorSelect = GameObject.Find("CursorSelect");

        for (int i = 0; i < 14; i++) {
            mBackPanel[i] = GameObject.Find("BackPanel" + i);
        }

        mCursorSelect.transform.SetParent(mBackPanel[0].transform, false);
    }

    void Update() {
        mMouseX = mMouseXSpeed * Input.GetAxis("Mouse X"); //마우스의 움직임을 받음
        mMouseY = mMouseYSpeed * Input.GetAxis("Mouse Y");

        CursorMoveSelect();

        Debug.Log("mMouseX : " + mMouseX); //debug로 console창에서 값이 제대로 들어오는지 체크
        Debug.Log("mCursorSelect.transform.parent : " + mCursorSelect.transform.parent);
    }

    void CursorMoveSelect() {
        for (int i = 0; i < 14; i++) {
            if (mCursorSelect.transform.parent == mBackPanel[i].transform) { // 1의 위치
                if (mMouseX > 0) {
                    if (mCursorSelect.transform.parent == mBackPanel[13].transform) {
                        mCursorSelect.transform.SetParent(mBackPanel[0].transform, false);
                    }
                    else {
                        mCursorSelect.transform.SetParent(mBackPanel[i + 1].transform, false);
                    }
                }
                else if (mMouseX < 0) {
                    if (mCursorSelect.transform.parent == mBackPanel[0].transform) {
                        mCursorSelect.transform.SetParent(mBackPanel[13].transform, false);
                    }
                    else {
                        mCursorSelect.transform.SetParent(mBackPanel[i - 1].transform, false);
                    }
                }
            }
        }
    }
}