using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DataStructure;
using LitJson;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using NAudio.Wave;
using NAudio;
using System.IO;

public class Listen : MonoBehaviour
{
    private NewWord[] newWords;
    List<GameObject> words = new List<GameObject>();
    List<GameObject> speakers = new List<GameObject>();
    List<GameObject> meanings = new List<GameObject>();
    void Awake()
    {
        ReadJson();
        LoadNewWords();
    }
    // Start is called before the first frame update
    void Start()
    {
       
    }
    /// <summary>
    /// 读取json文件
    /// </summary>
    void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("newWordsTest");
        string jsonFileContent = jsonTextAsset.text;
        newWords = JsonMapper.ToObject<NewWord[]>(jsonFileContent);
    }
    /// <summary>
    /// 将读取到的字词数据放入 UI 界面中
    /// </summary>
    void LoadNewWords()
    {
        // 加载预制体
        GameObject word_prefab = Resources.Load("Prefabs/Word") as GameObject;
        GameObject speaker_prefab = Resources.Load("Prefabs/Speaker") as GameObject;
        GameObject meaning_prefab = Resources.Load("Prefabs/Meaning") as GameObject;
        foreach(NewWord newWord in newWords)
        {
            GameObject word = Instantiate(word_prefab, gameObject.transform);
            GameObject speaker = Instantiate(speaker_prefab, gameObject.transform);
            GameObject meaning = Instantiate(meaning_prefab, gameObject.transform);
            word.GetComponent<TextMeshProUGUI>().text = newWord.word;
            meaning.GetComponent<TextMeshProUGUI>().text = newWord.meaning;
            LoadAudio(speaker, newWord.audio, newWord.word);
            words.Add(word);
            speakers.Add(speaker);
            meanings.Add(meaning);
        }
    }

    void LoadAudio(GameObject speaker, string base64, string word)
    {
        AudioSource audioSource = speaker.GetComponent<AudioSource>();
        Button btn = speaker.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => OnClickPlay(audioSource));
        byte[] decodedBytes = Convert.FromBase64String(base64);
        float[] _clipData = new float[decodedBytes.Length / 2];
        for (int i = 0; i < decodedBytes.Length; i += 2)
        {
            _clipData[i / 2] = (short)((decodedBytes[i + 1] << 8) | decodedBytes[i]) / 32768.0f;
        }
        audioSource.clip = AudioClip.Create(word, 8000*600, 1, 8000, false);
        audioSource.clip.SetData(_clipData, 0);
    }

    private void OnClickPlay(AudioSource audio)
    {
        audio.Play();
    }

    public static float[] bytesToFloat(byte[] byteArray)//byte[]数组转化为AudioClip可读取的float[]类型
    {
        float[] sounddata = new float[byteArray.Length / 2];
        for (int i = 0; i < sounddata.Length; i++)
        {
            sounddata[i] = bytesToFloat(byteArray[i * 2], byteArray[i * 2 + 1]);
        }
        return sounddata;
    }
    static float bytesToFloat(byte firstByte, byte secondByte)
    {
        //小端和大端顺序要调整
        short s;
        if (BitConverter.IsLittleEndian)
            s = (short)((secondByte << 8) | firstByte);
        else
            s = (short)((firstByte << 8) | secondByte);
        return s / 32768.0F;
    }
}
