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

        Transform mTrCursor;
        int mCursorIndex;
        Transform mTrCursorDiff;
        int mCursorDiffIndex;
        int mMaxDiffIndex;

        float mCurSpeed;
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

            mCurSpeed = 1.0f;
        }

        public void UpdateView(List<MusicData> musicList) {
            mCurMusicList = musicList;
            mCursorIndex = 0;
            mCursorDiffIndex = 0;

            mMaxDiffIndex = musicList.Count - 1;
            for (int i = 0; i < 4; i++) {
                mListDifficulty[i].gameObject.SetActive(i < musicList.Count);
            }

            mBpm = musicList[0].mBpm;
            mLabelBpm.text = mBpm.ToString();
            mLableTitle.text = musicList[0].mTitle;

            mLabelCalculated.text = Mathf.Round(mCurSpeed * mBpm).ToString();
            mLabelLevel.text = musicList[mCursorDiffIndex].mLevel.ToString();
            mTrCursor.localPosition = mTrSpeed.localPosition;
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
                float prev = mCurSpeed;
                if (positiveDirection)
                    mCurSpeed = Mathf.Max(mCurSpeed - .25f, 1f);
                else
                    mCurSpeed = Mathf.Min(mCurSpeed + .25f, 5f);

                if (mCurSpeed != prev) {
                    mSprSpeed.spriteName = "Speed_" + (int)Mathf.Round(mCurSpeed * 100);
                    mLabelCalculated.text = Mathf.Round(mCurSpeed * mBpm).ToString();
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

            IngameEngine.inst.StartGame(mCurMusicList[mCursorDiffIndex], mCurSpeed);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
        }
    }
}