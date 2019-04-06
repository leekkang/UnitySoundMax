using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SoundMax;
using System.Linq;

public class PlaybackEngine {
    public int m_playbackTime;  // 현재 시간
    List<TimingPoint> m_timingPoints;
    List<ChartStop> m_chartStops;
    List<ObjectDataBase> m_objects;
    List<ZoomControlPoint> m_zoomPoints;
    List<LaneHideTogglePoint> m_laneTogglePoints;
    bool m_initialEffectStateSent = false;

    public TimingPoint m_currentTiming;
    ObjectDataBase m_currentObj;
    ObjectDataBase m_currentLaserObj;
    ObjectDataBase m_currentAlertObj;
    LaneHideTogglePoint m_currentLaneTogglePoint;
    ZoomControlPoint m_currentZoomPoint;

    ZoomControlPoint[] m_zoomStartPoints = new ZoomControlPoint[4];
    ZoomControlPoint[] m_zoomEndPoints = new ZoomControlPoint[4];

    public List<ObjectDataBase> m_hittableObjects;
    List<ObjectDataBase> m_holdObjects;
    List<ObjectDataBase> m_effectObjects;

    Dictionary<EventKey, EventData> m_eventMapping;

    public float m_barTime;
    public float m_beatTime;

    Beatmap m_beatmap;

    bool Reset(int startTime) {
        m_effectObjects.Clear();
        m_timingPoints = m_beatmap.mListTimingPoint;
        m_chartStops = m_beatmap.mListChartStop;
        m_objects = m_beatmap.mListObjectState;
        m_zoomPoints = m_beatmap.mListZoomPoint;
        m_laneTogglePoints = m_beatmap.mListLaneTogglePoint;

        if (m_objects.Count == 0)
            return false;
        if (m_timingPoints.Count == 0)
            return false;

        Debug.Log(string.Format("Resetting BeatmapPlayback with StartTime = {0}", startTime));
        m_playbackTime = startTime;
        m_currentObj = m_objects[0];
        m_currentAlertObj = m_objects[0];
        m_currentLaserObj = m_objects[0];
        m_currentTiming = m_timingPoints[0];
        m_currentZoomPoint = m_zoomPoints.Count == 0 ? null : m_zoomPoints[0];
        for (int i = 0; i < m_zoomPoints.Count; i++) {
            if (m_zoomPoints[i].time != Int32.MinValue) //Not a starting point.
                break;

            m_zoomStartPoints[m_zoomPoints[i].index] = m_zoomPoints[i];
        }
        m_currentLaneTogglePoint = m_laneTogglePoints.Count == 0 ? null : m_laneTogglePoints[0];

        //hittableLaserEnter = (*m_currentTiming)->beatDuration * 4.0;
        //alertLaserThreshold = (*m_currentTiming)->beatDuration * 6.0;
        m_hittableObjects.Clear();
        m_holdObjects.Clear();

        m_barTime = 0;
        m_beatTime = 0;
        m_initialEffectStateSent = false;
        return true;
    }

    public void Update(int newTime) {

    }
}