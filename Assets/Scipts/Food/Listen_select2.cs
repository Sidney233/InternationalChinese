using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DataStructure;
using LitJson;
using UnityEngine.SceneManagement; 

public class Listen_select2 : MonoBehaviour
{
    //读取文档
    private MultipleChoice[] questions;
    private int topicMax = 0;//最大题数
    private List<bool> isAnserList = new List<bool>();//存放是否答过题的状态

    //加载题目
    public Slider slider;//进度条
    public TextMeshProUGUI indexText;//当前题号
    public TextMeshProUGUI allIndex;//总题数
    public Button skipBtn;//跳过按钮
    public Button favoriteBtn;//收藏按钮
    public GameObject scorePanel1;
    public GameObject scorePanel2;
    public List<Button> toggleList;//答题Toggle
    // public TextMeshProUGUI TM_Text;//当前题目
    private int topicIndex = 0;//第几题
    public Button Submit;//提交答案
    // public TextMeshProUGUI resultText;//提交后的提示词
    private char currentChioce='0';//当前选择选项
    private int currentUnitNumber = 1;

    public Button playButton;  // 播放按钮
    public AudioSource audioSource;
    public AudioClip audioClip;

    void Awake()
    {
        ReadJson();
        LoadAnswer();
    }

    void Start()
    {
        toggleList[0].onClick.AddListener(() => onSelet('A'));
        toggleList[1].onClick.AddListener(() => onSelet('B'));
        toggleList[2].onClick.AddListener(() => onSelet('C'));
        toggleList[3].onClick.AddListener(() => onSelet('D'));

        Submit.onClick.AddListener(() => onSubmit());

        // audioClip = Resources.Load<AudioClip>("Audio/ke");
        playButton.onClick.AddListener(() => PlayAudio(audioClip));
    }

    void Update()
    {
        // 监听返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }

    // 读取json文件
    void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("listenQuestionsFood");
        string jsonFileContent = jsonTextAsset.text;
        questions = JsonMapper.ToObject<MultipleChoice[]>(jsonFileContent);
        //设置题目状态
        topicMax = questions.Length;
        allIndex.text = questions.Length.ToString();
        for (int x = 0; x < topicMax + 1; x++)
        {
            isAnserList.Add(false);
        }
    }

    /// <summary>
    /// 加载题目
    /// </summary>
    void LoadAnswer()
    {
        // TM_Text.text = (topicIndex + 1) + "、" + questions[topicIndex].question;//题目
        // TM_Text.text = questions[topicIndex].question;
        indexText.text = (topicIndex + 1).ToString();
        slider.value = (topicIndex + 1) / (float)questions.Length;
        Debug.Log(indexText.text);
        toggleList[0].GetComponentInChildren<TextMeshProUGUI>().text = questions[topicIndex].option.A;
        toggleList[1].GetComponentInChildren<TextMeshProUGUI>().text = questions[topicIndex].option.B;
        toggleList[2].GetComponentInChildren<TextMeshProUGUI>().text = questions[topicIndex].option.C;
        toggleList[3].GetComponentInChildren<TextMeshProUGUI>().text = questions[topicIndex].option.D;
    }

    void onSelet(char index)
    {
        currentChioce = index;
    }

    void onSubmit()
    {
        if (currentChioce >= 'A' && currentChioce <= 'D')
        {
            //bool isRight;
            char rightAnswer = questions[topicIndex].answer;

            if (rightAnswer == currentChioce)
            {
                // resultText.text = "<color=#00BA25FF>" + "恭喜你，答对了！" + "</color>";
                scorePanel1.SetActive(true);
                //isRight = true;
                //if (topicIndex != (topicMax - 1))
                //{
                //    topicIndex++;
                //    LoadAnswer();
                //} else
                //{
                //    // resultText.text = "<color=#00BA25FF>" + "恭喜你，完成所有题目！" + "</color>";
                    
                //}
            }
            else
            {
                // resultText.text = "<color=#FF0020FF>" + "对不起，答错了！" + "</color>";
                scorePanel2.SetActive(true);
                //isRight = false;
                //LoadAnswer();
            }
        }
    }

    public void OnNextClick()
    {
        if (topicIndex == questions.Length - 1)
        {
            OnCompleteLearningButtonClicked();
        }
        else
        {
            scorePanel1.SetActive(false);
            scorePanel2.SetActive(false);
            topicIndex = topicIndex + 1;
            LoadAnswer();
        }
    }

    public void OnCompleteLearningButtonClicked()
    {
        PlayerPrefs.SetInt("unit2lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("unit2lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("Unit2Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Unit2Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit Food");
    }

    void PlayAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Listen Food");
    }
}
