using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NextLevel : MonoBehaviour
{
    public GameObject loadingPanel;
    public Scrollbar scrollbar;
    public TextMeshProUGUI text;
    private Button btn;
    // Start is called before the first frame update
    void Start()
    {
        btn = gameObject.GetComponent<Button>();
        btn.onClick.AddListener(LoadNextLevel);
    }

    void Update()
    {
        
    }

    void LoadNextLevel()
    {
        StartCoroutine(Loadlevel());
    }

    IEnumerator Loadlevel()
    {
        loadingPanel.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync("Selection");
        operation.allowSceneActivation = false;//控制不自动跳转到加载好的场景，progress值停在0.9
        while (!operation.isDone)
        {
            scrollbar.value = operation.progress;
            text.text = operation.progress * 100 + "%";//百分比
            if (operation.progress >= 0.9f)//如果进度条已经到达90%
            {
                scrollbar.value = 1; //那就让进度条的值编变成1
                text.text = "加载完成！请按任意键";
                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;//完成最后10%的工作，显示加载好的场景
                }
            }
            yield return null;
        }
    }
}
