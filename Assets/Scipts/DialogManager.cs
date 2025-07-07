using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogManager : MonoBehaviour
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

    public void OnStartClick()
    {
        SceneManager.LoadScene("Fill Blank");
    }
    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit");
    }
}
