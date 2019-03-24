using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiManager : Singleton<GuiManager> { //manager를 singleton으로 설정
    public enum CurPanel { //현재 패널 상태를 확인
        Main,
        Select,
        Option,
        Ingame,
        Result
    }

    private GameObject mPanelMain; //패널들을 확인하기 위한 변수선언
    private GameObject mPanelSelect;
    private GameObject mPanelOption;
    private GameObject mPanelIngame;
    private GameObject mPanelResult;
    
    public void Open() {
        mPanelMain = GameObject.Find("panel_main"); //각 변수에 패널을 할당
        mPanelSelect = GameObject.Find("panel_select");
        mPanelOption = GameObject.Find("panel_option");
        mPanelIngame = GameObject.Find("panel_ingame");
        mPanelResult = GameObject.Find("panel_result");
    }
    
    public CurPanel GetCurrentPanel() { //현재 어떤 패널인지 확인 후 해당 패널에 해당하는 값을 리턴
        if (mPanelMain.activeInHierarchy)
            return CurPanel.Main;
        else if (mPanelSelect.activeInHierarchy)
            return CurPanel.Select;
        else if (mPanelOption.activeInHierarchy)
            return CurPanel.Option;
        else if (mPanelIngame.activeInHierarchy)
            return CurPanel.Ingame;
        else if (mPanelResult.activeInHierarchy)
            return CurPanel.Result;

        return CurPanel.Main;
    }
}
