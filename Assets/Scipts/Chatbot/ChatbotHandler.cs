using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro; // 添加 TextMeshPro 命名空间
using System;
using TencentCloud.Common;
using TencentCloud.Common.Profile;

public class ChatbotHandler : MonoBehaviour
{
    public TMP_Text botResponseText;        // 回复文本框
    public Button recordButton;             // 录音按钮
    public TMP_InputField recordedText;     // 录音文本框
    public Button clearButton;              // 清空按钮
    public Button sendButton;               // 发送按钮

    private AudioClip recordedClip;
    private string pcmFilePath; // 保存为 PCM 格式的录音文件路径

    public Image backgroundImage;  // 麦克风背景图片
    public Image microphoneIcon;   // 麦克风图标
    public Image botResponseImage; // 气泡图的 Image 组件

    public TTS tts; // 腾讯文字转语音服务
    public LLM llm; // 调用本地大语言模型

    private enum MicStatus
    {
        Initialized,    // 初始化
        Recording,      // 录音中
        RecordingEnd,   // 录音结束
        Processing      // 处理录音
    }

    private MicStatus micStatus = MicStatus.Initialized;

    void Start()
    {
        // 设置 PCM 文件路径为持久化数据路径
        pcmFilePath = Path.Combine(Application.persistentDataPath, "output.pcm");
        Debug.Log("PCM File Path: " + pcmFilePath);

        // 添加按钮监听事件
        recordButton.onClick.AddListener(OnRecordButtonClick);
        clearButton.onClick.AddListener(OnClearButtonClick);
        sendButton.onClick.AddListener(OnSendButtonClick);

        // 设置录音按钮的初始背景颜色
        backgroundImage.color = new Color(124 / 255f, 209 / 255f, 24 / 255f, 1f);

        botResponseText.gameObject.SetActive(false);
        botResponseImage.gameObject.SetActive(false);

        // 检查麦克风设备
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("缺少麦克风设备！");
        }
        else
        {
            Debug.Log("麦克风设备数量: " + Microphone.devices.Length);
            foreach (var device in Microphone.devices)
            {
                Debug.Log("麦克风设备: " + device);
            }
        }
    }

    /*
     * 录音按钮点击事件
     */
    void OnRecordButtonClick()
    {
        Debug.Log("录音按钮被点击。当前状态: " + micStatus);

        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.RecordingEnd:
                StartRecording();
                break;

            case MicStatus.Recording:
                StopRecordingAndProcess();
                break;

            case MicStatus.Processing:
                Debug.LogWarning("当前正在处理录音，请稍候...");
                break;
        }
    }

    /*
     * 开始录音的逻辑
     */
    void StartRecording()
    {
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("没有检测到麦克风设备！");
            return;
        }

        micStatus = MicStatus.Recording;
        backgroundImage.color = Color.red;
        recordedText.gameObject.SetActive(true);
        recordedText.text = "Recording: Please Speak";

        // 使用 null 作为设备名称，以确保兼容性
        recordedClip = Microphone.Start(null, false, 60, 16000); // 采样率为 16kHz，录音时长 60 秒
        Debug.Log("录音已开始。");
    }

    /*
     * 停止录音并处理的逻辑
     */
    void StopRecordingAndProcess()
    {
        if (micStatus != MicStatus.Recording)
        {
            Debug.LogWarning("尝试停止录音，但当前状态不是录音中。");
            return;
        }

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
            Debug.Log("录音已停止。");
            backgroundImage.color = new Color(124 / 255f, 209 / 255f, 24 / 255f, 1f);
            micStatus = MicStatus.Processing;

            SaveAudioClipAsPcm(recordedClip, pcmFilePath); // 保存为 PCM 格式

            // 调用腾讯云 ASR API 进行语音识别
            StartCoroutine(SendAudioToTencentASR(pcmFilePath));
        }
        else
        {
            Debug.LogWarning("尝试停止录音，但麦克风未在录音。");
        }
    }

    /*
     * 保存录音为PCM 格式
     */
    void SaveAudioClipAsPcm(AudioClip clip, string filepath)
    {
        if (clip == null)
        {
            Debug.Log("AudioClip 是 null");
            return;
        }

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = ConvertFloatArrayToPCM16(samples);
        try
        {
            File.WriteAllBytes(filepath, pcmData);
            Debug.Log("音频已保存到 " + filepath);
        }
        catch (Exception e)
        {
            Debug.Log("保存音频失败: " + e.Message);
        }
    }

    /*
     * 将浮点数组转换为PCM 16位
     */
    byte[] ConvertFloatArrayToPCM16(float[] samples)
    {
        int length = samples.Length;
        byte[] pcmData = new byte[length * 2]; // 每个采样点 16 位，即 2 字节

        for (int i = 0; i < length; i++)
        {
            short intSample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            byte[] byteSample = BitConverter.GetBytes(intSample);

            pcmData[i * 2] = byteSample[0];
            pcmData[i * 2 + 1] = byteSample[1];
        }

        return pcmData;
    }

    /*
     * 清空录音文本
     */
    void OnClearButtonClick()
    {
        recordedText.text = "";
    }

    /*
     * 发送录音文本
     */
    void OnSendButtonClick()
    {
        string userInput = recordedText.text;
        if (!string.IsNullOrEmpty(userInput))
        {
            // 显示气泡图和文本框，提示用户等待回复
            botResponseImage.gameObject.SetActive(true);
            botResponseText.gameObject.SetActive(true);
            botResponseText.text = "Please wait for a reply...";

            StartCoroutine(llm.SendRequestToChatbot(userInput));
        }

        // 清空输入框
        recordedText.text = "";
    }

    /*
     * 将录音文件发送到腾讯语音识别服务
     */
    IEnumerator SendAudioToTencentASR(string pcmFilePath)
    {
        var cred = new Credential
        {
            SecretId = "AKIDWvAF6kDTXP4Y6avQktoaBbqofbdU8HG2",   
            SecretKey = "d4JSQG2d0A90hDcz95oLhplU2NWUN6bt",
        };

        var hpf = new HttpProfile
        {
            ReqMethod = "POST",
            Endpoint = "asr.tencentcloudapi.com",
        };
        var cpf = new ClientProfile(ClientProfile.SIGN_TC3SHA256, hpf);

        var client = new CommonClient("asr", "2019-06-14", cred, "", cpf);
        byte[] pcmData;
        try
        {
            pcmData = File.ReadAllBytes(pcmFilePath);
        }
        catch (Exception e)
        {
            Debug.Log("读取 PCM 文件失败: " + e.Message);
            micStatus = MicStatus.Initialized; // 重置状态
            yield break;
        }

        var base64Data = Convert.ToBase64String(pcmData);
        var param = new JObject
        {
            { "EngSerViceType", "16k_zh" },
            { "SourceType", 1 },
            { "VoiceFormat", "pcm" },
            { "Data", base64Data },
            { "DataLen", pcmData.Length }
        }.ToString();

        var req = new CommonRequest(param);
        string resp;
        try
        {
            resp = client.Call(req, "SentenceRecognition");
        }
        catch (Exception e)
        {
            Debug.Log("ASR API 调用失败: " + e.Message);
            micStatus = MicStatus.Initialized; // 重置状态
            yield break;
        }

        try
        {
            var jsonResponse = JObject.Parse(resp);
            string result = jsonResponse["Response"]["Result"].ToString();
            Debug.Log("识别结果: " + result);
            recordedText.text = result;
        }
        catch (Exception e)
        {
            Debug.Log("解析 ASR 响应时出错: " + e.ToString());
        }

        micStatus = MicStatus.RecordingEnd; // 更新状态
        yield return null;
    }
}
