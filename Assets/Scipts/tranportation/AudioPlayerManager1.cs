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

public class AudioPlayerManager1 : MonoBehaviour
{
    private Reading[] readings;
    private bool micConnected = false;//��˷��Ƿ�����
    private int minFreq, maxFreq;//��С�����Ƶ��
    public AudioClip RecordedClip;//¼��
    public AudioClip QuestionClip;//��Ŀ��Ƶ
    public AudioSource audioSource;//���ŵ���Ƶ
    public TextMeshProUGUI Infotxt;//��ʾ��Ϣ
    public Button micButton;//��˷簴ť
    public Button playButton;//���Ű�ť
    public Slider slider;//������
    public TextMeshProUGUI indexText;//��ǰ���
    public TextMeshProUGUI allIndex;//������
    public Button skipBtn;//������ť
    public Button favoriteBtn;//�ղذ�ť
    public Button nextBtn;//��һ��
    public TextMeshProUGUI chinese;
    public TextMeshProUGUI pinyin;
    public TextMeshProUGUI translation;
    public GameObject recodingPanel;
    public GameObject loading;
    public GameObject scorePanel;
    private string curTtsPath;
    private Sprite pause;
    private Sprite play;
    private Color btnColor;
    private string fileName;//������ļ���
    private byte[] data;
    private Coroutine playRemoteWavCoroutine;
    private int currentIndex;
    private int currentUnitNumber = 2;  // ��ǰѧϰ��Ԫ���

    private enum MicStatus
    {
        Initialized,//��ʼ��
        Recording,//¼����
        RecordingEnd,//¼������
        Playing,//������
        PlayingEnd,//���Ž���
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
            Infotxt.text = "ȱ����˷��豸��";
        }
        else
        {
            //Infotxt.text = "�豸����Ϊ��" + Microphone.devices[0].ToString() + "����Start��ʼ¼����";
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
        // �������ؼ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
    }
    /// <summary>
    /// ��ȡJSON�ļ�
    /// </summary>
    private void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("reading_transportation");
        string jsonFileContent = jsonTextAsset.text;
        readings = JsonMapper.ToObject<Reading[]>(jsonFileContent);
        allIndex.text = readings.Length.ToString();
        currentIndex = 0;
    }
    /// <summary>
    /// ��reading�����ж�ȡ�ڼ��������ݵ�UI��
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
    /// ��ʼ¼��
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
            Infotxt.text = "��ȷ����˷��豸�Ƿ������ӣ�";
        }
    }
    /// <summary>
    /// ֹͣ¼��
    /// </summary>
    public void StopRecording()
    {
        ColorUtility.TryParseHtmlString("#7ED118", out btnColor);
        micButton.GetComponent<Image>().color = btnColor;
        data = GetRealAudio(ref RecordedClip);
        Microphone.End(null);
        playButton.GetComponent<Button>().interactable = true;
        //Infotxt.text = "¼��������";
    }
    /// <summary>
    /// ����¼��
    /// </summary>
    public void BeginPlaying()
    {
        if (!Microphone.IsRecording(null))
        {
            audioSource.clip = RecordedClip;
            audioSource.Play();
            playButton.GetComponent<Image>().sprite = pause;//�ı䰴ťͼ��
        }
        else
        {
            //Infotxt.text = "����¼���У�����ֹͣ¼����";
        }
    }
    /// <summary>
    /// ֹͣ����¼��
    /// </summary>
    void StopPlaying()
    {
        if (!Microphone.IsRecording(null))
        {
            audioSource.clip = RecordedClip;
            audioSource.Stop();
            playButton.GetComponent<Image>().sprite = play;//�ı䰴ťͼ��
        }
    }
    /// <summary>
    /// ����¼��
    /// </summary>
    public void Save()
    {
        if (!Microphone.IsRecording(null))
        {
            fileName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            if (!fileName.ToLower().EndsWith(".wav"))
            {//������ǡ�.wav����ʽ�ģ����Ϻ�׺
                fileName += ".wav";
            }
            string path = Path.Combine(Application.persistentDataPath, fileName);//¼������·��
            print(path);//���·��
            //Adress.text = path;
            using (FileStream fs = CreateEmpty(path))
            {
                fs.Write(data, 0, data.Length);
                WriteHeader(fs, RecordedClip); //wav�ļ�ͷ
            }
        }
        else
        {
            Infotxt.text = "����¼���У�����ֹͣ¼����";
        }
    }
    /// <summary>
    /// ��ȡ������С��¼��
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
    /// д�ļ�ͷ
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
    /// ����wav��ʽ�ļ�ͷ
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns></returns>
    private FileStream CreateEmpty(string filepath)
    {
        FileStream fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++) //Ϊwav�ļ�ͷ�����ռ�
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }
    /// <summary>
    /// ���֣�����ʾ���Ǹ���
    /// </summary>
    private void Scoring()
    {
        //TODO: ������Ѷ�Ľӿ������д�֣�Ŀǰֱ��չʾDEMO���
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
    /// ������˷簴ť�ĵ���¼�
    /// </summary>
    public void OnMicClick()
    {
        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.PlayingEnd:
            case MicStatus.RecordingEnd: // ���Ϊ������״̬����ʼ¼��
                micStatus = MicStatus.Recording;
                BeginRecording();
                break;
            case MicStatus.Recording: // ���Ϊ¼��״̬����ֹͣ¼���������棬Ȼ���������
                micStatus = MicStatus.RecordingEnd;
                StopRecording();
                Save();
                Scoring();
                break;
            case MicStatus.Playing: // ���Ϊ����״̬����ֹͣ���Ų���ʼ¼��
                StopPlaying();
                micStatus = MicStatus.Recording;
                BeginRecording();
                break;
        }
    }
    /// <summary>
    /// ���²��Ű�ť�ĵ���¼�
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
            //��¼��������Զ����Ƶ��Э�̣����ڹر�
            playRemoteWavCoroutine =
                StartCoroutine(IELoadExternalAudioWebRequest(curTtsPath, AudioType.WAV));

        }
        else
        {
            //Infotxt.text = "����¼���У�����ֹͣ¼����";
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
            scorePanel.SetActive(false);
            micStatus = MicStatus.Initialized;
            playButton.GetComponent<Button>().interactable = false;
            currentIndex = currentIndex + 1;
            LoadReading(currentIndex);
        }
    }
    // �û���������ѧϰ����ťʱ����
    public void OnCompleteLearningButtonClicked()
    {
        PlayerPrefs.SetInt("unit1lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("unit1lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("unit1Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("unit1Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit 1");
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
            scorePanel.SetActive(true);
            recodingPanel.SetActive(true);
            loading.SetActive(false);
        }
        else
        {
            Debug.Log("���ͳɹ�");
            scorePanel.SetActive(true);
            recodingPanel.SetActive(true);
            loading.SetActive(false);
        }
    }

    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit 1");
    }
}
