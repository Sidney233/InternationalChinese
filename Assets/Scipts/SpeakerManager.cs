using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeakerManager : MonoBehaviour
{
    private AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        AudioClip clip = Resources.Load<AudioClip>("Audio/ke");
        audioSource.clip = clip;
        Button btn = GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => OnClickPlay());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnClickPlay()
    {
        audioSource.Play();
    }
}
