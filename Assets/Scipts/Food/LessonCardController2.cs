using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;

public class LessonCardController2 : MonoBehaviour
{
    public Image state;
    public Image decorate;
    public Button unitButton;
    public Text errorMessage;  // 用来显示错误信息的 Text 组件，确保已经在 UI 中设置

    [Header("State Icons")]
    public Sprite iconPending;      // 待学习状态图标
    public Sprite iconInProgress;   // 学习中状态图标
    public Sprite iconCompleted;    // 已完成状态图标

    [Header("Decoration Sprites (Optional)")]
    public Sprite decorationPending;      // 待学习装饰
    public Sprite decorationInProgress;   // 学习中装饰
    public Sprite decorationCompleted;    // 已完成装饰
    public GameObject loadingPanel;


     // 当前单元编号
    public int unitNumber;

    // 初始化按钮点击事件
    private void Start()
    {
        if (unitButton != null)
        {
            unitButton.onClick.AddListener(OnUnitCardClick);
        }
        // 初始时隐藏错误信息
        if (errorMessage != null)
        {
            errorMessage.gameObject.SetActive(false);  // 确保初始时按钮提示文本不可见
        }

    }

    // 更新单元状态的方法
    public void UpdateState(UnitState learning_state)
    {   
        switch (learning_state)
        {
            case UnitState.Pending:
                state.sprite = iconPending;
                if (decorate != null)
                    decorate.sprite = decorationPending;
                break;

            case UnitState.InProgress:
                state.sprite = iconInProgress;
                if (decorate != null)
                    decorate.sprite = decorationInProgress;
                break;

            case UnitState.Completed:
                state.sprite = iconCompleted;
                if (decorate != null)
                    decorate.sprite = decorationCompleted;
                break;
        }

    }

    private void OnUnitCardClick()
    {
        // 如果前一个单元未完成，直接返回
        Debug.Log("当前lesson："+unitNumber);
        UnitState previousUnitState = (UnitState)PlayerPrefs.GetInt("unit2lesson" + (unitNumber - 1) + "State", (int)UnitState.Pending);
        Debug.Log("前一个lesson状态："+previousUnitState);
        if (unitNumber > 1 && previousUnitState != UnitState.Completed)
        {
            if (errorMessage != null)
            {
                errorMessage.gameObject.SetActive(true);  // 显示提示文本
                errorMessage.text = "请完成前一个lesson";  // 提示用户完成前一个单元
            }
            // 启动一个 Coroutine 延迟 2 秒后隐藏错误信息
            StartCoroutine(HideErrorMessageAfterDelay(2f));  // 2 秒后隐藏错误信息
            return; // 如果前一个单元没有完成，跳出
        }
        string sceneName = GetSceneNameForUnit(unitNumber);
        StartCoroutine(LoadWithLoadingPanel(sceneName));  // 跳转到对应的学习页面
    }

    // Coroutine 来延迟 2 秒隐藏错误信息
    private IEnumerator HideErrorMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);  // 等待指定的时间
        if (errorMessage != null)
        {
            errorMessage.gameObject.SetActive(false);  // 隐藏错误提示
        }
    }

    // 获取对应的场景名称
    private string GetSceneNameForUnit(int unitNumber)
    {
        switch (unitNumber)
        {
            case 1:
                return "Listen Food";
            case 2:
                return "Speak Food";
            case 3:
                return "Read Food";
            case 4:
                return "Write Food";
            default:
                return "Listen Food";
        }
    }
    IEnumerator LoadWithLoadingPanel(string sceneName)
    {
        loadingPanel.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        yield return null;
    }
}
