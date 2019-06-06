using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace SoundMax {
    public class ObjectDataBase {
        public int mTime;
        public ButtonType mType;
        public UISprite mNote;
    }

    public class NormalButtonData : ObjectDataBase {
        public int mIndex = 0;  // 0~3 : normal, 4~5 : fx
        public bool mHasSample = false;
        public int mSampleIndex;
        public float mSampleVolume = 1.0f;

        public NormalButtonData() {
            mType = ButtonType.Single;
        }
    }

    public class HoldButtonData : NormalButtonData {
        public int mDuration = 0;   // hold button length
        public EffectType mEffectType = EffectType.None;
        public short[] mArrEffectParams = new short[2];

        public ParticleSystem mHitParticle;

        // Set for hold notes that are a continuation of the previous one, but with a different effect
        public HoldButtonData mNext;
        public HoldButtonData mPrev;

        public HoldButtonData() {
            mType = ButtonType.Hold;
        }

        public HoldButtonData GetRoot() {
            HoldButtonData ptr = this;
            while (ptr.mPrev != null)
                ptr = ptr.mPrev;
            return ptr;
        }

        public HoldButtonData GetTail() {
            HoldButtonData ptr = this;
            while (ptr.mNext != null)
                ptr = ptr.mNext;
            return ptr;
        }
    }

    public class SpinStruct {
        public SpinType mType = SpinType.None;
        public float mDirection;
        public int mDuration;
        public int mAmplitude;
        public int mFrequency;
        public int mDecay;
    }

    public class LaserData : ObjectDataBase {
        public int mDuration;   // duration of laser segment
        public int mIndex;      // 0 or 1 for left and right respectively
        public byte mFlags;      // special options
        public float[] mPoints = new float[2];
        public int mOrder;    // Beatmap 생성 후 정렬시에만 사용

        public ParticleSystem mHitParticle;
        /// <summary>
        /// 해당 데이터가 관리하는 게임오브젝트 리스트. 히트 시 스프라이트를 변경하기 위해 존재함
        /// </summary>
        public List<UISprite> mListNote = new List<UISprite>();

        // Set the to the object state that connects to this laser, if any, otherwise null
        public LaserData mNext;
        public LaserData mPrev;

        public SpinStruct mSpin = new SpinStruct();

        public LaserData() {
            mType = ButtonType.Laser;
        }

        // Indicates that this segment is instant and should generate a laser slam segment
        public static byte mFlagInstant = 0x1;
        // Indicates that the range of this laser is extended from -0.5 to 1.5
        public static byte mFlagExtended = 0x2;

        public LaserData GetRoot() {
            LaserData ptr = this;
            while (ptr.mPrev != null)
                ptr = ptr.mPrev;
            return ptr;
        }

        public LaserData GetTail() {
            LaserData ptr = this;
            while (ptr.mNext != null)
                ptr = ptr.mNext;
            return ptr;
        }

        public int GetDirection() {
            return Math.Sign(mPoints[1] - mPoints[0]);
        }

        /// <summary> 현재 시각에 판정선에 접하는 레이저의 위치를 반환 </summary>
        public float CurJudgelinePosition(int time) {
            LaserData laser = this;
            while (laser.mNext != null && (laser.mTime + laser.mDuration) < time) {
                laser = laser.mNext;
            }
            float f = Mathf.Clamp((float)(time - laser.mTime) / Math.Max(1, laser.mDuration), 0.0f, 1.0f);
            return (laser.mPoints[1] - laser.mPoints[0]) * f + laser.mPoints[0];
        }

        /// <summary> 
        /// 레이저 스프라이트를 변경하는 함수. 연결된 모든 레이저를 다 변경한다.
        /// 루트에서 실행해야함.
        /// </summary>
        public void ChangeSprite(bool b_hit) {
            LaserData laser = this;
            for (; laser != null; laser = laser.mNext) {
                if (laser.mListNote.Count == 0)
                    continue;

                for (int i = 0; i < laser.mListNote.Count; i++) {
                    UISprite spr = laser.mListNote[i];
                    string spr_name = string.Empty;
                    bool b_corner = spr.spriteName.Contains("Corner");
                    if (b_hit) {
                        if (laser.mIndex == 0)
                            spr_name = b_corner ? "Nobe_Left_Corner_Glow" : "Nobe_Left_Glow";
                        else
                            spr_name = b_corner ? "Nobe_Right_Corner_Glow" : "Nobe_Right_Glow";
                    } else {
                        if (laser.mIndex == 0)
                            spr_name = b_corner ? "Nobe_Left_Corner" : "Nobe_Left";
                        else
                            spr_name = b_corner ? "Nobe_Right_Corner" : "Nobe_Right";
                    }
                    spr.spriteName = spr_name;
                }
            }
        }

        /// <summary>
        /// 지나간 레이저 오브젝트의 부모를 이동하지 않는 트랙으로 변경
        /// 루트에서 실행해야함.
        /// </summary>
        public void ChangeParent(Transform parent) {
            LaserData laser = this;
            for (; laser != null; laser = laser.mNext) {
                if (laser.mListNote.Count == 0)
                    continue;

                for (int i = 0; i < laser.mListNote.Count; i++) {
                    Transform tr = laser.mListNote[i].transform;
                    tr.parent = parent;
                    tr.gameObject.SetActive(false);
                }
            }
        }
    }

    public class EventData : ObjectDataBase {
        public EventKey mKey;

        public EventData() {
            mType = ButtonType.Event;
        }

        // 아래 값들 중 하나만 채워진다.
        public float mFloatVal;
        public int mIntVal;
        public EffectType mEffectVal;
        public TrackRollBehaviour mRollVal;
    }
    
    /// <summary>
    /// 특정 시각에 곡의 빠르기 관련 정보를 저장하는 클래스. 변속이 없으면 1개만 생성된다.
    /// </summary>
    public class TimingPoint {
        public int mTime;
        public double mBeatDuration;
        public int mNumerator;
        public int mDenominator = 4;

        public TimingPoint() { }
        public TimingPoint(TimingPoint target) {
            mTime = target.mTime;
            mBeatDuration = target.mBeatDuration;
            mNumerator = target.mNumerator;
            mDenominator = target.mDenominator;
        }

        public double GetWholeNoteLength() {
            return mBeatDuration * 4;
        }
        public double GetBarDuration() {
            if (mDenominator == 0)
                return 0;

            return GetWholeNoteLength() * ((double)mNumerator / mDenominator);
        }
        public double GetBPM() {
            return 60000.0f / mBeatDuration;
        }
    }

    public class LaneHideTogglePoint {
        // Position in ms when to hide or show the lane
        public int time;

        // How long the transition to/from hidden should take in 1/192nd notes
        public int duration = 192;
    };

    // Control point for track zoom levels
    public class ZoomControlPoint {
        public int time;
        // What zoom to control
        // 0 = bottom
        // 1 = top
        public int index = 0;
        // The zoom value
        // in the range -1 to 1
        // 1 being fully zoomed in
        public float zoom = 0.0f;
    };

    // Chart stop object
    public class ChartStop {
        public int time;
        public int duration;
    };

    public class BeatmapSetting {
        // Basic song meta data
        public string title;
        public string artist;
        public string effector;
        public string illustrator;
        public string tags;
        // Reported BPM range by the map
        public string bpm;
        // Offset in ms for the map to start
        public int offset;
        // Both audio tracks specified for the map / if any is set
        public string audioNoFX;
        public string audioFX;
        // Path to the jacket image
        public string jacketPath;
        // Path to the background and foreground shader files
        public string backgroundPath;
        public string foregroundPath;

        // Level, as indicated by map creator
        public int level;

        // Difficulty, as indicated by map creator
        public int difficulty;

        // Total, total gauge gained when played perfectly
        public float total = 210f;

        // Preview offset
        public int previewOffset;
        // Preview duration
        public int previewDuration;

        // Initial audio settings
        public float slamVolume = 1.0f;
        public float laserEffectMix = 1.0f;
        public float musicVolume = 1.0f;
        public EffectType laserEffectType = EffectType.PeakingFilter;
    }

    public class Beatmap {
        public Dictionary<EffectType, AudioEffect> mDicCustomEffect = new Dictionary<EffectType, AudioEffect>();
        public Dictionary<EffectType, AudioEffect> mDicCustomFilter = new Dictionary<EffectType, AudioEffect>();

        public List<TimingPoint> mListTimingPoint = new List<TimingPoint>();
        public List<ChartStop> mListChartStop = new List<ChartStop>();
        public List<LaneHideTogglePoint> mListLaneTogglePoint = new List<LaneHideTogglePoint>();
        public List<ObjectDataBase> mListObjectState = new List<ObjectDataBase>();
        public List<ZoomControlPoint> mListZoomPoint = new List<ZoomControlPoint>();
        public List<string> mListSamplePath = new List<string>();

        public BeatmapSetting mSetting;

        void Reset() {
            mDicCustomEffect.Clear();
            mDicCustomFilter.Clear();
            mListTimingPoint.Clear();
            mListChartStop.Clear();
            mListLaneTogglePoint.Clear();
            mListObjectState.Clear();
            mListZoomPoint.Clear();
            mListSamplePath.Clear();
        }

        public void Load(MusicData data, bool bMetaOnly) {
            if (data == null)
                return;

            Reset();

            Dictionary<EffectType, short> dicDefaultEffectParam = new Dictionary<EffectType, short> {
            {EffectType.None, 0},
            {EffectType.BitCrusher, 4},
            {EffectType.Gate, 8},
            {EffectType.Retrigger, 8},
            {EffectType.Phaser, 2000},
            {EffectType.Flanger, 2000},
            {EffectType.Wobble, 12},
            {EffectType.SideChain, 8},
            {EffectType.TapeStop, 50}
        };

            foreach (var key in data.mDicFxDefine) {
                EffectType type = GetEffectTypeIncludeCustom(key.Key);
                if (!mDicCustomEffect.ContainsKey(type))
                    mDicCustomEffect.Add(type, MakeCustomEffect(key.Value));
            }
            mCustomTypeIndex = EffectType.UserDefined0;
            foreach (var key in data.mDicFilterDefine) {
                EffectType type = GetEffectTypeIncludeCustom(key.Key);
                if (!mDicCustomEffect.ContainsKey(type))
                    mDicCustomEffect.Add(type, MakeCustomEffect(key.Value));
            }

            System.Func<string, EffectType> parseFilter = delegate (string str) {
                EffectType type = EffectType.None;
                if (str == "hpf1")
                    type = EffectType.HighPassFilter;
                else if (str == "lpf1")
                    type = EffectType.LowPassFilter;
                else if (str == "fx;bitc" || str == "bitc")
                    type = EffectType.BitCrusher;
                else if (str == "peak")
                    type = EffectType.PeakingFilter;
                else {
                    type = GetEffectType(str);
                    if (type == EffectType.None)
                        Debug.Log(string.Format("[KSH]Unknown filter type: {0}", str));
                }

                return type;
            };

            // Process map settings
            // TODO : MusicData에 담아놓고 꺼내쓰는 방법으로 변경
            mSetting = new BeatmapSetting();
            foreach (var s in data.mDicSettings) {
                if (s.Key == "title")
                    mSetting.title = s.Value;
                else if (s.Key == "artist")
                    mSetting.artist = s.Value;
                else if (s.Key == "effect")
                    mSetting.effector = s.Value;
                else if (s.Key == "illustrator")
                    mSetting.illustrator = s.Value;
                else if (s.Key == "t")
                    mSetting.bpm = s.Value;
                else if (s.Key == "jacket")
                    mSetting.jacketPath = s.Value;
                else if (s.Key == "bg")
                    mSetting.backgroundPath = s.Value;
                else if (s.Key == "layer")
                    mSetting.foregroundPath = s.Value;
                else if (s.Key == "m") {
                    string[] splitted = s.Value.Split(';');
                    mSetting.audioNoFX = splitted[0];
                    if (splitted.Length > 1)
                        mSetting.audioFX = splitted[1].Split(';')[0];
                } else if (s.Key == "o")
                    mSetting.offset = int.Parse(s.Value);
                else if (s.Key == "filtertype")
                    mSetting.laserEffectType = parseFilter(s.Value);
                else if (s.Key == "pfiltergain")
                    mSetting.laserEffectMix = int.Parse(s.Value) / 100.0f;
                else if (s.Key == "chokkakuvol")
                    mSetting.slamVolume = int.Parse(s.Value) / 100.0f;
                else if (s.Key == "level")
                    mSetting.level = int.Parse(s.Value);
                else if (s.Key == "difficulty") {
                    if (s.Value == "challenge")
                        mSetting.difficulty = 1;
                    else if (s.Value == "extended")
                        mSetting.difficulty = 2;
                    else if (s.Value == "infinite")
                        mSetting.difficulty = 3;
                    else
                        mSetting.difficulty = 0;
                } else if (s.Key == "po")
                    mSetting.previewOffset = int.Parse(s.Value);
                else if (s.Key == "plength")
                    mSetting.previewDuration = int.Parse(s.Value);
                else if (s.Key == "total")
                    mSetting.total = float.Parse(s.Value);
                else if (s.Key == "mvol")
                    mSetting.musicVolume = int.Parse(s.Value) / 100.0f;
            }

            // Temporary map for timing points
            Dictionary<int, TimingPoint> timingPointMap = new Dictionary<int, TimingPoint>();

            // Process initial timing point
            TimingPoint lastTimingPoint = new TimingPoint();
            lastTimingPoint.mTime = mSetting.offset;
            double bpm = Convert.ToDouble(data.mDicSettings["t"]);
            lastTimingPoint.mBeatDuration = 60000.0 / bpm;
            lastTimingPoint.mNumerator = 4;

            // Block offset for current timing point
            int timingPointBlockOffset = 0;
            // Tick offset into block for current timing point
            int timingTickOffset = 0;
            // Duration of first timing block
            double timingFirstBlockDuration = 0.0f;

            // Add First timing point
            mListTimingPoint.Add(lastTimingPoint);
            timingPointMap.Add(lastTimingPoint.mTime, lastTimingPoint);

            // Add First Lane Toggle Point
            LaneHideTogglePoint startLaneTogglePoint = new LaneHideTogglePoint();
            startLaneTogglePoint.time = 0;
            startLaneTogglePoint.duration = 1;
            mListLaneTogglePoint.Add(startLaneTogglePoint);

            // Stop here if we're only going for metadata
            if (bMetaOnly)
                return;

            // Button hold states
            TempButtonState[] buttonStates = new TempButtonState[6];
            // Laser segment states
            TempLaserState[] laserStates = new TempLaserState[2];

            EffectType[] currentButtonEffectTypes = new EffectType[2];
            // 2 per button
            short[] currentButtonEffectParams = new short[4] { -1, -1, -1, -1 };
            const int maxEffectParamsPerButtons = 2;
            float[] laserRanges = new float[2] { 1.0f, 1.0f };

            ZoomControlPoint[] firstControlPoints = new ZoomControlPoint[4];
            int lastint = 0;
            for (data.mTime.Reset(0, -1); data.NextTime();) {
                KShootBlock block = data.GetCurBlock();
                KShootTime time = data.mTime;
                KShootTick tick = data.GetCurTick();
                float[] fxSampleVolume = new float[2] { 1.0f, 1.0f };
                bool[] useFxSample = new bool[2];
                int[] fxSampleIndex = new int[2];
                // Calculate int from current tick
                double blockDuration = lastTimingPoint.GetBarDuration();
                int blockFromStartOfTimingPoint = (time.mIndexBlock - timingPointBlockOffset);
                int tickFromStartOfTimingPoint;

                if (blockFromStartOfTimingPoint == 0) // Use tick offset when in first block
                    tickFromStartOfTimingPoint = (time.mIndexTick - timingTickOffset);
                else
                    tickFromStartOfTimingPoint = time.mIndexTick;

                // Get the offset calculated by adding block durations together
                double blockDurationOffset = 0;
                if (timingTickOffset > 0) { // First block might have a shorter length because of the timing point being mid tick
                    if (blockFromStartOfTimingPoint > 0)
                        blockDurationOffset = timingFirstBlockDuration + blockDuration * (blockFromStartOfTimingPoint - 1);
                } else {
                    blockDurationOffset = blockDuration * blockFromStartOfTimingPoint;
                }

                // Sub-Block offset by adding ticks together
                double blockPercent = (double)tickFromStartOfTimingPoint / (double)block.mListTick.Count;
                double tickOffset = blockPercent * blockDuration;
                int mapTime = lastTimingPoint.mTime + Convert.ToInt32(blockDurationOffset + tickOffset);

                bool lastTick = block == data.mListBlocks.Last() && tick == block.mListTick.Last();

                // flag set when a new effect parameter is set and a new hold notes should be created
                bool[] splitupHoldNotes = new bool[2];

                bool isManualTilt = false;

                // Process settings
                foreach (var p in tick.mDicSetting) {
                    // Functions that adds a new timing point at current location if it's not yet there
                    System.Action<double, int, int> AddTimingPoint = delegate (double newDuration, int newNum, int newDenom) {
                        // Does not yet exist at current time?
                        if (!timingPointMap.ContainsKey(mapTime)) {
                            lastTimingPoint.mTime = mapTime;
                            TimingPoint lastPoint = new TimingPoint(lastTimingPoint);
                            mListTimingPoint.Add(lastPoint);
                            timingPointMap.Add(mapTime, lastPoint);
                            timingPointBlockOffset = time.mIndexBlock;
                            timingTickOffset = time.mIndexTick;
                        }

                        lastTimingPoint.mNumerator = newNum;
                        lastTimingPoint.mDenominator = newDenom;
                        lastTimingPoint.mBeatDuration = newDuration;

                        // Calculate new block duration
                        blockDuration = lastTimingPoint.GetBarDuration();

                        // Set new first block duration based on remaining ticks
                        timingFirstBlockDuration = (double)(block.mListTick.Count - time.mIndexTick) / (double)block.mListTick.Count * blockDuration;
                    };

                    if (p.Key == "beat") {
                        if (!p.Value.Contains("/"))
                            Debug.Log("beat key doesn't have \"/\" character : " + p.Value);
                        string[] beat = p.Value.Split('/');

                        int num = int.Parse(beat[0]);
                        int denom = 0;
                        if (beat.Length > 1)
                            denom = int.Parse(beat[1]);
                        //assert(denom % 4 == 0);

                        AddTimingPoint(lastTimingPoint.mBeatDuration, num, denom);
                    } else if (p.Key == "t") {
                        double tickBpm = double.Parse(p.Value);
                        AddTimingPoint(60000.0 / tickBpm, lastTimingPoint.mNumerator, lastTimingPoint.mDenominator);
                    } else if (p.Key == "laserrange_l") {
                        laserRanges[0] = 2.0f;
                    } else if (p.Key == "laserrange_r") {
                        laserRanges[1] = 2.0f;
                    } else if (p.Key == "fx-l" || p.Key == "fx-r") { // KSH 1.6
                                                                     // Parser the effect and parameters of an FX button (1.60)
                        int index = p.Key == "fx-l" ? 0 : 1;
                        string[] parseFx = p.Value.Split(';');

                        EffectType type = GetEffectType(parseFx[0]);
                        if (type == EffectType.None) {
                            Debug.Log(string.Format("Invalid custom effect name in ksh map: {0}", p.Value));
                        } else {
                            if (parseFx.Length > 1) {
                                string[] parseParam = parseFx[1].Trim().Split(';');

                                currentButtonEffectParams[index * maxEffectParamsPerButtons] = short.Parse(parseParam[0]);
                                if (parseParam.Length > 1)
                                    currentButtonEffectParams[index * maxEffectParamsPerButtons + 1] = short.Parse(parseParam[1]);
                            }
                        }

                        currentButtonEffectTypes[index] = type;
                        splitupHoldNotes[index] = true;
                    } else if (p.Key == "fx-l_param1") {
                        currentButtonEffectParams[0] = short.Parse(p.Value);
                        splitupHoldNotes[0] = true;
                    } else if (p.Key == "fx-r_param1") {
                        currentButtonEffectParams[maxEffectParamsPerButtons] = short.Parse(p.Value);
                        splitupHoldNotes[1] = true;
                    } else if (p.Key == "filtertype" || p.Key == "pfiltergain" || p.Key == "chokkakuvol") {
                        // Inser filter type change event
                        EventData evtData = new EventData();
                        evtData.mTime = mapTime;
                        bool bFilterType = p.Key == "filtertype";
                        if (bFilterType) {
                            evtData.mKey = EventKey.LaserEffectType;
                            evtData.mEffectVal = parseFilter(p.Value);
                        } else {
                            evtData.mKey = EventKey.LaserEffectMix;
                            evtData.mFloatVal = float.Parse(p.Value) / 100.0f;
                        }

                        mListObjectState.Add(evtData);
                    } else if (p.Key == "zoom_bottom" || p.Key == "zoom_top" || p.Key == "zoom_side") {
                        ZoomControlPoint point = new ZoomControlPoint();
                        point.time = mapTime;
                        point.index = p.Key == "zoom_bottom" ? 0 : p.Key == "zoom_top" ? 1 : 2;
                        point.zoom = (float)(int.Parse(p.Value) / 100.0);
                        mListZoomPoint.Add(point);
                        if (firstControlPoints[point.index] == null)
                            firstControlPoints[point.index] = point;
                    } /*else if (p.Key == "roll") { //OLD USC MANUAL ROLL, KEPT JUST IN CASE
                    ZoomControlPoint point = new ZoomControlPoint();
                    point.time = mapTime;
                    point.index = 3;
                    point.zoom = float.Parse(p.Value) / 360.0f;
                    mListZoomPoint.Add(point);
                    if (firstControlPoints[point.index] == null)
                        firstControlPoints[point.index] = point;
                }*/ else if (p.Key == "lane_toggle") {
                        LaneHideTogglePoint point = new LaneHideTogglePoint();
                        point.time = mapTime;
                        point.duration = int.Parse(p.Value);
                        mListLaneTogglePoint.Add(point);
                    } else if (p.Key == "tilt") {
                        EventData evtTilt = new EventData();
                        evtTilt.mTime = mapTime;
                        evtTilt.mKey = EventKey.TrackRollBehaviour;
                        evtTilt.mRollVal = TrackRollBehaviour.Zero;

                        string v = p.Value;
                        if (v.Contains("keep_")) {
                            evtTilt.mRollVal = TrackRollBehaviour.Keep;
                            v = v.Substring(v.IndexOf("keep_") + 5);
                        }

                        bool bManual = false;
                        if (v == "normal")
                            evtTilt.mRollVal = evtTilt.mRollVal | TrackRollBehaviour.Normal;
                        else if (v == "bigger")
                            evtTilt.mRollVal = evtTilt.mRollVal | TrackRollBehaviour.Bigger;
                        else if (v == "biggest")
                            evtTilt.mRollVal = evtTilt.mRollVal | TrackRollBehaviour.Biggest;
                        else {
                            evtTilt.mRollVal = TrackRollBehaviour.Manual;

                            ZoomControlPoint point = new ZoomControlPoint();
                            point.time = mapTime;
                            point.index = 3;
                            point.zoom = float.Parse(p.Value) / -(360.0f / 14.0f);
                            mListZoomPoint.Add(point);
                            if (firstControlPoints[point.index] == null)
                                firstControlPoints[point.index] = point;

                            isManualTilt = true;
                            bManual = true;
                        }

                        if (isManualTilt && !bManual) {
                            ZoomControlPoint point = new ZoomControlPoint();
                            point.time = mapTime;
                            point.index = 3;
                            point.zoom = mListZoomPoint.Last().zoom;
                            mListZoomPoint.Add(point);
                            if (firstControlPoints[point.index] == null)
                                firstControlPoints[point.index] = point;
                        }

                        mListObjectState.Add(evtTilt);
                    } else if (p.Key == "fx-l_se" || p.Key == "fx-r_se") {
                        int fxi = p.Key == "fx-l_se" ? 0 : 1;
                        useFxSample[fxi] = true;
                        string[] parse = p.Value.Split(';');
                        if (parse.Length > 1)
                            fxSampleVolume[fxi] = float.Parse(parse[1]) / 100.0f;

                        int index = mListSamplePath.FindIndex((x) => x == parse[0]);
                        if (index == -1) {
                            fxSampleIndex[fxi] = mListSamplePath.Count;
                            mListSamplePath.Add(parse[0]);
                        } else {
                            fxSampleIndex[fxi] = index;
                        }
                    } else if (p.Key == "stop") {
                        ChartStop cs = new ChartStop() {
                            time = mapTime,
                            duration = Convert.ToInt32((int.Parse(p.Value) / 192.0f) * lastTimingPoint.mBeatDuration * 4)
                        };
                        mListChartStop.Add(cs);
                    } else {
                        Debug.Log(string.Format("[KSH]Unkown map parameter at {0}:{1}: {2}", data.mTime.mIndexBlock, data.mTime.mIndexTick, p.Key));
                    }
                }

                // Set button states
                for (int i = 0; i < 6; i++) {
                    char c = i < 4 ? tick.mButtons[i] : tick.mFx[i - 4];
                    TempButtonState tmpBtnState = buttonStates[i];
                    HoldButtonData lastHoldObject = null;

                    System.Func<bool> IsHoldState = delegate () {
                        return tmpBtnState != null && tmpBtnState.numTicks > 0 && tmpBtnState.fineSnap;
                    };
                    System.Action CreateButton = delegate () {
                        if (IsHoldState()) {
                            HoldButtonData obj = new HoldButtonData();
                            obj.mTime = tmpBtnState.startTime;
                            obj.mIndex = i;
                            obj.mDuration = mapTime - tmpBtnState.startTime;
                            obj.mEffectType = tmpBtnState.effectType;
                            if (tmpBtnState.lastHoldObject != null)
                                tmpBtnState.lastHoldObject.mNext = obj;
                            obj.mPrev = tmpBtnState.lastHoldObject;
                            obj.mArrEffectParams[0] = tmpBtnState.effectParams[0];
                            obj.mArrEffectParams[1] = tmpBtnState.effectParams[1];
                            mListObjectState.Add(obj);
                            lastHoldObject = obj;
                        } else {
                            NormalButtonData obj = new NormalButtonData();

                            obj.mTime = tmpBtnState.startTime;
                            obj.mIndex = i;
                            obj.mHasSample = tmpBtnState.usingSample;
                            obj.mSampleIndex = tmpBtnState.sampleIndex;
                            obj.mSampleVolume = tmpBtnState.sampleVolume;
                            mListObjectState.Add(obj);
                        }

                        // Reset 
                        buttonStates[i] = null;
                        tmpBtnState = null;
                    };

                    // Split up multiple hold notes
                    if (i > 3 && IsHoldState() && splitupHoldNotes[i - 4]) {
                        CreateButton();
                    }

                    if (c == '0') {
                        // Terminate hold button
                        if (tmpBtnState != null) {
                            CreateButton();
                        }

                        if (i >= 4) {
                            // Unset effect parameters
                            currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons] = -1;
                        }
                    } else if (tmpBtnState == null) {
                        // Create new hold state
                        buttonStates[i] = new TempButtonState(mapTime);
                        tmpBtnState = buttonStates[i];
                        int div = (int)block.mListTick.Count;

                        if (lastHoldObject != null)
                            tmpBtnState.lastHoldObject = lastHoldObject;

                        if (i < 4) {
                            // Normal '1' notes are always individual
                            tmpBtnState.fineSnap = c != '1';
                        } else {
                            // FX object '2' is always individual
                            tmpBtnState.fineSnap = c != '2';

                            // Set effect
                            if (c == 'B') {
                                tmpBtnState.effectType = EffectType.BitCrusher;
                                if (currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons] != -1)
                                    tmpBtnState.effectParams[0] = currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons];
                                else
                                    tmpBtnState.effectParams[0] = 5;
                            } else if (c >= 'G' && c <= 'L') {      // Gate 4/8/16/32/12/24
                                tmpBtnState.effectType = EffectType.Gate;
                                short[] paramMap = { 4, 8, 16, 32, 12, 24 };
                                tmpBtnState.effectParams[0] = paramMap[c - 'G'];
                            } else if (c >= 'S' && c <= 'W') {      // Retrigger 8/16/32/12/24
                                tmpBtnState.effectType = EffectType.Retrigger;
                                short[] paramMap = { 8, 16, 32, 12, 24 };
                                tmpBtnState.effectParams[0] = paramMap[c - 'S'];
                            } else if (c == 'Q') {
                                tmpBtnState.effectType = EffectType.Phaser;
                            } else if (c == 'F') {
                                tmpBtnState.effectType = EffectType.Flanger;
                                tmpBtnState.effectParams[0] = 5000;
                            } else if (c == 'X') {
                                tmpBtnState.effectType = EffectType.Wobble;
                                tmpBtnState.effectParams[0] = 12;
                            } else if (c == 'D') {
                                tmpBtnState.effectType = EffectType.SideChain;
                            } else if (c == 'A') {
                                tmpBtnState.effectType = EffectType.TapeStop;
                                if (currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons] != -1) {
                                    tmpBtnState.effectParams[0] = currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons];
                                    tmpBtnState.effectParams[1] = currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons + 1];
                                } else
                                    tmpBtnState.effectParams[0] = 50;
                            } else if (c == '2') {
                                tmpBtnState.sampleIndex = fxSampleIndex[i - 4];
                                tmpBtnState.usingSample = useFxSample[i - 4];
                                tmpBtnState.sampleVolume = fxSampleVolume[i - 4];
                            } else {
                                // Use settings method of setting effects+params (1.60)
                                tmpBtnState.effectType = currentButtonEffectTypes[i - 4];
                                if (currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons] != -1) {
                                    tmpBtnState.effectParams[0] = currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons];
                                    tmpBtnState.effectParams[1] = currentButtonEffectParams[(i - 4) * maxEffectParamsPerButtons + 1];
                                } else {
                                    tmpBtnState.effectParams[0] = dicDefaultEffectParam[tmpBtnState.effectType];
                                    tmpBtnState.effectParams[1] = 0;
                                }
                            }
                        }
                    } else {    // c != 1 && tmpBtnState != null
                                // For buttons not using the 1/32 grid
                        if (!tmpBtnState.fineSnap) {
                            CreateButton();

                            // Create new hold state
                            buttonStates[i] = new TempButtonState(mapTime);
                            tmpBtnState = buttonStates[i];
                            int div = block.mListTick.Count;

                            if (i < 4) {
                                // Normal '1' notes are always individual
                                tmpBtnState.fineSnap = c != '1';
                            } else {
                                // Hold are always on a high enough snap to make suere they are seperate when needed
                                if (c == '2') {
                                    tmpBtnState.fineSnap = false;
                                    tmpBtnState.sampleIndex = fxSampleIndex[i - 4];
                                    tmpBtnState.usingSample = useFxSample[i - 4];
                                    tmpBtnState.sampleVolume = fxSampleVolume[i - 4];
                                } else
                                    tmpBtnState.fineSnap = true;
                            }
                        } else {
                            // Update current hold state
                            tmpBtnState.numTicks++;
                        }
                    }

                    // Terminate last item
                    if (lastTick && tmpBtnState != null)
                        CreateButton();
                }

                // Set laser states
                for (int i = 0; i < 2; i++) {
                    TempLaserState state = laserStates[i];
                    char c = tick.mLaser[i];
                    
                    // Function that creates a new segment out of the current state
                    System.Func<float, LaserData> CreateLaserSegment = delegate (float endPos) {
                        // Process existing segment
                        //assert(state.numTicks > 0);

                        LaserData obj = new LaserData();
                        obj.mTime = state.startTime;
                        obj.mDuration = mapTime - state.startTime;
                        obj.mIndex = i;
                        obj.mPoints[0] = state.startPosition;
                        obj.mPoints[1] = endPos;

                        if (laserRanges[i] > 1.0f) {
                            obj.mFlags |= LaserData.mFlagExtended;
                        }
                        // Threshold for laser segments to be considered instant
                        int laserSlamThreshold = (int)Math.Ceiling(state.tpStart.mBeatDuration / 8.0f);
                        if (obj.mDuration <= laserSlamThreshold && (obj.mPoints[1] != obj.mPoints[0])) {
                            obj.mFlags |= LaserData.mFlagInstant;
                            if (state.spinType != 0) {
                                obj.mSpin.mDuration = state.spinDuration;
                                obj.mSpin.mAmplitude = state.spinBounceAmplitude;
                                obj.mSpin.mFrequency = state.spinBounceFrequency;
                                obj.mSpin.mDecay = state.spinBounceDecay;

                                if (state.spinIsBounce)
                                    obj.mSpin.mType = SpinType.Bounce;
                                else {
                                    switch (state.spinType) {
                                        case '(':
                                        case ')':
                                        obj.mSpin.mType = SpinType.Full;
                                        break;
                                        case '<':
                                        case '>':
                                        obj.mSpin.mType = SpinType.Quarter;
                                        break;
                                        default:
                                        break;
                                    }
                                }

                                switch (state.spinType) {
                                    case '<':
                                    case '(':
                                    obj.mSpin.mDirection = -1.0f;
                                    break;
                                    case ')':
                                    case '>':
                                    obj.mSpin.mDirection = 1.0f;
                                    break;
                                    default:
                                    break;
                                }
                            }
                        }

                        // Link segments together
                        if (state.last != null) {
                            // Always fixup duration so they are connected by duration as well
                            obj.mPrev = state.last;
                            obj.mOrder = obj.mPrev.mOrder + 1;
                            //int actualPrevDuration = obj.mTime - obj.mPrev.mTime;
                            // 슬램레이저의 duration을 0으로 만들어버린다. 왜?
                            //if (obj.mPrev.mDuration != actualPrevDuration) {
                            //    obj.mPrev.mDuration = actualPrevDuration;
                            //}
                            obj.mPrev.mNext = obj;

                        }

                        // Add to list of objects
                        mListObjectState.Add(obj);

                        return obj;
                    };

                    if (c == '-') {
                        // Terminate laser
                        if (state != null) {
                            // Reset state
                            laserStates[i] = null;
                            state = null;

                            // Reset range extension
                            laserRanges[i] = 1.0f;
                        }
                    } else if (c == ':') {
                        // Update current laser state
                        if (state != null) {
                            state.numTicks++;
                        }
                    } else {
                        float pos = DataBase.inst.TranslateLaserChar(c);
                        LaserData last = null;
                        if (state != null) {
                            last = CreateLaserSegment(pos);

                            // Reset state
                            state = null;
                            laserStates[i] = null;
                        }

                        int startTime = mapTime;
                        if (last != null && (last.mFlags & LaserData.mFlagInstant) != 0) {
                            // Move offset to be the same as last segment, as in ksh maps there is a 1 tick delay after laser slams
                            startTime = last.mTime + 1;
                        }
                        laserStates[i] = new TempLaserState(startTime, 0, lastTimingPoint);
                        state = laserStates[i];
                        state.last = last; // Link together
                        state.startPosition = pos;

                        //@[Type][Speed] = spin
                        //Types
                        //) or ( = full spin
                        //> or < = quarter spin
                        //Speed is number of 192nd notes
                        if (!string.IsNullOrEmpty(tick.mAdd) && (tick.mAdd[0] == '@' || tick.mAdd[0] == 'S')) {
                            state.spinIsBounce = tick.mAdd[0] == 'S';
                            state.spinType = tick.mAdd[1];

                            string add = tick.mAdd.Substring(2);
                            if (state.spinIsBounce) {
                                string[] option = add.Split(';');
                                state.spinDuration = int.Parse(option[0]);
                                if (option.Length > 1)
                                    state.spinBounceAmplitude = int.Parse(option[1]);
                                if (option.Length > 2)
                                    state.spinBounceFrequency = int.Parse(option[2]);
                                if (option.Length > 3)
                                    state.spinBounceDecay = int.Parse(option[3]);
                            } else {
                                state.spinDuration = int.Parse(add);
                                // TODO : ??? 필요한건가?
                                //if (state.spinType == '(' || state.spinType == ')')
                                //    state.spinDuration = state.spinDuration;
                            }
                        }
                    }
                }

                lastint = mapTime;
            }

            for (int i = 0; i < firstControlPoints.Length; i++) {
                ZoomControlPoint point = firstControlPoints[i];
                if (point == null)
                    continue;

                ZoomControlPoint dup = new ZoomControlPoint();
                dup.index = point.index;
                dup.zoom = point.zoom;
                dup.time = Int32.MinValue;

                mListZoomPoint.Insert(0, dup);
            }

            //Add chart end event
            EventData evtChartEnd = new EventData();
            evtChartEnd.mTime = lastint + 2000;
            evtChartEnd.mKey = EventKey.ChartEnd;
            mListObjectState.Add(evtChartEnd);

            // Re-sort collection to fix some inconsistencies caused by corrections after laser slams
            mListObjectState.Sort((l, r) => {
                int ret = l.mTime.CompareTo(r.mTime);
                if (ret == 0 && l.mType == ButtonType.Laser && r.mType == ButtonType.Laser && ((LaserData)l).mIndex == ((LaserData)r).mIndex) {
                    ret = ((LaserData)l).mOrder.CompareTo(((LaserData)r).mOrder);
                }
                return ret;
            });
        }

        EffectType GetEffectType(string type) {
            if (Enum.IsDefined(typeof(EffectType), type))
                return (EffectType)Enum.Parse(typeof(EffectType), type);

            if (type == "LPF")
                return EffectType.LowPassFilter;
            else if (type == "HPF")
                return EffectType.HighPassFilter;
            else if (type == "PEAK")
                return EffectType.PeakingFilter;
            else
                return EffectType.None;
        }
        EffectType mCustomTypeIndex = EffectType.UserDefined0;
        EffectType GetEffectTypeIncludeCustom(string type) {
            EffectType eType = GetEffectType(type);
            if (eType == EffectType.None)
                return mCustomTypeIndex++;
            else
                return eType;
        }

        class MultiParams {
            public bool isFloat;
            public bool isRange;
            public bool isSample;
            public float[] fval = new float[2];
            public int[] ival = new int[2];

            public MultiParams(string[] vals) {
                if (vals.Length == 1) {
                    FillValues(vals[0], 0);
                    return;
                }

                isRange = true;
                for (int i = 0; i < 2; i++) {
                    FillValues(vals[i], i);
                }
            }

            void FillValues(string val, int idx) {
                if (val.Contains(".")) {
                    isFloat = true;
                    fval[idx] = float.Parse(val);
                } else if (val.Contains("/")) {
                    isFloat = true;
                    string[] devide = val.Split('/');
                    fval[idx] = float.Parse(devide[0]) / float.Parse(devide[1]);
                } else if (val.Contains("sample")) {
                    isSample = true;
                    ival[idx] = int.Parse(val);
                } else {
                    ival[idx] = int.Parse(val);
                }
            }

            public EffectParam<float> GetFloatParam() {
                EffectParam<float> param = null;
                if (isFloat)
                    param = new EffectParam<float>(fval[0], fval[1]);
                else
                    param = new EffectParam<float>(ival[0], ival[1]);
                param.mIsRange = isRange;
                return param;
            }

            public EffectParam<EffectDuration> GetDurationParam() {
                EffectParam<EffectDuration> param = null;
                if (isFloat)
                    param = new EffectParam<EffectDuration>(new EffectDuration(fval[0]), new EffectDuration(fval[1]));
                else
                    param = new EffectParam<EffectDuration>(new EffectDuration(ival[0]), new EffectDuration(ival[1]));
                param.mIsRange = isRange;
                return param;
            }

            public EffectParam<int> GetIntParam() {
                EffectParam<int> param = null;
                if (!isFloat)
                    param = new EffectParam<int>(ival[0], ival[1]);
                param.mIsRange = isRange;
                return param;
            }
        }

        AudioEffect MakeCustomEffect(KShootEffectDefinition def) {
            AudioEffect effect = null;
            bool bTypeSet = false;
            Dictionary<string, MultiParams> dicParams = new Dictionary<string, MultiParams>();
            foreach (var key in def.mParameters) {
                if (key.Key == "type") {
                    EffectType type = GetEffectType(key.Value);
                    if (type == EffectType.None) {
                        Debug.Log(string.Format("Unknown base effect type for custom effect type: {0}", key.Value));
                        continue;
                    }

                    effect = DataBase.inst.mEffectSetting.CreateDefault(type);
                } else {
                    dicParams.Add(key.Key, new MultiParams(key.Value.Split('-')));
                }
            }

            if (!bTypeSet) {
                Debug.Log(string.Format("Type not set for custom effect type: {0}", def.mTypeName));
                return effect;
            }

            if (dicParams.ContainsKey("mix")) {
                effect.mMix = dicParams["mix"].GetFloatParam();
            }

            if (effect.mType == EffectType.PitchShift) {
                if (dicParams.ContainsKey("pitch")) {
                    ((AudioEffectPitchshift)effect).amount = dicParams["pitch"].GetFloatParam();
                }
            } else if (effect.mType == EffectType.BitCrusher) {
                if (dicParams.ContainsKey("amount")) {
                    ((AudioEffectBitcrusher)effect).reduction = dicParams["amount"].GetIntParam();
                }
            } else if (effect.mType == EffectType.Echo) {
                AudioEffectEcho echo = (AudioEffectEcho)effect;
                if (dicParams.ContainsKey("waveLength")) {
                    echo.mDuration = dicParams["waveLength"].GetDurationParam();
                }
                if (dicParams.ContainsKey("feedbackLevel")) {
                    echo.feedback = dicParams["feedbackLevel"].GetFloatParam();
                }
            } else if (effect.mType == EffectType.Flanger) {
                if (dicParams.ContainsKey("period")) {
                    ((AudioEffectFlanger)effect).mDuration = dicParams["period"].GetDurationParam();
                }
            } else if (effect.mType == EffectType.Gate) {
                AudioEffectGate gate = (AudioEffectGate)effect;
                if (dicParams.ContainsKey("waveLength")) {
                    gate.mDuration = dicParams["waveLength"].GetDurationParam();
                }
                if (dicParams.ContainsKey("rate")) {
                    gate.gate = dicParams["rate"].GetFloatParam();
                }
            } else if (effect.mType == EffectType.Retrigger) {
                AudioEffectRetrigger retrigger = (AudioEffectRetrigger)effect;
                if (dicParams.ContainsKey("waveLength")) {
                    retrigger.mDuration = dicParams["waveLength"].GetDurationParam();
                }
                if (dicParams.ContainsKey("rate")) {
                    retrigger.gate = dicParams["rate"].GetFloatParam();
                }
                if (dicParams.ContainsKey("updatePeriod")) {
                    retrigger.reset = dicParams["updatePeriod"].GetDurationParam();
                }
            } else if (effect.mType == EffectType.Wobble) {
                AudioEffectWobble wobble = (AudioEffectWobble)effect;
                if (dicParams.ContainsKey("waveLength")) {
                    wobble.mDuration = dicParams["waveLength"].GetDurationParam();
                }
                if (dicParams.ContainsKey("loFreq")) {
                    wobble.min = dicParams["loFreq"].GetFloatParam();
                }
                if (dicParams.ContainsKey("hiFreq")) {
                    wobble.max = dicParams["hiFreq"].GetFloatParam();
                }
                if (dicParams.ContainsKey("Q")) {
                    wobble.q = dicParams["Q"].GetFloatParam();
                }
            } else if (effect.mType == EffectType.TapeStop) {
                if (dicParams.ContainsKey("speed")) {
                    effect.mDuration = dicParams["speed"].GetDurationParam();
                }
            }

            return effect;
        }

        public AudioEffect GetEffect(EffectType type) {
            if (type >= EffectType.UserDefined0) {
                return mDicCustomEffect[type];
            }
            return DataBase.inst.mEffectSetting.GetDefault(type);
        }
        public AudioEffect GetFilter(EffectType type) {
            if (type >= EffectType.UserDefined0) {
                return mDicCustomFilter[type];
            }
            return DataBase.inst.mEffectSetting.GetDefault(type);
        }

    }



    // Temporary object to keep track if a button is a hold button
    public class TempButtonState {
        public int startTime;
        public int numTicks;
        public EffectType effectType;
        public short[] effectParams = new short[2];
        // If using the smalles grid to indicate hold note duration
        public bool fineSnap = false;       // true 이면 롱노트
                                            // Set for hold continuations, this is where there is a hold right after an existing one but with different effects
        public HoldButtonData lastHoldObject;

        public int sampleIndex;
        public bool usingSample;
        public float sampleVolume = 1.0f;

        public TempButtonState(int time) {
            startTime = time;
        }
    }
    public class TempLaserState {
        // Timing point at which this segment started
        public TimingPoint tpStart;
        public int startTime;
        public int numTicks;
        public int effectType;
        public bool spinIsBounce;
        public char spinType;
        public int spinDuration;
        public int spinBounceAmplitude;
        public int spinBounceFrequency;
        public int spinBounceDecay;
        public byte effectParams;
        public float startPosition; // Entry position
        public LaserData last;      // Previous segment

        public TempLaserState(int time, int effType, TimingPoint tp) {
            startTime = time;
            effectType = effType;
            tpStart = tp;
        }
    }
}
