using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;

public class chapterCardController : MonoBehaviour
{
    public Button last;
    public Button next;

    // 存储所有章节的容器
    public GameObject[] chapters = new GameObject[3];

    // 当前显示的章节索引
    private int currentChapterIndex = 0;

    // 单元进入锁定状态（与 Unit 页面交互）
    public bool isUnitLocked = true;


    // 初始化按钮点击事件
   public void Start()
    {

        if (last != null)
        {
            last.onClick.AddListener(OnLastClick);
        }
        if (next != null)
        {
            next.onClick.AddListener(OnNextClick);
            // Debug.Log("Next button listener added.");
        }

        // 初始化章节状态（只在首次启动时设置）
        InitializeChapterProgress();

        UpdateChapterDisplay();
    }

    private void InitializeChapterProgress()
    {
        if (!PlayerPrefs.HasKey("chapter1State"))
        {
            PlayerPrefs.SetInt("chapter1State", (int)UnitState.InProgress); // 第一个章节学习中
            for (int i = 2; i <= chapters.Length; i++)
            {
                PlayerPrefs.SetInt($"chapter{i}State", (int)UnitState.Pending); // 其他章节待学习
            }
            PlayerPrefs.Save();
        }
    }

   public void OnLastClick()
    {
        if (currentChapterIndex > 0)
        {
            currentChapterIndex--;
            UpdateChapterDisplay();
        }
    }

    public void OnNextClick()
    {
        if (currentChapterIndex < chapters.Length - 1)
        {
            currentChapterIndex++;
            UpdateChapterDisplay();
        }
    }

    private void UpdateChapterDisplay()
    {
        // 确保只有当前章节显示，其余隐藏
        for (int i = 0; i < chapters.Length; i++)
        {
            chapters[i].SetActive(i == currentChapterIndex);
        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
        // 更新是否允许访问单元的状态
        int currentChapterState = PlayerPrefs.GetInt($"chapter{currentChapterIndex + 1}State", (int)UnitState.Pending);
        isUnitLocked = currentChapterState != (int)UnitState.InProgress;

        // 更新按钮状态
        last.gameObject.SetActive(currentChapterIndex > 0);
        next.gameObject.SetActive(currentChapterIndex < chapters.Length - 1);
    }

    public void OnClearClick()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void OnAmigoClick()
    {
        SceneManager.LoadScene("ChatBot");
    }
}