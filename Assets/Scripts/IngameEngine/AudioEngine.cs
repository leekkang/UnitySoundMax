﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SoundMax;
using System.Linq;

public class AudioEngine {
    public PlaybackEngine m_playback;
    string m_beatmapRootPath;

    AudioSource m_music;
    AudioSource m_fxtrack;
    public bool m_paused = false;
    bool m_fxtrackEnabled = true;

    EffectType m_laserEffectType = EffectType.None;
    AudioEffect m_laserEffect;
    DSP m_laserDSP;
    float m_laserEffectMix = 1.0f;
    float m_laserInput = 0.0f;

    AudioEffect[] m_buttonEffects = new AudioEffect[2];
    DSP[] m_buttonDSPs = new DSP[2];
    HoldButtonData[] m_currentHoldEffects = new HoldButtonData[2];
    float[] m_effectMix = new float[2];

    float PlaybackSpeed = 1.0f;
    List<DSP> mMusicDSPs = new List<DSP>();
    List<DSP> mFxDSPs = new List<DSP>();

    void m_CleanupDSP(DSP dsp) {
        if (dsp == null)
            return;

        if (m_fxtrack != null)
            mFxDSPs.Remove(dsp);
        else
            mMusicDSPs.Remove(dsp);

        dsp = null;
    }

    public bool Init(PlaybackEngine playback, string mapRootPath) {
        m_currentHoldEffects[0] = null;
        m_currentHoldEffects[1] = null;
        m_CleanupDSP(m_buttonDSPs[0]);
        m_CleanupDSP(m_buttonDSPs[1]);
        m_CleanupDSP(m_laserDSP);

        m_playback = playback;
        m_beatmapRootPath = mapRootPath;

        // Set default effect type
        SetLaserEffect(EffectType.PeakingFilter);

        //BeatmapSetting mapSettings = playback.m_beatmap.mSetting;
        //string audioPath = Path.Normalize(m_beatmapRootPath + Path.sep + mapSettings.audioNoFX);
        //audioPath.Trim(' ');
        //WString audioPathUnicode = Utility.ConvertToWString(audioPath);
        //if (!Path.FileExists(audioPath)) {
        //    Logf("Audio file for beatmap does not exists at: \"%s\"", Logger.Error, audioPath);
        //    return false;
        //}
        //m_music = g_audio.CreateStream(audioPath, true);
        //if (!m_music) {
        //    Logf("Failed to load any audio for beatmap \"%s\"", Logger.Error, audioPath);
        //    return false;
        //}
        //m_music.SetVolume(mapSettings.musicVolume);

        //// Load FX track
        //audioPath = Path.Normalize(m_beatmapRootPath + Path.sep + mapSettings.audioFX);
        //audioPath.TrimBack(' ');
        //audioPathUnicode = Utility.ConvertToWString(audioPath);
        //if (!audioPath.empty()) {
        //    if (!Path.FileExists(audioPath) || Path.IsDirectory(audioPath)) {
        //        Logf("FX audio for for beatmap does not exists at: \"%s\" Using real-time effects instead.", Logger.Warning, audioPath);
        //    } else {
        //        m_fxtrack = g_audio.CreateStream(audioPath, true);
        //        if (m_fxtrack) {
        //            // Initially mute normal track if fx is enabled
        //            m_music.SetVolume(0.0f);
        //        }
        //    }
        //}

        return true;
    }
    void Tick(float deltaTime) {

    }
    void Play() {
        m_music.Play();
        if (m_fxtrack)
            m_fxtrack.Play();
    }
    void Advance(int ms) {
        SetPosition(GetPosition() + ms);
    }
    int GetPosition() {
        return (int)(m_music.time * 1000);
    }
    public void SetPosition(int time) {
        m_music.time = time * 0.001f;
        if (m_fxtrack)
            m_fxtrack.time = time * 0.001f;
    }
    void TogglePause() {
        if (m_paused) {
            m_music.Play();
            if (m_fxtrack)
                m_fxtrack.Play();
        } else {
            m_music.Pause();
            if (m_fxtrack)
                m_fxtrack.Pause();
        }
        m_paused = !m_paused;
    }
    bool HasEnded() {
        return !m_music.isPlaying;
    }
    public void SetEffect(int index, HoldButtonData obj, PlaybackEngine playback) {
        // 필요한건가?
        // Don't use effects when using an FX track
        //if(m_fxtrack.IsValid())
        //	return;

        //assert(index >= 0 && index <= 1);
        m_CleanupDSP(m_buttonDSPs[index]);
        m_currentHoldEffects[index] = obj;

        // For Time based effects
        TimingPoint timingPoint = playback.GetTimingPointAt(obj.mTime);
        // Duration of a single bar
        double barDelay = timingPoint.mNumerator * timingPoint.mBeatDuration;

        DSP dsp = m_buttonDSPs[index];

        m_buttonEffects[index] = m_playback.m_beatmap.GetEffect(obj.mEffectType);
        dsp = m_buttonEffects[index].CreateDSP(this);

        if (dsp != null) {
            m_buttonEffects[index].SetParams(dsp, this, obj);
            // Initialize mix value to previous value
            dsp.mix = m_effectMix[index];
            dsp.startTime = obj.mTime;
            dsp.chartOffset = playback.m_beatmap.mSetting.offset;
            dsp.lastTimingPoint = playback.GetCurrentTimingPoint().mTime;
        }
    }
    void SetEffectEnabled(int index, bool enabled) {
        //assert(index >= 0 && index <= 1);
        m_effectMix[index] = enabled ? 1.0f : 0.0f;
        if (m_buttonDSPs[index] != null) {
            m_buttonDSPs[index].mix = m_effectMix[index];
        }
    }
    public void ClearEffect(int index, HoldButtonData obj) {
        //assert(index >= 0 && index <= 1);
        if (m_currentHoldEffects[index] == obj) {
            m_CleanupDSP(m_buttonDSPs[index]);
            m_currentHoldEffects[index] = null;
        }
    }
    public void SetLaserEffect(EffectType type) {
        if (type != m_laserEffectType) {
            m_CleanupDSP(m_laserDSP);
            m_laserEffectType = type;
            m_laserEffect = m_playback.m_beatmap.GetFilter(type);
        }
    }
    void SetLaserFilterInput(float input, bool active) {
        if (m_laserEffect.mType != EffectType.None && (active || (input != 0.0f))) {
            // Create DSP
            if (m_laserDSP == null) {
                // Don't use Bitcrush effects over FX track
                if (m_fxtrack.isPlaying && m_laserEffectType == EffectType.Bitcrusher)
                    return;

                m_laserDSP = m_laserEffect.CreateDSP(this);
                if (m_laserDSP == null) {
                    Debug.Log(string.Format("Failed to create laser DSP with type {0}", m_laserEffect.mType));
                    return;
                }
            }

            // Set params
            m_SetLaserEffectParameter(input);
            m_laserInput = input;
        } else {
            m_CleanupDSP(m_laserDSP);
            m_laserInput = 0.0f;
        }
    }
    public float GetLaserFilterInput() {
        return m_laserInput;
    }
    public void SetLaserEffectMix(float mix) {
        m_laserEffectMix = mix;
    }
    float GetLaserEffectMix() {
        return m_laserEffectMix;
    }
    void SetFXTrackEnabled(bool enabled) {
        if (!m_fxtrack)
            return;
        if (m_fxtrackEnabled != enabled) {
            if (enabled) {
                m_fxtrack.volume = 1.0f;
                m_music.volume = 0.0f;
            } else {
                m_fxtrack.volume = 0.0f;
                m_music.volume = 1.0f;
            }
        }
        m_fxtrackEnabled = enabled;
    }
    PlaybackEngine GetBeatmapPlayback() {
        return m_playback;
    }
    Beatmap GetBeatmap() {
        return m_playback.m_beatmap;
    }
    string GetBeatmapRootPath() {
        return m_beatmapRootPath;
    }
    void SetVolume(float volume) {
        m_music.volume = volume;
        if (m_fxtrack)
            m_fxtrack.volume = volume;
    }
    void m_SetLaserEffectParameter(float input) {
        if (m_laserDSP == null)
            return;

        //assert(input >= 0.0f && input <= 1.0f);

        // Mix float biquad filters, these are applied manualy by changing the filter parameters (gain,q,freq,etc.)
        float mix = m_laserEffectMix;
        double noteDuration = m_playback.GetCurrentTimingPoint().GetWholeNoteLength();
        int actualLength = m_laserEffect.mDuration.SampleDuration(input, noteDuration);

        if (input < 0.1f)
            mix *= input / 0.1f;

        switch (m_laserEffect.mType) {
            case EffectType.Bitcrusher:
                m_laserDSP.mix = m_laserEffect.mMix.Sample(input);
                // TODO : 샘플레이트를 구하는 방법을 모르겠음.
                // increment, period 저장
                int assist = 1 << 16;
                m_laserDSP.mListInt.Add(assist);
                m_laserDSP.mListInt.Add((int)(assist * ((AudioEffectBitcrusher)m_laserEffect).reduction.Sample(input)));
            break;
            case EffectType.Echo:
            m_laserDSP.mix = m_laserEffect.mMix.Sample(input);
            // feedback 저장
            m_laserDSP.mListFloat.Add(((AudioEffectEcho)m_laserEffect).feedback.Sample(input));
            break;
            case EffectType.PeakingFilter:
            m_laserDSP.mix = m_laserEffectMix;
            if (input > 0.8f)
                mix *= 1.0f - (input - 0.8f) / 0.2f;

            //BQFDSP* bqfDSP = (BQFDSP*)m_laserDSP;
            //bqfDSP.SetPeaking(m_laserEffect.peaking.q.Sample(input), m_laserEffect.peaking.freq.Sample(input), m_laserEffect.peaking.gain.Sample(input) * mix);
            break;
            case EffectType.LowPassFilter:
            m_laserDSP.mix = m_laserEffectMix;
            //BQFDSP* bqfDSP = (BQFDSP*)m_laserDSP;
            //bqfDSP.SetLowPass(m_laserEffect.lpf.q.Sample(input) * mix + 0.1f, m_laserEffect.lpf.freq.Sample(input));
            break;
            case EffectType.HighPassFilter:
            m_laserDSP.mix = m_laserEffectMix;
            //BQFDSP* bqfDSP = (BQFDSP*)m_laserDSP;
            //bqfDSP.SetHighPass(m_laserEffect.hpf.q.Sample(input) * mix + 0.1f, m_laserEffect.hpf.freq.Sample(input));
            break;
            case EffectType.PitchShift:
            m_laserDSP.mix = m_laserEffect.mMix.Sample(input);
            // amount 저장
            m_laserDSP.mListFloat.Add(((AudioEffectPitchshift)m_laserEffect).amount.Sample(input));
            break;
            case EffectType.Gate:
            m_laserDSP.mix = m_laserEffect.mMix.Sample(input);
            // amount 저장
            //AudioSettings.outputSampleRate
            //m_laserDSP.mListFloat.Add(((AudioEffectPitchshift)m_laserEffect).amount.Sample(input));
            //GateDSP* gd = (GateDSP*)m_laserDSP;
            //gd.SetLength(actualLength);
            break;
            case EffectType.Retrigger:
            //m_laserDSP.mix = m_laserEffect.mMix.Sample(input);
            //RetriggerDSP* rt = (RetriggerDSP*)m_laserDSP;
            //rt.SetLength(actualLength);
            break;
        }
    }
}