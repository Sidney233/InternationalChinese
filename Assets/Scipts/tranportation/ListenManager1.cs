using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ListenManager1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // �������ؼ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }

    public void OnLearnClick()
    {
        SceneManager.LoadScene("Listen_selection 1");
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit 1");
    }
}
