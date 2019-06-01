using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class OptionPanel : PanelBase {
        List<MusicData> mCurMusicList;

        Transform mTrSpeed;
        Transform mTrDifficulty;
        Transform[] mListDifficulty;
        Transform mTrStart;

        UILabel mLableTitle;
        UILabel mLabelBpm;
        UISprite mSprSpeed;
        UILabel mLabelCalculated;
        UILabel mLabelLevel;

        string mMusic;

        Transform mTrCursor;
        int mCursorIndex;
        Transform mTrCursorDiff;
        int mCursorDiffIndex;
        int mMaxDiffIndex;

        int mCurSpeedIndex;
        int mBpm;

        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            mTrSpeed = transform.Find("Speeds");
            mTrDifficulty = transform.Find("difficulties");
            mTrStart = transform.Find("start");
            mListDifficulty = new Transform[4];
            for (int i = 0; i < 4; i++)
                mListDifficulty[i] = mTrDifficulty.Find("difficulty" + i);

            mLableTitle = transform.Find("title").GetComponent<UILabel>();
            mLabelBpm = transform.Find("bpm").GetComponent<UILabel>();
            mSprSpeed = mTrSpeed.Find("speed").GetComponent<UISprite>();
            mLabelCalculated = mTrSpeed.FindRecursive("Label").GetComponent<UILabel>();
            mLabelLevel = mTrStart.Find("label").GetComponent<UILabel>();

            mTrCursor = transform.Find("CursorP");
            mTrCursorDiff = mTrDifficulty.Find("CursorDiff");
        }

        public void UpdateView(string music) {
            mMusic = music;
            mCurMusicList = DataBase.inst.mDicMusic[music];

            mCursorIndex = 0;
            mBpm = mCurMusicList[0].mBpm;
            mLabelBpm.text = mBpm.ToString();
            mLableTitle.text = mCurMusicList[0].mTitle;
            mMaxDiffIndex = mCurMusicList.Count - 1;
            for (int i = 0; i < 4; i++) {
                mListDifficulty[i].gameObject.SetActive(i < mCurMusicList.Count);
            }

            // load user data
            MusicSaveData savedData = DataBase.inst.mUserData.GetMusicData(mMusic);
            mCursorDiffIndex = savedData.mDifficulty;
            mCurSpeedIndex = savedData.mSpeed;

            mSprSpeed.spriteName = "Speed_" + (int)Mathf.Round(GetSpeed(mCurSpeedIndex) * 100);
            mLabelCalculated.text = Mathf.Round(GetSpeed(mCurSpeedIndex) * mBpm).ToString();
            mLabelLevel.text = mCurMusicList[mCursorDiffIndex].mLevel.ToString();
            mTrCursor.localPosition = mTrSpeed.localPosition;
            mTrCursorDiff.localPosition = mListDifficulty[mCursorDiffIndex].localPosition;
        }

        float GetSpeed(int index) {
            return 1f + index * 0.25f;
        }

        void SetUserData() {
            MusicSaveData savedData = DataBase.inst.mUserData.GetMusicData(mMusic);
            savedData.mDifficulty = mCursorDiffIndex;
            savedData.mSpeed = mCurSpeedIndex;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {
            if (positiveDirection)
                mCursorIndex = Mathf.Min(++mCursorIndex, 2);
            else
                mCursorIndex = Mathf.Max(--mCursorIndex, 0);

            mTrCursor.localPosition = mCursorIndex == 0 ? mTrSpeed.localPosition : 
                                    mCursorIndex == 1 ? mTrDifficulty.localPosition : mTrStart.localPosition;
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {
            if (mCursorIndex == 0) {
                int prev = mCurSpeedIndex;
                if (positiveDirection)
                    mCurSpeedIndex = Mathf.Max(--mCurSpeedIndex, 0);
                else
                    mCurSpeedIndex = Mathf.Min(++mCurSpeedIndex, 16);

                if (mCurSpeedIndex != prev) {
                    mSprSpeed.spriteName = "Speed_" + (int)Mathf.Round(GetSpeed(mCurSpeedIndex) * 100);
                    mLabelCalculated.text = Mathf.Round(GetSpeed(mCurSpeedIndex) * mBpm).ToString();
                }
            } else if (mCursorIndex == 1) {
                if (positiveDirection)
                    mCursorDiffIndex = Mathf.Max(mCursorDiffIndex - 1, 0);
                else
                    mCursorDiffIndex = Mathf.Min(mCursorDiffIndex + 1, mMaxDiffIndex);

                mTrCursorDiff.localPosition = mListDifficulty[mCursorDiffIndex].localPosition;
                mLabelLevel.text = mCurMusicList[mCursorDiffIndex].mLevel.ToString();
            }
        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            if (mCursorIndex != 2)
                return;

            SetUserData();
            IngameEngine.inst.StartGame(mCurMusicList[mCursorDiffIndex], GetSpeed(mCurSpeedIndex));
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
            GuiManager.inst.PlayLoading("select Music!", "from option");
            SelectPanel sel = (SelectPanel)GuiManager.inst.GetPanel(PanelType.Select);
            SetUserData();
            sel.UpdateMusicMetaData();
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
        }
    }
}