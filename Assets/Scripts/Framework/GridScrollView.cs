using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridScrollView : MonoBehaviour {
    UIPanel mScrollView;
    UIGrid mGrid;
    public UIScrollBar mScrollBar;
    
    /// <summary> 스크롤뷰가 가장 아래로 내려갔을 때 최고 인덱스 </summary>
    int mMaxScrollIndex;
    /// <summary> 전체 오브젝트의 개수 </summary>
    int mMaxObjectNum;
    /// <summary> 스크롤뷰 내에 온전하게 보이는 오브젝트의 개수 </summary>
    int mMaxViewObjectNum;
    /// <summary> 스크롤뷰가 어느 방향으로 가있는지 확인. TRUE면 아래로, FALSE이면 위로 치우쳐져 있는 형태 </summary>
    bool mViewPivot;
    /// <summary> 스크롤 아이템의 크기 </summary>
    int mObjectSize;
    /// <summary> 스크롤뷰에 가려서 보이지 않는 오브젝트 부분의 길이 == 스크롤뷰가 움직이는 최소 거리 </summary>
    int mMovePadding;
    /// <summary> 움직이는 방향의 뷰 크기 </summary>
    int mScrollSize;
    /// <summary> 스크롤뷰가 움직일 수 있는 전체 거리 </summary>
    int mMaxMoveSize;
    /// <summary> 현재 스크롤뷰가 움직인 거리 </summary>
    int mCurMoveSize;
    Vector2 mTmpPos;


    public void Init(int maxCount) {
        mMaxObjectNum = maxCount;
        mScrollView = transform.parent.GetComponent<UIPanel>();
        mGrid = GetComponent<UIGrid>();
        int lineNum = mGrid.maxPerLine;

        if (mGrid.arrangement == UIGrid.Arrangement.Horizontal) {
            mObjectSize = (int)mGrid.cellHeight;
            mScrollSize = (int)mScrollView.GetViewSize().y;
        } else if (mGrid.arrangement == UIGrid.Arrangement.Vertical) {
            mObjectSize = (int)mGrid.cellWidth;
            mScrollSize = (int)mScrollView.GetViewSize().x;
        } else {

        }

        mMaxViewObjectNum = lineNum * (mScrollSize / mObjectSize);
        mMaxScrollIndex = lineNum * (maxCount / lineNum + 1);

        mMovePadding = (mMaxViewObjectNum / lineNum + 1) * mObjectSize - mScrollSize;
        mMaxMoveSize = (mMaxScrollIndex / lineNum) * mObjectSize - mScrollSize;

        mCurMoveSize = 0;
        mViewPivot = false;
        mTmpPos = Vector2.zero;
    }

    public void MoveForward(int index) {
        if (index == 0 || index < mMaxViewObjectNum) {
            mCurMoveSize = 0;
            mViewPivot = false;
        } else if (mCurMoveSize < mMaxMoveSize && index % mGrid.maxPerLine == 0) {
            // 뷰포인트가 위로 가있으면 mMovePadding 만큼 이동한다. 아래로 가있으면 정상적으로 이동
            mCurMoveSize += mViewPivot ? mObjectSize : mMovePadding;
            mViewPivot = true;
        }

        // 스크롤뷰, 스크롤바 움직이기
        MoveScroll();
    }

    public void MoveBackward(int index) {
        if (index == mMaxObjectNum - 1 || index > (mMaxScrollIndex - mMaxViewObjectNum - 1)) {
            mCurMoveSize = mMaxMoveSize;
            mViewPivot = true;
        } else if (mCurMoveSize > 0 && index % mGrid.maxPerLine == (mGrid.maxPerLine - 1)) {
            // 뷰포인트가 아래로 가있으면 mMovePadding 만큼 이동한다. 위로 가있으면 정상적으로 이동
            mCurMoveSize -= mViewPivot ? mMovePadding : mObjectSize;
            mViewPivot = false;
        }

        // 스크롤뷰, 스크롤바 움직이기
        MoveScroll();
    }
    
    void MoveScroll() {
        if (mScrollBar != null)
            mScrollBar.value = (float)mCurMoveSize / mMaxMoveSize;

        if (mGrid.arrangement == UIGrid.Arrangement.Horizontal) {
            mTmpPos.y = -mCurMoveSize;
            mScrollView.clipOffset = mTmpPos;
            mTmpPos.y = mCurMoveSize;
            mScrollView.transform.localPosition = mTmpPos;
        } else if (mGrid.arrangement == UIGrid.Arrangement.Vertical) {
            mTmpPos.x = -mCurMoveSize;
            mScrollView.clipOffset = mTmpPos;
            mTmpPos.x = mCurMoveSize;
            mScrollView.transform.localPosition = mTmpPos;
        }
        //mScrollView.GetComponent<UIScrollView>().OnScrollBar();
    }
}
