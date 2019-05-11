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
            mLabelDmaxNum.text = Scoring.inst.categorizedHits[2].ToString();
            mLabelMaxNum.text = Scoring.inst.categorizedHits[1].ToString();
            mLabelMissNum.text = Scoring.inst.categorizedHits[0].ToString();

            // rank, result
            if (IngameEngine.inst.mForceEnd) {
                mSprRank.spriteName = "Rank_F";
                mSprResult.spriteName = "Result_Destroyed";
            } else {
                string rank = Scoring.inst.CalculateGrade(score);
                mSprRank.spriteName = "Rank_" + rank;
                mSprResult.spriteName = rank == "F" ? "Result_Destroyed" :
                                        Scoring.inst.comboState == 2 ? "Result_Perfect" :
                                        Scoring.inst.comboState == 1 ? "Result_Allcombo" : "Result_Clear";
            }

            // guage
            mSprGuage.fillAmount = Scoring.inst.currentGauge;
            mSprGuage.color = mSprGuage.fillAmount < 0.7f ? IngameEngine.inst.mHealthUnder70 : IngameEngine.inst.mHealthOver70;
        }

        /// <summary> X축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorXMoveProcess(bool positiveDirection) {

        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            GuiManager.inst.ActivatePanel(PanelType.Select, true);
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