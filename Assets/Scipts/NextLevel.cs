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
        operation.allowSceneActivation = false;//���Ʋ��Զ���ת�����غõĳ�����progressֵͣ��0.9
        while (!operation.isDone)
        {
            scrollbar.value = operation.progress;
            text.text = operation.progress * 100 + "%";//�ٷֱ�
            if (operation.progress >= 0.9f)//����������Ѿ�����90%
            {
                scrollbar.value = 1; //�Ǿ��ý�������ֵ����1
                text.text = "������ɣ��밴�����";
                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;//������10%�Ĺ�������ʾ���غõĳ���
                }
            }
            yield return null;
        }
    }
}
