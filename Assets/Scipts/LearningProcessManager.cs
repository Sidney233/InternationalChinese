using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProgressManager
{
    // 保存状态
    public static void SaveState(string key, UnitState state)
    {
        PlayerPrefs.SetInt(key, (int)state);
        PlayerPrefs.Save();
    }

    // 读取状态，支持默认值
    public static UnitState LoadState(string key, UnitState defaultState = UnitState.Pending)
    {
        return (UnitState)PlayerPrefs.GetInt(key, (int)defaultState);
    }

    // 删除所有进度（测试用）
    public static void ClearAllProgress()
    {
        PlayerPrefs.DeleteAll();
    }
}
