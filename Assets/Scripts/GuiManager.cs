using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiManager : Singleton<GuiManager> {
    public enum CurPanel {
        Main,
        Select,
        Option,
        Ingame,
        Result
    }

    private GameObject mPanelMain;
    private GameObject mPanelSelect;
    private GameObject mPanelOption;
    private GameObject mPanelIngame;
    private GameObject mPanelResult;
    
    public void Open() {
        mPanelMain = GameObject.Find("panel_main");
        mPanelSelect = GameObject.Find("panel_select");
        mPanelOption = GameObject.Find("panel_option");
        mPanelIngame = GameObject.Find("panel_ingame");
        mPanelResult = GameObject.Find("panel_result");
    }
    

    public CurPanel GetCurrentPanel() {
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
