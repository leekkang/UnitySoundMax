using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class ResultPanel : PanelBase {
        UITexture mJacketImage;
        UILabel mLabelName;
        UILabel mLabelLevel;

        TweenPosition mTweenRight;
        UILabel mLabelScore;
        UILabel mLabelDmaxNum;
        UILabel mLabelMaxNum;
        UILabel mLabelMissNum;
        UISprite mSprGuage;

        TweenPosition mTweenLeft;
        UISprite mSprRank;
        UISprite mSprResult;

        bool mTweening;
        
        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            Transform tr = transform.Find("MusicInfo");
            mJacketImage = tr.Find("JacketTexture").GetComponent<UITexture>();
            mLabelName = tr.Find("name").GetComponent<UILabel>();
            mLabelLevel = tr.Find("level").GetComponent<UILabel>();

            mTweenRight = transform.Find("TweenRight").GetComponent<TweenPosition>();
            mLabelScore = mTweenRight.transform.FindRecursive("ScoreLabel").GetComponent<UILabel>();
            tr = mTweenRight.transform.Find("StatusPanel");
            mLabelDmaxNum = tr.Find("DmaxLabel").GetComponent<UILabel>();
            mLabelMaxNum = tr.Find("MaxLabel").GetComponent<UILabel>();
            mLabelMissNum = tr.Find("MissLabel").GetComponent<UILabel>();
            mSprGuage = tr.FindRecursive("HPRemain").GetComponent<UISprite>();

            mTweenLeft = transform.Find("TweenLeft").GetComponent<TweenPosition>();
            mSprRank = mTweenLeft.transform.FindRecursive("RankSprite").GetComponent<UISprite>();
            mSprResult = mTweenLeft.transform.FindRecursive("ResultSprite").GetComponent<UISprite>();
        }

        public void UpdateView() {
            mTweening = true;
            IngameEngine.inst.SetUIActivity(false);
            mTweenLeft.ResetToBeginning();
            mTweenLeft.enabled = false;
            mTweenRight.ResetToBeginning();
            mTweenRight.enabled = false;

            // music info
            MusicData data = IngameEngine.inst.mMusicData;
            mJacketImage.mainTexture = data.mJacketImage;
            mLabelName.text = data.mTitle;
            mLabelLevel.text = data.mLevel.ToString();

            // initialize for tween
            // score, status
            int score = Scoring.inst.CalculateCurrentScore();
            mLabelScore.text = "0";
            mLabelDmaxNum.text = "0";
            mLabelMaxNum.text = "0";
            mLabelMissNum.text = "0";

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

            // guage
            mSprGuage.fillAmount = 0;
            mSprGuage.color = IngameEngine.inst.mHealthUnder70;
            
            // user data
            int musicIndex = ((SelectPanel)GuiManager.inst.GetPanel(PanelType.Select)).mCurIndex;
            MusicSaveData tmpData = DataBase.inst.mUserData.GetMusicData(DataBase.inst.mMusicList[musicIndex]);
            MusicDifficultySaveData savedData = tmpData.mListPlayData[(int)data.mDifficulty];
            // save hiscore status
            if (savedData.mScore < score) {
                savedData.mScore = score;
                savedData.mDmaxNum = (int)Scoring.inst.categorizedHits[2];
                savedData.mMaxNum = (int)Scoring.inst.categorizedHits[1];
                savedData.mMissNum = (int)Scoring.inst.categorizedHits[0];

                savedData.mGuage = Scoring.inst.currentGauge;
                savedData.mClearStatus = rank == "F" ? 1 :
                                         Scoring.inst.comboState == 0 ? 2 :
                                         Scoring.inst.comboState == 1 ? 3 : 4;
            }
        }

        public void StartPlay() {
            StartCoroutine(CoPlay());
        }

        IEnumerator CoPlay() {
            mTweenLeft.PlayForward();
            mTweenRight.PlayForward();

            yield return new WaitForSeconds(mTweenLeft.duration);

            // score, status
            float score = Scoring.inst.CalculateCurrentScore();
            EasingFunction.Function func = EasingFunction.GetEasingFunction(EasingFunction.Ease.EaseOutCubic);

            float dmaxNum = (int)Scoring.inst.categorizedHits[2];
            float maxNum = (int)Scoring.inst.categorizedHits[1];
            float missNum = (int)Scoring.inst.categorizedHits[0];
            float maxGuage = Scoring.inst.currentGauge;
            bool bChangeGuage = false;

            float maxTime = 1f;
            float curTime = 0f;
            float interval = 0.05f;
            WaitForSeconds waitSecond = new WaitForSeconds(interval);
            while (maxTime + 0.03f >= curTime) {
                float t = curTime / maxTime;
                mLabelScore.text = ((int)Mathf.Round(func(0f, score, t))).ToString();
                mLabelDmaxNum.text = ((int)Mathf.Round(func(0f, dmaxNum, t))).ToString();
                mLabelMaxNum.text = ((int)Mathf.Round(func(0f, maxNum, t))).ToString();
                mLabelMissNum.text = ((int)Mathf.Round(func(0f, missNum, t))).ToString();

                mSprGuage.fillAmount = func(0f, maxGuage, t);
                if (mSprGuage.fillAmount >= 0.7f && !bChangeGuage) {
                    mSprGuage.color = IngameEngine.inst.mHealthOver70;
                    bChangeGuage = true;
                }

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