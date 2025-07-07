using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Navigator : MonoBehaviour
{
    public Button button1;
    public Button button2; 
    public Button button3;
    // public Button button4; 
    // public Button button5;

    public Sprite button1Selected; 
    public Sprite button1Unselected; 
    public Sprite button2Selected; 
    public Sprite button2Unselected;
    // public Sprite button3Selected; 
    // public Sprite button3Unselected;
    // public Sprite button4Selected; 
    // public Sprite button4Unselected;
    // public Sprite button5Selected; 
    // public Sprite button5Unselected;

    public string selectedTextColorHex = "#7ED118"; // 文字选中颜色
    public string unselectedTextColorHex = "#8F94A3"; // 文字未选中颜色

    private TextMeshProUGUI button1Text;
    private TextMeshProUGUI button2Text;
    // private Text button3Text;
    // private Text button4Text;
    // private Text button5Text;

    void Start()
    {
        // 获取按钮的文字组件
        button1Text = button1.GetComponentInChildren<TextMeshProUGUI>();
        button2Text = button2.GetComponentInChildren<TextMeshProUGUI>();

        // 初始化按钮状态
        UpdateButtonState();

        // 添加按钮点击事件
        button1.onClick.AddListener(() => LoadScene("Home"));
        button2.onClick.AddListener(() => LoadScene("Review"));
        button3.onClick.AddListener(() => LoadScene("ChatBot"));
    }

    void LoadScene(string sceneName)
    {
        // 切换到指定场景
        SceneManager.LoadScene(sceneName);
    }

    void UpdateButtonState()
    {
        // 根据当前场景更新按钮状态
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Home")
        {
            button1.image.sprite = button1Selected;
            button2.image.sprite = button2Unselected;
            // 更新文字颜色
            button1Text.color = HexToColor(selectedTextColorHex);
            button2Text.color = HexToColor(unselectedTextColorHex);
        }
        else if (currentScene == "Review")
        {
            button1.image.sprite = button1Unselected;
            button2.image.sprite = button2Selected;
            // 更新文字颜色
            button1Text.color = HexToColor(unselectedTextColorHex);
            button2Text.color = HexToColor(selectedTextColorHex);
        }
    }

    Color HexToColor(string hex)
    {
        // 去掉开头的 `#` 符号
        hex = hex.Replace("#", "");

        // 转换 R、G、B 分量
        if (hex.Length == 6)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }
        else
        {
            Debug.LogError("Invalid hex color format: " + hex);
            return Color.white; // 返回默认白色
        }
    }
}
