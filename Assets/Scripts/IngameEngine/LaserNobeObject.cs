using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class LaserNobeObject : MonoBehaviour {
        const float TARGET_DIAPPEAR_TIME = 0.3f;
        const float TARGET_DIAPPEAR_DELAY = 1f;
        TweenAlpha mAlpha;
        float mRemainTime;      // 오브젝트가 사라지는데까지 걸리는 시간
        Vector3 mLoc;           // 오브젝트 위치
        float mPrevLocation;    // 오브젝트의 이전 위치. 0 ~ 1 사이값
        bool mIsTweening;       // 사라지고 있는지 확인
        bool mIsHitTarget;      // 레이저 타겟을 맞추고 있는지 확인

        void Start() {
            mRemainTime = 0;
            mAlpha = GetComponent<TweenAlpha>();
            mAlpha.duration = TARGET_DIAPPEAR_TIME;
            mLoc = transform.localPosition;
            mPrevLocation = .5f;
            mIsTweening = true;
        }
        
        void Update() {
            if (!mIsTweening && !mIsHitTarget)
                mRemainTime += Time.deltaTime;

            if (mRemainTime > TARGET_DIAPPEAR_DELAY) {
                Disappear();
                mRemainTime = 0;
            }
        }

        public void Move(float location, bool bHitTarget) {
            mIsHitTarget = bHitTarget;
            if (mPrevLocation == location)
                return;

            if (mIsTweening) {
                mAlpha.enabled = false;
                mAlpha.ResetToBeginning();
                mIsTweening = false;
            }
            mLoc.x = -450f + 900f * location;
            transform.localPosition = mLoc;
            mPrevLocation = location;
        }

        void Disappear() {
            mIsTweening = true;
            mAlpha.PlayForward();
        }
    }
}
