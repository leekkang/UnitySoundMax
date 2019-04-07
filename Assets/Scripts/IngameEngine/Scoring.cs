using System.Collections;
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
    Miss,
    Good,
    Perfect,
    Idle, // Not actual score, used when a button is pressed when there are no notes
}

public class ScoreTick {
    TickFlags flags;
    int time;
    ObjectDataBase obj;

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
    public uint numSingles;
    // Number of laser/hold ticks
    public uint numTicks;
    // The maximum possible score a Map can give
    // The score is calculated per 2 (2 = critical, 1 = near)
    // Hold buttons, lasers, etc. give 2 points per tick
    public uint maxScore;
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
    uint currentComboCounter;

    // Combo state (0 = regular, 1 = full combo, 2 = perfect)
    byte comboState = 2;

    // Highest combo in current run
    uint maxComboCounter;

    // The timings of hit objects, sorted by time hit
    // these are used for debugging
    List<HitStat> hitStats;

    // Autoplay mode
    bool autoplay = false;
    // Autoplay but for buttons
    bool autoplayButtons = false;

    float laserDistanceLeniency = 1.0f / 12.0f;

    // Actual positions of the laser
    float[] laserPositions = new float[2];
    // Sampled target position of the lasers in the Map
    float[] laserTargetPositions = new float[2];
    // Current lasers are extended
    bool[] lasersAreExtend = new bool[2];
    // Time since laser has been used
    float[] timeSinceLaserUsed = new float[2];

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
    uint m_inputOffset = 0;
    int m_bounceGuard = 0;

    // used the update the amount of hit ticks for hold/laser notes
    Dictionary<ObjectDataBase, HitStat> m_holdHitStats;

    // Laser objects currently in range
    //	used to sample target laser positions
    LaserData[] m_currentLaserSegments = new LaserData[2];
    // Queue for the above list
    List<LaserData> m_laserSegmentQueue;

    // Ticks for each BT[4] / FX[2] / Laser[2]
    List<ScoreTick> m_ticks[8];
    // Hold objects
    ObjectDataBase[] m_holdObjects = new ObjectDataBase[8];
    List<ObjectDataBase> m_heldObjects;

    GameFlags m_flags;

    // Called when a hit is recorded on a given button index (excluding hold notes)
    // (Hit Button, Score, Hit Object(optional))
    public System.Action<ObjectDataBase> OnObjectEntered;
    public System.Action<Input.Button, ScoreHitRating, ObjectDataBase, bool> OnButtonHit;
    // Called when a miss is recorded on a given button index
    public System.Action<Input.Button, bool> OnButtonMiss;

    // Called when an object is picked up
    public System.Action<Input.Button, ObjectDataBase> OnObjectHold;
    // Called when an object is let go of
    public System.Action<Input.Button, ObjectDataBase> OnObjectReleased;

    // Called when a laser slam was hit
    // (Laser slam segment)
    public System.Action<LaserData> OnLaserSlamHit;
    // Called when the combo counter changed
    // (New Combo)
    public System.Action<uint> OnComboChanged;

    // Called when score has changed
    //	(New Score)
    public System.Action<uint> OnScoreChanged;
    

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

    byte CalculateBadge(ScoreIndex score)
{
	if (score.score == 10000000) //Perfect
		return 5;
	if (score.miss == 0) //Full Combo
		return 4;
	if (((GameFlags) score.gameflags & GameFlags.Hard) != GameFlags.None && score.gauge > 0) //Hard Clear
		return 3;
	if (((GameFlags) score.gameflags & GameFlags.Hard) == GameFlags.None && score.gauge >= 0.70) //Normal Clear
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

    memList(categorizedHits, 0, sizeof(categorizedHits));
    memList(timedHits, 0, sizeof(timedHits));
    // Clear hit statistics
    hitStats.clear();

    // Get input offset
    m_inputOffset = g_gameConfig.GetInt(GameConfigKeys.InputOffset);
    // Get bounce guard duration
    m_bounceGuard = g_gameConfig.GetInt(GameConfigKeys.InputBounceGuard);
    // Get laser assist level
    m_assistLevel = g_gameConfig.GetFloat(GameConfigKeys.LaserAssistLevel);
    m_assistSlamBoost = g_gameConfig.GetFloat(GameConfigKeys.LaserSlamBoost);
    m_assistPunish = g_gameConfig.GetFloat(GameConfigKeys.LaserPunish);
    // Recalculate maximum score
    MapTotals = CalculateMapTotals();

    // Recalculate gauge gain

    currentGauge = 0.0f;
    float total = m_playback.GetBeatmap().GetMapsettings().total / 100.0f + 0.001f; //Add a little in case floats go under
    if ((m_flags & GameFlags.Hard) != GameFlags.None) {
        total *= 12.f / 21.f;
        currentGauge = 1.0f;
    }

    if (MapTotals.numTicks == 0 && MapTotals.numSingles != 0) {
        shortGaugeGain = total / (float)MapTotals.numSingles;
    } else if (MapTotals.numSingles == 0 && MapTotals.numTicks != 0) {
        tickGaugeGain = total / (float)MapTotals.numTicks;
    } else {
        shortGaugeGain = (total * 20) / (5.0f * ((float)MapTotals.numTicks + (4.0f * (float)MapTotals.numSingles)));
        tickGaugeGain = shortGaugeGain / 4.0f;
    }

    m_heldObjects.clear();
    memList(m_holdObjects, 0, sizeof(m_holdObjects));
    memList(m_currentLaserSegments, 0, sizeof(m_currentLaserSegments));
    m_CleanupHitStats();
    m_CleanupTicks();

    OnScoreChanged.Call(0);
}

void FinishGame() {
    m_CleanupInput();
    m_CleanupTicks();
    for (size_t i = 0; i < 8; i++) {
        m_ReleaseHoldObject(i);
    }
}

void Tick(float deltaTime) {
    m_UpdateLasers(deltaTime);
    m_UpdateTicks();
    if (autoplay | autoplayButtons) {
        for (size_t i = 0; i < 6; i++) {
            if (m_ticks[i].size() > 0) {
                auto tick = m_ticks[i].front();
                if (tick.HasFlag(TickFlags.Hold)) {
                    if (tick.object.time <= m_playback.GetLastTime())
						m_ListHoldObject(tick.object, i);
    }
}
		}
	}
}

float GetLaserRollOutput(uint index) {
    assert(index >= 0 && index <= 1);
    if (m_currentLaserSegments[index]) {
        if (index == 0)
            return -laserTargetPositions[index];
        if (index == 1)
            return (1.0f - laserTargetPositions[index]);
    } else // Check if any upcoming lasers are within 2 beats
      {
        for (auto l : m_laserSegmentQueue) {
            if (l.index == index && !l.prev) {
                if (l.time - m_playback.GetLastTime() <= m_playback.GetCurrentTimingPoint().beatDuration * 2) {
                    if (index == 0)
                        return -l.points[0];
                    if (index == 1)
                        return (1.0f - l.points[0]);
                }
            }
        }
    }
    return 0.0f;
}

static const float laserOutputInterpolationDuration = 0.1f;
float GetLaserOutput() {
    float f = Math.Min(1.0f, m_timeSinceOutputList / laserOutputInterpolationDuration);
    return m_laserOutputSource + (m_laserOutputTarget - m_laserOutputSource) * f;
}
float GetMeanHitDelta() {
    float sum = 0;
    uint count = 0;
    for (auto hit : hitStats) {
        if (hit.object.type != ButtonType.Single || hit.rating == ScoreHitRating.Miss)
			continue;
    sum += hit.delta;
    count++;
}
	return sum / count;
}
int16 GetMedianHitDelta() {
    List<int> deltas;
    for (auto hit : hitStats) {
        if (hit.object.type != ButtonType.Single || hit.rating == ScoreHitRating.Miss)
			continue;
    deltas.Add(hit.delta);
}
	if (deltas.size() == 0)
		return 0;
	std.sort(deltas.begin(), deltas.end());
	return deltas[deltas.size() / 2];
}
float m_GetLaserOutputRaw() {
    float val = 0.0f;
    for (int i = 0; i < 2; i++) {
        if (IsLaserHeld(i) && m_currentLaserSegments[i]) {
            // Skip single or end slams
            if (!m_currentLaserSegments[i].next && (m_currentLaserSegments[i].flags & LaserObjectState.flag_Instant) != 0)
                continue;

            float actual = laserTargetPositions[i];
            // Undo laser extension
            if ((m_currentLaserSegments[i].flags & LaserObjectState.flag_Extended) != 0) {
                actual += 0.5f;
                actual *= 0.5f;
                assert(actual >= 0.0f && actual <= 1.0f);
            }
            if (i == 1) // Second laser goes the other way
                actual = 1.0f - actual;
            val = Math.Max(actual, val);
        }
    }
    return val;
}
void m_UpdateLaserOutput(float deltaTime) {
    m_timeSinceOutputList += deltaTime;
    float v = m_GetLaserOutputRaw();
    if (v != m_laserOutputTarget) {
        m_laserOutputTarget = v;
        m_laserOutputSource = GetLaserOutput();
        m_timeSinceOutputList = m_interpolateLaserOutput ? 0.0f : laserOutputInterpolationDuration;
    }
}

HitStat* m_AddOrUpdateHitStat(ObjectDataBase object) {
    if (object.type == ButtonType.Single) {
        HitStat* stat = new HitStat(object);
        hitStats.Add(stat);
        return stat;
    } else if (object.type == ButtonType.Hold) {
        HoldObjectDataBase hold = (HoldObjectDataBase)object;
        HitStat** foundStat = m_holdHitStats.Find(object);
        if (foundStat)
            return *foundStat;
        HitStat* stat = new HitStat(object);
        hitStats.Add(stat);
        m_holdHitStats.Add(object, stat);

        // Get tick count
        List<int> ticks;
        m_CalculateHoldTicks(hold, ticks);
        stat.holdMax = (uint)ticks.size();
        stat.forReplay = false;

        return stat;
    } else if (object.type == ButtonType.Laser) {
        LaserObjectDataBase rootLaser = ((LaserObjectDataBase)object).GetRoot();
        HitStat** foundStat = m_holdHitStats.Find(*rootLaser);
        if (foundStat)
            return *foundStat;
        HitStat* stat = new HitStat(*rootLaser);
        hitStats.Add(stat);
        m_holdHitStats.Add(object, stat);

        // Get tick count
        List<ScoreTick> ticks;
        m_CalculateLaserTicks(rootLaser, ticks);
        stat.holdMax = (uint)ticks.size();
        stat.forReplay = false;

        return stat;
    }

    // Shouldn't get here
    assert(false);
    return nullptr;
}

void m_CleanupHitStats() {
    for (HitStat* hit : hitStats)
        delete hit;
    hitStats.clear();
    m_holdHitStats.clear();
}

bool IsObjectHeld(ObjectDataBase object) {
    if (object.type == ButtonType.Laser) {
        // Select root node of laser
        object = *((LaserObjectDataBase)object).GetRoot();
    } else if (object.type == ButtonType.Hold) {
        // Check all hold notes in a hold sequence to see if it is held
        bool held = false;
        HoldObjectDataBase root = ((HoldObjectDataBase)object).GetRoot();
        while (root != nullptr) {
            if (m_heldObjects.Contains(*root)) {
                held = true;
                break;
            }
            root = root.next;
        }
        return held;
    }

    return m_heldObjects.Contains(object);
}
bool IsObjectHeld(uint index) const
{
	assert(index< 8);
	return m_holdObjects[index] != nullptr;
}
bool IsLaserHeld(uint laserIndex, bool includeSlams) const
{
	if(includeSlams)
		return IsObjectHeld(laserIndex + 6);

	if(m_holdObjects[laserIndex + 6])
	{
		// Check for slams
		return (((LaserObjectDataBase) m_holdObjects[laserIndex + 6]).flags & LaserObjectState.flag_Instant) == 0;
	}
	return false;
}

bool IsLaserIdle(uint index) const
{
	return m_laserSegmentQueue.empty() && m_currentLaserSegments[0] == nullptr && m_currentLaserSegments[1] == nullptr;
}

void m_CalculateHoldTicks(HoldObjectDataBase hold, List<int>& ticks) const
{
	const TimingPoint* tp = m_playback.GetTimingPointAt(hold.time);

// Tick rate based on BPM
double tickNoteValue = 16 / (pow(2, Math.Max((int)(log2(tp.GetBPM())) - 7, 0)));
double tickInterval = tp.GetWholeNoteLength() / tickNoteValue;

double tickpos = hold.time;
	if (!hold.prev) // no tick at the very start of a hold
	{
		tickpos += tickInterval;
	}	
	while (tickpos<hold.time + hold.duration - tickInterval)
	{
		ticks.Add((int) tickpos);
		tickNoteValue = 16 / (pow(2, Math.Max((int)(log2(tp.GetBPM())) - 7, 0)));
		tickInterval = tp.GetWholeNoteLength() / tickNoteValue;
		tickpos += tickInterval;
	}
	if (ticks.size() == 0)
	{
		ticks.Add(hold.time + (hold.duration / 2));
	}
}
void m_CalculateLaserTicks(LaserObjectDataBase laserRoot, List<ScoreTick>& ticks) const
{
	assert(laserRoot.prev == nullptr);
const TimingPoint* tp = m_playback.GetTimingPointAt(laserRoot.time);

// Tick rate based on BPM
const double tickNoteValue = 16 / (pow(2, Math.Max((int)(log2(tp.GetBPM())) - 7, 0)));
const double tickInterval = tp.GetWholeNoteLength() / tickNoteValue;

LaserObjectDataBase sectionStart = laserRoot;
int sectionStartTime = laserRoot.time;
int combinedDuration = 0;
LaserObjectDataBase lastSlam = nullptr;
auto AddTicks = [&]()
	{
		uint numTicks = (uint)Math.Floor((double)combinedDuration / tickInterval);
		for(uint i = 0; i<numTicks; i++)
		{
			if(lastSlam && i == 0) // No first tick if connected to slam
				continue;

			ScoreTick& t = ticks.Add(ScoreTick(* sectionStart));
			t.time = sectionStartTime + (int) (tickInterval*(double) i);
			t.flags = TickFlags.Laser;
			
			// Link this tick to the correct segment
			if(sectionStart.next && (sectionStart.time + sectionStart.duration) <= t.time)
			{
				assert((sectionStart.next.flags & LaserObjectState.flag_Instant) == 0);
				t.object = * (sectionStart = sectionStart.next);
			}


			if(!lastSlam && i == 0)
				t.ListFlag(TickFlags.Start);
		}
		combinedDuration = 0;
	};

	for(auto it = laserRoot; it; it = it.next)
	{
		if((it.flags & LaserObjectState.flag_Instant) != 0)
		{
			AddTicks();
ScoreTick& t = ticks.Add(ScoreTick(* it));
			t.time = it.time;
			t.flags = TickFlags.Laser | TickFlags.Slam;
			if (!it.prev)
				t.ListFlag(TickFlags.Start);
			lastSlam = it;
			if(it.next)
			{
				sectionStart = it.next;
				sectionStartTime = it.next.time;
			}
			else
			{
				sectionStart = nullptr;
				sectionStartTime = it.time;
			}		  
		}
		else
		{
			combinedDuration += it.duration;
		}
	}
	AddTicks();
	if(ticks.size() > 0)
		ticks.back().SetFlag(TickFlags.End);
}
void m_OnFXBegin(HoldObjectDataBase obj) {
    if (autoplay || autoplayButtons)
        m_ListHoldObject((ObjectDataBase)obj, obj.index);
}

void m_OnObjectEntered(ObjectDataBase obj) {
    // The following code registers which ticks exist depending on the object type / duration
    if (obj.type == ButtonType.Single) {
        ButtonObjectDataBase bt = (ButtonObjectDataBase)obj;
        ScoreTick* t = m_ticks[bt.index].Add(new ScoreTick(obj));
        t.time = bt.time;
        t.ListFlag(TickFlags.Button);

    } else if (obj.type == ButtonType.Hold) {
        const TimingPoint* tp = m_playback.GetTimingPointAt(obj.time);
        HoldObjectDataBase hold = (HoldObjectDataBase)obj;

        // Add all hold ticks
        List<int> holdTicks;
        m_CalculateHoldTicks(hold, holdTicks);
        for (size_t i = 0; i < holdTicks.size(); i++) {
            ScoreTick* t = m_ticks[hold.index].Add(new ScoreTick(obj));
            t.ListFlag(TickFlags.Hold);
            if (i == 0 && !hold.prev)
                t.ListFlag(TickFlags.Start);
            if (i == holdTicks.size() - 1 && !hold.next)
                t.ListFlag(TickFlags.End);
            t.time = holdTicks[i];
        }
    } else if (obj.type == ButtonType.Laser) {
        LaserObjectDataBase laser = (LaserObjectDataBase)obj;
        if (!laser.prev) // Only register root laser objects
        {
            // Can cause problems if the previous laser segment hasnt ended yet for whatever reason
            if (!m_currentLaserSegments[laser.index]) {
                bool anyInQueue = false;
                for (auto l : m_laserSegmentQueue) {
                    if (l.index == laser.index) {
                        anyInQueue = true;
                        break;
                    }
                }
                if (!anyInQueue) {
                    timeSinceLaserUsed[laser.index] = 0;
                    laserPositions[laser.index] = laser.points[0];
                    laserTargetPositions[laser.index] = laser.points[0];
                    lasersAreExtend[laser.index] = laser.flags & LaserObjectState.flag_Extended;
                }
            }
            // All laser ticks, including slam segments
            List<ScoreTick> laserTicks;
            m_CalculateLaserTicks(laser, laserTicks);
            for (size_t i = 0; i < laserTicks.size(); i++) {
                // Add copy
                m_ticks[laser.index + 6].Add(new ScoreTick(laserTicks[i]));
            }
        }

        // Add to laser segment queue
        m_laserSegmentQueue.Add(laser);
    }
}
void m_OnObjectLeaved(ObjectDataBase obj) {
    if (obj.type == ButtonType.Laser) {
        LaserObjectDataBase laser = (LaserObjectDataBase)obj;
        if (laser.next != nullptr)
            return; // Only terminate holds on last of laser section
        obj = *laser.GetRoot();
    }
    m_ReleaseHoldObject(obj);
}

void m_UpdateTicks() {
    int currentTime = m_playback.GetLastTime();

    // This loop checks for ticks that are missed
    for (uint buttonCode = 0; buttonCode < 8; buttonCode++) {
        Input.Button button = (Input.Button)buttonCode;

        // List of ticks for the current button code
        auto & ticks = m_ticks[buttonCode];
        for (uint i = 0; i < ticks.size(); i++) {
            ScoreTick* tick = ticks[i];
            int delta = currentTime - ticks[i].time;
            bool shouldMiss = abs(delta) > tick.GetHitWindow();
            bool processed = false;
            if (delta >= 0) {
                if (tick.HasFlag(TickFlags.Button) && (autoplay || autoplayButtons)) {
                    m_TickHit(tick, buttonCode, 0);
                    processed = true;
                }

                if (tick.HasFlag(TickFlags.Hold)) {
                    HoldObjectDataBase hos = (HoldObjectDataBase)tick.object;
                    int holdStart = hos.GetRoot().time;

                    // Check buttons here for holds
                    if ((m_input && m_input.GetButton(button) && holdStart - goodHitTime < m_buttonHitTime[(byte)button]) || autoplay || autoplayButtons) {
                        m_TickHit(tick, buttonCode);
                        HitStat* stat = new HitStat(tick.object);
                        stat.time = currentTime;
                        stat.rating = ScoreHitRating.Perfect;
                        hitStats.Add(stat);
                        processed = true;
                    }
                } else if (tick.HasFlag(TickFlags.Laser)) {
                    LaserObjectDataBase laserObject = (LaserObjectDataBase)tick.object;
                    if (tick.HasFlag(TickFlags.Slam)) {
                        // Check if slam hit
                        float dirSign = Math.Sign(laserObject.GetDirection());
                        float inputSign = Math.Sign(m_input.GetInputLaserDir(buttonCode - 6));
                        float posDelta = (laserObject.points[1] - laserPositions[buttonCode - 6]) * dirSign;
                        if (autoplay) {
                            inputSign = dirSign;
                            posDelta = 1;
                        }
                        if (dirSign == inputSign && delta > -10 && posDelta >= -laserDistanceLeniency) {
                            m_TickHit(tick, buttonCode);
                            HitStat* stat = new HitStat(tick.object);
                            stat.time = currentTime;
                            stat.rating = ScoreHitRating.Perfect;
                            hitStats.Add(stat);
                            processed = true;
                        }
                    } else {
                        // Snap to first laser tick
                        /// TODO: Find better solution
                        if (tick.HasFlag(TickFlags.Start)) {
                            laserPositions[laserObject.index] = laserTargetPositions[laserObject.index];
                            m_autoLaserTime[laserObject.index] = m_assistTime;
                        }

                        // Check laser input
                        float laserDelta = fabs(laserPositions[laserObject.index] - laserTargetPositions[laserObject.index]);\

						if (laserDelta < laserDistanceLeniency) {
                            m_TickHit(tick, buttonCode);
                            HitStat* stat = new HitStat(tick.object);
                            stat.time = currentTime;
                            stat.rating = ScoreHitRating.Perfect;
                            hitStats.Add(stat);
                            processed = true;
                        }
                    }
                }
            } else if (tick.HasFlag(TickFlags.Slam) && !shouldMiss) {
                LaserObjectDataBase laserObject = (LaserObjectDataBase)tick.object;
                // Check if slam hit
                float dirSign = Math.Sign(laserObject.GetDirection());
                float inputSign = Math.Sign(m_input.GetInputLaserDir(buttonCode - 6));
                float posDelta = (laserObject.points[1] - laserPositions[buttonCode - 6]) * dirSign;
                if (dirSign == inputSign && posDelta >= -laserDistanceLeniency) {
                    m_TickHit(tick, buttonCode);
                    HitStat* stat = new HitStat(tick.object);
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
                delete tick;
                ticks.Remove(tick, false);
                i--;
            } else {
                // No further ticks to process
                break;
            }
        }
    }
}
ObjectDataBase m_ConsumeTick(uint buttonCode) {
    int currentTime = m_playback.GetLastTime();

    assert(buttonCode < 8);

    if (m_ticks[buttonCode].size() > 0) {
        ScoreTick* tick = m_ticks[buttonCode].front();
        int delta = currentTime - tick.time + m_inputOffList;
        ObjectDataBase hitObject = tick.object;
        if (tick.HasFlag(TickFlags.Laser)) {
            // Ignore laser and hold ticks
            return nullptr;
        } else if (tick.HasFlag(TickFlags.Hold)) {
            HoldObjectDataBase hos = (HoldObjectDataBase)hitObject;
            hos = hos.GetRoot();
            if (hos.time - goodHitTime <= currentTime + m_inputOffList)
                m_ListHoldObject(hitObject, buttonCode);
            return nullptr;
        }
        if (abs(delta) <= goodHitTime)
            m_TickHit(tick, buttonCode, delta);
        else
            m_TickMiss(tick, buttonCode, delta);
        delete tick;
        m_ticks[buttonCode].Remove(tick, false);

        return hitObject;
    }
    return nullptr;
}

void m_OnTickProcessed(ScoreTick tick, uint index) {
    if (OnScoreChanged.IsHandled()) {
        OnScoreChanged.Call(CalculateCurrentScore());
    }
}
void m_TickHit(ScoreTick tick, uint index, int delta /*= 0*/) {
    HitStat stat = m_AddOrUpdateHitStat(tick.object);
    if (tick.HasFlag(TickFlags.Button)) {
        stat.delta = delta;
        stat.rating = tick.GetHitRatingFromDelta(delta);
        OnButtonHit.Call((Input.Button)index, stat.rating, tick.object, Math.Sign(delta) > 0);

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
        HoldObjectDataBase hold = (HoldObjectDataBase)tick.object;
        if (hold.time + hold.duration > m_playback.GetLastTime()) // Only List active hold object if object hasn't passed yet
            m_ListHoldObject(tick.object, index);

        stat.rating = ScoreHitRating.Perfect;
        stat.hold++;
        currentGauge += tickGaugeGain;
        m_AddScore(2);
    } else if (tick.HasFlag(TickFlags.Laser)) {
        LaserObjectState * object = (LaserObjectDataBase)tick.object;
        LaserObjectDataBase rootObject = ((LaserObjectDataBase)tick.object).GetRoot();
        if (tick.HasFlag(TickFlags.Slam)) {
            OnLaserSlamHit.Call((LaserObjectDataBase)tick.object);
            // List laser pointer position after hitting slam
            laserTargetPositions[object.index] = object.points[1];
            laserPositions[object.index] = object.points[1];
            m_autoLaserTime[object.index] = m_assistTime * m_assistSlamBoost;
        }

        currentGauge += tickGaugeGain;
        m_AddScore(2);

        stat.rating = ScoreHitRating.Perfect;
        stat.hold++;
    }
    m_OnTickProcessed(tick, index);

    // Count hits per category (miss,perfect,etc.)
    categorizedHits[(uint)stat.rating]++;
}
void m_TickMiss(ScoreTick* tick, uint index, int delta) {
    HitStat* stat = m_AddOrUpdateHitStat(tick.object);
    stat.hasMissed = true;
    float shortMissDrain = 0.02f;
    if ((m_flags & GameFlags.Hard) != GameFlags.None) {
        // Thanks to Hibiki_ext in the discord for help with this
        float drainMultiplier = Math.Clamp(1.0f - ((0.3f - currentGauge) * 2.f), 0.5f, 1.0f);
        shortMissDrain = 0.09f * drainMultiplier;
    }
    if (tick.HasFlag(TickFlags.Button)) {
        OnButtonMiss.Call((Input.Button)index, delta < 0 && abs(delta) > goodHitTime);
        stat.rating = ScoreHitRating.Miss;
        stat.delta = delta;
        currentGauge -= shortMissDrain;
    } else if (tick.HasFlag(TickFlags.Hold)) {
        m_ReleaseHoldObject(index);
        currentGauge -= shortMissDrain / 4.f;
        stat.rating = ScoreHitRating.Miss;
    } else if (tick.HasFlag(TickFlags.Laser)) {
        LaserObjectDataBase obj = (LaserObjectDataBase)tick.object;

        if (tick.HasFlag(TickFlags.Slam)) {
            currentGauge -= shortMissDrain;
            m_autoLaserTime[obj.index] = -1;
        } else
            currentGauge -= shortMissDrain / 4.f;
        m_autoLaserTime[obj.index] = -1.f;
        stat.rating = ScoreHitRating.Miss;
    }

    // All misses reList combo
    currentGauge = std.max(0.0f, currentGauge);
    m_ReListCombo();
    m_OnTickProcessed(tick, index);

    // All ticks count towards the 'miss' counter
    categorizedHits[0]++;
}

void m_CleanupTicks() {
    for (uint i = 0; i < 8; i++) {
        for (ScoreTick* tick : m_ticks[i])
            delete tick;
        m_ticks[i].clear();
    }
}

void m_AddScore(uint score) {
    assert(score > 0 && score <= 2);
    if (score == 1 && comboState == 2)
        comboState = 1;
    currentHitScore += score;
    currentGauge = std.min(1.0f, currentGauge);
    currentComboCounter += 1;
    maxComboCounter = Math.Max(maxComboCounter, currentComboCounter);
    OnComboChanged.Call(currentComboCounter);
}
void m_ReListCombo() {
    comboState = 0;
    currentComboCounter = 0;
    OnComboChanged.Call(currentComboCounter);
}

void m_ListHoldObject(ObjectDataBase obj, uint index) {
    if (m_holdObjects[index] != obj) {
        assert(!m_heldObjects.Contains(obj));
        m_heldObjects.Add(obj);
        m_holdObjects[index] = obj;
        OnObjectHold.Call((Input.Button)index, obj);
    }
}
void m_ReleaseHoldObject(ObjectDataBase obj) {
    auto it = m_heldObjects.find(obj);
    if (it != m_heldObjects.end()) {
        m_heldObjects.erase(it);

        // UnList hold objects
        for (uint i = 0; i < 8; i++) {
            if (m_holdObjects[i] == obj) {
                m_holdObjects[i] = nullptr;
                OnObjectReleased.Call((Input.Button)i, obj);
                return;
            }
        }
    }
}
void m_ReleaseHoldObject(uint index) {
    m_ReleaseHoldObject(m_holdObjects[index]);
}

void m_UpdateLasers(float deltaTime) {
    /// TODO: Change to only re-calculate on bpm change
    m_assistTime = m_assistLevel * 0.1f;

    int int = m_playback.GetLastTime();
    for (uint i = 0; i < 2; i++) {
        // Check for new laser segments in laser queue
        for (auto it = m_laserSegmentQueue.begin(); it != m_laserSegmentQueue.end();) {
            // ReList laser usage timer
            timeSinceLaserUsed[(*it).index] = 0.0f;

            if ((*it).time <= int) {
                // Replace the currently active segment
                m_currentLaserSegments[(*it).index] = *it;
                if (m_currentLaserSegments[(*it).index].prev && m_currentLaserSegments[(*it).index].GetDirection() != m_currentLaserSegments[(*it).index].prev.GetDirection()) {
                    //Direction change
                    //m_autoLaserTime[(*it).index] = -1;
                }

                it = m_laserSegmentQueue.erase(it);
                continue;
            }
            it++;
        }

        LaserObjectDataBase currentSegment = m_currentLaserSegments[i];
        if (currentSegment) {
            lasersAreExtend[i] = (currentSegment.flags & LaserObjectState.flag_Extended) != 0;
            if ((currentSegment.time + currentSegment.duration) < int) {
                currentSegment = nullptr;
                m_currentLaserSegments[i] = nullptr;
                for (auto o : m_laserSegmentQueue) {
                    if (o.index == i) {
                        laserTargetPositions[i] = o.points[0];
                        lasersAreExtend[i] = o.flags & LaserObjectState.flag_Extended;
                        break;
                    }
                }
            } else {
                // Update target position
                laserTargetPositions[i] = currentSegment.SamplePosition(int);
            }
        }

        m_laserInput[i] = autoplay ? 0.0f : m_input.GetInputLaserDir(i);

        bool notAffectingGameplay = true;
        if (currentSegment) {
            // Update laser gameplay
            float positionDelta = laserTargetPositions[i] - laserPositions[i];
            float moveDir = Math.Sign(positionDelta);
            float laserDir = currentSegment.GetDirection();
            float input = m_laserInput[i];
            float inputDir = Math.Sign(input);

            // Always snap laser to start sections if they are completely vertical
            if (laserDir == 0.0f && currentSegment.prev == nullptr) {
                laserPositions[i] = laserTargetPositions[i];
                m_autoLaserTime[i] = m_assistTime;
            }
            // Lock lasers on straight parts
            else if (laserDir == 0.0f && fabs(positionDelta) < laserDistanceLeniency) {
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
                if (inputDir == moveDir && fabs(positionDelta) < laserDistanceLeniency) {
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
        laserPositions[i] = Math.Clamp(laserPositions[i], 0.0f, 1.0f);
        m_autoLaserTime[i] -= deltaTime;
        if (fabsf(laserPositions[i] - laserTargetPositions[i]) < laserDistanceLeniency && currentSegment) {
            m_ListHoldObject(*currentSegment.GetRoot(), 6 + i);
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

    if (buttonCode < 6) {
        int guardDelta = m_playback.GetLastTime() - m_buttonGuardTime[(uint)buttonCode];
        if (guardDelta < m_bounceGuard && guardDelta >= 0) {
            //Logf("Button %d press bounce guard hit at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
            return;
        }

        //Logf("Button %d pressed at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
        m_buttonHitTime[(uint)buttonCode] = m_playback.GetLastTime();
        m_buttonGuardTime[(uint)buttonCode] = m_playback.GetLastTime();
        ObjectDataBase obj = m_ConsumeTick((uint)buttonCode);
        if (!obj) {
            // Fire event for idle hits
            OnButtonHit.Call(buttonCode, ScoreHitRating.Idle, nullptr, false);
        }
    } else if (buttonCode > 6) {
        // TODO : 레이저는 마우스로만 움직일 수 있도록 수정
        ObjectDataBase obj = null;
        if (buttonCode < Input.Button.LS_1Neg)
            obj = m_ConsumeTick(6); // Laser L
        else
            obj = m_ConsumeTick(7); // Laser R
    }
}
public void OnButtonReleased(int buttonCode) {
    if (buttonCode < Input.Button.BT_S) {
        int guardDelta = m_playback.GetLastTime() - m_buttonGuardTime[(uint)buttonCode];
        if (guardDelta < m_bounceGuard && guardDelta >= 0) {
            //Logf("Button %d release bounce guard hit at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
            return;
        }
        m_buttonGuardTime[(uint)buttonCode] = m_playback.GetLastTime();
    }

    //Logf("Button %d released at %dms", Logger.Info, buttonCode, m_playback.GetLastTime());
    m_ReleaseHoldObject((uint)buttonCode);
}

MapTotals CalculateMapTotals() const
{
	MapTotals ret = { 0 };
const Beatmap& map = m_playback.GetBeatmap();

List<LaserObjectDataBase> processedLasers;

assert(m_playback);
auto& objects = map.GetLinearObjects();
	for(auto& _obj : objects)
	{
		MultiObjectDataBase obj = *_obj;
const TimingPoint* tp = m_playback.GetTimingPointAt(obj.time);
		if(obj.type == ButtonType.Single)
		{
			ret.maxScore += (uint) ScoreHitRating.Perfect;
ret.numSingles += 1;
		}
		else if(obj.type == ButtonType.Hold)
		{
			List<int> holdTicks;
m_CalculateHoldTicks((HoldObjectDataBase) obj, holdTicks);
ret.maxScore += (uint) ScoreHitRating.Perfect * (uint) holdTicks.size();
ret.numTicks += (uint) holdTicks.size();
		}
		else if(obj.type == ButtonType.Laser)
		{
			LaserObjectDataBase laserRoot = obj.laser.GetRoot();

			// Don't evaluate ticks for every segment, only for entire chains of segments
			if(!processedLasers.Contains(laserRoot))
			{
				List<ScoreTick> laserTicks;
m_CalculateLaserTicks((LaserObjectDataBase) obj, laserTicks);
ret.maxScore += (uint) ScoreHitRating.Perfect * (uint) laserTicks.size();
ret.numTicks += (uint) laserTicks.size();
processedLasers.Add(laserRoot);
			}
		}
	}

	return ret;
}

uint CalculateCurrentScore() const
{
	return CalculateScore(currentHitScore);
}

uint CalculateScore(uint hitScore) const
{
	return (uint) (((double) hitScore / (double) MapTotals.maxScore) * 10000000.0);
}

uint CalculateCurrentGrade() const
{
	uint value = (uint)((double)CalculateCurrentScore() * (double)0.9 + currentGauge * 1000000.0);
	if(value > 9800000) // AAA
		return 0;
	if(value > 9400000) // AA
		return 1;
	if(value > 8900000) // A
		return 2;
	if(value > 8000000) // B
		return 3;
	if(value > 7000000) // C
		return 4;
	return 5; // D
}

int ScoreTick.GetHitWindow() const
{
	// Hold ticks don't have a hit window, but the first ones do
	if(HasFlag(TickFlags.Hold) && !HasFlag(TickFlags.Start))
		return 0;
	// Laser ticks also don't have a hit window except for the first ticks and slam segments
	if(HasFlag(TickFlags.Laser))
	{
		if(!HasFlag(TickFlags.Start) && !HasFlag(TickFlags.Slam))
			return 0;
		return perfectHitTime;
	}
	return missHitTime;
}
ScoreHitRating ScoreTick.GetHitRating(int currentTime) const
{
	int delta = abs(time - currentTime);
	return GetHitRatingFromDelta(delta);
}
ScoreHitRating ScoreTick.GetHitRatingFromDelta(int delta) const
{
	delta = abs(delta);
	if(HasFlag(TickFlags.Button))
	{
		// Button hit judgeing
		if(delta <= perfectHitTime)
			return ScoreHitRating.Perfect;
		if(delta <= goodHitTime)
			return ScoreHitRating.Good;
		return ScoreHitRating.Miss;
	}
	return ScoreHitRating.Perfect;
}

}