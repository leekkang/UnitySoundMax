using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class SelectPanel : PanelBase {
        Transform mCursorSelect;
        Transform[] mBackPanel;
        GridScrollView mGridScrollView;

        int mMusicCount;
        int mCurIndex;
        bool mCompleteLoad;

        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            mMusicCount = DataBase.inst.mMusicList.Length;
            mCursorSelect = transform.FindRecursive("CursorSelect");
            mGridScrollView = transform.FindRecursive("UIGrid").GetComponent<GridScrollView>();
            
            mBackPanel = new Transform[mMusicCount];
            Transform jacketParent = mGridScrollView.transform;
            mBackPanel[0] = jacketParent.Find("BackPanel0");
            for (int i = 1; i < mMusicCount; i++) {
                mBackPanel[i] = Instantiate(mBackPanel[0], jacketParent);
                mBackPanel[i].name = "BackPanel" + i;
            }

            mGridScrollView.Init(mMusicCount);

            mCurIndex = 0;
            mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
            mCursorSelect.localPosition = Vector3.zero;
            mCompleteLoad = false;
        }

        public void Update() {
            if (!DataBase.inst.mOpenComplete)
                return;

            if (mCompleteLoad)
                return;

            for (int i = 0; i < mMusicCount; i++) {
                UITexture spr = mBackPanel[i].GetComponent<UITexture>();
                List<MusicData> data = DataBase.inst.mDicMusic[DataBase.inst.mMusicList[i]];
                bool complete = false;
                for (int j = data.Count-1; j >= 0; j--) {
                    if (!data[j].mDefaultJacketImage) {
                        spr.mainTexture = data[j].mJacketImage;
                        complete = true;
                        break;
                    }
                }

                if (!complete)
                    spr.mainTexture = data[0].mJacketImage;
            }

            mCompleteLoad = true;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {
            if (positiveDirection) {
                mCurIndex = ++mCurIndex % mMusicCount;
                mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
                mGridScrollView.MoveForward(mCurIndex);
            } else {
                mCurIndex = (--mCurIndex + mMusicCount) % mMusicCount; // 음수처리
                mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
                mGridScrollView.MoveBackward(mCurIndex);
            }
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            OptionPanel opt = (OptionPanel)GuiManager.inst.GetPanel(PanelType.Option);
            opt.UpdateView(DataBase.inst.mDicMusic[DataBase.inst.mMusicList[mCurIndex]]);
            GuiManager.inst.ActivatePanel(PanelType.Option, false);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {

        }
    }
}