using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogManager2 : MonoBehaviour
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

    public void OnStartClick()
    {
        SceneManager.LoadScene("Fill Blank Food");
    }
    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit Food");
    }
}
