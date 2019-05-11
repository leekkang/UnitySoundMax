using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

namespace SoundMax {
    public class IngameEngine : Singleton<IngameEngine> {
        /// <summary> 트랙 노트 간격 조절용 </summary>
        public const float TRACK_HEIGHT_INTERVAL = 0.01f;
        /// <summary> 트랙의 버튼홈 너비 </summary>
        public const float TRACK_NOTE_WIDTH = 180f;
        /// <summary> 리소스에 저장되어있는 버튼 높이 </summary>
        public const int TRACK_NOTE_HEIGHT = 50;
        /// <summary> BPM에 따른 버튼 높이 증분값 </summary>
        public const float NOTE_HEIGHT_INCREMENT_FROM_BPM = 10f;
        /// <summary> 가로로 누운 레이저의 너비 증분값 </summary>
        public const float LASER_NOTE_WIDTH_INCREMENT = 2f;
        /// <summary> center를 기준으로 하는 트랙의 크기 </summary>
        public const float TRACK_WIDTH = 900f;
        /// <summary> Laser_Guide를 표시할 간격 </summary>
        public const float LASER_START_INTERVAL = 1000f;
        /// <summary> 오브젝트를 렌더링 할 시간 간격. 현재시간 + 변수 이내에 있는 오브젝트만 setactive(true) 가 된다. </summary>
        public const float OBJECT_ACTIVE_INTERVAL = 10000f;

        public bool mPlaying;
        public bool mForceEnd;
        public bool m_paused;
        public bool m_ended;
        public float mSpeed = 1f;

        // Map object approach speed, scaled by BPM
        float m_hispeed = 1.0f;

        // Current lane toggle status
        bool m_hideLane = false;

        // Use m-mod and what m-mod speed
        bool m_usemMod = false;
        bool m_usecMod = false;
        float m_modSpeed = 400;

        public MusicData mMusicData;

        Beatmap m_beatmap;
        Scoring m_scoring;
        PlaybackEngine m_playback;              // Beatmap playback manager (object and timing point selector)
        AudioEngine m_audioPlayback;            // Audio playback manager (music and FX))

        int m_audioOffset = 0;                  // Applied audio offset
        int m_fpsTarget = 0;

        Transform mTrackAnchor;                 // 움직이는 트랙 오브젝트
        Transform mStaticTrack;                 // 움직이지 않는 트랙 오브젝트
        Transform mJudgeLine;                   // 판정선 오브젝트
        Transform mTrackerPanel;                // 파티클에 가려지지 않아야 하는 친구들의 패널

        UITexture mJacketImage;                 // 좌측 상단 앨범 재킷
        UILabel mDifficultyLabel;               // 좌측 상단 난이도
        UILabel mLabelLevel;                    // 좌측 상단 곡 레벨
        UILabel mLabelBpm;                      // 좌측 상단 곡 bpm
        UILabel mLabelSpeed;                    // 좌측 상단 곡 스피드
        UILabel mLabelScore;                    // 우측 상단 스코어 보드
        UILabel mLabelBoardCombo;               // 우측 상단 콤보 보드
        UISprite mSprHealthBar;                 // 우측의 체력 게이지
        bool mIsHPOver70;                       // 컬러값을 바꿔야 하는지 확인
        public Color mHealthUnder70 = new Color(118f / 255f, 1f, 1f, 1f);  // 체력 70% 미만일 때의 컬러값
        public Color mHealthOver70 = Color.white;      // 체력 70% 이상일 때의 컬러값

        UILabel mLabelCombo;                     // 트랙 가운데 뜨는 콤보 텍스트
        TweenScale mComboTweenScale;            // 트랙 가운데 뜨는 콤보의 스케일 트윈
        DisappearObject mComboDisappearScript;  // 트랙 가운데 뜨는 콤보의 알파 트윈

        // 결과창 뜰 때 꺼줘야 하는 오브젝트
        public GameObject mSprMusicInfo;
        public GameObject mSprScoreBoard;

        // The camera watching the playfield
        CameraMotion m_camera;

        // Currently active timing point
        TimingPoint m_currentTiming;
        // Currently visible gameplay objects
        int mInvisibleObjIndex;                  // 현재 카메라에 보이지 않는 첫번째 오브젝트의 인덱스
        int m_lastMapTime;                       // 현재 오디오 재생 시간

        // Combo gain animation
        //Timer m_comboAnimation;
        Transform mAudioRoot;
        AudioSource m_slamSample;
        AudioSource[] m_clickSamples = new AudioSource[2];
        List<AudioSource> m_fxSamples = new List<AudioSource>();

        GameFlags m_flags;

        // pool
        public ParticleSystem[] mNormalHitEffect;
        public ParticleSystem[] mHoldHitEffect;
        public ParticleSystem[] mLaserHitEffect;
        public ParticleSystem[] mSlamHitEffect;
        public DisappearObject[] mLaserNobeObject = new DisappearObject[2];
        public DisappearObject[] mJudgeObject = new DisappearObject[8];         // 판정 오브젝트
        public GameObject[] mNoteClickObject = new GameObject[6];           // 노트 누를 때 뒤에 나오는 하얀 이펙트

        public void Open() {
            m_beatmap = new Beatmap();
            m_playback = new PlaybackEngine();
            m_audioPlayback = new AudioEngine();
            
            // 트랙 관련 게임 오브젝트
            mAudioRoot = transform.Find("Audio");
            m_camera = transform.FindRecursive("CameraAnchor").GetComponent<CameraMotion>();
            mTrackAnchor = transform.FindRecursive("Anchor");
            mStaticTrack = mTrackAnchor.parent.Find("Track");
            mJudgeLine = transform.FindRecursive("JudgeLine");
            mTrackerPanel = mJudgeLine.Find("TrackerPanel");

            // 오버트랙 이미지 관련
            mLabelCombo = transform.FindRecursive("ComboText").GetComponent<UILabel>();
            mComboTweenScale = mLabelCombo.GetComponent<TweenScale>();
            mComboDisappearScript = mLabelCombo.GetComponent<DisappearObject>();
            mComboDisappearScript.Open(1f, 0.3f);

            // 백그라운드 이미지 관련
            Transform scoreBoard = transform.FindRecursive("Scoreboard");
            mLabelScore = scoreBoard.Find("ScoreLabel").GetComponent<UILabel>();
            mLabelBoardCombo = scoreBoard.Find("ComboLabel").GetComponent<UILabel>();
            mSprHealthBar = transform.FindRecursive("HPRemain").GetComponent<UISprite>();
            Transform musicInfo = transform.FindRecursive("MusicInfo");
            mJacketImage = musicInfo.Find("JacketImage").GetComponent<UITexture>();
            mDifficultyLabel = musicInfo.Find("MusicDifficulty").GetComponent<UILabel>();
            mLabelLevel = musicInfo.Find("MusicLevel").GetComponent<UILabel>();
            mLabelBpm = musicInfo.Find("MusicBpm").GetComponent<UILabel>();
            mLabelSpeed = musicInfo.Find("MusicSpeed").GetComponent<UILabel>();

            mSprMusicInfo = musicInfo.gameObject;
            mSprScoreBoard = scoreBoard.gameObject;

            // 샘플 오디오 로드
            m_slamSample = mAudioRoot.Find("SlamSound").GetComponent<AudioSource>();
            m_clickSamples[0] = mAudioRoot.Find("ClickSound1").GetComponent<AudioSource>();
            m_clickSamples[1] = mAudioRoot.Find("ClickSound2").GetComponent<AudioSource>();

            string rootPath = Path.Combine(Application.streamingAssetsPath, "fxAudio");
            string audioPath = Path.Combine(rootPath, "laser_slam.wav").Trim();
            DataBase.inst.LoadAudio(audioPath, (audio) => {
                m_slamSample.clip = audio;
            });
            audioPath = Path.Combine(rootPath, "click-01.wav").Trim();
            DataBase.inst.LoadAudio(audioPath, (audio) => {
                m_clickSamples[0].clip = audio;
            });
            audioPath = Path.Combine(rootPath, "click-02.wav").Trim();
            DataBase.inst.LoadAudio(audioPath, (audio) => {
                m_clickSamples[1].clip = audio;
            });

            // 풀링 오브젝트 생성
            CreatePoolObject();
        }

        /// <summary> 곡 정보, 스코어보드 ui의 상태를 변경하는 함수 </summary>
        public void SetUIActivity(bool active) {
            mSprMusicInfo.SetActive(active);
            mSprScoreBoard.SetActive(active);
        }

        /// <summary>
        /// 해당 데이터로 게임을 실행한다.
        /// </summary>
        /// <param name="data"> 필요한 게임 데이터 </param>
        /// <param name="speed"> 게임 속도 </param>
        public void StartGame(MusicData data, float speed) {
            mSpeed = speed;
            mMusicData = data;
            mJacketImage.mainTexture = data.mJacketImage;
            mDifficultyLabel.text = data.mDifficulty.ToString();
            mLabelLevel.text = data.mLevel.ToString();
            mLabelBpm.text = data.mBpm.ToString();
            mLabelSpeed.text = mSpeed.ToString();

            SetUIActivity(true);
            StartCoroutine(CoLoadData(data));
        }

        public void Restart() {
            SetUIActivity(true);
            StartCoroutine(CoLoadData(mMusicData));
        }

        IEnumerator CoLoadData(MusicData data) {
            m_beatmap.Load(data, false);
            m_scoring = Scoring.inst;

            BeatmapSetting mapSettings = m_beatmap.mSetting;
            
            int firstObjectTime = m_beatmap.mListObjectState[0].mTime;
            int idx = m_beatmap.mListObjectState.Count - 1;
            while (m_beatmap.mListObjectState[idx].mType == ButtonType.Event &&
                    m_beatmap.mListObjectState[idx] != m_beatmap.mListObjectState[0]) {
                idx--;
            }

            ObjectDataBase lastObj = m_beatmap.mListObjectState[idx];
            int lastObjectTime = m_beatmap.mListObjectState[idx].mTime;
            if (lastObj.mType == ButtonType.Hold) {
                HoldButtonData lastHold = (HoldButtonData)lastObj;
                lastObjectTime += lastHold.mDuration;
            } else if (lastObj.mType == ButtonType.Laser) {
                LaserData lastHold = (LaserData)lastObj;
                lastObjectTime += lastHold.mDuration;
            }

            // mmod, cmod는 고려하지 않음

            // 아래부터는 필요한 리소스 로드 타임

            // Initialize input/scoring
            if (!InitGameplay()) {
                Debug.LogError("Fail to initialize Gameplay");
                yield break;
            }

            // Load beatmap audio
            if (!m_audioPlayback.Init(m_playback, data))
                yield break;
            yield return new WaitUntil(() => m_audioPlayback.bCompleteInit);

            ApplyAudioLeadin();

            // 오디오 싱크. 나중에 확인
            // Load audio offset
            //m_audioOffset = g_gameConfig.GetInt(GameConfigKeys::GlobalOffset);
            //m_playback.audioOffset = m_audioOffset;

            yield return CoInitSFX();

            // Do this here so we don't get input events while still loading
            m_scoring.SetFlags(m_flags);
            m_scoring.SetPlayback(m_playback);
            //m_scoring.SetInput(&g_input);
            m_scoring.Reset(); // Initialize

            // Create Note, particle, etc...
            CreateObject();

            // start audio play == start game!
            yield return CoPlayGame();
        }

        /// <summary> 샘플오디오 로드 </summary>
        IEnumerator CoInitSFX() {
            string rootPath = Path.Combine(Application.streamingAssetsPath, "fxAudio");
            string audioPath;

            for (int i = 0; i < m_fxSamples.Count; i++)
                Destroy(m_fxSamples[i].gameObject);

            GameObject obj = Resources.Load("Prefab/SFXSound") as GameObject;

            GameObject copy;
            List<string> samples = m_beatmap.mListSamplePath;
            for (int i = 0; i < samples.Count; i++) {
                copy = Instantiate(obj, mAudioRoot);
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localScale = Vector3.one;
                m_fxSamples.Add(copy.GetComponent<AudioSource>());
            }

            bool bLoad = false;
            for (int i = 0; i < samples.Count; i++) {
                audioPath = Path.Combine(rootPath, samples[i]).Trim();
                bLoad = false;
                if (!DataBase.inst.LoadAudio(audioPath, (audio) => {
                    m_fxSamples[i].clip = audio;
                    bLoad = true;
                })) {
                    Debug.LogError("Fail to initialize SFX file");
                    yield break;
                }
                yield return new WaitUntil(() => bLoad);
            }
        }

        IEnumerator CoPlayGame() {
            WaitForEndOfFrame waitFrame = new WaitForEndOfFrame();
            yield return waitFrame;
            GuiManager.inst.DeactivateAllPanel();

            while (!m_ended) {
                Tick(Time.deltaTime);
                yield return waitFrame;
            }
        }

        // Wait before start of map
        void ApplyAudioLeadin() {
            // Select the correct first object to set the intial playback position
            // if it starts before a certain time frame, the song starts at a negative time (lead-in)
            int index = 0;
            while (m_beatmap.mListObjectState[index].mType == ButtonType.Event && index < m_beatmap.mListObjectState.Count) {
                index++;
            }

            ObjectDataBase firstObj = m_beatmap.mListObjectState[index];
            m_lastMapTime = 0;
            int firstObjectTime = firstObj.mTime;
            if (firstObjectTime < 3000) {
                // Set start time
                m_lastMapTime = Math.Max(0, firstObjectTime - 5000);
                m_audioPlayback.SetPosition(m_lastMapTime);
            }

            // Reset playback
            m_playback.Reset(m_lastMapTime);
        }

        /// <summary> 게임 시작 전 초기화를 담당하는 함수 </summary>
        bool InitGameplay() {
            mPlaying = false;
            m_paused = false;
            m_ended = false;
            mForceEnd = false;

            // Playback and timing
            m_playback.SetBeatmap(m_beatmap);
            m_playback.OnEventChanged = OnEventChanged;
            m_playback.OnLaneToggleChanged = OnLaneToggleChanged;
            m_playback.OnFXBegin = OnFXBegin;
            m_playback.OnFXEnd = OnFXEnd;
            m_playback.OnLaserAlertEntered = OnLaserAlertEntered;
            m_playback.Reset(0);

            // Set camera start position
            //m_camera.pLaneZoom = m_playback.GetZoom(0);
            //m_camera.pLanePitch = m_playback.GetZoom(1);
            //m_camera.pLaneOffset = m_playback.GetZoom(2);
            //m_camera.pLaneTilt = m_playback.GetZoom(3);

            // If c-mod is used
            if (m_usecMod) {
                m_playback.OnTimingPointChanged = OnTimingPointChanged;
            }
            m_playback.cMod = m_usecMod;
            m_playback.cModSpeed = m_hispeed * (float)m_playback.GetCurrentTimingPoint().GetBPM();
            // Register input bindings
            m_scoring.OnButtonMiss = OnButtonMiss;
            m_scoring.OnLaserSlamHit = OnLaserSlamHit;
            m_scoring.OnButtonHit = OnButtonHit;
            m_scoring.OnComboChanged = OnComboChanged;
            m_scoring.OnObjectHold = OnObjectHold;
            m_scoring.OnObjectReleased = OnObjectReleased;
            m_scoring.OnScoreChanged = OnScoreChanged;

            m_playback.hittableObjectEnter = m_scoring.missHitTime;
            m_playback.hittableObjectLeave = m_scoring.goodHitTime;

            mIsHPOver70 = false;
            mSprHealthBar.color = mHealthUnder70;

            for (int i = 0; i < 6; i++)
                mNoteClickObject[i].SetActive(false);

            return true;
        }

        /// <summary>
        /// 인게임에서 계속 사용되는 오브젝트들을 생성 및 초기화
        /// </summary>
        void CreatePoolObject() {
            // make effect and judge object pool
            mNormalHitEffect = new ParticleSystem[30];
            GameObject eff = Resources.Load("Prefab/EffectNormalHit") as GameObject;
            Vector3 pos = new Vector3(-2000f, 0f, 0f);
            for (int i = 0; i < 30; i++) {
                ParticleSystem particle = Instantiate(eff, mJudgeLine).GetComponent<ParticleSystem>();
                particle.name = string.Format("EffectNormalHit_{0}", i);
                particle.transform.position = pos;
                mNormalHitEffect[i] = particle;
            }
            mHoldHitEffect = new ParticleSystem[10];
            eff = Resources.Load("Prefab/EffectHoldHit") as GameObject;
            for (int i = 0; i < 10; i++) {
                ParticleSystem particle = Instantiate(eff, mJudgeLine).GetComponent<ParticleSystem>();
                particle.name = string.Format("EffectHoldHit_{0}", i);
                particle.transform.position = pos;
                particle.Stop();
                particle.gameObject.SetActive(false);
                mHoldHitEffect[i] = particle;
            }
            mLaserHitEffect = new ParticleSystem[4];
            eff = Resources.Load("Prefab/EffectLaserHit") as GameObject;
            for (int i = 0; i < 4; i++) {
                ParticleSystem particle = Instantiate(eff, mJudgeLine).GetComponent<ParticleSystem>();
                particle.name = string.Format("EffectLaserHit_{0}", i);
                particle.transform.position = pos;
                particle.Stop();
                particle.gameObject.SetActive(false);
                mLaserHitEffect[i] = particle;
            }
            mSlamHitEffect = new ParticleSystem[30];
            eff = Resources.Load("Prefab/EffectSlam") as GameObject;
            for (int i = 0; i < 30; i++) {
                ParticleSystem particle = Instantiate(eff, mJudgeLine).GetComponent<ParticleSystem>();
                particle.name = string.Format("EffectSlam_{0}", i);
                particle.transform.position = pos;
                mSlamHitEffect[i] = particle;
            }

            // make laser nobe object
            GameObject nobeObject = Resources.Load("Prefab/ObjectLaserNobe") as GameObject;
            mLaserNobeObject[0] = Instantiate(nobeObject, mTrackerPanel).GetComponent<DisappearObject>();
            mLaserNobeObject[0].Open(1f, 0.3f);
            mLaserNobeObject[0].GetComponent<UISprite>().spriteName = "Nobe_Tracker_L";
            mLaserNobeObject[0].Move(0, false);

            mLaserNobeObject[1] = Instantiate(nobeObject, mTrackerPanel).GetComponent<DisappearObject>();
            mLaserNobeObject[1].Open(1f, 0.3f);
            mLaserNobeObject[1].GetComponent<UISprite>().spriteName = "Nobe_Tracker_R";
            mLaserNobeObject[1].Move(1, false);

            // make judgement object
            GameObject judgeObject = Resources.Load("Prefab/ObjectJudgement") as GameObject;
            pos = new Vector3(0f, 152f, 0f);
            Quaternion camera_angle = Quaternion.Euler(-68f, 0f, 0f);
            for (int i = 0; i < 8; i++) {
                mJudgeObject[i] = Instantiate(judgeObject, mTrackerPanel).GetComponent<DisappearObject>();
                mJudgeObject[i].transform.localPosition = pos;
                mJudgeObject[i].transform.localRotation = camera_angle;
                mJudgeObject[i].Open(0, 0.3f);
                mJudgeObject[i].GetComponent<UISprite>().spriteName = "JudgementObject_" + i;
                if (i < 4)
                    mJudgeObject[i].Move(0.2f * (1 + i));
                else if (i < 6)
                    mJudgeObject[i].Move(0.3f + 0.4f * (i - 4));
                else
                    mJudgeObject[i].Move(i - 6);
            }

            // make note click effect object
            GameObject clickObject = Resources.Load("Prefab/NoteClickEff") as GameObject;
            GameObject clickFxObject = Resources.Load("Prefab/NoteFXClickEff") as GameObject;
            pos = Vector3.zero;
            for (int i = 0; i < 6; i++) {
                pos.x = GetButtonXPos(i);
                mNoteClickObject[i] = Instantiate(i < 4 ? clickObject : clickFxObject, mTrackerPanel);
                mNoteClickObject[i].transform.localPosition = pos;
                mNoteClickObject[i].name = string.Format("NoteClickEffect_{0}", i);
                mNoteClickObject[i].SetActive(false);
            }
        }

        /// <summary> CreateObject 함수에서 생성한 오브젝트 중 노트를 제외한 오브젝트 전체 </summary>
        List<Transform> mListObj = new List<Transform>();
        /// <summary> 더이상 화면에 보이지 않을 오브젝트를 다른 곳으로 옮기기 위한 지표 </summary>
        int mListObjIndex = 0;
        /// <summary> 게임 실행에 필요한 오브젝트를 생성하는 함수 </summary>
        void CreateObject() {
            for (int i = 0; i < mListObj.Count; i++)
                Destroy(mListObj[i].gameObject);
            mListObj.Clear();
            mListObjIndex = 0;

            // init track

            // 트랙 세우기
            //m_camera.transform.parent = mTrackAnchor.parent;
            //mTrackAnchor.parent.localRotation = Quaternion.Euler(Vector3.zero);
            Vector3 pos = Vector3.zero;
            mTrackAnchor.localPosition = pos;

            // make track highlight
            GameObject trackHighlight = Resources.Load("Prefab/TrackHighlight") as GameObject;
            int nEndTime = m_beatmap.mListObjectState[m_beatmap.mListObjectState.Count - 1].mTime;
            TimingPoint curTiming = m_playback.GetCurrentTimingPoint();
            double interval = curTiming.mBeatDuration;
            double length = curTiming.GetBPM() * TRACK_HEIGHT_INTERVAL * mSpeed;
            pos = Vector3.zero;
            for (double i = interval; i < nEndTime; i += interval) {
                Transform tr = Instantiate(trackHighlight, mTrackAnchor).transform;
                pos.Set(0f, (float)(length * i), 0f);
                tr.localPosition = pos;

                mListObj.Add(tr);
            }

            // make button and laser
            // TODO : bpm 변경되는 곡 구현하지 않음
            //Dictionary<double, float> dicBpmChange = new Dictionary<double, float>();
            GameObject fxNote = Resources.Load("Prefab/NoteFX") as GameObject;
            GameObject normalNote = Resources.Load("Prefab/Note") as GameObject;
            GameObject laserNote = Resources.Load("Prefab/NoteLaser") as GameObject;
            GameObject laserCorner = Resources.Load("Prefab/NoteLaserCorner") as GameObject;
            GameObject laserStart = Resources.Load("Prefab/NoteLaserStart") as GameObject;

            float[] mLastLaserEndTime = new float[2]{ 0f, 0f};   // 레이저가 끝난 시간을 저장. laserStart를 출력하기 위해서임
            List<ObjectDataBase> listState = m_beatmap.mListObjectState;
            pos = Vector3.zero;
            mInvisibleObjIndex = 0;

            for (int i = 0; i < listState.Count; i++) {
                ObjectDataBase objBase = listState[i];
                TimingPoint timing = m_playback.GetTimingPointAt(objBase.mTime, false);
                float bpmPerLength = (float)timing.GetBPM() * TRACK_HEIGHT_INTERVAL * mSpeed;
                float yPos = bpmPerLength * objBase.mTime;
                int normalButtonHeight = (int)(bpmPerLength * NOTE_HEIGHT_INCREMENT_FROM_BPM);

                if (objBase.mType == ButtonType.Single || objBase.mType == ButtonType.Hold) {
                    NormalButtonData btnNormal = (NormalButtonData)objBase;
                    GameObject obj = Instantiate(btnNormal.mIndex >= 4 ? fxNote : normalNote, mTrackAnchor);
                    obj.transform.parent = mTrackAnchor;

                    // TODO : bpm 변경되는 곡 구현하지 않음
                    //if (!dicBpmChange.ContainsKey(timing.GetBPM())) {
                    //    dicBpmChange.Add(timing.GetBPM(), timing.mTime);
                    //}
                    //float yLoc = 0;
                    //if (dicBpmChange.Count > 1) {
                    //    foreach(var key in dicBpmChange) {

                    //    }
                    //}

                    float xPos = GetButtonXPos(btnNormal.mIndex);
                    pos.Set(xPos, yPos, 0f);
                    obj.transform.localPosition = pos;

                    if (objBase.mType == ButtonType.Hold) {
                        HoldButtonData btnHold = (HoldButtonData)objBase;
                        obj.GetComponent<UISprite>().height = (int)(TRACK_NOTE_HEIGHT + bpmPerLength * btnHold.mDuration);
                    } else {
                        obj.GetComponent<UISprite>().height = Math.Max(normalButtonHeight, TRACK_NOTE_HEIGHT);
                    }
                    objBase.mNote = obj.GetComponent<UISprite>();

                    if (objBase.mTime > OBJECT_ACTIVE_INTERVAL) {
                        obj.SetActive(false);
                        if (mInvisibleObjIndex == 0)
                            mInvisibleObjIndex = i;
                    }
                } else if (objBase.mType == ButtonType.Laser) {
                    LaserData btnLaser = (LaserData)objBase;
                    UISprite sprLaser = Instantiate(laserNote, mTrackAnchor).GetComponent<UISprite>();
                    float track_width = TRACK_NOTE_WIDTH * 5;

                    float xPos = -TRACK_NOTE_WIDTH * 2.5f + btnLaser.mPoints[0] * track_width;
                    pos.Set(xPos, yPos, 0f);
                    sprLaser.transform.localPosition = pos;

                    bool b_right = btnLaser.mIndex == 1;

                    sprLaser.spriteName = b_right ? "Nobe_Right" : "Nobe_Left";
                    sprLaser.depth = b_right ? sprLaser.depth + 2 : sprLaser.depth + 0;

                    int laserSlamThreshold = (int)Math.Ceiling(timing.mBeatDuration / 8.0f);
                    // 가로로 누운 형태의 레이저. 
                    // TODO : 짧은거같으니 height와 localPosition을 조절해주자
                    int dir = btnLaser.GetDirection();
                    if (btnLaser.mDuration <= laserSlamThreshold) {
                        sprLaser.transform.localRotation = Quaternion.Euler(0f, 0f, -dir * 90f);
                        sprLaser.height = (int)(track_width * Math.Abs(btnLaser.mPoints[1] - btnLaser.mPoints[0]));

                        // 잘 보이게 너비를 늘려준다
                        //sprLaser.pivot = dir > 0 ? UIWidget.Pivot.BottomRight : UIWidget.Pivot.BottomLeft;
                        sprLaser.width = (int)(sprLaser.width * LASER_NOTE_WIDTH_INCREMENT);

                        // 레이저 시작부분에 laserCorner 오브젝트를 생성한다.
                        #region Start Corner Note
                        UISprite sprCorner = Instantiate(laserCorner, mTrackAnchor).GetComponent<UISprite>();
                        sprCorner.spriteName = b_right ? "Nobe_Right_Corner" : "Nobe_Left_Corner";
                        sprCorner.depth = b_right ? sprCorner.depth + 3 : sprCorner.depth + 1;
                        // 위치, 회전값 조절
                        pos.Set(xPos, yPos, 0f);
                        sprCorner.transform.localPosition = pos;
                        // 왼쪽에서 시작할 때 (오른쪽에서 시작은 디폴트라 필요없음)
                        if (dir > 0) sprCorner.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                        // 잘 보이게 너비를 늘려준다
                        if (dir > 0)
                            sprCorner.width = (int)(sprCorner.width * LASER_NOTE_WIDTH_INCREMENT);
                        else
                            sprCorner.height = (int)(sprCorner.height * LASER_NOTE_WIDTH_INCREMENT);

                        btnLaser.mListNote.Add(sprCorner);
                        #endregion

                        // 첫 레이저이면 하단부에 일반 노트 1개랑 스타트 노트를 붙임
                        UISprite sprExtend = null;
                        if (btnLaser.mPrev == null) {
                            // 일반 노트
                            #region Additional Note
                            sprExtend = Instantiate(laserNote, mTrackAnchor).GetComponent<UISprite>();
                            sprExtend.spriteName = b_right ? "Nobe_Right" : "Nobe_Left";
                            sprExtend.depth = b_right ? sprExtend.depth + 2 : sprExtend.depth + 0;
                            // 위치 조절
                            sprExtend.pivot = UIWidget.Pivot.Center;
                            sprExtend.height = (int)(TRACK_NOTE_WIDTH * LASER_NOTE_WIDTH_INCREMENT);
                            pos.Set(xPos, yPos - sprExtend.height, 0f);
                            sprExtend.transform.localPosition = pos;

                            btnLaser.mListNote.Add(sprExtend);
                            #endregion

                            // 스타트 노트
                            // 해당 레이저가 루트이고 마지막 레이저로부터 10초가 지났을때 밑에 laserStart를 깔아놓는다.
                            // 10초 -> 1초로 수정
                            #region Additional Start Note
                            if (mLastLaserEndTime[btnLaser.mIndex] == 0f || btnLaser.mTime >= mLastLaserEndTime[btnLaser.mIndex] + LASER_START_INTERVAL) {
                                sprExtend = Instantiate(laserStart, mTrackAnchor).GetComponent<UISprite>();
                                sprExtend.spriteName = b_right ? "Nobe_Guide_R" : "Nobe_Guide_L";
                                pos.Set(xPos, yPos - TRACK_NOTE_WIDTH * 1.5f * LASER_NOTE_WIDTH_INCREMENT, 0f);
                                sprExtend.transform.localPosition = pos;
                                sprExtend.depth += 3;

                                btnLaser.mListNote.Add(sprExtend);
                            }
                            #endregion
                        }

                        // 레이저 끝부분에 laserCorner 오브젝트를 생성한다.
                        xPos = -TRACK_NOTE_WIDTH * 2.5f + btnLaser.mPoints[1] * track_width;
                        #region End Corner Note
                        sprCorner = Instantiate(laserCorner, mTrackAnchor).GetComponent<UISprite>();
                        sprCorner.spriteName = b_right ? "Nobe_Right_Corner" : "Nobe_Left_Corner";
                        sprCorner.depth = b_right ? sprCorner.depth + 3 : sprCorner.depth + 1;
                        // 위치, 회전값 조절
                        pos.Set(xPos, yPos, 0f);
                        sprCorner.transform.localPosition = pos;
                        sprCorner.transform.localRotation = Quaternion.Euler(0f, 0f, dir > 0 ? - 90f : 180f);

                        // 잘 보이게 너비를 늘려준다
                        if (dir > 0)
                            sprCorner.width = (int)(sprCorner.width * LASER_NOTE_WIDTH_INCREMENT);
                        else
                            sprCorner.height = (int)(sprCorner.height * LASER_NOTE_WIDTH_INCREMENT);

                        btnLaser.mListNote.Add(sprCorner);
                        #endregion

                        // 마지막 레이저이면 일반 노트 1개를 붙임
                        if (btnLaser.mNext == null) {
                            // 일반 노트
                            #region Additional Note
                            sprExtend = Instantiate(laserNote, mTrackAnchor).GetComponent<UISprite>();
                            sprExtend.spriteName = b_right ? "Nobe_Right" : "Nobe_Left";
                            sprExtend.depth = b_right ? sprExtend.depth + 2 : sprExtend.depth + 0;
                            // 위치 조절
                            sprExtend.pivot = UIWidget.Pivot.Center;
                            sprExtend.height = (int)(TRACK_NOTE_WIDTH * LASER_NOTE_WIDTH_INCREMENT);
                            pos.Set(xPos, yPos + sprExtend.height, 0f);
                            sprExtend.transform.localPosition = pos;

                            btnLaser.mListNote.Add(sprExtend);
                            #endregion
                        }

                    } else { // 각도가 있는 레이저
                        int width = (int)(track_width * Math.Abs(btnLaser.mPoints[1] - btnLaser.mPoints[0]));
                        int height = (int)(bpmPerLength * btnLaser.mDuration);
                        if (width != 0) {
                            double tan = (double)width / height;
                            double aatan = Math.Atan(tan) * Mathf.Rad2Deg;
                            sprLaser.transform.localRotation = Quaternion.Euler(0f, 0f, -dir * (float)(Math.Atan(tan) * Mathf.Rad2Deg));
                            height = (int)(height * Math.Sqrt(1 + tan * tan));
                        }

                        sprLaser.height = height + 40;

                        // 가장자리 보간
                        if (width != 0) {
                            sprLaser.pivot = UIWidget.Pivot.Center;
                            sprLaser.height += (int)(TRACK_NOTE_WIDTH * 0.5f);
                        }

                        // 해당 레이저가 루트이고 마지막 레이저로부터 10초가 지났을때 밑에 laserStart를 깔아놓는다.
                        // 10초 -> 1초로 수정
                        if (btnLaser.mPrev == null && ( mLastLaserEndTime[btnLaser.mIndex] == 0f ||
                                                        btnLaser.mTime >= mLastLaserEndTime[btnLaser.mIndex] + LASER_START_INTERVAL)) {
                            UISprite sprExtend = Instantiate(laserStart, mTrackAnchor).GetComponent<UISprite>();
                            sprExtend.spriteName = b_right ? "Nobe_Guide_R" : "Nobe_Guide_L";
                            pos.Set(xPos, yPos, 0f);
                            sprExtend.transform.localPosition = pos;
                            sprExtend.depth += 3;

                            btnLaser.mListNote.Add(sprExtend);
                        }
                    }

                    btnLaser.mListNote.Add(sprLaser);

                    // 마지막 레이저의 시간을 저장
                    if (btnLaser.mNext == null)
                        mLastLaserEndTime[btnLaser.mIndex] = btnLaser.mTime;


                    // 보이지 않을 오브젝트는 미리 꺼놓는다.
                    if (btnLaser.mTime > OBJECT_ACTIVE_INTERVAL) {
                        for (int j = 0; j < btnLaser.mListNote.Count; j++)
                            btnLaser.mListNote[j].gameObject.SetActive(false);

                        if (mInvisibleObjIndex == 0)
                            mInvisibleObjIndex = i;
                    }
                }
            }

            Debug.Log("length : " + mListObj.Count);

            // 트랙 눕히기
            //mTrackAnchor.parent.localRotation = Quaternion.Euler(new Vector3(68f, 0f, 0f));
            //m_camera.transform.parent = transform;
            m_camera.SetOriginPos();
        }

        /// <summary> Instantiate로 생성하고 저장하지 않은 오브젝트 전부 파괴 </summary>
        void DestroyNoteObject() {
            List<ObjectDataBase> listState = m_beatmap.mListObjectState;
            for (int i = 0; i < listState.Count; i++) {
                ObjectDataBase objBase = listState[i];
                if (objBase.mNote != null)
                    Destroy(objBase.mNote);
                if (objBase.mType == ButtonType.Laser) {
                    List<UISprite> spr = ((LaserData)objBase).mListNote;
                    for (int j = 0; j < spr.Count; j++)
                        Destroy(spr[j]);
                    spr.Clear();
                }
            }
        }

        /// <summary>
        /// 움직이는 트랙에 부착된 오브젝트를 움직이지 않는 트랙으로 붙여 연산량을 줄여준다.
        /// </summary>
        public void ChangeObjectParent(ObjectDataBase obj) {
            if (obj.mType == ButtonType.Laser) {
                ((LaserData)obj).ChangeParent(mStaticTrack);
            } else {
                if (obj.mNote != null) {
                    obj.mNote.transform.parent = mStaticTrack;
                    obj.mNote.gameObject.SetActive(false);
                }
            }
        }

        // 단일 파티클은 Emit, 다중 파티클은 Play
        public ParticleSystem ParticlePlay(ParticleType type, Vector3 target) {
            int tmp = 0;
            bool b_loop = false;
            ParticleSystem[] arr_particle = null;
            if (type == ParticleType.Normal) {
                arr_particle = mNormalHitEffect;
            } else if (type == ParticleType.Hold) {
                arr_particle = mHoldHitEffect;
                b_loop = true;
            } else if (type == ParticleType.Laser) {
                arr_particle = mLaserHitEffect;
                b_loop = true;
            } else if (type == ParticleType.Slam) {
                arr_particle = mSlamHitEffect;
            }

            if (arr_particle == null) {
                Debug.Log("impossible! arr_particle is null!!");
                return null;
            }

            while (arr_particle[tmp].isPlaying) {
                tmp = (tmp + 1) % arr_particle.Length;
            }
            arr_particle[tmp].transform.localPosition = target;

            if (b_loop) {
                arr_particle[tmp].gameObject.SetActive(true);
                arr_particle[tmp].Play();
            } else {
                arr_particle[tmp].Emit(1);
            }

            return arr_particle[tmp];
        }

        /// <summary> 버튼의 x축 위치를 리턴하는 함수 </summary>
        float GetButtonXPos(int index) {
            // fx는 일반 노트의 2배다
            return index >= 4 ? -TRACK_NOTE_WIDTH + TRACK_NOTE_WIDTH * 2 * (index - 4) :
                                -TRACK_NOTE_WIDTH * 1.5f + TRACK_NOTE_WIDTH * index;
        }

        void Tick(float deltaTime) {
            if (!m_paused)
                TickGameplay(deltaTime);

            if (mForceEnd)
                FinishGame();
        }

        // Processes input and Updates scoring, also handles audio timing management
        void TickGameplay(float deltaTime) {
            if (!mPlaying) {
                m_audioPlayback.Play();
                mPlaying = true;
            }

            BeatmapSetting beatmapSettings = m_beatmap.mSetting;

            int playbackPositionMs = m_audioPlayback.GetPosition() - m_audioOffset;
            m_playback.UpdateTime(playbackPositionMs);

            int delta = playbackPositionMs - m_lastMapTime;
            int beatStart = 0;
            uint numBeats = m_playback.CountBeats(m_lastMapTime, delta, ref beatStart, 1);
            if (numBeats > 0) {
                // Click Track
                //uint beat = beatStart % m_playback.GetCurrentTimingPoint().measure;
                //if(beat == 0)
                //{
                //	m_clickSamples[0].Play();
                //}
                //else
                //{
                //	m_clickSamples[1].Play();
                //}
            }

            /// #Scoring
            // Update music filter states
            // TODO : 오디오 필터는 추후 구현
            //m_audioPlayback.SetLaserFilterInput(m_scoring.GetLaserOutput(), m_scoring.IsLaserHeld(0, false) || m_scoring.IsLaserHeld(1, false));
            m_audioPlayback.Tick(deltaTime);

            // Link FX track to combo counter for now
            m_audioPlayback.SetFXTrackEnabled(m_scoring.currentComboCounter > 0);

            // Stop playing if gauge is on hard and at 0%
            if ((m_flags & GameFlags.Hard) != GameFlags.None && m_scoring.currentGauge == 0f) {
                FinishGame();
            }
            
            // Update scoring
            if (!m_ended) {
                m_scoring.Tick(deltaTime);
            }

            // Get the current timing point
            m_currentTiming = m_playback.GetCurrentTimingPoint();

            //Debug.Log((float)-(m_currentTiming.GetBPM() * 0.01f) * delta);
            mTrackAnchor.localPosition = new Vector3(0f, -(float)(m_currentTiming.GetBPM() * TRACK_HEIGHT_INTERVAL) * playbackPositionMs * mSpeed, 0f);
            // TODO : 왜인지 모르겠는데 엄청나게 빠르게 값이 커짐
            //mTrack.Translate(new Vector3(0f, (float)-(m_currentTiming.GetBPM() * 0.01f) * delta, 0f), Space.Self);

            m_lastMapTime = playbackPositionMs;

            // 게이지 수정
            if (mSprHealthBar.fillAmount != m_scoring.currentGauge) {
                mSprHealthBar.fillAmount = m_scoring.currentGauge;
                if (mIsHPOver70 && mSprHealthBar.fillAmount < 0.7f) {
                    mIsHPOver70 = false;
                    mSprHealthBar.color = mHealthUnder70;
                } else if (!mIsHPOver70 && mSprHealthBar.fillAmount >= 0.7f) {
                    mIsHPOver70 = true;
                    mSprHealthBar.color = mHealthOver70;
                }
            }

            // 트랙 하이라이트 부모 변경
            if (mListObj[mListObjIndex].localPosition.y + 200f < -mTrackAnchor.localPosition.y) {
                mListObj[mListObjIndex].parent = mStaticTrack;
                mListObj[mListObjIndex++].gameObject.SetActive(false);
            }

            if (m_audioPlayback.HasEnded()) {
                FinishGame();
            }

            // 카메라 회전
            m_camera.GetComponent<CameraMotion>().Tick(deltaTime);

            // 시야 내에 들어올 오브젝트 액티브 활성화
            #region Visible Object Activity Control
            if (m_beatmap.mListObjectState.Count <= mInvisibleObjIndex)
                return;
            ObjectDataBase obj = m_beatmap.mListObjectState[mInvisibleObjIndex];
            if (obj.mTime > m_lastMapTime + OBJECT_ACTIVE_INTERVAL)
                return;
        
            if (obj.mType == ButtonType.Laser) {
                LaserData laser = (LaserData)obj;
                for (int j = 0; j < laser.mListNote.Count; j++)
                    laser.mListNote[j].gameObject.SetActive(true);
            } else {
                if (obj.mNote != null)
                    obj.mNote.gameObject.SetActive(true);
            }
            ++mInvisibleObjIndex;
            #endregion
        }

        /// <summary> 게임중 퍼즈 버튼을 눌렀을 때 하는 작업 </summary>
        /// <param name="release"> true이면 다시 게임 재개 </param>
        public void OnClickPauseButton(bool release) {
            if (release) {
                GuiManager.inst.DeactivatePanel(PanelType.Ingame);
                m_audioPlayback.Play();
            } else {
                m_audioPlayback.Pause();
                GuiManager.inst.ActivatePanel(PanelType.Ingame, true);
            }
            m_paused = !release;
        }

        // Called when game is finished and the score screen should show up
        void FinishGame() {
            if (m_ended)
                return;

            StopAllCoroutines();
            DestroyNoteObject();
            m_scoring.FinishGame();
            m_camera.ResetVal();
            m_audioPlayback.Stop();
            m_ended = true;
            mPlaying = false;

            if (!m_paused) {
                ResultPanel result = (ResultPanel)GuiManager.inst.GetPanel(PanelType.Result);
                result.UpdateView();
                GuiManager.inst.ActivatePanel(PanelType.Result, true);
            }

            Debug.Log("Game end");
        }

        void OnLaserSlamHit(LaserData laser) {
            float slamSize = (laser.mPoints[1] - laser.mPoints[0]);
            float direction = Math.Sign(slamSize);
            slamSize = Math.Abs(slamSize);
            // 카메라 쉐이크 구현
            m_camera.AddCameraShake(50f, 30f * slamSize);
            m_slamSample.Play();
            
            if (laser.mSpin.mType != 0) {
                // 카메라 바운스 구현
                m_camera.AddCameraSpin(laser);
            }

            // 레이저 슬램 파티클
            ParticlePlay(ParticleType.Slam, new Vector3(-450f + laser.mPoints[1] * 900f, 0f, 0f));
        }

        /// <summary> 오버트랙 판정 오브젝트 출력 </summary>
        public void PrintJudgement(int index, ScoreHitRating rate, float pos = 0f) {
            UISprite spr = mJudgeObject[index].GetComponent<UISprite>();
            spr.spriteName = rate == ScoreHitRating.Perfect ? "Judge_DMAX" :
                             rate == ScoreHitRating.Good ? "Judge_MAX" : "Judge_Miss";
            if (index < 6)
                mJudgeObject[index].ResetTime();
            else
                mJudgeObject[index].Move(pos, false, true);
        }

        void OnButtonHit(int buttonIdx, ScoreHitRating rating, ObjectDataBase hitObject, bool late) {
            NormalButtonData st = (NormalButtonData)hitObject;

            // 레일에 빛나는 스프라이트 출력

            // fx버튼에 달려있는 오디오 출력
            if (st != null && st.mHasSample) {
                m_fxSamples[st.mSampleIndex].volume = st.mSampleVolume;
                m_fxSamples[st.mSampleIndex].Play();
            }

            if (rating != ScoreHitRating.Idle) {
                PrintJudgement(buttonIdx, rating);

                // 히트 이펙트
                // Create hit effect particle
                float xPos = GetButtonXPos(buttonIdx);
                ParticlePlay(ParticleType.Normal, new Vector3(xPos, 0f, 0f));
                if (hitObject.mType == ButtonType.Single)
                    hitObject.mNote.gameObject.SetActive(false);
            }

        }

        /// <summary> 오버트랙 콤보 제거 </summary>
        void OnButtonMiss(int buttonIdx, bool hitEffect) {
            if (hitEffect) {
            }
            PrintJudgement(buttonIdx, ScoreHitRating.Miss);

            if (mLabelCombo.gameObject.activeSelf) {
                mComboTweenScale.ResetToBeginning();
                mLabelCombo.gameObject.SetActive(false);
            }
        }
        
        /// <summary> 콤보 라벨 변경 및 오버트랙 콤보 출력 </summary>
        void OnComboChanged(int newCombo) {
            if (newCombo == 0)
                return;
            
            mLabelBoardCombo.text = m_scoring.maxComboCounter.ToString();
            mLabelCombo.text = newCombo.ToString();
            mComboDisappearScript.ResetTime();
            if (!mLabelCombo.gameObject.activeSelf) {
                mLabelCombo.gameObject.SetActive(true);
            } else {
                mComboTweenScale.ResetToBeginning();
                mComboTweenScale.PlayForward();
            }
        }

        /// <summary> 스코어 라벨 변경 </summary>
        void OnScoreChanged(int newScore) {
            //Debug.Log("OnScoreChanged : " + newScore);
            mLabelScore.text = newScore.ToString();
        }

        // These functions control if FX button DSP's are muted or not
        void OnObjectHold(int buttonIdx, ObjectDataBase obj) {
            if (obj.mType == ButtonType.Hold) {
                HoldButtonData hold = (HoldButtonData)obj;
                if (hold.mEffectType != EffectType.None) {
                    //m_audioPlayback.SetEffectEnabled(hold.mIndex - 4, true);
                }
                float xPos = GetButtonXPos(buttonIdx);
                hold.mHitParticle = ParticlePlay(ParticleType.Hold, new Vector3(xPos, 0f, 0f));
            } else if (obj.mType == ButtonType.Laser) {
                LaserData laser = (LaserData)obj;
                laser.ChangeSprite(true);
            }
        }

        void OnObjectReleased(int buttonIdx, ObjectDataBase obj) {
            if (obj.mType == ButtonType.Hold) {
                HoldButtonData hold = (HoldButtonData)obj;
                if (hold.mEffectType != EffectType.None) {
                    //m_audioPlayback.SetEffectEnabled(hold.mIndex - 4, false);
                }
                if (hold.mHitParticle != null) {
                    hold.mHitParticle.Stop();
                    hold.mHitParticle.gameObject.SetActive(false);
                    hold.mHitParticle = null;
                }

                int currentTime = m_playback.m_playbackTime;
                if (Math.Abs(currentTime - (hold.mDuration + hold.mTime)) <= m_scoring.goodHitTime || m_scoring.autoplay || m_scoring.autoplayButtons) {
                    obj.mNote.gameObject.SetActive(false);
                }
            } else if (obj.mType == ButtonType.Laser) {
                LaserData laser = (LaserData)obj;
                laser.ChangeSprite(false);
            }
        }

        void OnTimingPointChanged(TimingPoint tp) {
            m_hispeed = m_modSpeed / (float)tp.GetBPM();
        }

        void OnLaneToggleChanged(LaneHideTogglePoint tp) {
            // Calculate how long the transition should be in seconds
            double duration = m_currentTiming.mBeatDuration * 4.0f * (tp.duration / 192.0f) * 0.001f;
            //m_track.SetLaneHide(!m_hideLane, duration);
            m_hideLane = !m_hideLane;
        }

        void OnEventChanged(EventKey key, EventData data) {
            if (key == EventKey.LaserEffectType) {
                m_audioPlayback.SetLaserEffect(data.mEffectVal);
            } else if (key == EventKey.LaserEffectMix) {
                m_audioPlayback.SetLaserEffectMix(data.mFloatVal);
            } else if (key == EventKey.TrackRollBehaviour) {
                //m_camera.rollKeep = (data.mRollVal & TrackRollBehaviour.Keep) == TrackRollBehaviour.Keep;
                //byte i = (byte)data.mRollVal & 0x7;
                
                m_camera.SetTiltIntensity(data.mRollVal);
                if (data.mRollVal == TrackRollBehaviour.Manual) {
                    // switch to manual tilt mode
                } else {
                }
            } else if (key == EventKey.SlamVolume) {
                m_slamSample.volume = data.mFloatVal * 0.4f;
            } else if (key == EventKey.ChartEnd) {
                FinishGame();
            }
        }

        // These functions register / remove DSP's for the effect buttons
        // the actual hearability of these is toggled in the tick by wheneter the buttons are held down
        void OnFXBegin(HoldButtonData obj) {
            //assert(obj.mIndex >= 4 && obj.mIndex <= 5);
            m_audioPlayback.SetEffect(obj.mIndex - 4, obj, m_playback);
        }
        void OnFXEnd(HoldButtonData obj) {
            //assert(obj.mIndex >= 4 && obj.mIndex <= 5);
            int index = obj.mIndex - 4;
            m_audioPlayback.ClearEffect(index, obj);
        }
        void OnLaserAlertEntered(LaserData obj) {
            if (m_scoring.timeSinceLaserUsed[obj.mIndex] > 3.0f) {
                //m_track.SendLaserAlert(obj.mIndex);
                //lua_getglobal(m_lua, "laser_alert");
                //lua_pushboolean(m_lua, object.index == 1);
                //if (lua_pcall(m_lua, 1, 0, 0) != 0) {
                //    Logf("Lua error on calling laser_alert: %s", Logger.Error, lua_tostring(m_lua, -1));
                //}
            }
        }
    }
}