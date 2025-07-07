using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Welcome : MonoBehaviour
{
    public Button startBtn; // 按钮引用
    public GameObject panel; // Panel 对象
    private static bool isFirstLoad = true;

    void Start()
    {
        if (isFirstLoad)
        {
            panel.SetActive(true);
            isFirstLoad = false; // 更新状态
        }
        else
        {
            panel.SetActive(false); // 不显示 Panel
        }

        startBtn.onClick.AddListener(HidePanel);
    }

    void HidePanel()
    {
        panel.SetActive(false);
    }
}
