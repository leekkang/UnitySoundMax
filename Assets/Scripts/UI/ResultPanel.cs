using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class ResultPanel : PanelBase {
        UITexture mJacketImage;
        UILabel mLabelName;
        UILabel mLabelLevel;

        UILabel mLabelScore;
        UILabel mLabelDmaxNum;
        UILabel mLabelMaxNum;
        UILabel mLabelMissNum;
        UISprite mSprGuage;

        UISprite mSprRank;
        UISprite mSprResult;
        
        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            Transform tr = transform.Find("MusicInfo");
            mJacketImage = tr.Find("JacketTexture").GetComponent<UITexture>();
            mLabelName = tr.Find("name").GetComponent<UILabel>();
            mLabelLevel = tr.Find("level").GetComponent<UILabel>();

            mLabelScore = transform.FindRecursive("ScoreLabel").GetComponent<UILabel>();
            tr = transform.Find("StatusPanel");
            mLabelDmaxNum = tr.Find("DmaxLabel").GetComponent<UILabel>();
            mLabelMaxNum = tr.Find("MaxLabel").GetComponent<UILabel>();
            mLabelMissNum = tr.Find("MissLabel").GetComponent<UILabel>();
            mSprGuage = tr.FindRecursive("HPRemain").GetComponent<UISprite>();

            mSprRank = transform.FindRecursive("RankSprite").GetComponent<UISprite>();
            mSprResult = transform.FindRecursive("ResultSprite").GetComponent<UISprite>();
        }

        public void UpdateView() {
            IngameEngine.inst.SetUIActivity(false);

            // music info
            MusicData data = IngameEngine.inst.mMusicData;
            mJacketImage.mainTexture = data.mJacketImage;
            mLabelName.text = data.mTitle;
            mLabelLevel.text = data.mLevel.ToString();

            // score, status
            int score = Scoring.inst.CalculateCurrentScore();

            mLabelScore.text = score.ToString();
            mLabelDmaxNum.text = ((int)Scoring.inst.categorizedHits[2]).ToString();
            mLabelMaxNum.text = ((int)Scoring.inst.categorizedHits[1]).ToString();
            mLabelMissNum.text = ((int)Scoring.inst.categorizedHits[0]).ToString();

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

            // guage
            mSprGuage.fillAmount = Scoring.inst.currentGauge;
            mSprGuage.color = mSprGuage.fillAmount < 0.7f ? IngameEngine.inst.mHealthUnder70 : IngameEngine.inst.mHealthOver70;
            
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

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {

        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            SelectPanel sel = (SelectPanel)GuiManager.inst.GetPanel(PanelType.Select);
            sel.UpdateMusicMetaData();
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {
        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
            SelectPanel sel = (SelectPanel)GuiManager.inst.GetPanel(PanelType.Select);
            sel.UpdateMusicMetaData();
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
        }
    }
}