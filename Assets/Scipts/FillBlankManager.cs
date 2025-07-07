using LitJson;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DataStructure;


public class FillBlankManager : MonoBehaviour
{
    public Slider slider;//进度条
    public TextMeshProUGUI indexText;//当前题号
    public TextMeshProUGUI allIndex;//总题数
    public Button skipBtn;//跳过按钮
    public Button favoriteBtn;//收藏按钮
    public Button nextBtn;//下一题
    public TextMeshProUGUI chinese;
    public TextMeshProUGUI pinyin;
    public TMP_InputField inputField;
    public GameObject successPanel;
    public GameObject failPanel;
    public string jsonName;
    private FillBlank[] fillBlanks;
    private int currentIndex;
    private string answer;
    private string curTtsPath;
    private int currentUnitNumber = 3;


    // Start is called before the first frame update
    void Start()
    {
        ReadJson();
        LoadWriting(currentIndex);
    }
    void Update()
    {
        // 监听返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }
    private void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(jsonName);
        string jsonFileContent = jsonTextAsset.text;
        fillBlanks = JsonMapper.ToObject<FillBlank[]>(jsonFileContent);
        allIndex.text = fillBlanks.Length.ToString();
        currentIndex = 0;
    }
    private void LoadWriting(int index)
    {
        indexText.text = (index + 1).ToString();
        slider.value = (index + 1) / (float)fillBlanks.Length;
        chinese.text = fillBlanks[index].question;
        pinyin.text = fillBlanks[index].pinyin;
        answer = fillBlanks[index].answer;
        curTtsPath = "http://49.52.27.216" + fillBlanks[index].tts_path.Remove(0, 1);
    }
    public void OnCheckAnswer()
    {
        if(inputField.text == answer)
        {
            successPanel.SetActive(true);
        }
        else
        {
            failPanel.SetActive(true);
        }
    }
    public void OnNextClick()
    {
        if (currentIndex == fillBlanks.Length - 1)
        {
            OnCompleteLearningButtonClicked();
        }
        else
        {
            inputField.text = "";
            successPanel.SetActive(false);
            failPanel.SetActive(false);
            currentIndex = currentIndex + 1;
            LoadWriting(currentIndex);
        }
    }
    public void OnCompleteLearningButtonClicked()
    {
        PlayerPrefs.SetInt("lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit");
    }
    public void OnBackClick()
    {
        SceneManager.LoadScene("Read");
    }
}
