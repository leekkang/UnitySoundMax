using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class ResultPanel : PanelBase {
        TweenPosition mTweenTop;
        UITexture mJacketImage;
        UILabel mLabelName;
        UILabel mLabelLevel;

        TweenPosition mTweenRight;
        UILabel mLabelScore;
        UILabel mLabelCombo;
        UILabel mLabelDmaxNum;
        UILabel mLabelMaxNum;
        UILabel mLabelMissNum;
        UISprite mSprGuage;
        UILabel mLabelGuage;

        TweenPosition mTweenLeft;
        UISprite mSprRank;
        UISprite mSprResult;
        UILabel mLabelClearRate;

        TweenPosition mTweenBottom;
        UILabel mLabelScoreDelta;
        UILabel mLabelClearRateDelta;

        bool mTweening;
        int mClearRate;
        int mScoreDelta;
        int mClearRateDelta;
        
        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            mTweenTop = transform.Find("TweenTop").GetComponent<TweenPosition>();
            Transform tr = mTweenTop.transform.Find("MusicInfo");
            mJacketImage = tr.Find("JacketTexture").GetComponent<UITexture>();
            mLabelName = tr.Find("name").GetComponent<UILabel>();
            mLabelLevel = tr.Find("level").GetComponent<UILabel>();

            mTweenLeft = transform.Find("TweenLeft").GetComponent<TweenPosition>();
            mSprRank = mTweenLeft.transform.FindRecursive("RankSprite").GetComponent<UISprite>();
            mSprResult = mTweenLeft.transform.FindRecursive("ResultSprite").GetComponent<UISprite>();
            mLabelClearRate = mTweenLeft.transform.FindRecursive("RateLabel").GetComponent<UILabel>();

            mTweenRight = transform.Find("TweenRight").GetComponent<TweenPosition>();
            mLabelScore = transform.FindRecursive("ScoreLabel").GetComponent<UILabel>();
            mLabelCombo = transform.FindRecursive("ComboLabel").GetComponent<UILabel>();
            tr = mTweenRight.transform.Find("StatusPanel");
            mLabelDmaxNum = tr.Find("DmaxLabel").GetComponent<UILabel>();
            mLabelMaxNum = tr.Find("MaxLabel").GetComponent<UILabel>();
            mLabelMissNum = tr.Find("MissLabel").GetComponent<UILabel>();
            mSprGuage = tr.FindRecursive("HPRemain").GetComponent<UISprite>();
            mLabelGuage = tr.Find("HPLabel").GetComponent<UILabel>();

            mTweenBottom = transform.Find("TweenBottom").GetComponent<TweenPosition>();
            mLabelScoreDelta = mTweenBottom.transform.FindRecursive("TotalScoreLabel").GetComponent<UILabel>();
            mLabelClearRateDelta = mTweenBottom.transform.FindRecursive("ClearRateLabel").GetComponent<UILabel>();
        }

        public void UpdateView(bool forceEnd) {
            mTweening = true;
            IngameEngine.inst.SetUIActivity(false);
            ResetTween();

            // music info
            MusicData data = IngameEngine.inst.mMusicData;
            mJacketImage.mainTexture = data.mJacketImage;
            mLabelName.text = data.mTitle;
            mLabelLevel.text = data.mLevel.ToString();

            // initialize for tween
            // score, status
            int score = Scoring.inst.CalculateCurrentScore();
            mLabelScore.text = "0";
            mLabelCombo.text = "0";
            mLabelDmaxNum.text = "0";
            mLabelMaxNum.text = "0";
            mLabelMissNum.text = "0";
            int dmaxNum = Scoring.inst.categorizedHits[2];
            int maxNum = Scoring.inst.categorizedHits[1];
            int missNum = Scoring.inst.categorizedHits[0];

            // rank, result
            string rank = Scoring.inst.CalculateGrade(score);
            if (IngameEngine.inst.mForceEnd) {
                mSprRank.spriteName = "Rank_F";
                mSprResult.spriteName = "Result_Destroyed";
            } else {
                mSprRank.spriteName = "Rank_" + rank;
                mSprResult.spriteName = rank == "F" ? "Result_Destroyed" :
                                        Scoring.inst.comboState == 2 ? "Result_Perfect" :
                                        Scoring.inst.comboState == 1 ? "Result_Allcombo" : "Result_Clear";
            }
            TweenScale rankScale = mSprRank.GetComponent<TweenScale>();
            rankScale.ResetToBeginning();
            mSprRank.gameObject.SetActive(false);
            TweenAlpha resultAlpha = mSprResult.GetComponent<TweenAlpha>();
            resultAlpha.ResetToBeginning();
            mSprResult.gameObject.SetActive(false);
            mLabelClearRate.text = "0";

            // guage
            mSprGuage.fillAmount = 0;
            mSprGuage.color = IngameEngine.inst.mHealthUnder70;
            mLabelGuage.text = "0";
            
            // user data
            int musicIndex = ((SelectPanel)GuiManager.inst.GetPanel(PanelType.Select)).mCurIndex;
            MusicSaveData tmpData = DataBase.inst.mUserData.GetMusicData(DataBase.inst.mMusicList[musicIndex]);
            MusicDifficultySaveData savedData = tmpData.mListPlayData[(int)data.mDifficulty];
            mScoreDelta = score - savedData.mScore;
            // save hiscore status
            if (savedData.mScore < score) {
                savedData.mScore = score;
                savedData.mDmaxNum = dmaxNum;
                savedData.mMaxNum = maxNum;
                savedData.mMissNum = missNum;

                savedData.mGuage = Scoring.inst.currentGauge;
                savedData.mClearStatus = rank == "F" ? 1 :
                                         Scoring.inst.comboState == 0 ? 2 :
                                         Scoring.inst.comboState == 1 ? 3 : 4;
            }

            // information
            // 전체 노트랑 판정나온 개수가 달라서 임시방편으로 해결봄. 시간나면 확인
            int totalNote = forceEnd ? Scoring.inst.GetWholeNoteNum() : dmaxNum + maxNum + missNum;
            mClearRate = (int)((dmaxNum + maxNum * .5f) * 10000 / totalNote);
            mClearRateDelta = mClearRate - savedData.mClearRate;
            savedData.mClearRate = mClearRate;

            mLabelScoreDelta.text = "0";
            mLabelClearRateDelta.text = "0";
            //string rate = string.Format("{0}", (int)Mathf.Round(mClearRateDelta * 0.01f));
            //string rate = string.Format("{0:f2}%", mClearRateDelta * 0.01f);
        }

        void ResetTween() {
            mTweenTop.ResetToBeginning();
            mTweenTop.enabled = false;
            mTweenLeft.ResetToBeginning();
            mTweenLeft.enabled = false;
            mTweenRight.ResetToBeginning();
            mTweenRight.enabled = false;
            mTweenBottom.ResetToBeginning();
            mTweenBottom.enabled = false;
        }

        public void StartPlay() {
            StartCoroutine(CoPlay());
        }

        IEnumerator CoPlay() {
            mTweenTop.PlayForward();
            mTweenLeft.PlayForward();
            mTweenRight.PlayForward();
            mTweenBottom.PlayForward();

            yield return new WaitForSeconds(mTweenLeft.duration);

            EasingFunction.Function func = EasingFunction.GetEasingFunction(EasingFunction.Ease.EaseOutCubic);

            // score, status
            float score = Scoring.inst.CalculateCurrentScore();
            float combo = Scoring.inst.maxComboCounter;
            float clearRate = mClearRate * 0.01f;
            float clearRateDelta = mClearRateDelta * 0.01f;

            float dmaxNum = Scoring.inst.categorizedHits[2];
            float maxNum = Scoring.inst.categorizedHits[1];
            float missNum = Scoring.inst.categorizedHits[0];
            float maxGuage = Scoring.inst.currentGauge;
            bool bChangeGuage = false;

            string scoreDeltaFormat = mScoreDelta < 0 ? "{0}" : "+{0}";
            string clearRateDeltaFormat = clearRateDelta < 0 ? "{0}" : "+{0}";

            float maxTime = 1f;
            float curTime = 0f;
            float interval = 0.05f;
            WaitForSeconds waitSecond = new WaitForSeconds(interval);
            while (maxTime + 0.03f >= curTime) {
                float t = curTime / maxTime;
                mLabelScore.text = ((int)Mathf.Round(func(0f, score, t))).ToString();
                mLabelCombo.text = ((int)Mathf.Round(func(0f, combo, t))).ToString();
                mLabelDmaxNum.text = ((int)Mathf.Round(func(0f, dmaxNum, t))).ToString();
                mLabelMaxNum.text = ((int)Mathf.Round(func(0f, maxNum, t))).ToString();
                mLabelMissNum.text = ((int)Mathf.Round(func(0f, missNum, t))).ToString();

                mSprGuage.fillAmount = func(0f, maxGuage, t);
                if (mSprGuage.fillAmount >= 0.7f && !bChangeGuage) {
                    mSprGuage.color = IngameEngine.inst.mHealthOver70;
                    bChangeGuage = true;
                }
                mLabelGuage.text = ((int)Mathf.Round(mSprGuage.fillAmount * 100f)).ToString();

                mLabelClearRate.text = ((int)Mathf.Round(func(0f, clearRate, t))).ToString();
                mLabelScoreDelta.text = string.Format(scoreDeltaFormat, (int)Mathf.Round(func(0f, mScoreDelta, t)));
                mLabelClearRateDelta.text = string.Format(clearRateDeltaFormat, (int)Mathf.Round(func(0f, clearRateDelta, t)));

                curTime += interval;
                yield return waitSecond;
            }

            // rank, result
            TweenScale rankScale = mSprRank.GetComponent<TweenScale>();
            mSprRank.gameObject.SetActive(true);
            rankScale.PlayForward();
            yield return new WaitForSeconds(rankScale.duration);
            TweenAlpha resultAlpha = mSprResult.GetComponent<TweenAlpha>();
            mSprResult.gameObject.SetActive(true);
            resultAlpha.PlayForward();
            yield return new WaitForSeconds(resultAlpha.duration);

            mTweening = false;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {

        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            if (mTweening)
                return;

            SelectPanel sel = (SelectPanel)GuiManager.inst.GetPanel(PanelType.Select);
            sel.UpdateMusicMetaData();
            sel.PlayPreviewMusic();
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {
        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
        }
    }
}