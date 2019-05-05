using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMax {
    public class PanelBase : MonoBehaviour {

        /// <summary> X축 마우스 움직임을 체크하는 함수 </summary>
        /// <param name="positiveDirection"> 양의 방향이면 true, 음의 방향이면 false </param>
        public virtual void CursorXMoveProcess(bool positiveDirection) {

        }

        /// <summary> Y축 마우스 움직임을 체크하는 함수 </summary>
        /// <param name="positiveDirection"> 양의 방향이면 true, 음의 방향이면 false </param>
        public virtual void CursorYMoveProcess(bool positiveDirection) {

        }

        /// <summary> 스타트 버튼을 눌렀을 때 각 패널에서 해야 할 일 </summary>
        public virtual void OnClickBtnStart() {

        }

        /// <summary> 일반 버튼을 눌렀을 때 각 패널에서 해야 할 일 </summary>
        public virtual void OnClickBtnNormal() {

        }

        /// <summary> FX 버튼을 눌렀을 때 각 패널에서 해야 할 일 </summary>
        public virtual void OnClickBtnFX() {

        }
    }
}