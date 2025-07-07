using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
   
    void Awake()
    {
       
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void BtnLoadScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}
