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
        // ��������
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    /*
     * ʹ����Ѷ TTS API ���������ϳ�
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

            // ʵ����һ����֤���������Ҫ������Ѷ���˻� SecretId �� SecretKey
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
                { "VoiceType", 601000 } // ָ����������
            }.ToString();

            var req = new CommonRequest(param);
            var resp = client.Call(req, act);

            var jsonResponse = JObject.Parse(resp);
            string audioBase64 = jsonResponse["Response"]["Audio"].ToString(); // ��ȡ���ɵ���Ƶ Base64 ����

            // ���� Base64 ���ݲ�����
            byte[] audioData = Convert.FromBase64String(audioBase64);
            StartCoroutine(PlayAudioFromBytes(audioData));
        }
        catch (Exception e)
        {
            Debug.Log("TTS API call error: " + e.ToString());
        }
    }

    /*
     * ���Ŵ��ֽ����������ɵ���Ƶ
     */
    IEnumerator PlayAudioFromBytes(byte[] audioData)
    {
        // ���ֽ�����д�뵽��ʱ�ļ�
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
