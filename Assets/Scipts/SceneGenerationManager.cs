using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGenerationManager : MonoBehaviour
{
    public GameObject loadingPanel;
    public GameObject generatePanel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartClick()
    {
        StartCoroutine(LoadWithLoadingPanel("Home Travel"));
    }

    public void OnSkipClick()
    {
        StartCoroutine(LoadWithLoadingPanel("Home"));
    }

    IEnumerator LoadWithLoadingPanel(string sceneName)
    {
        if (sceneName == "Home Travel")
        {
            generatePanel.SetActive(true);
        }
        else
        {
            loadingPanel.SetActive(true);
        }
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        if (sceneName == "Home Travel")
        {
            yield return new WaitForSeconds(5.0f);
        }
        else
        {
            yield return null;
        }
        operation.allowSceneActivation = true;
    }
}
