using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;

public class LearningPageController2 : MonoBehaviour
{
    // 单元卡引用
    public LessonCardController2 unit1Card;
    public LessonCardController2 unit2Card;
    public LessonCardController2 unit3Card;
    public LessonCardController2 unit4Card;


    private void Start() 
    {   
        LoadProgress();  // 加载之前保存的进度
    }

    private void Update()
    {
        // 监听返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }

    // 加载单元的学习进度
    void LoadProgress()
    {
        // 检查第一个单元的状态
        if (!PlayerPrefs.HasKey("unit2lesson1State"))
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
    void UpdateUnitState(LessonCardController2 card, int unitNumber, UnitState? defaultState = null)
    {
        // 获取保存的单元状态（如果没有，则使用默认状态）
        UnitState state = (UnitState)PlayerPrefs.GetInt("unit2lesson" + unitNumber + "State", (int)(defaultState ?? UnitState.Pending));

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
        PlayerPrefs.SetInt("unit2lesson" + unitNumber + "State", (int)state);
        PlayerPrefs.Save();
        Debug.Log("Unit2 Lesson " + unitNumber + " state saved: " + state);
    }

    public void OnClearClick()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void OnBackClick()
    {
        SceneManager.LoadScene("Home Travel");
    }
}
