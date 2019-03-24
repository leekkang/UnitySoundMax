using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ButtonType {
    Invalid,
    Single,
    Hold,
    Laser, 
    Event
}

public enum EventKey {
    SlamVolume,
    LaserEffectType,
    LaserEffectMix,
    TrackRollBehaviour,
    ChartEnd
}
public enum TrackRollBehaviour : byte {
    Zero = 0,
    Normal = 0x1,
    Bigger = 0x2,
    Biggest = 0x3,
    Keep = 0x4
}

public class NormalButtonData {
    public byte mIndex = 0xff;  // 0~3 : normal, 4~5 : fx
    public bool mHasSample = false;
    public int mSampleIndex;
    public float mSampleVolume = 1.0f;
    public const ButtonType mButtonType = ButtonType.Single;
}

public class HoldButtonData : NormalButtonData {
    public int mDuration = 0;   // hold button length
    public EffectType mEffectType = EffectType.None;
    public int[] mEffectParams = new int[2];
    public new const ButtonType mButtonType = ButtonType.Hold;
}

public enum SpinType : byte {
    None = 0x0,
    Full = 0x1,
    Quarter = 0x2,
    Bounce = 0x3,
}
public class SpinStruct {
    public SpinType mType = SpinType.None;
    public float mDirection;
    public int mDuration;
    public int mAmplitude;
    public int mFrequency;
    public int mDecay;
}

public class LaserData {
    public int mDuration;   // duration of laser segment
    public int mIndex;      // 0 or 1 for left and right respectively
    public int mFlags;      // special options
    public float[] mPoints = new float[2];

    public SpinStruct mSpin = new SpinStruct();

    public const ButtonType mButtonType = ButtonType.Laser;
    // Indicates that this segment is instant and should generate a laser slam segment
    public const byte mFlagInstant = 0x1;
    // Indicates that the range of this laser is extended from -0.5 to 1.5
    public const byte mFlagExtended = 0x2;
}

public class EventData {
    public EventKey mKey;
    public int mData;   // always 32bit data
    public const ButtonType mButtonType = ButtonType.Event;
}


public class TimingPoint {
    public double mBeatDuration;
    public int mNumerator;
    public int mDenominator;

    public double GetWholeNoteLength() {
        return mBeatDuration * 4;
    }
    public double GetBarDuration() {
        return GetWholeNoteLength() * ((double)mNumerator / mDenominator);
    }
    public double GetBpm() {
        return 60000.0f / mBeatDuration;
    }
}

public class IngameEngine : Singleton<IngameEngine> {
    MusicData mCurMusic = new MusicData();

    public void Open() {
    }


}
