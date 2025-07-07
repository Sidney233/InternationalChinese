using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro; // ��� TextMeshPro �����ռ�
using System;
using UnityEditor;

public class Image2Text : MonoBehaviour
{

    public Button image2TextButton;       // ͼ���İ�ť
    public Image botResponseImage;       // ��������ͼ�� Image ���
    public TMP_Text botResponseText;     // ��ģ�͵Ļظ��ı���
    private string model_image = "http://49.52.27.74:11434/api/generate"; // ����ͼ���Ĵ�ģ�� API ��ַ
    public TTS tts;
    public LLM llm;

    //����ͼ��������ã��ѹ�����ͨ�� ThumbnailManager ����
    // public Button thumbnailButton;         // ����ͼ Button ���
    // public Image thumbnailImage;           // ����ͼ Image �����Button �� Image��
    // public GameObject imagePopupPanel;     // ��ͼ���� Panel
    // public Image fullImage;                // ��ͼ Image ���
    // public Button closeButton;             // �رյ������ڵ� Button
    // private Sprite currentThumbnailSprite; // ��ǰ����ͼ�� Sprite

    void Start()
    {
        // ��������ͼ�ͻظ��ı���
        botResponseImage.gameObject.SetActive(false);
        botResponseText.gameObject.SetActive(false);

        // Ϊͼ���İ�ť��Ӽ����¼�
        image2TextButton.onClick.AddListener(OnImage2TextButtonClick);

        // ����Ҫ���� thumbnailButton �� imagePopupPanel������ ThumbnailManager ����
    }

    /*
     * ���ͼ���İ�ť����߼�
     */
    void OnImage2TextButtonClick()
    {
#if UNITY_EDITOR
        // �༭����ʹ�� EditorUtility ѡ��ͼƬ
        string imagePath = UnityEditor.EditorUtility.OpenFilePanel("ѡ��һ��ͼƬ", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(imagePath))
        {
            // ����ͼ���Ĵ�ģ��
            StartCoroutine(UploadImageForDescription(imagePath));
            // ���ز���ʾ����ͼ
            LoadThumbnail(imagePath);
        }
        else
        {
            Debug.Log("δѡ��ͼƬ");
        }
#else
        // ���ƶ��豸��ʹ�� Native Gallery ѡ��ͼƬ
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // ����ͼ���Ĵ�ģ��
                StartCoroutine(UploadImageForDescription(path));
                // ���ز���ʾ����ͼ
                LoadThumbnail(path);
            }
            else
            {
                Debug.Log("δѡ��ͼƬ");
            }
        }, "ѡ��һ��ͼƬ", "image/png,image/jpg,image/jpeg");

        Debug.Log("ѡ��ͼƬȨ��״̬: " + permission);
#endif
    }

    /*
     * ����ͼ���Ĵ�ģ��
     */
    IEnumerator UploadImageForDescription(string imagePath)
    {
        // ����ļ��Ƿ����
        if (!File.Exists(imagePath))
        {
            Debug.Log("�ļ������ڣ�" + imagePath);
            yield break;
        }

        // ��ȡͼƬ������Ϊ base64
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64Image = Convert.ToBase64String(imageBytes);

        // ������������
        var jsonData = new JObject
        {
            { "model", "minicpm-v" },
            //{ "prompt", "������������ͼƬ��������80����" },
            { "prompt", "please describe this picture in english and in 80 words" },
            { "stream", false },
            { "images", new JArray(base64Image) }
        };

        string responseBody = null;
        HttpResponseMessage response = null;

        // ��ʾ����ͼ���ı�����ʾ�û��ȴ��ظ�
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Please wait for the description generated from the image";

        // ʹ��HttpClient��������
        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(jsonData.ToString(), Encoding.UTF8, "application/json");

            // �����첽 POST ����
            Task<HttpResponseMessage> task = client.PostAsync(model_image, jsonContent);

            while (!task.IsCompleted) // �ȴ��������
            {
                yield return null;
            }

            if (task.IsCompletedSuccessfully)
            {
                response = task.Result;
            }
            else
            {
                Debug.Log("����ʧ�ܣ�" + task.Exception?.Message);
                botResponseText.text = "����ʧ�ܣ��޷����ӵ���������";
                yield break;
            }
        }

        if (response != null && response.IsSuccessStatusCode)
        {
            Task<string> responseTask = response.Content.ReadAsStringAsync();

            while (!responseTask.IsCompleted) // �ȴ��������
            {
                yield return null;
            }

            if (responseTask.IsCompletedSuccessfully)
            {
                responseBody = responseTask.Result;

                try
                {
                    var responseJson = JObject.Parse(responseBody);
                    string description = responseJson["response"]?.ToString();
                    Debug.Log("ͼ��������Ϊ��" + description);
                    // ���ı�������ʾ�������

                    // �����ı�������
                    botResponseText.text = description;
                    //StartCoroutine(llm.SendRequestToChatbot_translate(description));
                    tts.SpeakWithTencentTTS(description);
                }
                catch (Exception e)
                {
                    Debug.Log("������Ӧʱ����" + e.ToString());
                    botResponseText.text = "������Ӧʱ����";
                }
            }
            else
            {
                Debug.Log("��ȡ��Ӧ����ʱ����");
                botResponseText.text = "��ȡ��Ӧ����ʱ����";
            }
        }
        else
        {
            Debug.Log($"����ʧ��: {response?.StatusCode} {response?.ReasonPhrase}");
            botResponseText.text = "����ʧ�ܣ�" + response?.ReasonPhrase;
        }
    }

    /*
     * ���ز���ʾ����ͼ��ͨ�� ThumbnailManager��
     */
    void LoadThumbnail(string imagePath)
    {
        // ����ͼƬΪ Texture2D
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes))
        {
            // ���� Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // ʹ�� ThumbnailManager ��������ͼ
            ThumbnailManager.Instance.SetThumbnail(sprite);
        }
        else
        {
            Debug.Log("�޷�����ͼƬ��Ϊ����ͼ��");
        }
    }

    /*
     * ��ʾ���ص�ͼƬ��ͼ����ģ�飩
     */
    IEnumerator DisplayReturnedImage2TextImage(string imageBase64)
    {
        // ���� Base64 ͼƬ
        byte[] imageBytes = Convert.FromBase64String(imageBase64);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes))
        {
            // ���� Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // ʹ�� ThumbnailManager ��������ͼ
            ThumbnailManager.Instance.SetThumbnail(sprite);

            botResponseText.text = "Description Completed";
        }
        else
        {
            Debug.Log("�޷����ط��ص� Base64 ͼƬ��");
            botResponseText.text = "�޷����ط��ص�ͼƬ��";
        }

        yield return null;
    }
}
