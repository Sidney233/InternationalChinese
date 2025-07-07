using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ListenManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // ¼àÌý·µ»Ø¼ü
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }

    public void OnLearnClick()
    {
        SceneManager.LoadScene("Listen_selection");
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit");
    }
}
