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
    private string model_language = "http://49.52.27.74:11434/api/generate"; // 本地大语言模型api
    public TMP_Text botResponseText;        // 回复文本框
    public TTS tts;

    /*
     * 将转好的文字发给本地大语言模型，得到回复
     */
    public IEnumerator SendRequestToChatbot(string userInput)
    {
        // 准备请求数据
        var payload = new JObject
        {
            { "model", "qwen2.5:32b" },
            { "prompt", "用英文回复下面的内容，回复长度不超过120个单词，内容：" + userInput },
            { "stream", false }
        };

        // 创建HTTP请求
        HttpResponseMessage response = null;
        string responseBody = null;

        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            // 发送请求
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

                // 调用 CozyVoice 生成语音
                //StartCoroutine(GenerateVoice(botResponse));


                Debug.Log("大语言模型的回复: " + botResponse);

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
    * 将转好的文字发给本地大语言模型，得到回复(翻译)
    */
    public IEnumerator SendRequestToChatbot_translate(string description)
    {
        // 准备请求数据
        var payload = new JObject
        {
            { "model", "qwen2.5:32b" },
            { "prompt", "给我生成下述描述的英语翻译，记住，一定要返回纯英文，字数一定不超过70词，记住了，不要超过70单词，描述："+description },
            { "stream", false }
        };

        // 创建HTTP请求
        HttpResponseMessage response = null;
        string responseBody = null;

        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

            // 发送请求
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
                Debug.Log("图生文描述的英文翻译为" + botResponse);
                // 调用 CozyVoice 生成语音
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
