using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;

public class UnitPageController : MonoBehaviour
{
    // 单元卡引用
    public UnitCardController unit1Card;
    public UnitCardController unit2Card;
    public UnitCardController unit3Card;
    public UnitCardController unit4Card;


    private void Start() 
    {   
        LoadProgress();  // 加载之前保存的进度
    }

    // 加载单元的学习进度
    void LoadProgress()
    {
        // 检查第一个单元的状态
        if (!PlayerPrefs.HasKey("unit1State"))
        {
            // 第一次进入时，默认第一个单元的状态为学习中
            UpdateUnitState(unit1Card, 1, UnitState.InProgress);
            UpdateUnitState(unit2Card, 2, UnitState.Pending);     // 第二个单元待学习
            UpdateUnitState(unit3Card, 3, UnitState.Pending);     // 第三个单元待学习
            UpdateUnitState(unit4Card, 4, UnitState.Pending);     // 第四个单元待学习
        }
        else
        {
            // 如果已经有进度，加载并显示已完成的单元
            UpdateUnitState(unit1Card, 1);  // 加载第一个单元的状态
            UpdateUnitState(unit2Card, 2);  // 加载第二个单元的状态
            UpdateUnitState(unit3Card, 3);  // 加载第三个单元的状态
            UpdateUnitState(unit4Card, 4);  // 加载第四个单元的状态
        }
    }

    // 更新每个单元卡的状态
    void UpdateUnitState(UnitCardController card, int unitNumber, UnitState? defaultState = null)
    {
        // 获取保存的单元状态（如果没有，则使用默认状态）
        UnitState state = (UnitState)PlayerPrefs.GetInt("unit" + unitNumber + "State", (int)(defaultState ?? UnitState.Pending));

        // 更新单元卡状态
        card.UpdateState(state);
        // Debug.Log("当前card:"+card+"card状态:"+state);
    }

    // 更新学习进度
    public void UpdateUnitState(int unitNumber, UnitState state)
    {
        // 根据单元编号更新状态
        switch (unitNumber)
        {
            case 1:
                unit1Card.UpdateState(state);
                break;
            case 2:
                unit2Card.UpdateState(state);
                break;
            case 3:
                unit3Card.UpdateState(state);
                break;
            case 4:
                unit4Card.UpdateState(state);
                break;
        }

        // 保存状态            
        PlayerPrefs.SetInt("unit" + unitNumber + "State", (int)state);
        PlayerPrefs.Save();
        Debug.Log("Unit " + unitNumber + " state saved: " + state);
    }

    public void OnClearClick()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void CheckAndCompleteChapter()
    {
        if (AllUnitsCompleted())
        {
            int currentChapter = PlayerPrefs.GetInt("currentChapter", 1);
            PlayerPrefs.SetInt($"chapter{currentChapter}State", (int)UnitState.Completed);
            PlayerPrefs.SetInt($"chapter{currentChapter + 1}State", (int)UnitState.InProgress);
            PlayerPrefs.Save();
            Debug.Log($"Chapter {currentChapter} completed!");
        }
    }

    private bool AllUnitsCompleted()
    {
        for (int i = 1; i <= 4; i++) // 假设每章有 4 个单元
        {
            if (PlayerPrefs.GetInt($"unit{i}State", (int)UnitState.Pending) != (int)UnitState.Completed)
            {
                return false;
            }
        }
        return true;
    }
}
