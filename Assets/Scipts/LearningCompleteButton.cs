using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LearningCompleteButton : MonoBehaviour
{
    public int unitNumber;  // 当前学习的单元编号
    public Button completeButton;  // 完成学习按钮
    public LearningPageController learningPageController;  // 学习首页的控制器

    private void Start()
    {
        // 绑定按钮点击事件
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    // 点击按钮后执行的操作
    private void OnCompleteButtonClicked()
    {
        // 保存当前单元学习进度
        SaveLearningProgress();

        // 跳转回学习首页
        SceneManager.LoadScene("LearningPage");  // "LearningPage" 是学习首页的场景名

        // 更新学习首页上的单元状态
        learningPageController.UpdateUnitState(unitNumber, UnitState.Completed);

        // 如果有下一个单元，则将下一个单元的状态设为“正在学习”
        learningPageController.UpdateUnitState(unitNumber + 1, UnitState.InProgress);
    }

    // 保存学习进度
    private void SaveLearningProgress()
    {
        PlayerPrefs.SetInt("unit" + unitNumber + "State", (int)UnitState.Completed);
        PlayerPrefs.Save();  // 保存到 PlayerPrefs
    }
}
