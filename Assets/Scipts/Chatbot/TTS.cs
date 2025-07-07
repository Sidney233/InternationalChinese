using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.IO;
using TencentCloud.Common.Profile;
using TencentCloud.Common;
using UnityEngine;
using UnityEngine.Networking;

public class TTS : MonoBehaviour
{
    public AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        // 语音播报
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    /*
     * 使用腾讯 TTS API 进行语音合成
     */
    public void SpeakWithTencentTTS(string text)
    {
        try
        {
            var mod = "tts";
            var ver = "2019-08-23";
            var act = "TextToVoice";
            var region = "ap-shanghai";
            var endpoint = "tts.tencentcloudapi.com";

            // 实例化一个认证对象，入参需要传入腾讯云账户 SecretId 和 SecretKey
            var cred = new Credential
            {

            };

            var hpf = new HttpProfile
            {
                ReqMethod = "POST",
                Endpoint = endpoint,
            };
            var cpf = new ClientProfile(ClientProfile.SIGN_TC3SHA256, hpf);

            var client = new CommonClient(mod, ver, cred, region, cpf);
            if (text.Length >= 490)
            {
                text = text.Substring(0, 490);
            }
            var param = new JObject
            {
                { "Text", text },
                { "SessionId","1234" },
                { "VoiceType", 601000 } // 指定语音类型
            }.ToString();

            var req = new CommonRequest(param);
            var resp = client.Call(req, act);

            var jsonResponse = JObject.Parse(resp);
            string audioBase64 = jsonResponse["Response"]["Audio"].ToString(); // 获取生成的音频 Base64 数据

            // 解码 Base64 数据并播放
            byte[] audioData = Convert.FromBase64String(audioBase64);
            StartCoroutine(PlayAudioFromBytes(audioData));
        }
        catch (Exception e)
        {
            Debug.Log("TTS API call error: " + e.ToString());
        }
    }

    /*
     * 播放从字节数组中生成的音频
     */
    IEnumerator PlayAudioFromBytes(byte[] audioData)
    {
        // 将字节数组写入到临时文件
        string tempFilePath = Path.Combine(Application.persistentDataPath, "tts_audio.wav");
        File.WriteAllBytes(tempFilePath, audioData);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error playing audio: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
                else
                {
                    Debug.Log("Failed to load audio clip.");
                }
            }
        }
    }
}
