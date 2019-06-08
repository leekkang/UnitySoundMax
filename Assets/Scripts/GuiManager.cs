using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public enum PanelType { // 이용가능한 패널 일체
        None,
        Main,
        Select,
        Option,
        Ingame,
        Result
    }

    public class GuiManager : Singleton<GuiManager> { //manager를 singleton으로 설정
        public const int MOUSE_MOVE_POINT = 10; // 기본값 5

        Transform mCursorSelect;

        float mMouseX; //마우스 축의 움직임을 받기 위한 변수
        float mMouseY;
        float mMouseXSpeed = 2.0f; // 마우스 움직임 민감도
        float mMouseYSpeed = 2.0f;

        // 사용할 수 있는 패널들의 모음
        Dictionary<int, PanelBase> mDicPanel = new Dictionary<int, PanelBase>();

        /// <summary> 현재 가장 위에 올라와있는 패널 </summary>
        public PanelType mCurPanelType;

        #region Loading

        /// <summary> 현재 패널 이동 중 로딩 스프라이트가 떠있는 상태인지 확인 </summary>
        public bool mLoading;
        GameObject mLoadingPanel;
        TweenPosition mTweenFirst;
        TweenPosition mTweenSecond;
        UISprite mLoadingSprite;
        UIAtlas mSelectAtlas;
        UIAtlas mOptionAtlas;

        #endregion

        public void Open() {
            mLoadingPanel = transform.Find("LoadingPanel").gameObject;
            mTweenFirst = mLoadingPanel.transform.Find("TweenFirst").GetComponent<TweenPosition>();
            mTweenSecond = mLoadingPanel.transform.Find("TweenSecond").GetComponent<TweenPosition>();
            mLoadingSprite = mLoadingPanel.transform.FindRecursive("Background").GetComponent<UISprite>();
            mSelectAtlas = mLoadingSprite.atlas;
            mOptionAtlas = transform.Find("OptionPanel").Find("mainUI").GetComponent<UISprite>().atlas;

            mDicPanel.Add((int)PanelType.Main, transform.Find("MainPanel").GetComponent<PanelBase>());
            mDicPanel.Add((int)PanelType.Select, transform.Find("SelectPanel").GetComponent<PanelBase>());
            mDicPanel.Add((int)PanelType.Option, transform.Find("OptionPanel").GetComponent<PanelBase>());
            mDicPanel.Add((int)PanelType.Ingame, transform.Find("IngamePanel").GetComponent<PanelBase>());
            mDicPanel.Add((int)PanelType.Result, transform.Find("ResultPanel").GetComponent<PanelBase>());

            mCurPanelType = PanelType.Main;

            // TODO : 에디터에서 시작 시 다른 패널이 켜져있을 수 있어서 확인함
            foreach (var panel in mDicPanel) {
                if (panel.Value.gameObject.activeSelf) {
                    mCurPanelType = (PanelType)panel.Key;
                }
            }

            mDicPanel[(int)mCurPanelType].Init();
        }

        /// <summary> 키보드 매니저에서 노멀 버튼이 눌렸다고 알려줌 </summary>
        public void OnClickBtnNormal() {
            if (mLoading)
                return;
            if (mCurPanelType != PanelType.None)
                mDicPanel[(int)mCurPanelType].OnClickBtnNormal();
        }

        /// <summary> 키보드 매니저에서 FX 버튼이 눌렸다고 알려줌 </summary>
        public void OnClickBtnFX() {
            if (mLoading)
                return;
            if (mCurPanelType != PanelType.None)
                mDicPanel[(int)mCurPanelType].OnClickBtnFX();
        }

        /// <summary> 키보드 매니저에서 스타트 버튼이 눌렸다고 알려줌 </summary>
        public void OnClickBtnStart() {
            if (mLoading)
                return;
            if (mCurPanelType != PanelType.None)
                mDicPanel[(int)mCurPanelType].OnClickBtnStart();
        }

        void Update() {
            ControlXAxis();
            ControlYAxis();
        }

        void ControlXAxis() {
            if (mLoading)
                return;

            float xMove = Input.GetAxis("Mouse X");
            if (xMove == 0)
                return;

            if (Mathf.Sign(xMove) != Mathf.Sign(mMouseX))
                mMouseX = 0f;

            mMouseX += mMouseXSpeed * xMove; //마우스의 움직임을 받음

            if (mMouseX >= MOUSE_MOVE_POINT || mMouseX <= -MOUSE_MOVE_POINT) {
                if (mCurPanelType != PanelType.None)
                    mDicPanel[(int)mCurPanelType].CursorXMoveProcess(mMouseX > 0);
                mMouseX = 0f;
            }

            //Debug.Log("mMouseX : " + mMouseX); //debug로 console창에서 값이 제대로 들어오는지 체크
            //Debug.Log("mCursorSelect.transform.parent : " + mCursorSelect.transform.parent);
        }

        void ControlYAxis() {
            if (mLoading)
                return;

            float yMove = Input.GetAxis("Mouse Y");
            if (yMove == 0)
                return;

            if (Mathf.Sign(yMove) != Mathf.Sign(mMouseY))
                mMouseY = 0f;

            mMouseY += mMouseYSpeed * yMove; //마우스의 움직임을 받음

            if (mMouseY >= MOUSE_MOVE_POINT || mMouseY <= -MOUSE_MOVE_POINT) {
                if (mCurPanelType != PanelType.None)
                    mDicPanel[(int)mCurPanelType].CursorYMoveProcess(mMouseY > 0);
                mMouseY = 0f;
            }

            //Debug.Log("mMouseX : " + mMouseX); //debug로 console창에서 값이 제대로 들어오는지 체크
            //Debug.Log("mCursorSelect.transform.parent : " + mCursorSelect.transform.parent);
        }

        /// <summary> <paramref name="type"/> 패널을 켜고 나머지 패널을 끄는 함수 </summary>
        /// <param name="type"> 켜고 싶은 패널 </param>
        /// <param name="HideOthers"> 다른 패널을 끄지 않고 싶을 때 사용 </param>
        public void ActivatePanel(PanelType type, bool HideOthers) {
            foreach (var panel in mDicPanel) {
                if (panel.Key == (int)type) {
                    if (!panel.Value.mInitialized)
                        panel.Value.Init();

                    panel.Value.gameObject.SetActive(true);
                } else if (HideOthers) {
                    panel.Value.gameObject.SetActive(false);
                }
            }

            mCurPanelType = type;
        }

        /// <summary> <paramref name="type"/> 패널을 끄고 싶을 때 사용 </summary>
        public void DeactivatePanel(PanelType type) {
            mDicPanel[(int)type].gameObject.SetActive(false);
        }

        /// <summary> <paramref name="type"/> 패널을 끄고 싶을 때 사용 </summary>
        public void DeactivateAllPanel() {
            foreach (var panel in mDicPanel) {
                panel.Value.gameObject.SetActive(false);
            }

            mCurPanelType = PanelType.None;
        }

        public PanelBase GetPanel(PanelType type) {
            PanelBase pb = mDicPanel[(int)type];
            if (!pb.mInitialized)
                pb.Init();

            return pb;
        }

        public void PlayLoading(bool bSelect = true) {
            mLoading = true;
            StartCoroutine(CoPlayLoading(bSelect));
        }

        IEnumerator CoPlayLoading(bool bSelect) {
            mLoadingSprite.atlas = bSelect ? mSelectAtlas : mOptionAtlas;
            mLoadingSprite.spriteName = bSelect ? "Music_Select_Alarm" : "Mode_Select_Alarm";
            mLoadingSprite.transform.SetParent(mTweenFirst.transform, false);
            mTweenFirst.PlayForward();
            yield return new WaitForSeconds(mTweenFirst.duration);

            mLoadingSprite.transform.SetParent(mTweenSecond.transform, false);
            mTweenSecond.PlayForward();
            yield return new WaitForSeconds(mTweenSecond.duration);

            mTweenFirst.ResetToBeginning();
            mLoadingSprite.transform.SetParent(mTweenFirst.transform, false);
            mTweenSecond.ResetToBeginning();

            yield return new WaitForSeconds(.1f);
            mLoading = false;
        }
    }
}