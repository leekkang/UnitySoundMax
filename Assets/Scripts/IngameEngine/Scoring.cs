﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SoundMax;
using System.Linq;

public enum TickFlags : byte {
    None = 0,
    // Used for segment start/end parts
    Start = 0x1,
    End = 0x2,
    // Hold notes (BT or FX)
    Hold = 0x4,
    // Normal/Single hit buttons
    Button = 0x8,
    // For lasers only
    Laser = 0x10,
    Slam = 0x20,
}

public enum ScoreHitRating {
    Miss = 0,
    Good,
    Perfect,
    Idle, // Not actual score, used when a button is pressed when there are no notes
}

public class ScoreTick {
    public TickFlags flags;
    public int time;
    public ObjectDataBase obj;

    public ScoreTick(ObjectDataBase src) {
        obj = src;
    }
    public ScoreTick(ScoreTick src) {
        flags = src.flags;
        time = src.time;
        obj = src.obj;
    }

    public int GetHitWindow() {
        // Hold ticks don't have a hit window, but the first ones do
        if (HasFlag(TickFlags.Hold) && !HasFlag(TickFlags.Start))
            return 0;
        // Laser ticks also don't have a hit window except for the first ticks and slam segments
        if (HasFlag(TickFlags.Laser)) {
            if (!HasFlag(TickFlags.Start) && !HasFlag(TickFlags.Slam))
                return 0;

            return Scoring.inst.perfectHitTime;
        }

        return Scoring.inst.missHitTime;
    }

    public ScoreHitRating GetHitRating(int currentTime) {
        int delta = Math.Abs(time - currentTime);
        return GetHitRatingFromDelta(delta);
    }
    public ScoreHitRating GetHitRatingFromDelta(int delta) {
        delta = Math.Abs(delta);

        if (HasFlag(TickFlags.Button)) {
            if (delta <= Scoring.inst.perfectHitTime)
                return ScoreHitRating.Perfect;
            if (delta <= Scoring.inst.goodHitTime)
                return ScoreHitRating.Good;
            return ScoreHitRating.Miss;
        }
        return ScoreHitRating.Perfect;
    }
    public bool HasFlag(TickFlags flag) {
        return (flags & flag) != TickFlags.None;
    }
}

public class MapTotals {
    // Number of single notes
    public int numSingles;
    // Number of laser/hold ticks
    public int numTicks;
    // The maximum possible score a Map can give
    // The score is calculated per 2 (2 = critical, 1 = near)
    // Hold buttons, lasers, etc. give 2 points per tick
    public int maxScore;
}

public class HitStat {
    public ObjectDataBase obj;
    public int time;
    public int delta;
    public ScoreHitRating rating;
    // Hold state
    // This is the amount of gotten ticks in a hold sequence
    public uint hold = 0;
    // This is the amount of total ticks in this hold sequence
    public uint holdMax = 0;
    // If at least one hold tick has been missed
    public bool hasMissed = false;

    public bool forReplay = true;

    public HitStat(ObjectDataBase src) {
        obj = src;
    }
}

class SimpleHitStat {
    // 0 = miss, 1 = near, 2 = crit, 3 = idle
    public byte rating;
    public byte lane;
    public int time;
    public int delta;
    // Hold state
    // This is the amount of gotten ticks in a hold sequence
    public uint hold;
    // This is the amount of total ticks in this hold sequence
    public uint holdMax;
}

class ScoreIndex {
    public int id;
    public int diffid;
    public int score;
    public int crit;
    public int almost;
    public int miss;
    public float gauge;
    public uint gameflags;
    public List<SimpleHitStat> hitStats;
    public long timestamp;
};

public class Scoring : Singleton<Scoring> {
    public readonly int missHitTime = 275;
    public readonly int goodHitTime = 100;
    public readonly int perfectHitTime = 42;
    public readonly float idleLaserSpeed = 1f;

    // Map total infos
    MapTotals mapTotals;
    // Maximum accumulated score of object that have been hit or missed
    // used to calculate accuracy up to a give point
    uint currentMaxScore = 0;
    // The actual amount of gotten score
    uint currentHitScore = 0;

    // Amount of gauge to gain on a tick
    float tickGaugeGain = 0.0f;
    // Hits per type in order:
    //	0 = Miss
    //	1 = Good
    //	2 = Perfect
    uint[] categorizedHits = new uint[3];

    // Early and Late count:
    // 0 = Early
    // 1 = Late
    uint[] timedHits = new uint[2];

    // Amount of gauge to gain on a short note
    float shortGaugeGain = 0.0f;

    // Current gauge 0 to 1
    public float currentGauge = 0.0f;

    // Current combo
    public int currentComboCounter;

    // Combo state (0 = regular, 1 = full combo, 2 = perfect)
    byte comboState = 2;

    // Highest combo in current run
    int maxComboCounter;

    // The timings of hit objects, sorted by time hit
    // these are used for debugging
    List<HitStat> hitStats = new List<HitStat>();

    // Autoplay mode
    bool autoplay = false;
    // Autoplay but for buttons
    public bool autoplayButtons = false;

    float laserDistanceLeniency = 1.0f / 12.0f;

    // Actual positions of the laser
    float[] laserPositions = new float[2];
    // Sampled target position of the lasers in the Map
    float[] laserTargetPositions = new float[2];
    // Current lasers are extended
    bool[] lasersAreExtend = new bool[2];
    // Time since laser has been used
    public float[] timeSinceLaserUsed = new float[2];

    bool m_interpolateLaserOutput = false;

    // Lerp for laser output
    float m_laserOutputSource = 0.0f;
    float m_laserOutputTarget = 0.0f;
    float m_timeSinceOutputSet = 0.0f;

    PlaybackEngine m_playback;

    // Input values for laser [-1,1]
    float[] m_laserInput = new float[2];
    // Keeps being set to the last direction the laser was moving in to create laser intertia
    float[] m_lastLaserInputDirection = new float[2];
    // Decides if the coming tick should be auto completed
    float[] m_autoLaserTime = new float[2];
    // Saves the time when a button was hit, used to decide if a button was held before a hold object was active
    int[] m_buttonHitTime = new int[6];
    // Saves the time when a button was hit or released for bounce guarding
    int[] m_buttonGuardTime = new int[6];
    // Max number of ticks to assist
    float m_assistLevel = 1.5f;
    float m_assistSlamBoost = 1.5f;
    float m_assistPunish = 1.5f;
    float m_assistTime = 0.0f;
    // Offet to use for calculating judge (ms)
    int m_inputOffset = 0;
    int m_bounceGuard = 0;

    // used the update the amount of hit ticks for hold/laser notes
    Dictionary<ObjectDataBase, HitStat> m_holdHitStats = new Dictionary<ObjectDataBase, HitStat>();

    // Laser objects currently in range
    //	used to sample target laser positions
    LaserData[] m_currentLaserSegments = new LaserData[2];
    // Queue for the above list
    List<LaserData> m_laserSegmentQueue = new List<LaserData>();

    // Ticks for each BT[4] / FX[2] / Laser[2]
    List<ScoreTick>[] m_ticks = new List<ScoreTick>[] {
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>(),
        new List<ScoreTick>()
    };
    // Hold objects
    ObjectDataBase[] m_holdObjects = new ObjectDataBase[8];
    List<ObjectDataBase> m_heldObjects = new List<ObjectDataBase>();

    GameFlags m_flags;

    // Called when a hit is recorded on a given button index (excluding hold notes)
    // (Hit Button, Score, Hit Object(optional))
    public System.Action<ObjectDataBase> OnObjectEntered;
    public System.Action<int, ScoreHitRating, ObjectDataBase, bool> OnButtonHit;
    // Called when a miss is recorded on a given button index
    public System.Action<int, bool> OnButtonMiss;

    // Called when an object is picked up
    public System.Action<int, ObjectDataBase> OnObjectHold;
    // Called when an object is let go of
    public System.Action<int, ObjectDataBase> OnObjectReleased;

    // Called when a laser slam was hit
    // (Laser slam segment)
    public System.Action<LaserData> OnLaserSlamHit;
    // Called when the combo counter changed
    // (New Combo)
    public System.Action<int> OnComboChanged;

    // Called when score has changed
    //	(New Score)
    public System.Action<int> OnScoreChanged;


    string CalculateGrade(uint score) {
        if (score >= 9900000) // S
            return "S";
        if (score >= 9800000) // AAA+
            return "AAA+";
        if (score >= 9700000) // AAA
            return "AAA";
        if (score >= 9500000) // AA+
            return "AA+";
        if (score >= 9300000) // AA
            return "AA";
        if (score >= 9000000) // A+
            return "A+";
        if (score >= 8700000) // A
            return "A";
        if (score >= 7500000) // B
            return "B";
        if (score >= 6500000) // C
            return "C";
        return "D"; // D
    }

    byte CalculateBadge(ScoreIndex score) {
        if (score.score == 10000000) //Perfect
            return 5;
        if (score.miss == 0) //Full Combo
            return 4;
        if (((GameFlags)score.gameflags & GameFlags.Hard) != GameFlags.None && score.gauge > 0) //Hard Clear
            return 3;
        if (((GameFlags)score.gameflags & GameFlags.Hard) == GameFlags.None && score.gauge >= 0.70) //Normal Clear
            return 2;

        return 1; //Failed
    }

    byte CalculateBestBadge(List<ScoreIndex> scores) {
        if (scores.Count < 1)
            return 0;
        byte top = 1;
        foreach (ScoreIndex score in scores) {
            byte temp = CalculateBadge(score);
            if (temp > top) {
                top = temp;
            }
        }
        return top;
    }

    public void SetPlayback(PlaybackEngine playback) {
        m_playback = playback;
        //m_playback.OnFXBegin.Add(this, &m_OnFXBegin);
        m_playback.OnObjectEntered = m_OnObjectEntered;
        m_playback.OnObjectLeaved = m_OnObjectLeaved;
    }

    public void SetFlags(GameFlags flags) {
        m_flags = flags;
    }

    public void Reset() {
        // Reset score/combo counters
        currentMaxScore = 0;
        currentHitScore = 0;
        currentComboCounter = 0;
        maxComboCounter = 0;
        comboState = 2;
        m_assistTime = m_assistLevel * 0.1f;

        // Reset laser positions
        laserTargetPositions[0] = 0.0f;
        laserTargetPositions[1] = 0.0f;
        laserPositions[0] = 0.0f;
        laserPositions[1] = 1.0f;
        timeSinceLaserUsed[0] = 1000.0f;
        timeSinceLaserUsed[1] = 1000.0f;

        for (int i = 0; i < categorizedHits.Length; i++)
            categorizedHits[0] = 0;
        for (int i = 0; i < timedHits.Length; i++)
            timedHits[0] = 0;
        // Clear hit statistics
        hitStats.Clear();

        // Get input offset
        //m_inputOffset = g_gameConfig.GetInt(GameConfigKeys.InputOffset);
        //// Get bounce guard duration
        //m_bounceGuard = g_gameConfig.GetInt(GameConfigKeys.InputBounceGuard);
        //// Get laser assist level
        //m_assistLevel = g_gameConfig.GetFloat(GameConfigKeys.LaserAssistLevel);
        //m_assistSlamBoost = g_gameConfig.GetFloat(GameConfigKeys.LaserSlamBoost);
        //m_assistPunish = g_gameConfig.GetFloat(GameConfigKeys.LaserPunish);
        // Recalculate maximum score
        mapTotals = CalculateMapTotals();

        // Recalculate gauge gain

        currentGauge = 0.0f;
        float total = m_playback.m_beatmap.mSetting.total / 100.0f + 0.001f; //Add a little in case floats go under
        if ((m_flags & GameFlags.Hard) != GameFlags.None) {
            total *= 12f / 21f;
            currentGauge = 1.0f;
        }

        if (mapTotals.numTicks == 0 && mapTotals.numSingles != 0) {
            shortGaugeGain = total / mapTotals.numSingles;
        } else if (mapTotals.numSingles == 0 && mapTotals.numTicks != 0) {
            tickGaugeGain = total / mapTotals.numTicks;
        } else {
            shortGaugeGain = (total * 20) / (5.0f * (mapTotals.numTicks + (4.0f * mapTotals.numSingles)));
            tickGaugeGain = shortGaugeGain / 4.0f;
        }

        m_heldObjects.Clear();
        for (int i = 0; i < m_holdObjects.Length; i++)
            m_holdObjects[0] = null;
        for (int i = 0; i < m_currentLaserSegments.Length; i++)
            m_currentLaserSegments[0] = null;
        m_CleanupHitStats();
        m_CleanupTicks();

        OnScoreChanged(0);
    }

    public void FinishGame() {
        m_CleanupTicks();
        for (int i = 0; i < 8; i++) {
            m_ReleaseHoldObject(i);
        }
    }

    public void Tick(float deltaTime) {
        m_UpdateLasers(deltaTime);
        m_UpdateTicks();
        if (!autoplay && !autoplayButtons)
            return;

        for (int i = 0; i < 6; i++) {
            if (m_ticks[i].Count > 0) {
                ScoreTick tick = m_ticks[i][0];
                if (tick.HasFlag(TickFlags.Hold)) {
                    if (tick.obj.mTime <= m_playback.GetLastTime())
                        m_ListHoldObject(tick.obj, i);
                }
            }
        }
    }

    float GetLaserRollOutput(uint index) {
        //assert(index >= 0 && index <= 1);
        if (m_currentLaserSegments[index] != null) {
            if (index == 0)
                return -laserTargetPositions[index];
            if (index == 1)
                return (1.0f - laserTargetPositions[index]);
        } else { // Check if any upcoming lasers are within 2 beats
            for (int i = 0; i < m_laserSegmentQueue.Count; i++) {
                LaserData laser = m_laserSegmentQueue[i];
                if (laser.mIndex == index && laser.mPrev == null) {
                    if (laser.mTime - m_playback.GetLastTime() <= m_playback.GetCurrentTimingPoint().mBeatDuration * 2) {
                        if (index == 0)
                            return -laser.mPoints[0];
                        if (index == 1)
                            return (1.0f - laser.mPoints[0]);
                    }
                }
            }
        }
        return 0.0f;
    }

    const float laserOutputInterpolationDuration = 0.1f;
    public float GetLaserOutput() {
        float f = Math.Min(1.0f, m_timeSinceOutputSet / laserOutputInterpolationDuration);
        return m_laserOutputSource + (m_laserOutputTarget - m_laserOutputSource) * f;
    }
    float GetMeanHitDelta() {
        float sum = 0;
        uint count = 0;
        for (int i = 0; i < hitStats.Count; i++) {
            HitStat hit = hitStats[i];
            if (hit.obj.mType != ButtonType.Single || hit.rating == ScoreHitRating.Miss)
                continue;

            sum += hit.delta;
            count++;
        }
        return sum / count;
    }
    int GetMedianHitDelta() {
        List<int> deltas = new List<int>();
        for (int i = 0; i < hitStats.Count; i++) {
            HitStat hit = hitStats[i];
            if (hit.obj.mType == ButtonType.Single && hit.rating != ScoreHitRating.Miss)
                deltas.Add(hit.delta);
        }

        if (deltas.Count == 0)
            return 0;

        deltas.Sort();
        return deltas[deltas.Count / 2];
    }

    float m_GetLaserOutputRaw() {
        float val = 0.0f;
        for (int i = 0; i < 2; i++) {
            if (!IsLaserHeld(i, false) || m_currentLaserSegments[i] == null)
                continue;

            // Skip single or end slams
            if (m_currentLaserSegments[i].mNext == null && (m_currentLaserSegments[i].mFlags & LaserData.mFlagInstant) != 0)
                continue;

            float actual = laserTargetPositions[i];
            // Undo laser extension
            if ((m_currentLaserSegments[i].mFlags & LaserData.mFlagExtended) != 0) {
                actual = (actual + 0.5f) * 0.5f;
                //assert(actual >= 0.0f && actual <= 1.0f);
            }
            // Second laser goes the other way
            if (i == 1)
                actual = 1.0f - actual;
            val = Math.Max(actual, val);
        }
        return val;
    }

    void m_UpdateLaserOutput(float deltaTime) {
        m_timeSinceOutputSet += deltaTime;
        float v = m_GetLaserOutputRaw();
        if (v != m_laserOutputTarget) {
            m_laserOutputTarget = v;
            m_laserOutputSource = GetLaserOutput();
            m_timeSinceOutputSet = m_interpolateLaserOutput ? 0.0f : laserOutputInterpolationDuration;
        }
    }

    HitStat m_AddOrUpdateHitStat(ObjectDataBase obj) {
        if (obj.mType == ButtonType.Single) {
            HitStat stat = new HitStat(obj);
            hitStats.Add(stat);
            return stat;
        } else if (obj.mType == ButtonType.Hold) {
            HoldButtonData hold = (HoldButtonData)obj;
            if (m_holdHitStats.ContainsKey(obj))
                return m_holdHitStats[obj];

            HitStat stat = new HitStat(obj);
            hitStats.Add(stat);
            m_holdHitStats.Add(obj, stat);

            // Get tick count
            List<int> ticks = m_CalculateHoldTicks(hold);
            stat.holdMax = (uint)ticks.Count;
            stat.forReplay = false;

            return stat;
        } else if (obj.mType == ButtonType.Laser) {
            LaserData rootLaser = ((LaserData)obj).GetRoot();
            if (m_holdHitStats.ContainsKey(rootLaser))
                return m_holdHitStats[rootLaser];

            HitStat stat = new HitStat(rootLaser);
            hitStats.Add(stat);
            m_holdHitStats.Add(obj, stat);

            // Get tick count
            List<ScoreTick> ticks = m_CalculateLaserTicks(rootLaser);
            stat.holdMax = (uint)ticks.Count;
            stat.forReplay = false;

            return stat;
        }

        // Shouldn't get here
        //assert(false);
        Debug.Log("m_AddOrUpdateHitStat : 뭔가 오류가 있음. 암튼 있음.");
        return null;
    }

    void m_CleanupHitStats() {
        hitStats.Clear();
        m_holdHitStats.Clear();
    }

    bool IsObjectHeld(ObjectDataBase obj) {
        if (obj.mType == ButtonType.Laser) {
            // Select root node of laser
            obj = ((LaserData)obj).GetRoot();
        } else if (obj.mType == ButtonType.Hold) {
            // Check all hold notes in a hold sequence to see if it is held
            bool held = false;
            HoldButtonData root = ((HoldButtonData)obj).GetRoot();
            for (; root != null; root = root.mNext) {
                if (m_heldObjects.Contains(root)) {
                    held = true;
                    break;
                }
            }
            return held;
        }

        return m_heldObjects.Contains(obj);
    }

    bool IsObjectHeld(int index) {
        //assert(index < 8);
        return m_holdObjects[index] != null;
    }

    public bool IsLaserHeld(int laserIndex, bool includeSlams) {
        if (includeSlams)
            return IsObjectHeld(laserIndex + 6);

        if (m_holdObjects[laserIndex + 6] != null) {
            // Check for slams
            return (((LaserData)m_holdObjects[laserIndex + 6]).mFlags & LaserData.mFlagInstant) == 0;
        }
        return false;
    }

    bool IsLaserIdle(uint index) {
        return m_laserSegmentQueue.Count == 0 && m_currentLaserSegments[0] == null && m_currentLaserSegments[1] == null;
    }

    List<int> m_CalculateHoldTicks(HoldButtonData hold) {
        List<int> ticks = new List<int>();
        TimingPoint tp = m_playback.GetTimingPointAt(hold.mTime);

        // Tick rate based on BPM
        double tickNoteValue = 16 / Math.Pow(2f, Math.Max((int)(Math.Log(tp.GetBPM(), 2f)) - 7, 0));
        double tickInterval = tp.GetWholeNoteLength() / tickNoteValue;

        double tickpos = hold.mTime;
        if (hold.mPrev == null) // no tick at the very start of a hold
            tickpos += tickInterval;

        while (tickpos < hold.mTime + hold.mDuration - tickInterval) {
            ticks.Add((int)tickpos);
            tickNoteValue = 16 / Math.Pow(2, Math.Max((int)(Math.Log(tp.GetBPM(), 2f)) - 7, 0));
            tickInterval = tp.GetWholeNoteLength() / tickNoteValue;
            tickpos += tickInterval;
        }

        if (ticks.Count == 0)
            ticks.Add(hold.mTime + (hold.mDuration / 2));

        return ticks;
    }

    List<ScoreTick> m_CalculateLaserTicks(LaserData laserRoot) {
        //assert(laserRoot.mPrev == null);
        List<ScoreTick> ticks = new List<ScoreTick>();
        TimingPoint tp = m_playback.GetTimingPointAt(laserRoot.mTime);

        // Tick rate based on BPM
        double tickNoteValue = 16f / Math.Pow(2, Math.Max((int)(Math.Log(tp.GetBPM(), 2f)) - 7, 0));
        double tickInterval = tp.GetWholeNoteLength() / tickNoteValue;

        LaserData sectionStart = laserRoot;
        int sectionStartTime = laserRoot.mTime;
        int combinedDuration = 0;
        LaserData lastSlam = null;
        System.Action AddTicks = delegate () {
            int numTicks = Convert.ToInt32(Math.Floor(combinedDuration / tickInterval));
            for (int i = 0; i < numTicks; i++) {
                if (lastSlam != null && i == 0) // No first tick if connected to slam
                    continue;

                ScoreTick t = new ScoreTick(sectionStart);
                t.time = sectionStartTime + (int)(tickInterval * i);
                t.flags = TickFlags.Laser;

                // Link this tick to the correct segment
                if (sectionStart.mNext != null && (sectionStart.mTime + sectionStart.mDuration) <= t.time) {
                    //assert((sectionStart.mNext.mFlags & LaserData.mFlagInstant) == 0);
                    t.obj = sectionStart = sectionStart.mNext;
                }

                if (lastSlam == null && i == 0)
                    t.flags |= TickFlags.Start;

                ticks.Add(t);
            }
            combinedDuration = 0;
        };

        for (LaserData it = laserRoot; it != null; it = it.mNext) {
            if ((it.mFlags & LaserData.mFlagInstant) == 0) {
                combinedDuration += it.mDuration;
                continue;
            }

            AddTicks();
            ScoreTick t = new ScoreTick(it);
            t.time = it.mTime;
            t.flags = TickFlags.Laser | TickFlags.Slam;
            if (it.mPrev == null)
                t.flags |= TickFlags.Start;
            lastSlam = it;
            if (it.mNext != null) {
                sectionStart = it.mNext;
                sectionStartTime = it.mNext.mTime;
            } else {
                sectionStart = null;
                sectionStartTime = it.mTime;
            }
            ticks.Add(t);
        }
        AddTicks();
        if (ticks.Count > 0)
            ticks.Last().flags = TickFlags.End;

        return ticks;
    }

    void m_OnFXBegin(HoldButtonData obj) {
        if (autoplay || autoplayButtons)
            m_ListHoldObject(obj, obj.mIndex);
    }

    void m_OnObjectEntered(ObjectDataBase obj) {
        // The following code registers which ticks exist depending on the object type / duration
        if (obj.mType == ButtonType.Single) {
            NormalButtonData bt = (NormalButtonData)obj;
            ScoreTick t = new ScoreTick(obj);
            t.time = bt.mTime;
            t.flags |= TickFlags.Button;

            m_ticks[bt.mIndex].Add(t);
        } else if (obj.mType == ButtonType.Hold) {
            TimingPoint tp = m_playback.GetTimingPointAt(obj.mTime);
            HoldButtonData hold = (HoldButtonData)obj;

            // Add all hold ticks
            List<int> holdTicks = m_CalculateHoldTicks(hold);
            for (int i = 0; i < holdTicks.Count; i++) {
                ScoreTick t = new ScoreTick(obj);
                t.flags |= TickFlags.Hold;
                if (i == 0 && hold.mPrev == null)
                    t.flags |= TickFlags.Start;
                if (i == holdTicks.Count - 1 && hold.mNext == null)
                    t.flags |= TickFlags.End;

                t.time = holdTicks[i];
                m_ticks[hold.mIndex].Add(t);
            }
        } else if (obj.mType == ButtonType.Laser) {
            LaserData laser = (LaserData)obj;
            if (laser.mPrev == null) { // Only register root laser objects
                // Can cause problems if the previous laser segment hasnt ended yet for whatever reason
                if (m_currentLaserSegments[laser.mIndex] == null) {
                    bool anyInQueue = false;
                    for (int i = 0; i < m_laserSegmentQueue.Count; i++) {
                        if (m_laserSegmentQueue[i].mIndex == laser.mIndex) {
                            anyInQueue = true;
                            break;
                        }
                    }

                    if (!anyInQueue) {
                        timeSinceLaserUsed[laser.mIndex] = 0;
                        laserPositions[laser.mIndex] = laser.mPoints[0];
                        laserTargetPositions[laser.mIndex] = laser.mPoints[0];
                        lasersAreExtend[laser.mIndex] = (laser.mFlags & LaserData.mFlagExtended) != 0;
                    }
                }
                // All laser ticks, including slam segments
                List<ScoreTick> laserTicks = m_CalculateLaserTicks(laser);
                for (int i = 0; i < laserTicks.Count; i++) {
                    // Add copy
                    m_ticks[laser.mIndex + 6].Add(new ScoreTick(laserTicks[i]));
                }
            }

            // Add to laser segment queue
            m_laserSegmentQueue.Add(laser);
        }
    }
    void m_OnObjectLeaved(ObjectDataBase obj) {
        if (obj.mType == ButtonType.Laser) {
            LaserData laser = (LaserData)obj;
            if (laser.mNext != null)
                return; // Only terminate holds on last of laser section
            obj = laser.GetRoot();
        }
        m_ReleaseHoldObject(obj);
    }

    void m_UpdateTicks() {
        int currentTime = m_playback.m_playbackTime;

        // This loop checks for ticks that are missed
        for (int buttonCode = 0; buttonCode < 8; buttonCode++) {
            // List of ticks for the current button code
            List<ScoreTick> ticks = m_ticks[buttonCode];
            for (int i = 0; i < ticks.Count; i++) {
                ScoreTick tick = ticks[i];
                int delta = currentTime - ticks[i].time;
                bool shouldMiss = Math.Abs(delta) > tick.GetHitWindow();
                bool processed = false;
                if (delta >= 0) {
                    if (tick.HasFlag(TickFlags.Button) && (autoplay || autoplayButtons)) {
                        m_TickHit(tick, buttonCode, 0);
                        processed = true;
                    }

                    if (tick.HasFlag(TickFlags.Hold)) {
                        HoldButtonData hold = (HoldButtonData)tick.obj;
                        int holdStart = hold.GetRoot().mTime;

                        // Check buttons here for holds
                        if ((KeyboardManager.inst.CheckHold(buttonCode) && holdStart - goodHitTime < m_buttonHitTime[buttonCode]) || autoplay || autoplayButtons) {
                            m_TickHit(tick, buttonCode);
                            HitStat stat = new HitStat(tick.obj);
                            stat.time = currentTime;
                            stat.rating = ScoreHitRating.Perfect;
                            hitStats.Add(stat);
                            processed = true;
                        }
                    } else if (tick.HasFlag(TickFlags.Laser)) {
                        LaserData laserObject = (LaserData)tick.obj;
                        if (tick.HasFlag(TickFlags.Slam)) {
                            // Check if slam hit
                            float dirSign = Math.Sign(laserObject.GetDirection());
                            float inputSign = KeyboardManager.inst.GetLaserDirection(buttonCode - 6);
                            float posDelta = (laserObject.mPoints[1] - laserPositions[buttonCode - 6]) * dirSign;
                            if (autoplay) {
                                inputSign = dirSign;
                                posDelta = 1;
                            }
                            if (dirSign == inputSign && delta > -10 && posDelta >= -laserDistanceLeniency) {
                                m_TickHit(tick, buttonCode);
                                HitStat stat = new HitStat(tick.obj);
                                stat.time = currentTime;
                                stat.rating = ScoreHitRating.Perfect;
                                hitStats.Add(stat);
                                processed = true;
                            }
                        } else {
                            // Snap to first laser tick
                            /// TODO: Find better solution
                            if (tick.HasFlag(TickFlags.Start)) {
                                laserPositions[laserObject.mIndex] = laserTargetPositions[laserObject.mIndex];
                                m_autoLaserTime[laserObject.mIndex] = m_assistTime;
                            }

                            // Check laser input
                            float laserDelta = Math.Abs(laserPositions[laserObject.mIndex] - laserTargetPositions[laserObject.mIndex]);

                            if (laserDelta < laserDistanceLeniency) {
                                m_TickHit(tick, buttonCode);
                                HitStat stat = new HitStat(tick.obj);
                                stat.time = currentTime;
                                stat.rating = ScoreHitRating.Perfect;
                                hitStats.Add(stat);
                                processed = true;
                            }
                        }
                    }
                } else if (tick.HasFlag(TickFlags.Slam) && !shouldMiss) {
                    LaserData laserObject = (LaserData)tick.obj;
                    // Check if slam hit
                    float dirSign = Math.Sign(laserObject.GetDirection());
                    float inputSign = KeyboardManager.inst.GetLaserDirection(buttonCode - 6);
                    float posDelta = (laserObject.mPoints[1] - laserPositions[buttonCode - 6]) * dirSign;
                    if (dirSign == inputSign && posDelta >= -laserDistanceLeniency) {
                        m_TickHit(tick, buttonCode);
                        HitStat stat = new HitStat(tick.obj);
                        stat.time = currentTime;
                        stat.rating = ScoreHitRating.Perfect;
                        hitStats.Add(stat);
                        processed = true;
                    }
                }

                if (delta > goodHitTime && !processed) {
                    m_TickMiss(tick, buttonCode, delta);
                    processed = true;
                }

                if (processed) {
                    ticks.Remove(tick);
                    i--;
                } else {
                    // No further ticks to process
                    break;
                }
            }
        }
    }

    ObjectDataBase m_ConsumeTick(int buttonCode) {
        //assert(buttonCode < 8);

        int currentTime = m_playback.m_playbackTime;
        if (m_ticks[buttonCode].Count > 0) {
            ScoreTick tick = m_ticks[buttonCode][0];
            int delta = currentTime - tick.time + m_inputOffset;
            ObjectDataBase hitObject = tick.obj;
            if (tick.HasFlag(TickFlags.Laser)) {
                // Ignore laser and hold ticks
                return null;
            } else if (tick.HasFlag(TickFlags.Hold)) {
                HoldButtonData hbd = (HoldButtonData)hitObject;
                hbd = hbd.GetRoot();
                if (hbd.mTime - goodHitTime <= currentTime + m_inputOffset)
                    m_ListHoldObject(hitObject, buttonCode);
                return null;
            }

            if (Math.Abs(delta) <= goodHitTime)
                m_TickHit(tick, buttonCode, delta);
            else
                m_TickMiss(tick, buttonCode, delta);

            m_ticks[buttonCode].Remove(tick);

            return hitObject;
        }
        return null;
    }

    void m_OnTickProcessed(ScoreTick tick, int index) {
        OnScoreChanged(CalculateCurrentScore());
    }

    void m_TickHit(ScoreTick tick, int index, int delta = 0) {
        Debug.Log("tick hit : " + tick.flags + ", index : " + index);
        HitStat stat = m_AddOrUpdateHitStat(tick.obj);
        if (tick.HasFlag(TickFlags.Button)) {
            stat.delta = delta;
            stat.rating = tick.GetHitRatingFromDelta(delta);
            OnButtonHit(index, stat.rating, tick.obj, Math.Sign(delta) > 0);

            if (stat.rating == ScoreHitRating.Perfect) {
                currentGauge += shortGaugeGain;
            } else {
                if (Math.Sign(delta) < 0)
                    timedHits[0]++;
                else
                    timedHits[1]++;

                currentGauge += shortGaugeGain / 3.0f;
            }
            m_AddScore((uint)stat.rating);
        } else if (tick.HasFlag(TickFlags.Hold)) {
            HoldButtonData hold = (HoldButtonData)tick.obj;
            if (hold.mTime + hold.mDuration > m_playback.m_playbackTime) // Only List active hold object if object hasn't passed yet
                m_ListHoldObject(tick.obj, index);

            stat.rating = ScoreHitRating.Perfect;
            stat.hold++;
            currentGauge += tickGaugeGain;
            m_AddScore(2);
        } else if (tick.HasFlag(TickFlags.Laser)) {
            LaserData laser = (LaserData)tick.obj;
            LaserData rootObject = ((LaserData)tick.obj).GetRoot();
            if (tick.HasFlag(TickFlags.Slam)) {
                OnLaserSlamHit(laser);
                // List laser pointer position after hitting slam
                laserTargetPositions[laser.mIndex] = laser.mPoints[1];
                laserPositions[laser.mIndex] = laser.mPoints[1];
                m_autoLaserTime[laser.mIndex] = m_assistTime * m_assistSlamBoost;
            }

            currentGauge += tickGaugeGain;
            m_AddScore(2);

            stat.rating = ScoreHitRating.Perfect;
            stat.hold++;
        }
        m_OnTickProcessed(tick, index);

        // Count hits per category (miss,perfect,etc.)
        categorizedHits[(int)stat.rating]++;
    }
    void m_TickMiss(ScoreTick tick, int index, int delta) {
        Debug.Log("tick miss : " + tick.flags + ", index : " + index);
        HitStat stat = m_AddOrUpdateHitStat(tick.obj);
        stat.hasMissed = true;
        float shortMissDrain = 0.02f;
        if ((m_flags & GameFlags.Hard) != GameFlags.None) {
            // Thanks to Hibiki_ext in the discord for help with this
            float drainMultiplier = Mathf.Clamp(1.0f - ((0.3f - currentGauge) * 2f), 0.5f, 1.0f);
            shortMissDrain = 0.09f * drainMultiplier;
        }
        if (tick.HasFlag(TickFlags.Button)) {
            OnButtonMiss(index, delta < 0 && Math.Abs(delta) > goodHitTime);
            stat.rating = ScoreHitRating.Miss;
            stat.delta = delta;
            currentGauge -= shortMissDrain;
        } else if (tick.HasFlag(TickFlags.Hold)) {
            m_ReleaseHoldObject(index);
            currentGauge -= shortMissDrain / 4f;
            stat.rating = ScoreHitRating.Miss;
        } else if (tick.HasFlag(TickFlags.Laser)) {
            LaserData obj = (LaserData)tick.obj;

            if (tick.HasFlag(TickFlags.Slam)) {
                currentGauge -= shortMissDrain;
                m_autoLaserTime[obj.mIndex] = -1;
            } else
                currentGauge -= shortMissDrain / 4f;
            m_autoLaserTime[obj.mIndex] = -1f;
            stat.rating = ScoreHitRating.Miss;
        }

        // All misses reList combo
        currentGauge = Math.Max(0.0f, currentGauge);
        m_ReListCombo();
        m_OnTickProcessed(tick, index);

        // All ticks count towards the 'miss' counter
        categorizedHits[0]++;
    }

    void m_CleanupTicks() {
        for (uint i = 0; i < 8; i++) {
            m_ticks[i].Clear();
        }
    }

    void m_AddScore(uint score) {
        //assert(score > 0 && score <= 2);
        if (score == 1 && comboState == 2)
            comboState = 1;
        currentHitScore += score;
        currentGauge = Math.Min(1.0f, currentGauge);
        currentComboCounter += 1;
        maxComboCounter = Math.Max(maxComboCounter, currentComboCounter);
        OnComboChanged(currentComboCounter);
    }
    void m_ReListCombo() {
        comboState = 0;
        currentComboCounter = 0;
        OnComboChanged(currentComboCounter);
    }

    void m_ListHoldObject(ObjectDataBase obj, int index) {
        if (m_holdObjects[index] == obj)
            return;

        //assert(!m_heldObjects.Contains(obj));
        m_heldObjects.Add(obj);
        m_holdObjects[index] = obj;
        OnObjectHold(index, obj);
    }
    void m_ReleaseHoldObject(int index) {
        m_ReleaseHoldObject(m_holdObjects[index]);
    }
    void m_ReleaseHoldObject(ObjectDataBase obj) {
        int index = m_heldObjects.FindIndex((x) => x == obj);
        if (index != - 1) {
            m_heldObjects.Remove(obj);

            // UnList hold objects
            for (int i = 0; i < 8; i++) {
                if (m_holdObjects[i] == obj) {
                    m_holdObjects[i] = null;
                    OnObjectReleased(i, obj);
                    return;
                }
            }
        }
    }

    void m_UpdateLasers(float deltaTime) {
        /// TODO: Change to only re-calculate on bpm change
        m_assistTime = m_assistLevel * 0.1f;

        int mapTime = m_playback.GetLastTime();
        for (int i = 0; i < 2; i++) {
            // Check for new laser segments in laser queue
            for (int j = 0; j < m_laserSegmentQueue.Count; j++) {
                LaserData laser = m_laserSegmentQueue[j];
                // ReList laser usage timer
                timeSinceLaserUsed[laser.mIndex] = 0.0f;

                if (laser.mTime <= mapTime) {
                    // Replace the currently active segment
                    m_currentLaserSegments[laser.mIndex] = laser;
                    if (laser.mPrev != null && laser.GetDirection() != laser.mPrev.GetDirection()) {
                        //Direction change
                        //m_autoLaserTime[(*it).index] = -1;
                    }

                    m_laserSegmentQueue.Remove(laser);
                    j--;
                }
            }

            LaserData currentSegment = m_currentLaserSegments[i];
            if (currentSegment != null) {
                lasersAreExtend[i] = (currentSegment.mFlags & LaserData.mFlagExtended) != 0;
                if ((currentSegment.mTime + currentSegment.mDuration) < mapTime) {
                    currentSegment = null;
                    m_currentLaserSegments[i] = null;
                    for (int j = 0; j < m_laserSegmentQueue.Count; j++) {
                        LaserData laser = m_laserSegmentQueue[j];
                        if (laser.mIndex == i) {
                            laserTargetPositions[i] = laser.mPoints[0];
                            lasersAreExtend[i] = (laser.mFlags & LaserData.mFlagExtended) != 0;
                            break;
                        }
                    }
                } else {
                    // Update target position
                    laserTargetPositions[i] = currentSegment.SamplePosition(mapTime);
                }
            }

            m_laserInput[i] = autoplay ? 0.0f : KeyboardManager.inst.GetLaserAxisValue(i);

            bool notAffectingGameplay = true;
            if (currentSegment != null) {
                // Update laser gameplay
                float positionDelta = laserTargetPositions[i] - laserPositions[i];
                float moveDir = Math.Sign(positionDelta);
                float laserDir = currentSegment.GetDirection();
                float input = m_laserInput[i];
                float inputDir = Math.Sign(input);

                // Always snap laser to start sections if they are completely vertical
                if (laserDir == 0.0f && currentSegment.mPrev == null) {
                    laserPositions[i] = laserTargetPositions[i];
                    m_autoLaserTime[i] = m_assistTime;
                }
                // Lock lasers on straight parts
                else if (laserDir == 0.0f && Math.Abs(positionDelta) < laserDistanceLeniency) {
                    laserPositions[i] = laserTargetPositions[i];
                    m_autoLaserTime[i] = m_assistTime;
                } else if (inputDir != 0.0f) {
                    if (laserDir < 0 && positionDelta < 0) {
                        laserPositions[i] = Math.Max(laserPositions[i] + input, laserTargetPositions[i]);
                    } else if (laserDir > 0 && positionDelta > 0) {
                        laserPositions[i] = Math.Min(laserPositions[i] + input, laserTargetPositions[i]);
                    } else if (laserDir < 0 && positionDelta > 0 || laserDir > 0 && positionDelta < 0) {
                        laserPositions[i] = laserPositions[i] + input;
                    } else if (laserDir == 0.0f) {
                        if (positionDelta > 0)
                            laserPositions[i] = Math.Min(laserPositions[i] + input, laserTargetPositions[i]);
                        if (positionDelta < 0)
                            laserPositions[i] = Math.Max(laserPositions[i] + input, laserTargetPositions[i]);
                    }
                    notAffectingGameplay = false;
                    if (inputDir == moveDir && Math.Abs(positionDelta) < laserDistanceLeniency) {
                        m_autoLaserTime[i] = m_assistTime;
                    }
                    if (inputDir != 0 && inputDir != laserDir) {
                        m_autoLaserTime[i] -= deltaTime * m_assistPunish;
                        //m_autoLaserTime[i] = Math.Min(m_autoLaserTime[i], m_assistTime * 0.2f);
                    }
                }
                timeSinceLaserUsed[i] = 0.0f;
            } else {
                timeSinceLaserUsed[i] += deltaTime;
                //laserPositions[i] = laserTargetPositions[i];
            }
            if (autoplay || m_autoLaserTime[i] >= 0) {
                laserPositions[i] = laserTargetPositions[i];
            }
            // Clamp cursor between 0 and 1
            laserPositions[i] = Mathf.Clamp(laserPositions[i], 0.0f, 1.0f);
            m_autoLaserTime[i] -= deltaTime;
            if (Math.Abs(laserPositions[i] - laserTargetPositions[i]) < laserDistanceLeniency && currentSegment != null) {
                m_ListHoldObject(currentSegment.GetRoot(), 6 + i);
            } else {
                m_ReleaseHoldObject(6 + i);
            }
        }

        // Interpolate laser output
        m_UpdateLaserOutput(deltaTime);
    }

    public void OnButtonPressed(int buttonCode) {
        // Ignore buttons on autoplay
        if (autoplay)
            return;

        //Debug.Log("OnButtonPressed : " + buttonCode);

        if (buttonCode < 6) {
            int guardDelta = m_playback.GetLastTime() - m_buttonGuardTime[buttonCode];
            if (guardDelta < m_bounceGuard && guardDelta >= 0) {
                //Logf("Button %d press bounce guard hit at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
                return;
            }

            //Logf("Button %d pressed at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
            m_buttonHitTime[buttonCode] = m_playback.GetLastTime();
            m_buttonGuardTime[buttonCode] = m_playback.GetLastTime();
            ObjectDataBase obj = m_ConsumeTick(buttonCode);
            if (obj == null) {
                // Fire event for idle hits
                OnButtonHit(buttonCode, ScoreHitRating.Idle, null, false);
            }
        } else if (buttonCode > 6) {
            // TODO : 레이저는 마우스로만 움직일 수 있도록 수정
            ObjectDataBase obj = null;
            if (buttonCode == 6)
                obj = m_ConsumeTick(6); // Laser L
            else
                obj = m_ConsumeTick(7); // Laser R
        }
    }
    public void OnButtonReleased(int buttonCode) {
        //Debug.Log("OnButtonReleased : " + buttonCode);

        if (buttonCode < 6) {
            int guardDelta = m_playback.GetLastTime() - m_buttonGuardTime[(uint)buttonCode];
            if (guardDelta < m_bounceGuard && guardDelta >= 0) {
                //Logf("Button %d release bounce guard hit at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
                return;
            }
            m_buttonGuardTime[buttonCode] = m_playback.m_playbackTime;
        }

        //Logf("Button %d released at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
        m_ReleaseHoldObject(buttonCode);
    }

    MapTotals CalculateMapTotals() {
        MapTotals ret = new MapTotals();
        Beatmap map = m_playback.m_beatmap;

        List<LaserData> processedLasers = new List<LaserData>();

        //assert(m_playback);
        for (int i = 0; i < map.mListObjectState.Count; i++) {
            ObjectDataBase obj = map.mListObjectState[i];
            TimingPoint tp = m_playback.GetTimingPointAt(obj.mTime);
            if (obj.mType == ButtonType.Single) {
                ret.maxScore += (int)ScoreHitRating.Perfect;
                ret.numSingles += 1;
            } else if (obj.mType == ButtonType.Hold) {
                List<int> holdTicks = m_CalculateHoldTicks((HoldButtonData)obj);
                ret.maxScore += (int)ScoreHitRating.Perfect * holdTicks.Count;
                ret.numTicks += holdTicks.Count;
            } else if (obj.mType == ButtonType.Laser) {
                LaserData laserRoot = ((LaserData)obj).GetRoot();

                // Don't evaluate ticks for every segment, only for entire chains of segments
                if (!processedLasers.Contains(laserRoot)) {
                    List<ScoreTick> laserTicks = m_CalculateLaserTicks((LaserData)obj);
                    ret.maxScore += (int)ScoreHitRating.Perfect * laserTicks.Count;
                    ret.numTicks += laserTicks.Count;
                    processedLasers.Add(laserRoot);
                }
            }
        }

        return ret;
    }

    int CalculateCurrentScore() {
        return CalculateScore(currentHitScore);
    }

    int CalculateScore(uint hitScore) {
        return (int)(((double)hitScore / mapTotals.maxScore) * 10000000f);
    }

    uint CalculateCurrentGrade() {
        uint value = (uint)((double)CalculateCurrentScore() * 0.9f + currentGauge * 1000000.0);
        if (value > 9800000) // AAA
            return 0;
        if (value > 9400000) // AA
            return 1;
        if (value > 8900000) // A
            return 2;
        if (value > 8000000) // B
            return 3;
        if (value > 7000000) // C
            return 4;
        return 5; // D
    }

}