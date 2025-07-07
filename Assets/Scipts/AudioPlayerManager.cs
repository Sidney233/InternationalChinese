using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DataStructure;
using static LearningPageController;

public class AudioPlayerManager : MonoBehaviour
{
    private Reading[] readings;
    private bool micConnected = false;//麦克风是否连接
    private int minFreq, maxFreq;//最小和最大频率
    public AudioClip RecordedClip;//录音
    public AudioClip QuestionClip;//题目音频
    public AudioSource audioSource;//播放的音频
    public TextMeshProUGUI Infotxt;//提示信息
    public Button micButton;//麦克风按钮
    public Button playButton;//播放按钮
    public Slider slider;//进度条
    public TextMeshProUGUI indexText;//当前题号
    public TextMeshProUGUI allIndex;//总题数
    public Button skipBtn;//跳过按钮
    public Button favoriteBtn;//收藏按钮
    public GameObject scorePanel;//分数面板
    public TextMeshProUGUI chinese;
    public TextMeshProUGUI pinyin;
    public TextMeshProUGUI translation;
    public GameObject recodingPanel;
    public GameObject loading;
    public string jsonName;
    private string curTtsPath;
    private Sprite pause;
    private Sprite play;
    private Color btnColor;
    private string fileName;//保存的文件名
    private byte[] data;
    private Coroutine playRemoteWavCoroutine;
    private int currentIndex;
    private int currentUnitNumber = 2;  // 当前学习单元编号

    private enum MicStatus
    {
        Initialized,//初始化
        Recording,//录音中
        RecordingEnd,//录音结束
        Playing,//播放中
        PlayingEnd,//播放结束
    }
    private MicStatus micStatus;

    private void Start()
    {
        Infotxt.text = "";
        micStatus = MicStatus.Initialized;
        playButton.GetComponent<Button>().interactable = false;
        QuestionClip = Resources.Load<AudioClip>("Audio/song");
        if (Microphone.devices.Length <= 0)
        {
            Infotxt.text = "缺少麦克风设备！";
        }
        else
        {
            //Infotxt.text = "设备名称为：" + Microphone.devices[0].ToString() + "请点击Start开始录音！";
            micConnected = true;
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
            if (minFreq == 0 && maxFreq == 0)
            {
                maxFreq = 44100;
            }
        }
        play = Resources.Load("Images/play", typeof(Sprite)) as Sprite;
        pause = Resources.Load("Images/pause", typeof(Sprite)) as Sprite;
        ReadJson();
        LoadReading(currentIndex);
    }
    void Update()
    {
        // 监听返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }
    /// <summary>
    /// 读取JSON文件
    /// </summary>
    private void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(jsonName);
        string jsonFileContent = jsonTextAsset.text;
        readings = JsonMapper.ToObject<Reading[]>(jsonFileContent);
        allIndex.text = readings.Length.ToString();
        currentIndex = 0;
    }
    /// <summary>
    /// 从reading数组中读取第几道题数据到UI中
    /// </summary>
    private void LoadReading(int index)
    {
        indexText.text = (index + 1).ToString();
        slider.value = (index + 1) / (float)readings.Length;
        chinese.text = readings[index].sentence;
        pinyin.text = readings[index].pinyin;
        translation.text = readings[index].English;
        curTtsPath = "http://49.52.27.216" + readings[index].tts_path.Remove(0, 1);
    }
    /// <summary>
    /// 开始录音
    /// </summary>
    public void BeginRecording()
    {
        
        if (micConnected)
        {
            if (!Microphone.IsRecording(null))
            {
                micButton.GetComponent<Image>().color = Color.red;
                RecordedClip = Microphone.Start(null, false, 60, maxFreq);
            }
        }
        else
        {
            Infotxt.text = "请确认麦克风设备是否已连接！";
        }
    }
    /// <summary>
    /// 停止录音
    /// </summary>
    public void StopRecording()
    {
        ColorUtility.TryParseHtmlString("#7ED118", out btnColor);
        micButton.GetComponent<Image>().color = btnColor;
        data = GetRealAudio(ref RecordedClip);
        Microphone.End(null);
        playButton.GetComponent<Button>().interactable = true;
        //Infotxt.text = "录音结束！";
    }
    /// <summary>
    /// 播放录音
    /// </summary>
    public void BeginPlaying()
    {
        if (!Microphone.IsRecording(null))
        {
            audioSource.clip = RecordedClip;
            audioSource.Play();
            playButton.GetComponent<Image>().sprite = pause;//改变按钮图标
        }
        else
        {
            //Infotxt.text = "正在录音中，请先停止录音！";
        }
    }
    /// <summary>
    /// 停止播放录音
    /// </summary>
    void StopPlaying()
    {
        if (!Microphone.IsRecording(null))
        {
            audioSource.clip = RecordedClip;
            audioSource.Stop();
            playButton.GetComponent<Image>().sprite = play;//改变按钮图标
        }
    }
    /// <summary>
    /// 保存录音
    /// </summary>
    public void Save()
    {
        if (!Microphone.IsRecording(null))
        {
            fileName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            if (!fileName.ToLower().EndsWith(".wav"))
            {//如果不是“.wav”格式的，加上后缀
                fileName += ".wav";
            }
            string path = Path.Combine(Application.persistentDataPath, fileName);//录音保存路径
            print(path);//输出路径
            //Adress.text = path;
            using (FileStream fs = CreateEmpty(path))
            {
                fs.Write(data, 0, data.Length);
                WriteHeader(fs, RecordedClip); //wav文件头
            }
        }
        else
        {
            Infotxt.text = "正在录音中，请先停止录音！";
        }
    }
    /// <summary>
    /// 获取真正大小的录音
    /// </summary>
    /// <param name="recordedClip"></param>
    /// <returns></returns>
    public static byte[] GetRealAudio(ref AudioClip recordedClip)
    {
        int position = Microphone.GetPosition(null);
        if (position <= 0 || position > recordedClip.samples)
        {
            position = recordedClip.samples;
        }
        float[] soundata = new float[position * recordedClip.channels];
        recordedClip.GetData(soundata, 0);
        recordedClip = AudioClip.Create(recordedClip.name, position,
        recordedClip.channels, recordedClip.frequency, false);
        recordedClip.SetData(soundata, 0);
        int rescaleFactor = 32767;
        byte[] outData = new byte[soundata.Length * 2];
        for (int i = 0; i < soundata.Length; i++)
        {
            short temshort = (short)(soundata[i] * rescaleFactor);
            byte[] temdata = BitConverter.GetBytes(temshort);
            outData[i * 2] = temdata[0];
            outData[i * 2 + 1] = temdata[1];
        }
        Debug.Log("position=" + position + "  outData.leng=" + outData.Length);
        return outData;
    }
    /// <summary>
    /// 写文件头
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="clip"></param>
    public static void WriteHeader(FileStream stream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        stream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(stream.Length - 8);
        stream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        stream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        stream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        stream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        stream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        stream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        stream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        stream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        stream.Write(subChunk2, 0, 4);
    }
    /// <summary>
    /// 创建wav格式文件头
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns></returns>
    private FileStream CreateEmpty(string filepath)
    {
        FileStream fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++) //为wav文件头留出空间
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }
    /// <summary>
    /// 评分，并显示星星个数
    /// </summary>
    private void Scoring()
    {
        //TODO: 调用腾讯的接口来进行打分，目前直接展示DEMO结果
        recodingPanel.SetActive(false);
        loading.SetActive(true);
        StartCoroutine(Post());
    }
    private IEnumerator IELoadExternalAudioWebRequest(string _url, AudioType _audioType)
    {
        UnityWebRequest _unityWebRequest = UnityWebRequestMultimedia.GetAudioClip(_url, _audioType);
        yield return _unityWebRequest.SendWebRequest();

        if (_unityWebRequest.result == UnityWebRequest.Result.ProtocolError ||
            _unityWebRequest.result == UnityWebRequest.Result.ConnectionError ||
            _unityWebRequest.result == UnityWebRequest.Result.DataProcessingError)
        {
              Debug.Log(_unityWebRequest.error.ToString());
        }
        else if(_unityWebRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip _audioClip = DownloadHandlerAudioClip.GetContent(_unityWebRequest);
            audioSource.clip = _audioClip;
            audioSource.Play();
        }
    }
    /// <summary>
    /// 按下麦克风按钮的点击事件
    /// </summary>
    public void OnMicClick()
    {
        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.PlayingEnd:
            case MicStatus.RecordingEnd: // 如果为这三种状态，则开始录音
                micStatus = MicStatus.Recording;
                BeginRecording();
                break;
            case MicStatus.Recording: // 如果为录音状态，则停止录音，并保存，然后进行评分
                micStatus = MicStatus.RecordingEnd;
                StopRecording();
                Save();
                Scoring();
                break;
            case MicStatus.Playing: // 如果为播放状态，则停止播放并开始录音
                StopPlaying();
                micStatus = MicStatus.Recording;
                BeginRecording();
                break;
        }
    }
    /// <summary>
    /// 按下播放按钮的点击事件
    /// </summary>
    public void OnPlayClick()
    {
        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.RecordingEnd:
            case MicStatus.PlayingEnd:
                micStatus = MicStatus.Playing;
                BeginPlaying();
                break;
            case MicStatus.Recording:
                break;
            case MicStatus.Playing:
                micStatus = MicStatus.PlayingEnd;
                StopPlaying();
                break;
        }
    }
    private void BeginSpeaker()
    {
        if (!Microphone.IsRecording(null))
        {
            //记录开启播放远程音频的协程，用于关闭
            playRemoteWavCoroutine =
                StartCoroutine(IELoadExternalAudioWebRequest(curTtsPath, AudioType.WAV));

        }
        else
        {
            //Infotxt.text = "正在录音中，请先停止录音！";
        }
    }
    void StopSpeaker()
    {
        if (!Microphone.IsRecording(null))
        {
            audioSource.clip = QuestionClip;
            audioSource.Stop();
        }
    }
    public void OnSpeakerClick()
    {
        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.RecordingEnd:
            case MicStatus.PlayingEnd:
                BeginSpeaker();
                break;
        }
    }
    public void OnNextClick()
    {
        if (currentIndex == readings.Length - 1)
        {
            OnCompleteLearningButtonClicked();
        }
        else
        {
            scorePanel.gameObject.SetActive(false);
            micStatus = MicStatus.Initialized;
            playButton.GetComponent<Button>().interactable = false;
            currentIndex = currentIndex + 1;
            LoadReading(currentIndex);
        }
    }
    // 用户点击“完成学习”按钮时调用
    public void OnCompleteLearningButtonClicked()
    {
        PlayerPrefs.SetInt("lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit");
    }
    private IEnumerator Post()
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("userid", "test"));
        formData.Add(new MultipartFormDataSection("ref_text", chinese.text));
        formData.Add(new MultipartFormFileSection("wav", data, indexText.text + ".wav", "audio/wav"));
        UnityWebRequest webRequest = UnityWebRequest.Post("http://49.52.27.216:5001/api/soe", formData);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
            recodingPanel.SetActive(true);
            loading.SetActive(false);
        }
        else
        {
            Debug.Log("发送成功");
            recodingPanel.SetActive(true);
            loading.SetActive(false);
            scorePanel.SetActive(true);
        }
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit");
    }
}
