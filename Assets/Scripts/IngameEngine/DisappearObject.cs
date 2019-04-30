using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class DisappearObject : MonoBehaviour {
        bool mIsOpen = false;
        float mDisappearDelay;  // 오브젝트가 사라지는데까지 걸리는 시간
        TweenAlpha mAlpha;
        float mRemainTime;      // 오브젝트가 보이고 나서 지난 시간
        Vector3 mLoc;           // 오브젝트 위치
        float mPrevLocation;    // 오브젝트의 이전 위치. 0 ~ 1 사이값
        bool mIsTweening;       // 사라지고 있는지 확인
        bool mIsHitTarget;      // 레이저 트래커일 경우 타겟을 맞추고 있는지 확인

        public void Open(float delay, float time) {
            mDisappearDelay = delay;
            mRemainTime = 0;
            mAlpha = GetComponent<TweenAlpha>();
            mAlpha.duration = time;
            mLoc = transform.localPosition;
            mPrevLocation = .5f;
            mIsTweening = true;
            mIsOpen = true;
        }
        
        void Update() {
            if (!mIsOpen)
                return;

            if (!mIsTweening && !mIsHitTarget)
                mRemainTime += Time.deltaTime;

            if (mRemainTime > mDisappearDelay) {
                Disappear();
                mRemainTime = 0;
            }
        }

        /// <summary>
        /// 움직이지 않는 오브젝트의 트윈을 리셋해줄 때 사용
        /// </summary>
        public void ResetTime() {
            if (mIsTweening) {
                mAlpha.enabled = false;
                mAlpha.ResetToBeginning();
                mIsTweening = false;
            }
        }

        /// <summary>
        /// 레이저 트래커 혹은 레이저 판정 처럼 움직이는 오브젝트일 경우 사용. 움직이지 않는다면 <see cref="ResetTime()"/>을 사용해야 한다.
        /// </summary>
        /// <param name="location"> 움직일 위치. 0 ~ 1의 값을 갖는다. </param>
        /// <param name="bHitTarget"> 레이저 트래커만 사용. 레이저를 맞추고 있을 때 자동으로 사라지지 않게 하는 옵션 </param>
        public void Move(float location, bool bHitTarget = false, bool bForceTween = false) {
            mIsHitTarget = bHitTarget;
            if (!bForceTween && mPrevLocation == location)
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
