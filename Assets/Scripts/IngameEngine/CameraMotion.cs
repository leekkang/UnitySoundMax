using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace SoundMax {
    /// <summary>
    /// 카메라 트랜스폼을 변경하는 기능들을 포함하는 클래스
    /// 시간단위는 ms, 각도 단위는 0.1도 이다.
    /// </summary>
    public class CameraMotion : MonoBehaviour {
        const int LASER_TILT_DEGREE = 1000;  // 10.0도, 단위 0.01도
        const int VELOCITY_DEGREE_PER_MS = 200;  // 20ms당 한번에 움직일 수 있는 최대 각도, 단위 0.01도

        Vector3 mOriginPos;             // 트랙이 눕혀지고 난 뒤에 갱신된다.
        Quaternion mOriginAngle;

        List<SpinBlock> mListCurSpins = new List<SpinBlock>();
        Vector3 mMovePos = new Vector3();

        float mTiltIntensity;                // LASER_TILT_DEGREE 의 배율. TrackRollBehaviour 옵션에 의해 조절된다.
        int mLastDegreeByLaser;              // 이전 프레임에 레이저에 의해 변경된 각도
        float[] mLastLaserValue = new float[2];

        float mShakeDuration;
        float mShakeAmount;
        bool mIsShaking;

        // 각도에 따른 위치값을 미리 저장해놓고 불러다 쓰자. 동적 계산은 비용이 비싸다.
        // 0.1도 단위로 90도까지 저장
        int mRadius;
        Dictionary<int, double> mDicSinVal = new Dictionary<int, double>();
        Dictionary<int, double> mDicCosVal = new Dictionary<int, double>();

        public void Start() {
            //mRadius = (int)transform.Find("CameraRotationCenter").localPosition.y;
            mOriginAngle = transform.localRotation;
            mTiltIntensity = 1f;

            for (int i = 0; i <= 900; i++) {
                double angle = Math.PI * i / 1800.0f;
                double sinx = Math.Sin(angle);
                mDicSinVal.Add(i, sinx);
                mDicCosVal.Add(900 - i, sinx);
            }
        }

        public void ResetVal() {
            transform.localPosition = mOriginPos;
            transform.localRotation = mOriginAngle;
            mTiltIntensity = 1f;
            mListCurSpins.Clear();
        }

        public void SetOriginPos() {
            mOriginPos = transform.localPosition;
        }

        public void SetTiltIntensity(TrackRollBehaviour type) {
            // 옵션에 keep이 있는데, 이건 계속 해당 상태로 눕혀달라는 의미이다. 현재 기본 구현이 지속이기 때문에 별 필요 없을듯
            if ((type & TrackRollBehaviour.Bigger) != 0) {
                mTiltIntensity = 3f;
            } else if ((type & TrackRollBehaviour.Biggest) != 0) {
                mTiltIntensity = 4f;
            } else if ((type & TrackRollBehaviour.Normal) != 0) {
                mTiltIntensity = 1f;
            }
        }

        public void Tick(float delta) {
            int degree = 0; // degree 단위는 0.1도, -1800 ~ 1800 사이

            degree += CalculateDegreeByLaser(delta);

            // 적용된 모든 옵션을 더하여 최종 각도 계산
            for (int i = 0; i < mListCurSpins.Count; i++) {
                degree += (int)mListCurSpins[i].GetCurValue(delta);
                if (mListCurSpins[i].mIsEnd) {
                    mListCurSpins.RemoveAt(i--);
                }
            }

            // 계산된 각도를 카메라에 적용
            transform.localRotation = Quaternion.Euler(-87f, 0f, degree * .1f);
            //MoveCameraByDegree(NormalizeDegree(degree));

            // 카메라 쉐이크 적용
            if (mShakeDuration > 0) {
                mShakeDuration -= delta * 1000f;
                Vector3 rand = UnityEngine.Random.insideUnitSphere;
                rand.y = 0f; rand.z = 0f;
                transform.localPosition = mOriginPos + rand * mShakeAmount;
            }
        }

        /// <summary> degree를 사용가능한 단위로 만들어준다. </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        int NormalizeDegree(int degree) {
            while (degree > 1800 || degree <= -1800) {
                if (degree > 1800)
                    degree -= 3600;
                else
                    degree += 3600;
            }
            return degree;
        }

        public void AddCameraSpin(LaserData laser) {
            Debug.Log("AddCameraSpin : " + laser.mSpin.mType);
            SpinBlock block = null;
            // TODO : 뭔지알아보고 구현할것
            if (laser.mSpin.mType == SpinType.Bounce) {
            } else if (laser.mSpin.mType == SpinType.Quarter) {
            } else {
                // 현재 회전하고 있으면 추가안함.
                if (mListCurSpins.Exists((x) => x.mStartTime - laser.mTime < 100f))
                    return;
                block = new SpinBlock(laser.GetDirection() > 0 ? -3600f : 3600f, laser.mTime, 
                                        laser.mSpin.mDuration * 9f, EasingFunction.Ease.EaseOutBack);
                mListCurSpins.Add(block);
            }
        }

        /// <summary> 카메라 쉐이크 효과를 추가하는 함수 </summary>
        public void AddCameraShake(float duration, float amount = 1f) {
            if (mShakeDuration < duration)
                mShakeDuration = duration;
            mShakeAmount = amount;
        }

        /// <summary> 레이저에 의해 변경되는 각도를 계산 </summary>
        int CalculateDegreeByLaser(float deltaTime) {
            int maxTiltDegree = (int)(LASER_TILT_DEGREE * mTiltIntensity);
            // 기본 레이저의 위치를 기반으로 각도 생성. 각도는 10을 곱해서 인트로 계산한다.
            int cur_laser_degree = (int)(maxTiltDegree * (Scoring.inst.laserTargetPositions[0] + Scoring.inst.laserTargetPositions[1] - 1f));

            // 멀어질때 빨라지고 되돌아올때 느려지게
            float moveLimit = VELOCITY_DEGREE_PER_MS * 50 * deltaTime;
            if (cur_laser_degree == 0) {
                moveLimit *= .6f;
            } else if (Math.Sign(cur_laser_degree) != Math.Sign(mLastDegreeByLaser)) {
                if (mLastDegreeByLaser == 0)
                    moveLimit *= 2f;
                else if (Mathf.Abs(cur_laser_degree - mLastDegreeByLaser) > maxTiltDegree * 1.5f)
                    moveLimit *= .3f;
            }

            mLastLaserValue[0] = Scoring.inst.laserTargetPositions[0];
            mLastLaserValue[1] = Scoring.inst.laserTargetPositions[1];

            // 이전 각도가 현재 목표 각도와 다를 경우 이전에 레이저에 의해 변경된 각도에 현재 각도를 추가로 더함
            if (cur_laser_degree != mLastDegreeByLaser) {
                if (cur_laser_degree > 0) {
                    mLastDegreeByLaser = Math.Min(mLastDegreeByLaser + (int)Mathf.Round(moveLimit), cur_laser_degree);
                } else {
                    mLastDegreeByLaser = Math.Max(mLastDegreeByLaser - (int)Mathf.Round(moveLimit), cur_laser_degree);
                }
            }

            return (int)Mathf.Round(mLastDegreeByLaser * .1f);
        }

        /// <summary>
        /// 각도만큼 카메라를 회전
        /// 3-4분면, 4-4분면 사이가 기준점임
        /// </summary>
        /// <param name="degree"> 
        /// degree의 단위는 0.1도이다.
        /// 범위는 -1800 ~ 1800 까지
        /// </param>
        [Obsolete("반지름을 기준으로 카메라를 돌리는 기능. 쓰지않음...")]
        void MoveCameraByDegree(int degree) {
            int abs = Math.Abs(degree);
            double deltaX = mDicSinVal[abs > 900 ? 1800 - abs : abs] * mRadius;
            double deltaY = mDicCosVal[abs > 900 ? 1800 - abs : abs] * mRadius;
            if (degree < -900) {        // 2-4분면
                deltaX = -deltaX;
                deltaY = mRadius + deltaY;
            } else if (degree < 0) {    // 3-4분면
                deltaX = -deltaX;
                deltaY = mRadius - deltaY;
            } else if (degree < 900) {  // 4-4분면
                deltaY = mRadius - deltaY;
            } else {                    // 1-4분면
                deltaY = mRadius + deltaY;
            }
            mMovePos.Set((float)deltaX, (float)deltaY, 0f);
            transform.localPosition = mOriginPos + mMovePos;
            transform.localRotation = Quaternion.Euler(0f, 0f, degree * .1f);
        }
    }

    /// <summary>
    /// 스핀에 의한 각도를 리턴. 내부 계산시 단위는 ms
    /// </summary>
    class SpinBlock {
        public float mAngle;
        public float mStartTime;
        public float mDuration;
        public bool mIsEnd;

        float mProgressedTime;
        EasingFunction.Function mEaseFunction;

        public SpinBlock(float angle, float start, float duration, EasingFunction.Ease easeType) {
            mAngle = angle;
            mStartTime = 
            mDuration = duration;

            mEaseFunction = EasingFunction.GetEasingFunction(easeType);
            mProgressedTime = 0;
            mIsEnd = false;
        }

        public float GetCurValue(float deltaTime) {
            mProgressedTime += deltaTime * 1000f;
            if (mProgressedTime > mDuration) {
                mIsEnd = true;
                return mAngle;
            }

            return mEaseFunction(0f, mAngle, mProgressedTime / mDuration);
        }
    }
}