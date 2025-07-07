using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Tsi.V20210325.Models;
using TMPro;
using UnityEngine;

public class LLM : MonoBehaviour
{
    private string model_language = "http://49.52.27.74:11434/api/generate"; // ���ش�����ģ��api
    public TMP_Text botResponseText;        // �ظ��ı���
    public TTS tts;

    /*
     * ��ת�õ����ַ������ش�����ģ�ͣ��õ��ظ�
     */
    public IEnumerator SendRequestToChatbot(string userInput)
    {
        // ׼����������
        var payload = new JObject
        {
            { "model", "qwen2.5:32b" },
            { "prompt", "��Ӣ�Ļظ���������ݣ��ظ����Ȳ�����120�����ʣ����ݣ�" + userInput },
            { "stream", false }
        };

        // ����HTTP����
        HttpResponseMessage response = null;
        string responseBody = null;

        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            // ��������
            Task<HttpResponseMessage> task = client.PostAsync(model_language, jsonContent);
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {
                response = task.Result;
            }
            catch (HttpRequestException e)
            {
                botResponseText.text = "Request Error: " + e.Message;
                yield break;
            }
        }

        if (response != null && response.IsSuccessStatusCode)
        {
            Task<string> responseTask = response.Content.ReadAsStringAsync();
            yield return new WaitUntil(() => responseTask.IsCompleted);

            responseBody = responseTask.Result;

            try
            {
                var jsonResponse = JObject.Parse(responseBody);
                string botResponse = jsonResponse["response"]?.ToString();
                botResponseText.text = botResponse;

                // ���� CozyVoice ��������
                //StartCoroutine(GenerateVoice(botResponse));


                Debug.Log("������ģ�͵Ļظ�: " + botResponse);

                tts.SpeakWithTencentTTS(botResponse);
            }
            catch (System.Exception e)
            {
                botResponseText.text = "Error parsing response: " + e.Message;
            }
        }
        else
        {
            botResponseText.text = "Error: Unable to contact chatbot";
        }
    }

    /*
    * ��ת�õ����ַ������ش�����ģ�ͣ��õ��ظ�(����)
    */
    public IEnumerator SendRequestToChatbot_translate(string description)
    {
        // ׼����������
        var payload = new JObject
        {
            { "model", "qwen2.5:32b" },
            { "prompt", "������������������Ӣ�﷭�룬��ס��һ��Ҫ���ش�Ӣ�ģ�����һ��������70�ʣ���ס�ˣ���Ҫ����70���ʣ�������"+description },
            { "stream", false }
        };

        // ����HTTP����
        HttpResponseMessage response = null;
        string responseBody = null;

        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            // ��������
            Task<HttpResponseMessage> task = client.PostAsync(model_language, jsonContent);
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {
                response = task.Result;
            }
            catch (HttpRequestException e)
            {
                botResponseText.text = "Request Error: " + e.Message;
                yield break;
            }
        }

        if (response != null && response.IsSuccessStatusCode)
        {
            Task<string> responseTask = response.Content.ReadAsStringAsync();
            yield return new WaitUntil(() => responseTask.IsCompleted);

            responseBody = responseTask.Result;

            try
            {
                var jsonResponse = JObject.Parse(responseBody);
                string botResponse = jsonResponse["response"]?.ToString();
                botResponseText.text = botResponse;
                Debug.Log("ͼ����������Ӣ�ķ���Ϊ" + botResponse);
                // ���� CozyVoice ��������
                //StartCoroutine(GenerateVoice(botResponse));


                tts.SpeakWithTencentTTS(botResponse);
            }
            catch (System.Exception e)
            {
                botResponseText.text = "Error parsing response: " + e.Message;
            }
        }
        else
        {
            botResponseText.text = "Error: Unable to contact chatbot";
        }
    }

}
