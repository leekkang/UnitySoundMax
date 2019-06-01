using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace SoundMax {
    public class PlaybackEngine {
        public int hittableObjectEnter = 500;
        public int hittableObjectLeave = 500;
        int alertLaserThreshold = 1500;
        int audioOffset = 0;
        public bool cMod = false;
        public float cModSpeed = 400;

        /* Playback events */
        // Called when an object became within the 'hittableObjectTreshold'
        public System.Action<ObjectDataBase> OnObjectEntered;
        // Called when a laser became within the 'alertLaserThreshold'
        public System.Action<LaserData> OnLaserAlertEntered;
        // Called after an object has passed the duration it can be hit in
        public System.Action<ObjectDataBase> OnObjectLeaved;
        // Called when an FX button with effect enters
        public System.Action<HoldButtonData> OnFXBegin;
        // Called when an FX button with effect leaves
        public System.Action<HoldButtonData> OnFXEnd;

        // Called when a new timing point becomes active
        public System.Action<TimingPoint> OnTimingPointChanged;
        public System.Action<EventKey, EventData> OnEventChanged;


        public int m_playbackTime;  // 현재 시간
        List<TimingPoint> m_timingPoints;
        List<ChartStop> m_chartStops;
        List<ObjectDataBase> m_objects;
        List<ZoomControlPoint> m_zoomPoints;
        bool m_initialEffectStateSent = false;

        public TimingPoint m_currentTiming;
        int mCurTimingIndex;
        int mCurObjIndex;
        int mCurLaserIndex;
        int mCurAlertIndex;
        int mCurLaneTogglePointIndex;
        int mCurZoomPointIndex;
        //ZoomControlPoint m_currentZoomPoint;

        ZoomControlPoint[] m_zoomStartPoints = new ZoomControlPoint[4];
        ZoomControlPoint[] m_zoomEndPoints = new ZoomControlPoint[4];

        public List<ObjectDataBase> m_hittableObjects = new List<ObjectDataBase>();
        List<ObjectDataBase> m_holdObjects = new List<ObjectDataBase>();
        List<ObjectDataBase> m_effectObjects = new List<ObjectDataBase>();

        Dictionary<EventKey, EventData> m_eventMapping;

        public float m_barTime;
        public float m_beatTime;

        public Beatmap m_beatmap { get; private set; }

        public void SetBeatmap(Beatmap map) {
            m_beatmap = map;
        }

        public bool Reset(int startTime) {
            m_effectObjects.Clear();
            m_timingPoints = m_beatmap.mListTimingPoint;
            m_chartStops = m_beatmap.mListChartStop;
            m_objects = m_beatmap.mListObjectState;
            m_zoomPoints = m_beatmap.mListZoomPoint;

            if (m_objects.Count == 0)
                return false;
            if (m_timingPoints.Count == 0)
                return false;

            Debug.Log(string.Format("Resetting BeatmapPlayback with StartTime = {0}", startTime));
            m_playbackTime = startTime;
            mCurObjIndex = 0;
            mCurLaserIndex = 0;
            mCurAlertIndex = 0;
            mCurZoomPointIndex = m_zoomPoints.Count == 0 ? -1 : 0;
            mCurTimingIndex = 0;
            
            m_currentTiming = m_timingPoints[0];
            //m_currentZoomPoint = m_zoomPoints.Count == 0 ? null : m_zoomPoints[0];
            for (int i = 0; i < m_zoomPoints.Count; i++) {
                if (m_zoomPoints[i].time != int.MinValue) //Not a starting point.
                    break;

                m_zoomStartPoints[m_zoomPoints[i].index] = m_zoomPoints[i];
            }

            //hittableLaserEnter = (*m_currentTiming).beatDuration * 4.0;
            //alertLaserThreshold = (*m_currentTiming).beatDuration * 6.0;
            m_hittableObjects.Clear();
            m_holdObjects.Clear();

            m_barTime = 0;
            m_beatTime = 0;
            m_initialEffectStateSent = false;
            return true;
        }

        public void UpdateTime(int newTime) {
            //int delta = newTime - m_playbackTime;
            if (newTime < m_playbackTime) {
                // Don't allow backtracking
                //Logf("New time was before last time %ull . %ull", Logger.Warning, m_playbackTime, newTime);
                return;
            }

            // Fire initial effect changes (only once)
            if (!m_initialEffectStateSent) {
                BeatmapSetting settings = m_beatmap.mSetting;
                EventData defaultEvent = new EventData();
                defaultEvent.mEffectVal = settings.laserEffectType;
                defaultEvent.mFloatVal = settings.laserEffectMix;
                OnEventChanged(EventKey.LaserEffectMix, defaultEvent);
                OnEventChanged(EventKey.LaserEffectType, defaultEvent);
                OnEventChanged(EventKey.SlamVolume, defaultEvent);
                m_initialEffectStateSent = true;
            }

            // Count bars
            //int beatID = 0;
            //uint nBeats = CountBeats(m_playbackTime - delta, delta, ref beatID);
            TimingPoint tp = GetCurrentTimingPoint();
            double effectiveTime = ((double)newTime - tp.mTime); // Time with offset applied
            m_barTime = (float)(effectiveTime / (tp.mBeatDuration * tp.mNumerator)) % 1f;
            m_beatTime = (float)(effectiveTime / tp.mBeatDuration) % 1f;

            // Set new time
            m_playbackTime = newTime;

            // Advance timing
            TimingPoint timingEnd = GetTimingPointAt(m_playbackTime, false);
            if (timingEnd != null && timingEnd != m_currentTiming) {
                m_currentTiming = timingEnd;
                /// TODO: Investigate why this causes score to be too high
                //hittableLaserEnter = (*m_currentTiming).beatDuration * 4.0;
                //alertLaserThreshold = (*m_currentTiming).beatDuration * 6.0;
                // TODO : 기능 모르겠으니 주석 처리
                //OnTimingPointChanged(m_currentTiming);
            }

            // Advance objects
            int index = GetSelectHitObjectIndex(m_playbackTime + hittableObjectEnter, false);
            if (index != -1 && index != mCurObjIndex) {
                for (int i = mCurObjIndex; i < index; i++) {
                    ObjectDataBase obj = m_objects[i];
                    if (obj.mType != ButtonType.Laser) {
                        if (obj.mType == ButtonType.Hold || obj.mType == ButtonType.Single) {
                            m_holdObjects.Add(obj);
                        }
                        m_hittableObjects.Add(obj);
                        OnObjectEntered(obj);
                    }
                }
                mCurObjIndex = index;
            }

            // Advance lasers
            if (index != -1 && index != mCurLaserIndex) {
                for (int i = mCurLaserIndex; i < index; i++) {
                    ObjectDataBase obj = m_objects[i];
                    if (obj.mType == ButtonType.Laser) {
                        m_holdObjects.Add(obj);
                        m_hittableObjects.Add(obj);
                        OnObjectEntered(obj);
                    }
                }
                mCurLaserIndex = index;
            }

            // Check for lasers within the alert time
            index = GetSelectHitObjectIndex(m_playbackTime + alertLaserThreshold, true);
            if (index != -1 && index != mCurAlertIndex) {
                // alert 대상을 줄이기 위해서 추가. criteria, m_playbackTime + alertLaserThreshold 사이의 시간을 가지는 오브젝트만 고려대상이 된다.
                float criteria = m_playbackTime + alertLaserThreshold - 500f;
                for (int i = mCurAlertIndex; i < index; i++) {
                    ObjectDataBase obj = m_objects[i];
                    if (obj.mTime < criteria)
                        continue;

                    if (obj.mType == ButtonType.Laser) {
                        LaserData laser = (LaserData)obj;
                        if (laser.mPrev == null)
                            OnLaserAlertEntered(laser);
                    }
                }
                mCurAlertIndex = index;
            }

            // Advance zoom points
            //if (m_currentZoomPoint != null) {
            //    index = GetSelectZoomObjectIndex(m_playbackTime);
            //    for (int i = mCurZoomPointIndex; i < index; i++) {
            //        ZoomControlPoint obj = m_zoomPoints[i];
            //        // Set this point as new start point
            //        int idx = obj.index;
            //        m_zoomStartPoints[idx] = obj;

            //        // Set next point
            //        m_zoomEndPoints[idx] = null;
            //        for (int j = index + 1; j < m_zoomPoints.Count; j++) {
            //            if (m_zoomPoints[j].index == idx) {
            //                m_zoomEndPoints[idx] = m_zoomPoints[j];
            //                break;
            //            }
            //        }
            //    }
            //    m_currentZoomPoint = m_zoomPoints[index];
            //    mCurZoomPointIndex = index;
            //}

            // Check passed hittable objects
            int objectPassTime = m_playbackTime - hittableObjectLeave;
            for (int i = 0; i < m_hittableObjects.Count; i++) {
                ObjectDataBase obj = m_hittableObjects[i];
                if (obj.mType == ButtonType.Hold) {
                    int endTime = ((HoldButtonData)obj).mDuration + obj.mTime;
                    if (endTime < objectPassTime) {
                        OnObjectLeaved(obj);
                        m_hittableObjects.RemoveAt(i--);
                        continue;
                    }
                    // Hold button with effect // Hold button in active range
                    if (((HoldButtonData)obj).mEffectType != EffectType.None &&
                        obj.mTime - 100 <= m_playbackTime + audioOffset && endTime - 100 > m_playbackTime + audioOffset) {
                        if (!m_effectObjects.Contains(obj)) {
                            OnFXBegin((HoldButtonData)obj);
                            m_effectObjects.Add(obj);
                        }
                    }
                } else if (obj.mType == ButtonType.Laser) {
                    if ((((LaserData)obj).mDuration + obj.mTime) < objectPassTime) {
                        OnObjectLeaved(obj);
                        m_hittableObjects.RemoveAt(i--);
                        continue;
                    }
                } else if (obj.mType == ButtonType.Single) {
                    if (obj.mTime < objectPassTime) {
                        OnObjectLeaved(obj);
                        m_hittableObjects.RemoveAt(i--);
                        continue;
                    }
                } else if (obj.mType == ButtonType.Event) {
                    EventData evt = (EventData)obj;
                    if (obj.mTime < (m_playbackTime + 2)) { // Tiny offset to make sure events are triggered before they are needed
                                                            // Trigger event
                        OnEventChanged(evt.mKey, evt);
                        //m_eventMapping[evt.mKey] = evt;
                        m_hittableObjects.RemoveAt(i--);
                        continue;
                    }
                }
            }

            // Remove passed hold objects
            for (int i = 0; i < m_holdObjects.Count; i++) {
                ObjectDataBase obj = m_holdObjects[i];
                if (obj.mType == ButtonType.Hold) {
                    int endTime = ((HoldButtonData)obj).mDuration + obj.mTime;
                    if (endTime < objectPassTime) {
                        m_holdObjects.RemoveAt(i--);
                        continue;
                    }
                    if (endTime < m_playbackTime) {
                        if (m_effectObjects.Contains(obj)) {
                            OnFXEnd((HoldButtonData)obj);
                            m_effectObjects.Remove(obj);
                        }
                    }
                } else if (obj.mType == ButtonType.Laser) {
                    if ((((LaserData)obj).mDuration + obj.mTime) < objectPassTime) {
                        m_holdObjects.RemoveAt(i--);
                        continue;
                    }
                } else if (obj.mType == ButtonType.Single) {
                    if (obj.mTime < objectPassTime) {
                        m_holdObjects.RemoveAt(i--);
                        continue;
                    }
                }
            }
        }

        /// <summary> 범위 내의 모든 홀드 오브젝트와 일반 오브젝트를 리턴 </summary>
        List<ObjectDataBase> GetObjectsInRange(int range) {
            //int earlyVisiblity = 200;
            //TimingPoint tp = GetCurrentTimingPoint();
            //int begin = m_playbackTime - earlyVisiblity;
            int end = m_playbackTime + range;
            List<ObjectDataBase> ret = new List<ObjectDataBase>();

            // Add hold objects
            for (int i = 0; i < m_holdObjects.Count; i++) {
                ObjectDataBase obj = m_holdObjects[i];
                if (ret.Find((x) => x == obj) == null)
                    ret.Add(obj);
            }

            // Return all objects that lie after the currently queued object and fall within the given range
            for (int i = mCurObjIndex; i < m_objects.Count; i++) {
                ObjectDataBase obj = m_objects[i];
                if (obj.mTime > end)
                    break;

                if (ret.Find((x) => x == obj) == null)
                    ret.Add(obj);
            }

            return ret;
        }

        public uint CountBeats(int start, int range, ref int startIndex, uint multiplier = 1) {
            TimingPoint tp = GetCurrentTimingPoint();
            long delta = (long)start - tp.mTime;
            double beatDuration = tp.GetWholeNoteLength() / tp.mDenominator;
            long beatStart = (long)Math.Floor(delta / (beatDuration / multiplier));
            long beatEnd = (long)Math.Floor((delta + range) / (beatDuration / multiplier));
            startIndex = ((int)beatStart + 1) % tp.mNumerator;
            return (uint)Math.Max(beatEnd - beatStart, 0);
        }

        int ViewDistanceToDuration(float distance) {
            //TimingPoint tp = GetTimingPointAt(m_playbackTime, true);
            int index = GetSelectTimingPointIndex(m_playbackTime, true);

            double time = 0;
            int currentTime = m_playbackTime;
            for (int i = index; i < m_timingPoints.Count; i++) {
                TimingPoint tp = m_timingPoints[i];
                double maxDist = (tp.mTime - currentTime) / tp.mBeatDuration;
                if (maxDist < distance) {
                    // Split up
                    time += maxDist * tp.mBeatDuration;
                    distance -= (float)maxDist;
                }
            }
            time += distance * m_timingPoints[m_timingPoints.Count - 1].mBeatDuration;

            /// TODO: Optimize?
            /// 뭐하는 코든지 모르겠음. 나중에 돌릴때 중단점으로 확인 필요
            uint processedStops = 0;
            List<ChartStop> ignoreStops = new List<ChartStop>();
            do {
                processedStops = 0;
                foreach (var cs in m_SelectChartStops(currentTime, Convert.ToInt32(time))) {
                    if (ignoreStops.Find((x) => x == cs) != ignoreStops.Last())
                        continue;

                    time += cs.duration;
                    processedStops++;
                    ignoreStops.Add(cs);
                }
            } while (processedStops != 0);

            return (int)time;
        }
        public float DurationToViewDistance(int duration) {
            return DurationToViewDistanceAtTime(m_playbackTime, duration);
        }

        float DurationToViewDistanceAtTimeNoStops(int time, int duration) {
            int endTime = time + duration;
            int direction = Math.Sign(duration);
            if (duration < 0) {
                int temp = time;
                time = endTime;
                endTime = temp;
                duration *= -1;
            }

            // Accumulated value
            double barTime = 0.0f;

            // Split up to see if passing other timing points on the way
            int index = GetSelectTimingPointIndex(time, true);
            for (int i = index; i < m_timingPoints.Count; i++) {
                TimingPoint tp = m_timingPoints[i];
                if (tp.mTime < endTime) {
                    // Split up
                    int myDuration = tp.mTime - time;
                    barTime += myDuration / tp.mBeatDuration;
                    duration -= myDuration;
                    time = tp.mTime;
                }
            }
            // Whole
            barTime += duration / m_timingPoints[m_timingPoints.Count - 1].mBeatDuration;

            return (float)barTime * direction;
        }

        float DurationToViewDistanceAtTime(int time, int duration) {
            if (cMod)
                return duration / 480000.0f;

            int endTime = time + duration;
            int direction = Math.Sign(duration);
            if (duration < 0) {
                int temp = time;
                time = endTime;
                endTime = temp;
                duration *= -1;
            }

            int startTime = time;

            // Accumulated value
            double barTime = 0.0f;

            // Split up to see if passing other timing points on the way
            int index = GetSelectTimingPointIndex(time, true);
            for (int i = index; i < m_timingPoints.Count; i++) {
                TimingPoint tp = m_timingPoints[i];
                if (tp.mTime < endTime) {
                    // Split up
                    int myDuration = tp.mTime - time;
                    barTime += myDuration / tp.mBeatDuration;
                    duration -= myDuration;
                    time = tp.mTime;
                }
            }
            // Whole
            barTime += duration / m_timingPoints[m_timingPoints.Count - 1].mBeatDuration;

            // calculate stop ViewDistance
            double stopTime = 0;
            foreach (var cs in m_SelectChartStops(startTime, endTime - startTime)) {
                int overlap = Math.Min(Math.Abs((cs.time + cs.duration) - startTime), Math.Abs((cs.time + cs.duration) - cs.time));
                overlap = Math.Min(Math.Abs(endTime - cs.time), overlap);
                overlap = Math.Min(Math.Abs(endTime - startTime), overlap);

                stopTime += DurationToViewDistanceAtTimeNoStops(Math.Max(cs.time, startTime), overlap);
            }
            barTime -= stopTime;

            return (float)barTime * direction;
        }

        float TimeToViewDistance(int time) {
            if (cMod)
                return (time - m_playbackTime) / 480000f;

            return DurationToViewDistanceAtTime(m_playbackTime, time - m_playbackTime);
        }

        float GetZoom(int index) {
            // assert(index >= 0 && index <= 3);
            int startTime = m_zoomStartPoints[index] != null ? m_zoomStartPoints[index].time : 0;
            float start = m_zoomStartPoints[index] != null ? m_zoomStartPoints[index].zoom : 0.0f;
            if (m_zoomEndPoints[index] == null) // Last point?
                return start;

            // Interpolate
            int duration = m_zoomEndPoints[index].time - startTime;
            int currentOffsetInto = m_playbackTime - startTime;
            float zoomDelta = m_zoomEndPoints[index].zoom - start;
            float f = (float)currentOffsetInto / duration;
            return start + zoomDelta * f;
        }

        public int GetLastTime() {
            return m_playbackTime;
        }

        public TimingPoint GetCurrentTimingPoint() {
            if (m_currentTiming == null)
                return m_timingPoints[0];

            return m_currentTiming;
        }
        public TimingPoint GetTimingPointAt(int time, bool allowReset = false) {
            int index = GetSelectTimingPointIndex(time, allowReset);
            if (index == -1)
                return null;

            return m_timingPoints[index];
        }

        int GetSelectTimingPointIndex(int time, bool allowReset) {
            int objStart = mCurTimingIndex;
            if (objStart >= m_timingPoints.Count - 1)
                return m_timingPoints.Count - 1;

            // Start at front of array if current object lies ahead of given input time
            if (m_timingPoints[objStart].mTime > time && allowReset)
                objStart = 0;

            // Keep advancing the start pointer while the next object's starting time lies before the input time
            for (; objStart < m_timingPoints.Count; objStart++) {
                if (m_timingPoints[objStart].mTime > time)
                    break;
            }

            return Math.Min(objStart, m_timingPoints.Count - 1);
        }

        List<ChartStop> m_SelectChartStops(int time, int duration) {
            List<ChartStop> stops = new List<ChartStop>();
            for (int i = 0; i < m_chartStops.Count; i++) {
                ChartStop cs = m_chartStops[i];
                if ((time <= cs.time + cs.duration) && (time + duration >= cs.time))
                    stops.Add(cs);
            }
            return stops;
        }

        /// <summary>
        /// 해당 시간 이후에 첫번째로 오는 오브젝트의 인덱스를 리턴.
        /// 현재 시간 ~ <paramref name="time"/> 시간 사이의 오브젝트를 파악하기 위해 사용하는 함수
        /// </summary>
        /// <param name="time"></param>
        /// <param name="isAlert"> 레이저 알람 체크용으로 사용하는지 확인 </param>
        /// <returns></returns>
        int GetSelectHitObjectIndex(int time, bool isAlert) {
            int objStart = isAlert ? mCurAlertIndex : mCurObjIndex;
            if (objStart >= m_objects.Count - 1)
                return m_objects.Count - 1;

            // Keep advancing the start pointer while the next object's starting time lies before the input time
            for (; objStart < m_objects.Count; objStart++) {
                if (m_objects[objStart].mTime >= time)
                    break;
            }

            return Math.Min(objStart, m_objects.Count - 1);
        }

        ZoomControlPoint GetZoomObjectAt(int time) {
            int index = GetSelectZoomObjectIndex(time);
            if (index == -1)
                return null;

            return m_zoomPoints[index];
        }

        int GetSelectZoomObjectIndex(int time) {
            int objStart = mCurZoomPointIndex;
            if (objStart >= m_zoomPoints.Count - 1)
                return m_zoomPoints.Count - 1;

            // Keep advancing the start pointer while the next object's starting time lies before the input time
            for (; objStart < m_zoomPoints.Count; objStart++) {
                if (m_zoomPoints[objStart].time >= time)
                    break;
            }

            return Math.Min(objStart, m_zoomPoints.Count - 1);
        }
    }
}