using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DataStructure;
using LitJson;
using UnityEngine.SceneManagement; 

public class Selection1 : MonoBehaviour
{
    //��ȡ�ĵ�
    private MultipleChoice[] questions;
    private int topicMax = 0;//�������
    private List<bool> isAnserList = new List<bool>();//����Ƿ������״̬

    //������Ŀ
    public Slider slider;//������
    public TextMeshProUGUI indexText;//��ǰ���
    public TextMeshProUGUI allIndex;//������
    public Button skipBtn;//������ť
    public Button favoriteBtn;//�ղذ�ť
    public GameObject scorePanel1;
    public GameObject scorePanel2;
    public List<Button> toggleList;//����Toggle
    public TextMeshProUGUI TM_Text;//��ǰ��Ŀ
    private int topicIndex = 0;//�ڼ���
    public Button Submit;//�ύ��
    public TextMeshProUGUI resultText;//�ύ�����ʾ��
    public string jsonName;
    private char currentChioce='0';//��ǰѡ��ѡ��
    private int currentUnitNumber = 3;

    void Awake()
    {
        ReadJson();
        LoadAnswer();
    }

    // Start is called before the first frame update
    void Start()
    {
        toggleList[0].onClick.AddListener(() => onSelet('A'));
        toggleList[1].onClick.AddListener(() => onSelet('B'));
        toggleList[2].onClick.AddListener(() => onSelet('C'));
        toggleList[3].onClick.AddListener(() => onSelet('D'));



        Submit.onClick.AddListener(() => onSubmit());
    }
    void Update()
    {
        // �������ؼ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }

    /// <summary>
    /// ��ȡjson�ļ�
    /// </summary>
    void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(jsonName);
        string jsonFileContent = jsonTextAsset.text;
        questions = JsonMapper.ToObject<MultipleChoice[]>(jsonFileContent);
        //������Ŀ״̬
        topicMax = questions.Length;
        allIndex.text = questions.Length.ToString();
        for (int x = 0; x < topicMax + 1; x++)
        {
            isAnserList.Add(false);
        }
    }

    /// <summary>
    /// ������Ŀ
    /// </summary>
    void LoadAnswer()
    {
        // TM_Text.text = (topicIndex + 1) + "��" + questions[topicIndex].question;//��Ŀ
        TM_Text.text = questions[topicIndex].question;
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
                // resultText.text = "<color=#00BA25FF>" + "��ϲ�㣬����ˣ�" + "</color>";
                scorePanel1.SetActive(true);
                //isRight = true;
                //if (topicIndex != (topicMax - 1))
                //{
                //    topicIndex++;
                //    LoadAnswer();
                //} else
                //{
                //    // resultText.text = "<color=#00BA25FF>" + "��ϲ�㣬���������Ŀ��" + "</color>";
                    
                //}
               
            }
            else
            {
                // resultText.text = "<color=#FF0020FF>" + "�Բ��𣬴���ˣ�" + "</color>";
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
        PlayerPrefs.SetInt("unit1lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("unit1lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("Unit " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Unit " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit 1");
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Read 1");
    }
}
