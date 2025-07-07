using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
using System.Net.Http;
using TMPro;
using System;
using System.Threading.Tasks;

public class Composition : MonoBehaviour
{
    // UI���
    public Button compositionButton;        // �������İ�ť
    public Image botResponseImage;          // ��������ͼ�� Image ���
    public TMP_Text botResponseText;        // ��ģ�͵Ļظ��ı���

    // ��˽ӿڵ�ַ
    private string apiUrl = "http://49.52.27.216:5000/composition/get";

    void Start()
    {
        // ���س�ʼUIԪ��
        botResponseImage.gameObject.SetActive(false);  // ��������ͼ
        botResponseText.gameObject.SetActive(false);    // ���ػظ��ı���

        // ��Ӽ����¼�
        compositionButton.onClick.AddListener(OnCompositionButtonClick);
    }

    /*
     * ����������İ�ť����߼�
     */
    void OnCompositionButtonClick()
    {
#if UNITY_EDITOR
        // �༭����ʹ�� EditorUtility ѡ��ͼƬ
        string imagePath = UnityEditor.EditorUtility.OpenFilePanel("ѡ��һ��ͼƬ", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(imagePath))
        {
            // �����������Ľӿ�
            StartCoroutine(UploadImageForComposition(imagePath));
            LoadCompositionThumbnail(imagePath);
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
                // �����������Ľӿ�
                StartCoroutine(UploadImageForComposition(path));
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
     * �ϴ�ͼƬ�������������Ľӿ�
     */
    IEnumerator UploadImageForComposition(string imagePath)
    {
        // ��ʾ����ͼ���ı�����ʾ�û��ȴ��ظ�
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Upload Image and Await Essay Correction";

        // ��ͼƬת��Ϊ Base64 ����
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string imageBase64 = Convert.ToBase64String(imageBytes);

        // ���� JSON ����
        var jsonData = new JObject
        {
            { "image_base64", imageBase64 }
        };

        string responseBody = null;

        // ʹ�� HttpClient ���� POST ����
        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(jsonData.ToString(), Encoding.UTF8, "application/json");

            // �����첽 POST ����
            Task<HttpResponseMessage> task = client.PostAsync(apiUrl, jsonContent);

            // �ȴ��������
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsCompletedSuccessfully)
            {
                HttpResponseMessage response = task.Result;
                if (response.IsSuccessStatusCode)
                {
                    Task<string> readTask = response.Content.ReadAsStringAsync();
                    while (!readTask.IsCompleted)
                    {
                        yield return null;
                    }

                    if (readTask.IsCompletedSuccessfully)
                    {
                        responseBody = readTask.Result;
                        Debug.Log("�������Ľӿ���Ӧ��" + responseBody);

                        try
                        {
                            var responseJson = JObject.Parse(responseBody);
                            string returnedImageBase64 = responseJson["image_base64"]?.ToString();
                            string message = responseJson["message"]?.ToString();

                            if (!string.IsNullOrEmpty(returnedImageBase64))
                            {
                                // ��ʾ���ص�ͼƬ
                                StartCoroutine(DisplayReturnedCompositionImage(returnedImageBase64));
                            }
                            else
                            {
                                Debug.Log("���ص�ͼƬ Base64 Ϊ�ա�");
                                botResponseText.text = "δ�յ�������ͼƬ��";
                            }
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
                    Debug.Log($"������������ʧ��: {response.StatusCode} {response.ReasonPhrase}");
                    botResponseText.text = $"����ʧ�ܣ�{response.ReasonPhrase}";
                }
            }
            else
            {
                Debug.Log("������������ʧ�ܣ�����δ�ɹ���ɡ�");
                botResponseText.text = "����ʧ�ܣ��޷����ӵ���������";
            }
        }

        yield return null;
    }

    /*
     * ���ز���ʾ�ϴ�������ͼ��ͨ�� ThumbnailManager��
     */
    void LoadCompositionThumbnail(string imagePath)
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
     * ��ʾ���ص�ͼƬ����������ģ�飩
     */
    IEnumerator DisplayReturnedCompositionImage(string imageBase64)
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

            botResponseText.text = "Essay Correction Completed. Please check the thumbnail in the top left corner.";
        }
        else
        {
            Debug.Log("�޷����ط��ص� Base64 ͼƬ��");
            botResponseText.text = "�޷����ط��ص�ͼƬ��";
        }

        yield return null;
    }
}
