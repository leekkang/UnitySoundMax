namespace SoundMax {
    public enum Difficulty {
        Light,
        Challenge,
        Extended,
        Infinite
    }

    public enum ButtonType : byte {
        Invalid,
        Single,
        Hold,
        Laser,
        Event
    }

    public enum EventKey : byte {
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
        Manual = 0x4,
        Keep = 0x8
    }

    public enum SpinType : byte {
        None = 0x0,
        Full = 0x1,
        Quarter = 0x2,
        Bounce = 0x3,
    }

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
        UserDefined0 = 0x40, // This ID or higher is user for user defined effects inside map objects
        UserDefined1,   // Keep this ID at least a few ID's away from the normal effect so more native effects can be added later
        UserDefined2
    }
}

public class Enums {

}
