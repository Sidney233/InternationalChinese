using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System;
using System.IO;
using UnityEngine.Android; // ����Ȩ�޹��������ռ�

public class SteamingTranslate : MonoBehaviour
{
    public Button translateButton;      // ��Translate��ť
    public Image botResponseImage;      // ��������ͼ�� Image ���
    public TMP_Text botResponseText;    // ��BotResponseText�ı���
    public Image backgroundImage;       // ��˷米��ͼƬ��������ɫ�仯��
    public TMP_InputField recordedText; // ¼���ı��򣨿�ѡ������������ӣ�

    private ClientWebSocket ws;         // WebSocket����
    private AudioClip recording;        // ��˷�¼��
    private bool isRecording = false;
    //private string websocketUri = "ws://49.52.27.216:8765"; // WebSocket��������ַ
    private string websocketUri = "ws://58.198.176.241:8765"; // WebSocket��������ַ
    private CancellationTokenSource cts;  // ����ȡ��WebSocket������Ϣ��token

    private string pcmFilePath;         // PCM �ļ�·��
    private string lastStartTime = "";  // ���ڱ�����һ�ε� start_time
    private string lastMessage = "";  // ���ڱ�����һ�ε� start_time������Ϣ
    private string translateMessage = "";
    private bool flag_translate = false;

    public LLM llm;

    private enum MicStatus
    {
        Initialized,    // ��ʼ��
        Recording,      // ¼����
        RecordingEnd,   // ¼������
        Processing      // ����¼��
    }

    private MicStatus micStatus = MicStatus.Initialized;

    private void Start()
    {
        // ���� PCM �ļ�·��Ϊ�־û�����·��
        pcmFilePath = Path.Combine(Application.persistentDataPath, "streaming_output.pcm");
        Debug.Log("PCM File Path: " + pcmFilePath);

        // ��ť����¼���
        translateButton.onClick.AddListener(OnTranslateButtonClick);

        // �����˷��豸
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("ȱ����˷��豸��");
            botResponseText.text = "ȱ����˷��豸��";
        }
        else
        {
            Debug.Log("��˷��豸����: " + Microphone.devices.Length);
            foreach (var device in Microphone.devices)
            {
                Debug.Log("��˷��豸: " + device);
            }
        }
    }

    /*
     * Translate��ť����¼�
     */
    private void OnTranslateButtonClick()
    {
        Debug.Log("Translate��ť���������ǰ״̬: " + micStatus);

        switch (micStatus)
        {
            case MicStatus.Initialized:
            case MicStatus.RecordingEnd:
                StartRecording();
                break;

            case MicStatus.Recording:
                StopRecording();
                break;

            case MicStatus.Processing:
                Debug.Log("��ǰ���ڴ���¼�������Ժ�...");
                break;
        }
    }

    /*
     * ��ʼ¼�����߼�
     */
    private async void StartRecording()
    {
        // �����˷��Ƿ�������
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("û�м�⵽��˷��豸��");
            botResponseText.text = "û�м�⵽��˷��豸��";
            return;
        }

        micStatus = MicStatus.Recording;
        backgroundImage.color = Color.red;
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Recording: Please Speak";

        // ʹ�� null ��Ϊ�豸���ƣ���ȷ��������
        recording = Microphone.Start(null, true, 60, 16000); // ������Ϊ 16kHz��¼��ʱ�� 60 ��
        Debug.Log("¼���ѿ�ʼ��");

        // ��ʼ�� WebSocket ����
        if (ws == null || ws.State == WebSocketState.Aborted || ws.State == WebSocketState.Closed)
        {
            ws = new ClientWebSocket();
            try
            {
                // ���� WebSocket
                await ws.ConnectAsync(new Uri(websocketUri), CancellationToken.None);
                Debug.Log("WebSocket connected.");
                botResponseText.text = "";
                StartReceivingMessages();  // ����������Ϣ
            }
            catch (Exception ex)
            {
                Debug.Log($"���� WebSocket ʧ��: {ex.Message}");
                botResponseText.text = "���� WebSocket ʧ�ܡ�";
                micStatus = MicStatus.Initialized;
                return;  // ����ʧ�ܣ���ǰ����
            }
        }
        else if (ws.State == WebSocketState.Open)
        {
            Debug.Log("WebSocket �Ѿ����ӡ�");
        }

        // ��ʼ������Ƶ��
        _ = SendAudioStream();
    }

    /*
     * ֹͣ¼�����߼�
     */
    private async void StopRecording()
    {
        if (micStatus != MicStatus.Recording)
        {
            Debug.Log("����ֹͣ¼��������ǰ״̬����¼���С�");
            return;
        }

        // ֹͣ¼��
        Microphone.End(null);
        micStatus = MicStatus.Processing;
        backgroundImage.color = new Color(124 / 255f, 209 / 255f, 24 / 255f, 1f);
        botResponseText.text = "Recording Stopped, Processing...";

        Debug.Log("¼����ֹͣ��");

        // �ر� WebSocket ����
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                cts?.Cancel(); // ȡ����Ϣ����

                try
                {
                    // �ر� WebSocket
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Recording stopped", CancellationToken.None);
                    ws.Dispose(); // �ͷ���Դ
                    ws = null;    // �ͷ� WebSocket ʵ��
                    Debug.Log("WebSocket �ѹرա�");
                }
                catch (WebSocketException ex)
                {
                    Debug.Log($"�ر� WebSocket ʧ��: {ex.Message}");
                }
            }
            else if (ws.State == WebSocketState.CloseReceived || ws.State == WebSocketState.CloseSent)
            {
                Debug.Log($"WebSocket ���ڹر���: {ws.State}�������رղ�����");
            }
            else if (ws.State == WebSocketState.Aborted)
            {
                Debug.Log("WebSocket ����ֹ�������رղ�����");
                ws.Dispose();
                ws = null;
            }
            else if (ws.State == WebSocketState.Closed)
            {
                Debug.Log("WebSocket �ѹرա������رղ�����");
                ws.Dispose();
                ws = null;
            }
            else
            {
                Debug.Log($"WebSocket ״̬��Ч: {ws.State}��");
            }
        }

        // ����״̬
        micStatus = MicStatus.RecordingEnd;
        botResponseText.text = "Recording Stopped, Waiting for Translation....";
    }

    /*
     * ��ʼ����WebSocket��Ϣ
     */

    private async void StartReceivingMessages()
    {
        cts = new CancellationTokenSource();
        byte[] buffer = new byte[1024];
        StringBuilder messageBuilder = new StringBuilder();  // ����ƴ�ӽ��յ��ķֶ���Ϣ

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // �����յ����ֽ����ݽ�����ı�
                    string messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(messagePart);  // ƴ�ӵ���Ϣ��

                    // �����Ϣ�������ģ�WebSocket �� Text ��Ϣ����һ�������İ��д��䣩
                    if (result.EndOfMessage)
                    {   
                        // ������յ���������Ϣ
                        string message = messageBuilder.ToString();
                        // ���յ�����Ϣ�е� Unicode ת���ַ������� "\u6211"��ת��Ϊʵ���ַ�
                        string decodedMessage = Regex.Unescape(message);
                        Debug.Log($"���յ�����ʽ��ϢΪ: {decodedMessage}");

                        // ���� JSON ��Ϣ�������ʽΪ {"voice_text_str": "...", "start_time": "..."}
                        try
                        {   
                            var jsonObject = JsonUtility.FromJson<MessageData>(decodedMessage);
                            Debug.Log("jsonObject.start_time��" + jsonObject.start_time);
                            Debug.Log("lastStartTime��" + lastStartTime);
                            // ��� start_time ����һ�εĲ�ͬ����׷�ӣ�����ֱ�Ӹ���
                            if (jsonObject.start_time != lastStartTime)
                            {   
                                lastMessage = botResponseText.text;
                                //if (lastMessage != null&& lastMessage!="")
                                //{
                                //    StartCoroutine(llm.SendRequestToChatbot_translate(lastMessage));

                                //}
                                botResponseText.text = lastMessage+jsonObject.voice_text_str;  // ׷��
                            }
                            else
                            {
                                botResponseText.text = lastMessage+jsonObject.voice_text_str+"\n";  // ����
                            }

                            // ���� lastStartTime Ϊ��ǰ�� start_time
                            lastStartTime = jsonObject.start_time;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"���� JSON ʱ����: {ex.Message}");
                        }

                        // ��� messageBuilder Ϊ��һ����Ϣ��׼��
                        messageBuilder.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("WebSocket ����������ر����ӡ�");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"������Ϣʱ����: {ex.Message}");
        }
    }

    void HandleBotResponse(string response)
    {
        Debug.Log("ͬ�����룺" + response);
        lastMessage = lastMessage+response+ "\n";
    }
    /*
     * ������Ƶ����WebSocket
     */
    private async Task SendAudioStream()
    {
        int bufferSize = 640; // ÿ�η��͵����ݿ��С������ʵ����Ҫ������
        float[] samples = new float[bufferSize];
        int lastPosition = 0;

        while (micStatus == MicStatus.Recording && Microphone.IsRecording(null))
        {
            int micPosition = Microphone.GetPosition(null);
            if (micPosition < 0)
            {
                await Task.Delay(100);
                continue;
            }

            // ��ֹ���
            if (micPosition < lastPosition)
            {
                lastPosition = micPosition;
            }

            int samplesToRead = micPosition - lastPosition;
            if (samplesToRead >= bufferSize)
            {
                // ��ȡ��Ƶ����
                recording.GetData(samples, lastPosition);
                lastPosition += bufferSize;

                // ����Ƶ����ת��Ϊ�ֽ�����16-bit PCM��
                byte[] byteArray = new byte[bufferSize * 2];
                for (int i = 0; i < bufferSize; i++)
                {
                    short sample = (short)(samples[i] * short.MaxValue); // ������ֵת��Ϊ 16 λ�� PCM ��ʽ
                    byteArray[i * 2] = (byte)(sample & 0xFF);
                    byteArray[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }

                // ������Ƶ���ݵ����
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(new ArraySegment<byte>(byteArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                        //Debug.Log("������Ƶ���ݡ�");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"������Ƶ����ʧ��: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log("WebSocket δ���ӻ��ѹرա�");
                }
            }

            // �ӳ�һ��ʱ�䣬���Ʒ���Ƶ��
            await Task.Delay(40); // ÿ40������һ��
        }

        Debug.Log("ֹͣ������Ƶ����");
    }

    /*
     * �����ٶ���ʱ�ر�WebSocket����
     */
    private async void OnDestroy()
    {
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                cts?.Cancel(); // ȡ����Ϣ����
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    ws.Dispose();
                    ws = null;
                    Debug.Log("WebSocket �ѹرգ�OnDestroy����");
                }
                catch (Exception ex)
                {
                    Debug.Log($"�ر� WebSocket ʧ�ܣ�OnDestroy��: {ex.Message}");
                }
            }
            else
            {
                ws.Dispose();
                ws = null;
                Debug.Log("WebSocket ���ͷţ�OnDestroy����");
            }
        }
    }

    /*
     * ��ʱ��ر�WebSocket����ѡ��
     */
    private async Task CloseWebSocketAfterDelay(int delaySeconds)
    {
        await Task.Delay(delaySeconds * 1000); // �ȴ�ָ������
        // ��ʱ�󣬼�鲢�ر� WebSocket
        if (ws != null && ws.State == WebSocketState.Open)
        {
            try
            {
                micStatus = MicStatus.Processing;
                Debug.Log("��ʱ���ر� WebSocket��");
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Timeout reached", CancellationToken.None);
                ws.Dispose();
                ws = null;
                Debug.Log("WebSocket �ѹرգ���ʱ����");
            }
            catch (Exception ex)
            {
                Debug.Log($"��ʱ�ر� WebSocket ʧ��: {ex.Message}");
            }
        }
    }
}
// �������ڽ��� JSON �����ݽṹ
[System.Serializable]
public class MessageData
{
    public string voice_text_str;
    public string start_time;
}
