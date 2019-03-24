using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType {
    None = 0,
    Retrigger,
    Flanger,
    Phaser,
    Gate,
    TapeStop,
    Bitcrush,
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

public class SoundManager : Singleton<SoundManager> {
    
    public void Open() {

    }
}
