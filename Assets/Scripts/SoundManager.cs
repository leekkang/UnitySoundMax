using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class SoundManager : Singleton<SoundManager> {
        public void Open() {
            Debug.Log("SoundManager Open");
        }
    }

    public class EffectDuration {
        public enum Type : byte {
            Rate, // Relative (1/4, 1/2, 0.5, etc), all relative to whole notes
            Time, // Absolute, in milliseconds
        }

        public Type type;

        // type에 따라 둘 중 하나에 값이 들어간다.
        public float mRate;
        public int mDuration;

        public EffectDuration(int duration) {
            mDuration = duration;
            type = Type.Time;
        }
        public EffectDuration(float rate) {
            mRate = rate;
            type = Type.Rate;
        }

        public int Absolute(double noteDuration) {
            return type == Type.Time ? mDuration : (int)(mRate * noteDuration);
        }
    }

    public class EffectParam<T> {
        // Either 1 or 2 values based on if this value should be interpolated by laser input or not
        object[] mValue = new object[2];
        // this means the parameter is a range
        public bool mIsRange;
        // TODO : tween 의 EaseType 을 사용해서 값을 바꾸는 함수 또는 변수 추가
        EasingFunction.Ease mEaseType;

        public EffectParam(object value) {
            mValue[0] = value;
            mIsRange = false;
        }
        public EffectParam(object from, object to, EasingFunction.Ease easeType = EasingFunction.Ease.Linear) {
            mValue[0] = from;
            mValue[1] = to;
            mEaseType = easeType;
            mIsRange = true;
        }

        // Sample based on laser input, or without parameters for just the actual value
        public int SampleDuration(float t, double noteDuration) {
            EasingFunction.Function func = EasingFunction.GetEasingFunction(mEaseType);
            EffectDuration val1 = null;
            EffectDuration val2 = null;
            if (typeof(T) == typeof(EffectDuration)) {
                val1 = mValue[0] as EffectDuration;
                if (mIsRange)
                    val2 = mValue[1] as EffectDuration;
            }
            bool bTime = val1.type == EffectDuration.Type.Time;
            if (bTime) {
                return mIsRange ? (int)func(val1.mDuration, val2.mDuration, t) : val1.Absolute(noteDuration);
            } else {
                return mIsRange ? (int)(func(val1.mRate, val2.mRate, t) * noteDuration) : val1.Absolute(noteDuration);
            }
        }
        public float Sample(float t) {
            EasingFunction.Function func = EasingFunction.GetEasingFunction(mEaseType);
            return mIsRange ? func((float)mValue[0], (float)mValue[1], t) : (float)mValue[0];
        }
    }

    #region AudioEffect

    public class DSP {
        public float mix = 1.0f;
        public int priority = 0;
        public int startTime = 0;
        public int chartOffset = 0;
        public int lastTimingPoint = 0;
        public List<float> mListFloat = new List<float>();
        public List<int> mListInt = new List<int>();
    }

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

        public virtual DSP CreateDSP(AudioEngine playback) {
            DSP ret = new DSP();

            //TimingPoint tp = playback.m_playback.GetCurrentTimingPoint();
            //double noteDuration = tp.GetWholeNoteLength();

            //float filterInput = playback.GetLaserFilterInput();
            //int actualLength = mDuration.SampleDuration(filterInput, noteDuration);
            //int maxLength = Mathf.Max(mDuration.SampleDuration(0, noteDuration), mDuration.SampleDuration(1, noteDuration));
            //switch (mType) {
            //    case EffectType.Bitcrusher: {
            //        BitCrusherDSP* bcDSP = new BitCrusherDSP();
            //        audioTrack.AddDSP(bcDSP);
            //        bcDSP.SetPeriod((float)bitcrusher.reduction.Sample(filterInput));
            //        ret = bcDSP;
            //        break;
            //    }
            //    case EffectType.Echo: {
            //        EchoDSP* echoDSP = new EchoDSP();
            //        audioTrack.AddDSP(echoDSP);
            //        echoDSP.feedback = echo.feedback.Sample(filterInput) / 100.0f;
            //        echoDSP.SetLength(actualLength);
            //        ret = echoDSP;
            //        break;
            //    }
            //    case EffectType.PeakingFilter:
            //    case EffectType.LowPassFilter:
            //    case EffectType.HighPassFilter: {
            //        // Don't set anthing for biquad Filters
            //        BQFDSP* bqfDSP = new BQFDSP();
            //        audioTrack.AddDSP(bqfDSP);
            //        ret = bqfDSP;
            //        break;
            //    }
            //    case EffectType.Gate: {
            //        GateDSP* gateDSP = new GateDSP();
            //        audioTrack.AddDSP(gateDSP);
            //        gateDSP.SetLength(actualLength);
            //        gateDSP.SetGating(gate.gate.Sample(filterInput));
            //        ret = gateDSP;
            //        break;
            //    }
            //    case EffectType.TapeStop: {
            //        TapeStopDSP* tapestopDSP = new TapeStopDSP();
            //        audioTrack.AddDSP(tapestopDSP);
            //        tapestopDSP.SetLength(actualLength);
            //        ret = tapestopDSP;
            //        break;
            //    }
            //    case EffectType.Retrigger: {
            //        RetriggerDSP* retriggerDSP = new RetriggerDSP();
            //        audioTrack.AddDSP(retriggerDSP);
            //        retriggerDSP.SetMaxLength(maxLength);
            //        retriggerDSP.SetLength(actualLength);
            //        retriggerDSP.SetGating(retrigger.gate.Sample(filterInput));
            //        retriggerDSP.SetResetDuration(retrigger.reset.Sample(filterInput).Absolute(noteDuration));
            //        ret = retriggerDSP;
            //        break;
            //    }
            //    case EffectType.Wobble: {
            //        WobbleDSP* wb = new WobbleDSP();
            //        audioTrack.AddDSP(wb);
            //        wb.SetLength(actualLength);
            //        wb.q = wobble.q.Sample(filterInput);
            //        wb.fmax = wobble.max.Sample(filterInput);
            //        wb.fmin = wobble.min.Sample(filterInput);
            //        ret = wb;
            //        break;
            //    }
            //    case EffectType.Phaser: {
            //        PhaserDSP* phs = new PhaserDSP();
            //        audioTrack.AddDSP(phs);
            //        phs.SetLength(actualLength);
            //        phs.dmin = phaser.min.Sample(filterInput);
            //        phs.dmax = phaser.max.Sample(filterInput);
            //        phs.fb = phaser.feedback.Sample(filterInput);
            //        ret = phs;
            //        break;
            //    }
            //    case EffectType.Flanger: {
            //        FlangerDSP* fl = new FlangerDSP();
            //        audioTrack.AddDSP(fl);
            //        fl.SetLength(actualLength);
            //        fl.SetDelayRange(flanger.offset.Sample(filterInput),
            //            flanger.depth.Sample(filterInput));
            //        ret = fl;
            //        break;
            //    }
            //    case EffectType.SideChain: {
            //        SidechainDSP* sc = new SidechainDSP();
            //        audioTrack.AddDSP(sc);
            //        sc.SetLength(actualLength);
            //        sc.amount = 1.0f;
            //        sc.curve = Interpolation.CubicBezier(0.39, 0.575, 0.565, 1);
            //        ret = sc;
            //        break;
            //    }
            //    case EffectType.PitchShift: {
            //        PitchShiftDSP* ps = new PitchShiftDSP();
            //        audioTrack.AddDSP(ps);
            //        ps.amount = pitchshift.amount.Sample(filterInput);
            //        ret = ps;
            //        break;
            //    }
            //}

            if (ret == null) {
                Debug.Log(string.Format("Failed to create game audio effect for type \"{0}\"", mType));
            }

            return ret;
        }

        public void SetParams(DSP dsp, AudioEngine playback, HoldButtonData obj) {
            //TimingPoint tp = playback.m_playback.GetCurrentTimingPoint();
            //double noteDuration = tp.GetWholeNoteLength();

            //switch (mType) {
            //    case EffectType.Bitcrush:
            //        BitCrusherDSP* bcDSP = (BitCrusherDSP*)dsp;
            //        bcDSP.SetPeriod((float)obj.effectParams[0]);
            //        break;
            //    case EffectType.Gate:
            //        GateDSP* gateDSP = (GateDSP*)dsp;
            //        gateDSP.SetLength(noteDuration / obj.effectParams[0]);
            //        gateDSP.SetGating(0.5f);
            //        break;
            //    case EffectType.TapeStop:
            //        TapeStopDSP* tapestopDSP = (TapeStopDSP*)dsp;
            //        tapestopDSP.SetLength((1000 * ((double)16 / Math.Max(obj.effectParams[0], (int16)1))));
            //        break;
            //    case EffectType.Retrigger:
            //        RetriggerDSP* retriggerDSP = (RetriggerDSP*)dsp;
            //        retriggerDSP.SetLength(noteDuration / obj.effectParams[0]);
            //        retriggerDSP.SetGating(0.65f);
            //        break;
            //    case EffectType.Echo:
            //        EchoDSP* echoDSP = (EchoDSP*)dsp;
            //        echoDSP.SetLength(noteDuration / obj.effectParams[0]);
            //        echoDSP.feedback = obj.effectParams[1] / 100.0f;
            //        break;
            //    case EffectType.Wobble:
            //        WobbleDSP* wb = (WobbleDSP*)dsp;
            //        wb.SetLength(noteDuration / obj.effectParams[0]);
            //        break;
            //    case EffectType.Phaser:
            //        PhaserDSP* phs = (PhaserDSP*)dsp;
            //        phs.time = obj.time;
            //        break;
            //    case EffectType.Flanger:
            //        FlangerDSP* fl = (FlangerDSP*)dsp;
            //        double delay = (noteDuration) / 1000.0;
            //        fl.SetLength(obj.effectParams[0]);
            //        fl.SetDelayRange(10, 40);
            //        break;
            //    case EffectType.PitchShift:
            //        PitchShiftDSP* ps = (PitchShiftDSP*)dsp;
            //        ps.amount = (float)obj.effectParams[0];
            //        break;
            //}
        }
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
                    freq = new EffectParam<float>(80f, 8000f, EasingFunction.Ease.EaseInExpo),
                    q = new EffectParam<float>(1f, 0.8f),
                    gain = new EffectParam<float>(20f, 20f)
                };
            } else if (type == EffectType.LowPassFilter) {
                effect = new AudioEffectLPF() {
                    freq = new EffectParam<float>(10000f, 700f, EasingFunction.Ease.EaseOutCubic),
                    q = new EffectParam<float>(7f, 10f)
                };
            } else if (type == EffectType.HighPassFilter) {
                effect = new AudioEffectHPF() {
                    freq = new EffectParam<float>(80f, 2000f, EasingFunction.Ease.EaseInExpo),
                    q = new EffectParam<float>(10f, 5f)
                };
            } else if (type == EffectType.BitCrusher) {
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

            if (effect == null) {
                effect = new AudioEffect();
            }

            effect.mType = type;
            if (effect.mDuration == null)
                effect.mDuration = new EffectParam<EffectDuration>(new EffectDuration(.25f));
            if (effect.mMix == null)
                effect.mMix = new EffectParam<float>(1f);

            return effect;
        }
    }

    #endregion
}