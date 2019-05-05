namespace SoundMax {

    /// <summary> 노트 히트 계산 시 사용되는 플래그 </summary>
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

    /// <summary> 점수 계산 시 사용되는 판정 </summary>
    public enum ScoreHitRating {
        Miss = 0,
        Good,
        Perfect,
        Idle, // Not actual score, used when a button is pressed when there are no notes
    }

    /// <summary> 난이도 </summary>
    public enum Difficulty {
        Light,
        Challenge,
        Extended,
        Infinite
    }
    
    /// <summary> 노트 생성 시 사용되는 플래그 </summary>
    public enum ButtonType : byte {
        Invalid,
        Single,
        Hold,
        Laser,
        Event
    }

    /// <summary>
    /// <see cref="ButtonType.Event"/>를 통해 만들어지는 버튼의 종류
    /// </summary>
    public enum EventKey : byte {
        SlamVolume,
        LaserEffectType,
        LaserEffectMix,
        TrackRollBehaviour,
        ChartEnd
    }

    /// <summary>
    /// 트랙 돌아가는 정도 구분
    /// </summary>
    public enum TrackRollBehaviour : byte {
        Zero = 0,
        Normal = 0x1,
        Bigger = 0x2,
        Biggest = 0x4,
        Manual = 0x8,
        Keep = 0x10,
    }

    /// <summary> 카메라 회전 타입 </summary>
    public enum SpinType : byte {
        None = 0x0,
        Full = 0x1,
        Quarter = 0x2,
        Bounce = 0x3,
    }

    /// <summary> 오디오 이펙트 타입 </summary>
    public enum EffectType {
        None = 0,
        Retrigger,
        Flanger,
        Phaser,
        Gate,
        TapeStop,
        BitCrusher,
        Wobble,
        SideChain,
        Echo,
        Panning,
        PitchShift,
        LowPassFilter,
        HighPassFilter,
        PeakingFilter,
        UserDefined0,   // This ID or higher is user for user defined effects inside map objects
        UserDefined1,   // Keep this ID at least a few ID's away from the normal effect so more native effects can be added later
        UserDefined2
    }

    /// <summary> 버튼 히트 시 사용되는 이펙트 타입 </summary>
    public enum ParticleType {
        Normal,
        Hold,
        Laser,
        Slam
    }

    /// <summary> 게임 추가 옵션 타입. 추후 개발용 </summary>
    public enum GameFlags : byte {
        None = 0,
        Hard = 1 << 0,
        Mirror = 1 << 1,
        Random = 1 << 2,
        AutoBT = 1 << 3,
        AutoFX = 1 << 4,
        AutoLaser = 1 << 5,
    }
}

public class Enums {

}
