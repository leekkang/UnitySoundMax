using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

using SoundMax;

public enum GameFlags : byte {
    None = 0,
    Hard = 1 << 0,
    Mirror = 1 << 1,
    Random = 1 << 2,
    AutoBT = 1 << 3,
    AutoFX = 1 << 4,
    AutoLaser = 1 << 5,
}

public class IngameEngine : Singleton<IngameEngine> {
    public bool m_playing = true;
    bool m_introCompleted = false;
    bool m_outroCompleted = false;
    bool m_paused = false;
    bool m_ended = false;
    bool m_transitioning = false;
    public float mSpeed = 1f;

    // Map object approach speed, scaled by BPM
    float m_hispeed = 1.0f;

    // Current lane toggle status
    bool m_hideLane = false;

    // Use m-mod and what m-mod speed
    bool m_usemMod = false;
    bool m_usecMod = false;
    float m_modSpeed = 400;

    // Game Canvas
    //Ref<HealthGauge> m_scoringGauge;
    //Ref<SettingsBar> m_settingsBar;
    //Ref<Label> m_scoreText;

    // Texture of the map jacket image, if available
    //Image m_jacketImage;
    //Texture m_jacketTexture;

    // The beatmap
    Beatmap m_beatmap;
    // Scoring system object
    Scoring m_scoring;
    // Beatmap playback manager (object and timing point selector)
    PlaybackEngine m_playback;
    // Audio playback manager (music and FX))
    AudioEngine m_audioPlayback;
    // Applied audio offset
    int m_audioOffset = 0;
    int m_fpsTarget = 0;
    // The play field
    Transform mTrack;
    //Track* m_track = nullptr;
    Transform mJudgeLine;

    // The camera watching the playfield
    Camera m_camera;

    // Currently active timing point
    TimingPoint m_currentTiming;
    // Currently visible gameplay objects
    List<ObjectDataBase> m_currentObjectSet;
    int m_lastMapTime;

    // Rate to sample gauge;
    int m_gaugeSampleRate;
    float[] m_gaugeSamples = new float[256];
    int m_endTime;

    // Combo gain animation
    //Timer m_comboAnimation;
    Transform mAudioRoot;
    AudioSource m_slamSample;
    AudioSource[] m_clickSamples = new AudioSource[2];
    List<AudioSource> m_fxSamples = new List<AudioSource>();

    // Roll intensity, default = 1
    float m_rollIntensity = 14 / 360.0f;
    bool m_manualTiltEnabled = false;
    GameFlags m_flags;
    bool m_manualExit = false;

    float m_shakeAmount = 3;
    float m_shakeDuration = 0.083f;

    // pool
    public ParticleSystem[] mNormalHitEffect;
    public ParticleSystem[] mHoldHitEffect;

    MusicData mCurMusic = new MusicData();  // 필요한건가?

    public void Open() {
        m_beatmap = new Beatmap();
        m_playback = new PlaybackEngine();
        m_audioPlayback = new AudioEngine();
        mTrack = GameObject.Find("Anchor").transform;
        m_camera = GameObject.Find("IngameCamera").GetComponent<Camera>();
        mAudioRoot = GameObject.Find("Audio").transform;
        mJudgeLine = GameObject.Find("JudgeLine").transform;

        // 샘플 오디오 로드
        m_slamSample = GameObject.Find("SlamSound").GetComponent<AudioSource>();
        m_clickSamples[0] = GameObject.Find("ClickSound1").GetComponent<AudioSource>();
        m_clickSamples[1] = GameObject.Find("ClickSound2").GetComponent<AudioSource>();

        string rootPath = Path.Combine(Application.streamingAssetsPath, "fxAudio");
        string audioPath = Path.Combine(rootPath, "laser_slam.wav").Trim();
        DataBase.inst.LoadAudio(audioPath, (audio) => {
            m_slamSample.clip = audio;
        });
        audioPath = Path.Combine(rootPath, "click-01.wav").Trim();
        DataBase.inst.LoadAudio(audioPath, (audio) => {
            m_slamSample.clip = audio;
        });
        audioPath = Path.Combine(rootPath, "click-02.wav").Trim();
        DataBase.inst.LoadAudio(audioPath, (audio) => {
            m_slamSample.clip = audio;
        });
    }

    public void StartGame(MusicData data, float speed) {
        mSpeed = speed;
        StartCoroutine(CoLoadData(data));
    }

    IEnumerator CoLoadData(MusicData data) {
        mCurMusic = data;
        m_beatmap.Load(data, false);
        m_scoring = Scoring.inst;

        BeatmapSetting mapSettings = m_beatmap.mSetting;

        for (int i = 0; i < m_gaugeSamples.Length; i++)
            m_gaugeSamples[0] = 0;
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

        m_endTime = lastObjectTime;
        m_gaugeSampleRate = lastObjectTime / 256;

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

        while(!m_ended) {
            //Debug.Log("delta time : " + Time.deltaTime);
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
            m_lastMapTime = firstObjectTime - 5000;
            m_audioPlayback.SetPosition(m_lastMapTime);
        }

        // Reset playback
        m_playback.Reset(m_lastMapTime);
    }

    bool InitGameplay() {
        m_playing = false;
        m_introCompleted = false;
        m_outroCompleted = false;
        m_paused = false;
        m_ended = false;
        m_transitioning = false;

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

        return true;
    }

    List<GameObject> mListObj = new List<GameObject>();
    void CreateObject() {
        for (int i = 0; i < mListObj.Count; i++)
            Destroy(mListObj[i]);
        mListObj.Clear();

        // init track
        mTrack.localPosition = new Vector3(0f, -1440f, 0f);

        // make effect pool
        mNormalHitEffect = new ParticleSystem[15];
        GameObject effNormal = Resources.Load("Prefab/NormalHitEffect") as GameObject;
        Vector3 pos = new Vector3(-2000f, 0f, 0f);
        for (int i = 0; i < 10; i++) {
            mNormalHitEffect[i] = Instantiate(effNormal, mJudgeLine).GetComponent<ParticleSystem>();
            mNormalHitEffect[i].name = string.Format("NormalHitEffect_{0}", i);
            mNormalHitEffect[i].transform.position = pos;
        }
        mHoldHitEffect = new ParticleSystem[10];
        GameObject effHit = Resources.Load("Prefab/HoldHitEffect") as GameObject;
        for (int i = 0; i < 10; i++) {
            mHoldHitEffect[i] = Instantiate(effHit, mJudgeLine).GetComponent<ParticleSystem>();
            mHoldHitEffect[i].name = string.Format("HoldHitEffect_{0}", i);
            mHoldHitEffect[i].transform.position = pos;
            mHoldHitEffect[i].Stop();
            mHoldHitEffect[i].gameObject.SetActive(false);
        }

        // init button
        // TODO : bpm 변경되는 곡 구현하지 않음
        //Dictionary<double, float> dicBpmChange = new Dictionary<double, float>();
        GameObject fxNote = Resources.Load("Prefab/FXNote") as GameObject;
        GameObject normalNote = Resources.Load("Prefab/Note") as GameObject;
        List<ObjectDataBase> listState = m_beatmap.mListObjectState;
        for (int i = 0; i < listState.Count; i++) {
            ObjectDataBase objBase = listState[i];
            if (objBase.mType == ButtonType.Single || objBase.mType == ButtonType.Hold) {
                NormalButtonData btnNormal = (NormalButtonData)objBase;
                GameObject obj = null;
                bool b_fx = btnNormal.mIndex >= 4;
                if (b_fx)
                    obj = Instantiate(fxNote, mTrack);
                else
                    obj = Instantiate(normalNote, mTrack);
                obj.transform.parent = mTrack;

                TimingPoint timing = m_playback.GetTimingPointAt(objBase.mTime, false);
                float bpmPerLength = (float)timing.GetBPM() * 0.01f;
                // TODO : bpm 변경되는 곡 구현하지 않음
                //if (!dicBpmChange.ContainsKey(timing.GetBPM())) {
                //    dicBpmChange.Add(timing.GetBPM(), timing.mTime);
                //}
                //float yLoc = 0;
                //if (dicBpmChange.Count > 1) {
                //    foreach(var key in dicBpmChange) {

                //    }
                //}
                float xPos = b_fx ? -180f + 360f * (btnNormal.mIndex - 4) : -270f + 180f * btnNormal.mIndex;

                obj.transform.localPosition = new Vector3(xPos, bpmPerLength * objBase.mTime * mSpeed, 0f);

                if (objBase.mType == ButtonType.Hold) {
                    HoldButtonData btnHold = (HoldButtonData)objBase;

                    obj.GetComponent<UISprite>().height = (int)(100f + bpmPerLength * btnHold.mDuration * mSpeed);
                }
                objBase.mNote = obj;

                mListObj.Add(obj);
            } else if (objBase.mType == ButtonType.Laser) {

            }
        }
    }

    // 단일 파티클은 Emit, 다중 파티클은 Play
    public ParticleSystem ParticlePlay(ParticleType type, Vector3 target) {
        int tmp = 0;
        if (type == ParticleType.Normal) {
            while (mNormalHitEffect[tmp].isPlaying) {
                tmp++;
                tmp = tmp % mNormalHitEffect.Length;
            }
            mNormalHitEffect[tmp].transform.localPosition = target;
            mNormalHitEffect[tmp].Emit(1);
            return mNormalHitEffect[tmp];
        } else if (type == ParticleType.Hold) {
            while (mHoldHitEffect[tmp].isPlaying) {
                tmp++;
                tmp = tmp % mHoldHitEffect.Length;
                Debug.Log("tetsaesdfasd");
            }
            mHoldHitEffect[tmp].transform.localPosition = target;
            mHoldHitEffect[tmp].gameObject.SetActive(true);
            mHoldHitEffect[tmp].Play();
            return mHoldHitEffect[tmp];
        } else if (type == ParticleType.Slam) {

        }
        return null;
    }

    void Tick(float deltaTime) {
        if (!m_paused)
            TickGameplay(deltaTime);
    }

    // Processes input and Updates scoring, also handles audio timing management
    void TickGameplay(float deltaTime) {
        if (!m_playing /*&& m_introCompleted*/) {
            // Start playback of audio in first gameplay tick
            m_audioPlayback.Play();
            m_playing = true;

            //if (g_application.GetAppCommandLine().Contains("-autoskip")) {
            //    SkipIntro();
            //}
        }

        BeatmapSetting beatmapSettings = m_beatmap.mSetting;

        // Update beatmap playback
        //Debug.Log("m_audioPlayback.GetPosition() : " + m_audioPlayback.GetPosition());

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
        m_audioPlayback.SetLaserFilterInput(m_scoring.GetLaserOutput(), m_scoring.IsLaserHeld(0, false) || m_scoring.IsLaserHeld(1, false));
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
            // Update scoring gauge
            int gaugeSampleSlot = playbackPositionMs;
            gaugeSampleSlot /= m_gaugeSampleRate;
            gaugeSampleSlot = Mathf.Clamp(gaugeSampleSlot, 0, 255);
            m_gaugeSamples[gaugeSampleSlot] = m_scoring.currentGauge;
        }
        
        // Get the current timing point
        m_currentTiming = m_playback.GetCurrentTimingPoint();


        // Update hispeed
        //if (g_input.GetButton(Input.Button.BT_S)) {
        //    for (int i = 0; i < 2; i++) {
        //        float change = g_input.GetInputLaserDir(i) / 3.0f;
        //        m_hispeed += change;
        //        m_hispeed = Math.Clamp(m_hispeed, 0.1f, 16f);
        //        if ((m_usecMod || m_usemMod) && change != 0.0f) {
        //            g_gameConfig.Set(GameConfigKeys.ModSpeed, m_hispeed * (float)m_currentTiming.GetBPM());
        //            m_modSpeed = m_hispeed * (float)m_currentTiming.GetBPM();
        //            m_playback.cModSpeed = m_modSpeed;
        //        }
        //    }
        //}

        //Debug.Log((float)-(m_currentTiming.GetBPM() * 0.01f) * delta);
        mTrack.localPosition = new Vector3(0f, -1440f - (float)(m_currentTiming.GetBPM() * 0.01f) * playbackPositionMs * mSpeed, 0f);
        // TODO : 왜인지 모르겠는데 엄청나게 빠르게 값이 커짐
        //mTrack.Translate(new Vector3(0f, (float)-(m_currentTiming.GetBPM() * 0.01f) * delta, 0f), Space.Self);

        m_lastMapTime = playbackPositionMs;

        if (m_audioPlayback.HasEnded()) {
            FinishGame();
        }
    }

    // Called when game is finished and the score screen should show up
    void FinishGame() {
        if (m_ended)
            return;

        m_scoring.FinishGame();
        m_ended = true;
        m_playing = false;
    }

    void OnLaserSlamHit(LaserData obj) {
        float slamSize = (obj.mPoints[1] - obj.mPoints[0]);
        float direction = Math.Sign(slamSize);
        slamSize = Math.Abs(slamSize);
        // 카메라 쉐이크 구현
        //CameraShake shake(m_shakeDuration, powf(slamSize, 0.5f) *m_shakeAmount * -direction);
        //m_camera.AddCameraShake(shake);
        m_slamSample.Play();


        if (obj.mSpin.mType != 0) {
            // 카메라 바운스 구현
            //if (obj.mSpin.mType == SpinType.Bounce)
            //    m_camera.SetXOffsetBounce(obj.GetDirection(), obj.spin.duration, obj.spin.amplitude, obj.spin.frequency, obj.spin.duration, m_playback);
            //else m_camera.SetSpin(obj.GetDirection(), obj.spin.duration, obj.spin.type, m_playback);
        }

        // 레이저 슬램 파티클
        //float laserPos = m_track.trackWidth * object.points[1] - m_track.trackWidth * 0.5f;
        //Ref<ParticleEmitter> ex = CreateExplosionEmitter(m_track.laserColors[obj.mIndex], new Vector3(direction, 0, 0));
        //ex.position = new Vector3(laserPos, 0.0f, -0.05f);
        //ex.position = m_track.TransformPoint(ex.position);
    }

    void OnButtonHit(int buttonIdx, ScoreHitRating rating, ObjectDataBase hitObject, bool late) {
        NormalButtonData st = (NormalButtonData)hitObject;

        // The color effect in the button lane
        // 버튼 히트 이펙트
        //m_track.AddEffect(new ButtonHitEffect(buttonIdx, c));

        if (st != null && st.mHasSample) {
            m_fxSamples[st.mSampleIndex].volume = st.mSampleVolume;
            m_fxSamples[st.mSampleIndex].Play();
        }

        if (rating != ScoreHitRating.Idle) {
            // Floating text effect
            //m_track.AddEffect(new ButtonHitRatingEffect(buttonIdx, rating));

            if (rating == ScoreHitRating.Good) {
                Debug.Log("Hit Good");
                // 판정 굿 이펙트??
                //m_track.timedHitEffect.late = late;
                //m_track.timedHitEffect.Reset(0.75f);
            }

            // 히트 이펙트
            // Create hit effect particle
            float xPos = buttonIdx >= 4 ? -180f + 360f * (buttonIdx - 4) : -270f + 180f * buttonIdx;
            ParticlePlay(ParticleType.Normal, new Vector3(xPos, 0f, 0f));
            if (hitObject.mType == ButtonType.Single)
                hitObject.mNote.SetActive(false);
            //Color hitColor = (buttonIdx < 4) ? Color.White : Color.FromHSV(20, 0.7f, 1.0f);
            //float hitWidth = (buttonIdx < 4) ? m_track.buttonWidth : m_track.fxbuttonWidth;
            //Ref<ParticleEmitter> emitter = CreateHitEmitter(hitColor, hitWidth);
            //emitter.position.x = m_track.GetButtonPlacement(buttonIdx);
            //emitter.position.z = -0.05f;
            //emitter.position.y = 0.0f;
            //emitter.position = m_track.TransformPoint(emitter.position);
        }

    }
    void OnButtonMiss(int buttonIdx, bool hitEffect) {
        //Debug.Log("Hit Miss");
        //if (hitEffect) {
        //    Color c = m_track.hitColors[0];
        //    m_track.AddEffect(new ButtonHitEffect(buttonIdx, c));
        //}
        //m_track.AddEffect(new ButtonHitRatingEffect(buttonIdx, ScoreHitRating.Miss));
    }
    void OnComboChanged(int newCombo) {
        // 콤보 체인지 이펙트 또는 라벨 변경
        //m_comboAnimation.Restart();
        Debug.Log("OnComboChanged : " + newCombo);
    }
    void OnScoreChanged(int newScore) {
        // 스코어 라벨 변경
        //Debug.Log("OnScoreChanged : " + newScore);
    }

    // These functions control if FX button DSP's are muted or not
    void OnObjectHold(int buttonIdx, ObjectDataBase obj) {
        if (obj.mType == ButtonType.Hold) {
            HoldButtonData hold = (HoldButtonData)obj;
            if (hold.mEffectType != EffectType.None) {
                //m_audioPlayback.SetEffectEnabled(hold.mIndex - 4, true);
            }
            float xPos = buttonIdx >= 4 ? -180f + 360f * (buttonIdx - 4) : -270f + 180f * buttonIdx;
            hold.mHitParticle = ParticlePlay(ParticleType.Hold, new Vector3(xPos, 0f, 0f));
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
            if (Math.Abs(currentTime - (hold.mDuration + hold.mTime)) <= Scoring.inst.goodHitTime) {
                obj.mNote.SetActive(false);
            }
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
            int i = (byte)data.mRollVal & 0x7;

            m_manualTiltEnabled = false;
            if (i == (byte)TrackRollBehaviour.Manual) {
                // switch to manual tilt mode
                m_manualTiltEnabled = true;
            } else if (i == 0)
                m_rollIntensity = 0;
            else {
                //m_rollIntensity = m_rollIntensityBase + (float)(i - 1) * 0.0125f;
                m_rollIntensity = (14 * (1.0f + 0.5f * (i - 1))) / 360.0f;
            }
        } else if (key == EventKey.SlamVolume) {
            m_slamSample.volume = data.mFloatVal;
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

    // 필요한건지?
    void m_OnButtonPressed(InputCode code) {
        // 강제 종료?
        //if (buttonCode == Input.Button.BT_S) {
        //    if (g_input.Are3BTsHeld()) {
        //        ObjectState *const* lastObj = &m_beatmap.GetLinearObjects().back();
        //        int timePastEnd = m_lastMapTime - (*lastObj).time;
        //        if (timePastEnd < 0)
        //            m_manualExit = true;

        //        FinishGame();
        //    }
        //}
    }

    // Skips ahead to the right before the first object in the map
    bool SkipIntro() {
        int index = 0;
        while (m_beatmap.mListObjectState[index].mType == ButtonType.Event && index < m_beatmap.mListObjectState.Count) {
            index++;
        }
        ObjectDataBase firstObj = m_beatmap.mListObjectState[index];
        int skipTime = firstObj.mTime - 1000;
        if (skipTime > m_lastMapTime) {
            m_audioPlayback.SetPosition(skipTime);
            return true;
        }
        return false;
    }
    // Skips ahead at the end to the score screen
    void SkipOutro() {
        // Just to be sure
        if (m_beatmap.mListObjectState.Count == 0) {
            FinishGame();
            return;
        }

        // Check if last object has passed
        ObjectDataBase lastObj = m_beatmap.mListObjectState.Last();
        int timePastEnd = m_lastMapTime - lastObj.mTime;
        if (timePastEnd > 250) {
            FinishGame();
        }
    }

    bool GetTickRate(int rate) {
        if (!m_audioPlayback.m_paused) {
            rate = m_fpsTarget;
            return true;
        }
        return false; // Default otherwise
    }
}