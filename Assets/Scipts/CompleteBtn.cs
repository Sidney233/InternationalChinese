using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class CompleteBtn : MonoBehaviour
{
    public int currentUnitNumber;  // 当前学习单元编号
    public LearningPageController learningPageController;  // 引用单元主页控制器

    // 用户点击“完成学习”按钮时调用
    public void OnCompleteLearningButtonClicked()
    {
        // 更新当前单元的状态为已完成
        learningPageController.UpdateUnitState(currentUnitNumber, UnitState.Completed);

        // 如果有下一个单元，更新下一个单元的状态为学习中
        if (currentUnitNumber < 4)
        {
            learningPageController.UpdateUnitState(currentUnitNumber + 1, UnitState.InProgress);
        }

        // 返回单元主页
        SceneManager.LoadScene("Unit"); 
    }
}
