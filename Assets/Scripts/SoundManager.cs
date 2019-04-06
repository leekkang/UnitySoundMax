using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoundMax;

public class EffectDuration {
    enum Type : byte {
        Rate, // Relative (1/4, 1/2, 0.5, etc), all relative to whole notes
        Time, // Absolute, in milliseconds
    }

    Type type;

    // type에 따라 둘 중 하나에 값이 들어간다.
    float mRate;
    int mDuration;

    public EffectDuration(int duration) {
        mDuration = duration;
        type = Type.Time;
    }
    public EffectDuration(float rate) {
        mRate = rate;
        type = Type.Rate;
    }
}

public class EffectParam<T> {
    // Either 1 or 2 values based on if this value should be interpolated by laser input or not
    T[] mValue = new T[2];
    // this means the parameter is a range
    public bool mIsRange;
    // TODO : tween 의 EaseType 을 사용해서 값을 바꾸는 함수 또는 변수 추가
    string mEaseType;
    
    public EffectParam(T value) {
        mValue[0] = value;
        mIsRange = false;
    }
    public EffectParam(T from, T to, string easeType = "") {
        mValue[0] = from;
        mValue[1] = to;
        mEaseType = easeType;
        mIsRange = true;
    }
}

#region AudioEffect

public class AudioEffect {
    public EffectType mType;

    // Timing division for time based effects
    // Wobble:		length of single cycle
    // Phaser:		length of single cycle
    // Flanger:		length of single cycle
    // Tapestop:	duration of stop
    // Gate:		length of a single
    // Sidechain:	duration before reset
    // Echo:		delay
    public EffectParam<EffectDuration> mDuration;// = new EffectParam<EffectDuration>(new EffectDuration(0.25f)); // 1/4

    // How much of the effect is mixed in with the source audio
    public EffectParam<float> mMix; // = 0f;
}

public class AudioEffectRetrigger : AudioEffect {
    // Amount of gating on this effect (0-1)
    public EffectParam<float> gate;
    // Duration after which the retriggered sample area resets
    // 0 for no reset
    // TODO: This parameter allows this effect to be merged with gate
    public EffectParam<EffectDuration> reset;
}
public class AudioEffectGate : AudioEffect {
    // Amount of gating on this effect (0-1)
    public EffectParam<float> gate;
}
public class AudioEffectFlanger : AudioEffect {
    // Number of samples that is offset from the source audio to create the flanging effect (Samples)
    public EffectParam<int> offset;
    // Depth of the effect (samples)
    public EffectParam<int> depth;
}
public class AudioEffectPhaser : AudioEffect {
    // Minimum frequency (Hz)
    public EffectParam<float> min;
    // Maximum frequency (Hz)
    public EffectParam<float> max;
    // Depth of the effect (>=0)
    public EffectParam<float> depth;
    // Feedback (0-1)
    public EffectParam<float> feedback;
}
public class AudioEffectBitcrusher : AudioEffect {
    // The duration in samples of how long a sample in the source audio gets reduced (creating square waves) (samples)
    public EffectParam<int> reduction;
}
public class AudioEffectWobble : AudioEffect {
    // Top frequency of the wobble (Hz)
    public EffectParam<float> max;
    // Bottom frequency of the wobble (Hz)
    public EffectParam<float> min;
    // Q factor for filter (>0)
    public EffectParam<float> q;
}
public class AudioEffectEcho : AudioEffect {
    // Ammount of echo (0-1)
    public EffectParam<float> feedback;
}
public class AudioEffectPanning : AudioEffect {
    // Panning position, 0 is center (-1-1)
    public EffectParam<float> panning;
}
public class AudioEffectPitchshift : AudioEffect {
    // Pitch shift amount, in semitones
    public EffectParam<float> amount;
}
public class AudioEffectLPF : AudioEffect {
    // Peak Q factor (>=0)
    public EffectParam<float> peakQ;
    // Peak amplification (>=0)
    public EffectParam<float> gain;
    // Q factor for filter (>0)
    public EffectParam<float> q;
    // Cuttoff frequency (Hz)
    public EffectParam<float> freq;
}
public class AudioEffectHPF : AudioEffect {
    // Peak Q factor (>=0)
    public EffectParam<float> peakQ;
    // Peak amplification (>=0)
    public EffectParam<float> gain;
    // Q factor for filter (>0)
    public EffectParam<float> q;
    // Cuttoff frequency (Hz)
    public EffectParam<float> freq;
}
public class AudioEffectPeaking : AudioEffect {
    // Peak amplification (>=0)
    public EffectParam<float> gain;
    // Q factor for filter (>0)
    public EffectParam<float> q;
    // Cuttoff frequency (Hz)
    public EffectParam<float> freq;
}

public class DefaultEffectSettings {
    List<AudioEffect> mListEffect;

    public DefaultEffectSettings() {
        mListEffect = new List<AudioEffect>();

        for (EffectType type = EffectType.Retrigger; type < EffectType.UserDefined0; type++) {
            mListEffect.Add(CreateDefault(type));
        }
    }

    public AudioEffect GetDefault(EffectType type) {
        return mListEffect.Find((x) => x.mType == type);
    }

    public AudioEffect CreateDefault(EffectType type) {
        AudioEffect effect = null;

        if (type == EffectType.PeakingFilter) {
            effect = new AudioEffectPeaking() {
                freq = new EffectParam<float>(80f, 8000f, "EaseInExpo"),
                q = new EffectParam<float>(1f, 0.8f),
                gain = new EffectParam<float>(20f, 20f)
            };
        } else if (type == EffectType.LowPassFilter) {
            effect = new AudioEffectLPF() {
                freq = new EffectParam<float>(10000f, 700f, "EaseOutCubic"),
                q = new EffectParam<float>(7f, 10f)
            };
        } else if (type == EffectType.HighPassFilter) {
            effect = new AudioEffectHPF() {
                freq = new EffectParam<float>(80f, 2000f, "EaseInExpo"),
                q = new EffectParam<float>(10f, 5f)
            };
        } else if (type == EffectType.Bitcrusher) {
            effect = new AudioEffectBitcrusher() {
                reduction = new EffectParam<int>(0, 45)
            };
        } else if (type == EffectType.Gate) {
            effect = new AudioEffectGate() {
                gate = new EffectParam<float>(0.5f)
            };
        } else if (type == EffectType.Retrigger) {
            effect = new AudioEffectRetrigger() {
                gate = new EffectParam<float>(0.7f),
                reset = new EffectParam<EffectDuration>(new EffectDuration(0.5f))
            };
        } else if (type == EffectType.Echo) {
            effect = new AudioEffectEcho() {
                feedback = new EffectParam<float>(0.6f)
            };
        } else if (type == EffectType.Panning) {
            effect = new AudioEffectPanning() {
                panning = new EffectParam<float>(0f)
            };
        } else if (type == EffectType.Phaser) {
            effect = new AudioEffectPhaser() {
                min = new EffectParam<float>(1500f),
                max = new EffectParam<float>(20000f),
                feedback = new EffectParam<float>(0.35f),
                mDuration = new EffectParam<EffectDuration>(new EffectDuration(1f))
            };
        } else if (type == EffectType.Wobble) {
            effect = new AudioEffectWobble() {
                mDuration = new EffectParam<EffectDuration>(new EffectDuration(1f / 12f)),
                min = new EffectParam<float>(500f),
                max = new EffectParam<float>(20000f),
                q = new EffectParam<float>(2f)
            };
        } else if (type == EffectType.Flanger) {
            effect = new AudioEffectFlanger() {
                offset = new EffectParam<int>(10),
                depth = new EffectParam<int>(40)
            };
        } else if (type == EffectType.TapeStop) {
            effect = new AudioEffect();
        }

        if (effect != null) {
            effect.mType = type;
            effect.mDuration = new EffectParam<EffectDuration>(new EffectDuration(.25f));
            effect.mMix = new EffectParam<float>(1f);
        }

        return effect;
    }
}

#endregion

public class SoundManager : Singleton<SoundManager> {
    
    public void Open() {

    }
}
