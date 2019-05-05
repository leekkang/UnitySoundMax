using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class SelectPanel : PanelBase {
        Transform mCursorSelect;
        Transform[] mBackPanel;

        int mMusicCount;
        int mCurIndex;
        bool mCompleteLoad;

        void Start() {
            mMusicCount = DataBase.inst.mMusicList.Length;
            mCursorSelect = transform.FindRecursive("CursorSelect");
            mBackPanel = new Transform[mMusicCount];
            Transform jacketParent = transform.FindRecursive("UIGrid");
            for (int i = 0; i < mMusicCount; i++) {
                mBackPanel[i] = jacketParent.Find("BackPanel" + i);
            }

            mCurIndex = 0;
            mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
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
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {
            if (positiveDirection) {
                mCurIndex = ++mCurIndex % mMusicCount;
                mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
            } else {
                mCurIndex = (--mCurIndex + mMusicCount) % mMusicCount; // 음수처리
                mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
            }
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            MusicData data = DataBase.inst.mDicMusic[DataBase.inst.mMusicList[mCurIndex]].Find((x) => x.mDifficulty == Difficulty.Extended);
            Scoring.inst.autoplay = true;
            KeyboardManager.inst.mIsLaserUseMouse = true;
            IngameEngine.inst.StartGame(data, 5f);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {

        }
    }
}