using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class SelectPanel : PanelBase {
        Transform mCursorSelect;
        Transform[] mBackPanel;
        GridScrollView mGridScrollView;
        
        int mMusicCount;
        bool mCompleteLoad;
        public int mCurIndex { get; private set; }

        #region Music Information

        UITexture mAlbumJacket;
        UILabel mTitle;
        UILabel mArtist;
        UILabel mEffector;
        UILabel mIllustrator;
        UILabel mBpm;
        UILabel mHiScore;
        UILabel mLevelNovice;
        UILabel mLevelAdvanced;
        UILabel mLevelExhausted;
        UILabel mLevelInfinite;
        UISprite mClearStatus;
        GameObject mObjDifficultyInf;

        #endregion

        /// <summary> 해당 패널의 초기화에 필요한 정보를 로드하는 함수 </summary>
        public override void Init() {
            base.Init();

            Transform parent = transform.Find("Upside");
            mAlbumJacket = parent.Find("AlbumArt").GetComponent<UITexture>();
            mTitle = parent.Find("MusicTitle").GetComponent<UILabel>();
            mArtist = parent.Find("Artist").GetComponent<UILabel>();
            mEffector = parent.Find("Effector").GetComponent<UILabel>();
            mIllustrator = parent.Find("illustrator").GetComponent<UILabel>();
            mBpm = parent.Find("BPM").GetComponent<UILabel>();
            mHiScore = parent.Find("HI_Score").GetComponent<UILabel>();
            mClearStatus = parent.Find("ClearStatus").GetComponent<UISprite>();
            mLevelNovice = parent.Find("Difficulty_NOV").GetComponentInChildren<UILabel>(true);
            mLevelAdvanced = parent.Find("Difficulty_ADV").GetComponentInChildren<UILabel>(true);
            mLevelExhausted = parent.Find("Difficulty_EXH").GetComponentInChildren<UILabel>(true);
            mObjDifficultyInf = parent.Find("Difficulty_INF").gameObject;
            mLevelInfinite = mObjDifficultyInf.GetComponentInChildren<UILabel>(true);

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
            SetMusicInformation();
            PlayPreviewMusic();
            mCursorSelect.SetParent(mBackPanel[mCurIndex], false);
            mCursorSelect.localPosition = new Vector3(0f, 15f, 0f);
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

        /// <summary> Upside 오브젝트의 정보 갱신 </summary>
        void SetMusicInformation() {
            string music = DataBase.inst.mMusicList[mCurIndex];
            List<MusicData> listMusicData = DataBase.inst.mDicMusic[music];

            mLevelNovice.text = string.Format("{0:02}", listMusicData[(int)Difficulty.Novice].mLevel.ToString());
            mLevelAdvanced.text = string.Format("{0:02}", listMusicData[(int)Difficulty.Advanced].mLevel.ToString());
            mLevelExhausted.text = string.Format("{0:02}", listMusicData[(int)Difficulty.Exhausted].mLevel.ToString());
            bool bExistInf = listMusicData.Count > (int)Difficulty.Infinity;
            mLevelInfinite.text = bExistInf ? string.Format("{0:02}", listMusicData[(int)Difficulty.Infinity].mLevel.ToString()) : "00";

            UpdateMusicMetaData();
        }

        /// <summary> 곡의 메타데이터 갱신. 유저데이터 포함 </summary>
        public void UpdateMusicMetaData() {
            string music = DataBase.inst.mMusicList[mCurIndex];
            MusicSaveData savedData = DataBase.inst.mUserData.GetMusicData(music);
            MusicData musicData = DataBase.inst.mDicMusic[music][savedData.mDifficulty];

            mAlbumJacket.mainTexture = musicData.mJacketImage;
            mTitle.text = musicData.mTitle;
            mArtist.text = musicData.mArtist;
            mEffector.text = musicData.mEffector;
            mIllustrator.text = musicData.mIllustrator;
            mBpm.text = musicData.mBpm.ToString();

            MusicDifficultySaveData diffData = savedData.mListPlayData[savedData.mDifficulty];
            mHiScore.text = diffData.mScore.ToString();
            int clearStatus = diffData.mClearStatus;
            mClearStatus.spriteName = clearStatus == 4 ? "Result_Perfect" :
                                      clearStatus == 3 ? "Result_Allcombo" :
                                      clearStatus == 2 ? "Result_Clear" :
                                      clearStatus == 1 ? "Result_Destroyed" : "";
        }

        void PlayPreviewMusic() {
            string music = DataBase.inst.mMusicList[mCurIndex];
            MusicSaveData savedData = DataBase.inst.mUserData.GetMusicData(music);
            MusicData musicData = DataBase.inst.mDicMusic[music][savedData.mDifficulty];
            SoundManager.inst.PlayPreview(musicData);
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

            SetMusicInformation();
            PlayPreviewMusic();
        }

        /// <summary> Y축 마우스가 움직이면 해야할 일 </summary>
        public override void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnStart() {
            GuiManager.inst.PlayLoading("Option Select!", "from select");
            OptionPanel opt = (OptionPanel)GuiManager.inst.GetPanel(PanelType.Option);
            opt.UpdateView(DataBase.inst.mMusicList[mCurIndex]);
            GuiManager.inst.ActivatePanel(PanelType.Option, false);
        }

        /// <summary> 일반 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 해야 할 일 </summary>
        public override void OnClickBtnFX() {
            SoundManager.inst.StopPreviewNaturally();
            GuiManager.inst.ActivatePanel(PanelType.Main, true);
        }
    }
}