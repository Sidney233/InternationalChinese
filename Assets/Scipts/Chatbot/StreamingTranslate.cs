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
using UnityEngine.Android; // 引入权限管理命名空间

public class SteamingTranslate : MonoBehaviour
{
    public Button translateButton;      // 绑定Translate按钮
    public Image botResponseImage;      // 引用气泡图的 Image 组件
    public TMP_Text botResponseText;    // 绑定BotResponseText文本框
    public Image backgroundImage;       // 麦克风背景图片（用于颜色变化）
    public TMP_InputField recordedText; // 录音文本框（可选，根据需求添加）

    private ClientWebSocket ws;         // WebSocket连接
    private AudioClip recording;        // 麦克风录音
    private bool isRecording = false;
    //private string websocketUri = "ws://49.52.27.216:8765"; // WebSocket服务器地址
    private string websocketUri = "ws://58.198.176.241:8765"; // WebSocket服务器地址
    private CancellationTokenSource cts;  // 用于取消WebSocket接收消息的token

    private string pcmFilePath;         // PCM 文件路径
    private string lastStartTime = "";  // 用于保存上一次的 start_time
    private string lastMessage = "";  // 用于保存上一次的 start_time最后的消息
    private string translateMessage = "";
    private bool flag_translate = false;

    public LLM llm;

    private enum MicStatus
    {
        Initialized,    // 初始化
        Recording,      // 录音中
        RecordingEnd,   // 录音结束
        Processing      // 处理录音
    }

    private MicStatus micStatus = MicStatus.Initialized;

    private void Start()
    {
        // 设置 PCM 文件路径为持久化数据路径
        pcmFilePath = Path.Combine(Application.persistentDataPath, "streaming_output.pcm");
        Debug.Log("PCM File Path: " + pcmFilePath);

        // 按钮点击事件绑定
        translateButton.onClick.AddListener(OnTranslateButtonClick);

        // 检查麦克风设备
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("缺少麦克风设备！");
            botResponseText.text = "缺少麦克风设备！";
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
     * Translate按钮点击事件
     */
    private void OnTranslateButtonClick()
    {
        Debug.Log("Translate按钮被点击。当前状态: " + micStatus);

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
                Debug.Log("当前正在处理录音，请稍候...");
                break;
        }
    }

    /*
     * 开始录音的逻辑
     */
    private async void StartRecording()
    {
        // 检查麦克风是否已连接
        if (Microphone.devices.Length <= 0)
        {
            Debug.Log("没有检测到麦克风设备！");
            botResponseText.text = "没有检测到麦克风设备！";
            return;
        }

        micStatus = MicStatus.Recording;
        backgroundImage.color = Color.red;
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Recording: Please Speak";

        // 使用 null 作为设备名称，以确保兼容性
        recording = Microphone.Start(null, true, 60, 16000); // 采样率为 16kHz，录音时长 60 秒
        Debug.Log("录音已开始。");

        // 初始化 WebSocket 连接
        if (ws == null || ws.State == WebSocketState.Aborted || ws.State == WebSocketState.Closed)
        {
            ws = new ClientWebSocket();
            try
            {
                // 连接 WebSocket
                await ws.ConnectAsync(new Uri(websocketUri), CancellationToken.None);
                Debug.Log("WebSocket connected.");
                botResponseText.text = "";
                StartReceivingMessages();  // 启动接收消息
            }
            catch (Exception ex)
            {
                Debug.Log($"连接 WebSocket 失败: {ex.Message}");
                botResponseText.text = "连接 WebSocket 失败。";
                micStatus = MicStatus.Initialized;
                return;  // 连接失败，提前返回
            }
        }
        else if (ws.State == WebSocketState.Open)
        {
            Debug.Log("WebSocket 已经连接。");
        }

        // 开始发送音频流
        _ = SendAudioStream();
    }

    /*
     * 停止录音的逻辑
     */
    private async void StopRecording()
    {
        if (micStatus != MicStatus.Recording)
        {
            Debug.Log("尝试停止录音，但当前状态不是录音中。");
            return;
        }

        // 停止录音
        Microphone.End(null);
        micStatus = MicStatus.Processing;
        backgroundImage.color = new Color(124 / 255f, 209 / 255f, 24 / 255f, 1f);
        botResponseText.text = "Recording Stopped, Processing...";

        Debug.Log("录音已停止。");

        // 关闭 WebSocket 连接
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                cts?.Cancel(); // 取消消息接收

                try
                {
                    // 关闭 WebSocket
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Recording stopped", CancellationToken.None);
                    ws.Dispose(); // 释放资源
                    ws = null;    // 释放 WebSocket 实例
                    Debug.Log("WebSocket 已关闭。");
                }
                catch (WebSocketException ex)
                {
                    Debug.Log($"关闭 WebSocket 失败: {ex.Message}");
                }
            }
            else if (ws.State == WebSocketState.CloseReceived || ws.State == WebSocketState.CloseSent)
            {
                Debug.Log($"WebSocket 正在关闭中: {ws.State}。跳过关闭操作。");
            }
            else if (ws.State == WebSocketState.Aborted)
            {
                Debug.Log("WebSocket 已中止。跳过关闭操作。");
                ws.Dispose();
                ws = null;
            }
            else if (ws.State == WebSocketState.Closed)
            {
                Debug.Log("WebSocket 已关闭。跳过关闭操作。");
                ws.Dispose();
                ws = null;
            }
            else
            {
                Debug.Log($"WebSocket 状态无效: {ws.State}。");
            }
        }

        // 更新状态
        micStatus = MicStatus.RecordingEnd;
        botResponseText.text = "Recording Stopped, Waiting for Translation....";
    }

    /*
     * 开始接收WebSocket消息
     */

    private async void StartReceivingMessages()
    {
        cts = new CancellationTokenSource();
        byte[] buffer = new byte[1024];
        StringBuilder messageBuilder = new StringBuilder();  // 用于拼接接收到的分段消息

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // 将接收到的字节数据解码成文本
                    string messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(messagePart);  // 拼接到消息中

                    // 如果消息是完整的（WebSocket 的 Text 消息会在一次完整的包中传输）
                    if (result.EndOfMessage)
                    {   
                        // 处理接收到的完整消息
                        string message = messageBuilder.ToString();
                        // 将收到的消息中的 Unicode 转义字符（例如 "\u6211"）转换为实际字符
                        string decodedMessage = Regex.Unescape(message);
                        Debug.Log($"接收到的流式消息为: {decodedMessage}");

                        // 解析 JSON 消息，假设格式为 {"voice_text_str": "...", "start_time": "..."}
                        try
                        {   
                            var jsonObject = JsonUtility.FromJson<MessageData>(decodedMessage);
                            Debug.Log("jsonObject.start_time：" + jsonObject.start_time);
                            Debug.Log("lastStartTime：" + lastStartTime);
                            // 如果 start_time 和上一次的不同，则追加，否则直接更新
                            if (jsonObject.start_time != lastStartTime)
                            {   
                                lastMessage = botResponseText.text;
                                //if (lastMessage != null&& lastMessage!="")
                                //{
                                //    StartCoroutine(llm.SendRequestToChatbot_translate(lastMessage));

                                //}
                                botResponseText.text = lastMessage+jsonObject.voice_text_str;  // 追加
                            }
                            else
                            {
                                botResponseText.text = lastMessage+jsonObject.voice_text_str+"\n";  // 更新
                            }

                            // 更新 lastStartTime 为当前的 start_time
                            lastStartTime = jsonObject.start_time;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"解析 JSON 时出错: {ex.Message}");
                        }

                        // 清空 messageBuilder 为下一个消息做准备
                        messageBuilder.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("WebSocket 服务器请求关闭连接。");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"接收消息时出错: {ex.Message}");
        }
    }

    void HandleBotResponse(string response)
    {
        Debug.Log("同声翻译：" + response);
        lastMessage = lastMessage+response+ "\n";
    }
    /*
     * 发送音频流到WebSocket
     */
    private async Task SendAudioStream()
    {
        int bufferSize = 640; // 每次发送的数据块大小（根据实际需要调整）
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

            // 防止溢出
            if (micPosition < lastPosition)
            {
                lastPosition = micPosition;
            }

            int samplesToRead = micPosition - lastPosition;
            if (samplesToRead >= bufferSize)
            {
                // 获取音频数据
                recording.GetData(samples, lastPosition);
                lastPosition += bufferSize;

                // 将音频数据转换为字节流（16-bit PCM）
                byte[] byteArray = new byte[bufferSize * 2];
                for (int i = 0; i < bufferSize; i++)
                {
                    short sample = (short)(samples[i] * short.MaxValue); // 将浮动值转换为 16 位的 PCM 格式
                    byteArray[i * 2] = (byte)(sample & 0xFF);
                    byteArray[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }

                // 发送音频数据到后端
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(new ArraySegment<byte>(byteArray), WebSocketMessageType.Binary, true, CancellationToken.None);
                        //Debug.Log("发送音频数据。");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"发送音频数据失败: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log("WebSocket 未连接或已关闭。");
                }
            }

            // 延迟一段时间，控制发送频率
            await Task.Delay(40); // 每40毫秒检查一次
        }

        Debug.Log("停止发送音频流。");
    }

    /*
     * 当销毁对象时关闭WebSocket连接
     */
    private async void OnDestroy()
    {
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                cts?.Cancel(); // 取消消息接收
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    ws.Dispose();
                    ws = null;
                    Debug.Log("WebSocket 已关闭（OnDestroy）。");
                }
                catch (Exception ex)
                {
                    Debug.Log($"关闭 WebSocket 失败（OnDestroy）: {ex.Message}");
                }
            }
            else
            {
                ws.Dispose();
                ws = null;
                Debug.Log("WebSocket 已释放（OnDestroy）。");
            }
        }
    }

    /*
     * 超时后关闭WebSocket（可选）
     */
    private async Task CloseWebSocketAfterDelay(int delaySeconds)
    {
        await Task.Delay(delaySeconds * 1000); // 等待指定秒数
        // 超时后，检查并关闭 WebSocket
        if (ws != null && ws.State == WebSocketState.Open)
        {
            try
            {
                micStatus = MicStatus.Processing;
                Debug.Log("超时，关闭 WebSocket。");
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Timeout reached", CancellationToken.None);
                ws.Dispose();
                ws = null;
                Debug.Log("WebSocket 已关闭（超时）。");
            }
            catch (Exception ex)
            {
                Debug.Log($"超时关闭 WebSocket 失败: {ex.Message}");
            }
        }
    }
}
// 定义用于解析 JSON 的数据结构
[System.Serializable]
public class MessageData
{
    public string voice_text_str;
    public string start_time;
}
