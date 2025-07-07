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
using UnityEditor;

public class Image2Text : MonoBehaviour
{

    public Button image2TextButton;       // 图生文按钮
    public Image botResponseImage;       // 引用气泡图的 Image 组件
    public TMP_Text botResponseText;     // 大模型的回复文本框
    private string model_image = "http://49.52.27.74:11434/api/generate"; // 本地图生文大模型 API 地址
    public TTS tts;
    public LLM llm;

    //缩略图的组件引用（已共享，需通过 ThumbnailManager 处理）
    // public Button thumbnailButton;         // 缩略图 Button 组件
    // public Image thumbnailImage;           // 缩略图 Image 组件（Button 的 Image）
    // public GameObject imagePopupPanel;     // 大图弹出 Panel
    // public Image fullImage;                // 大图 Image 组件
    // public Button closeButton;             // 关闭弹出窗口的 Button
    // private Sprite currentThumbnailSprite; // 当前缩略图的 Sprite

    void Start()
    {
        // 隐藏气泡图和回复文本框
        botResponseImage.gameObject.SetActive(false);
        botResponseText.gameObject.SetActive(false);

        // 为图生文按钮添加监听事件
        image2TextButton.onClick.AddListener(OnImage2TextButtonClick);

        // 不需要管理 thumbnailButton 和 imagePopupPanel，这由 ThumbnailManager 负责
    }

    /*
     * 点击图生文按钮后的逻辑
     */
    void OnImage2TextButtonClick()
    {
#if UNITY_EDITOR
        // 编辑器中使用 EditorUtility 选择图片
        string imagePath = UnityEditor.EditorUtility.OpenFilePanel("选择一张图片", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(imagePath))
        {
            // 调用图生文大模型
            StartCoroutine(UploadImageForDescription(imagePath));
            // 加载并显示缩略图
            LoadThumbnail(imagePath);
        }
        else
        {
            Debug.Log("未选择图片");
        }
#else
        // 在移动设备上使用 Native Gallery 选择图片
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // 调用图生文大模型
                StartCoroutine(UploadImageForDescription(path));
                // 加载并显示缩略图
                LoadThumbnail(path);
            }
            else
            {
                Debug.Log("未选择图片");
            }
        }, "选择一张图片", "image/png,image/jpg,image/jpeg");

        Debug.Log("选择图片权限状态: " + permission);
#endif
    }

    /*
     * 调用图生文大模型
     */
    IEnumerator UploadImageForDescription(string imagePath)
    {
        // 检查文件是否存在
        if (!File.Exists(imagePath))
        {
            Debug.Log("文件不存在：" + imagePath);
            yield break;
        }

        // 读取图片并编码为 base64
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64Image = Convert.ToBase64String(imageBytes);

        // 创建请求数据
        var jsonData = new JObject
        {
            { "model", "minicpm-v" },
            //{ "prompt", "简略描述这张图片，不超过80个词" },
            { "prompt", "please describe this picture in english and in 80 words" },
            { "stream", false },
            { "images", new JArray(base64Image) }
        };

        string responseBody = null;
        HttpResponseMessage response = null;

        // 显示气泡图和文本框，提示用户等待回复
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Please wait for the description generated from the image";

        // 使用HttpClient发送请求
        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(jsonData.ToString(), Encoding.UTF8, "application/json");

            // 发送异步 POST 请求
            Task<HttpResponseMessage> task = client.PostAsync(model_image, jsonContent);

            while (!task.IsCompleted) // 等待任务完成
            {
                yield return null;
            }

            if (task.IsCompletedSuccessfully)
            {
                response = task.Result;
            }
            else
            {
                Debug.Log("请求失败：" + task.Exception?.Message);
                botResponseText.text = "请求失败：无法连接到服务器。";
                yield break;
            }
        }

        if (response != null && response.IsSuccessStatusCode)
        {
            Task<string> responseTask = response.Content.ReadAsStringAsync();

            while (!responseTask.IsCompleted) // 等待任务完成
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
                    Debug.Log("图生文描述为：" + description);
                    // 在文本框中显示描述结果

                    // 更新文本框内容
                    botResponseText.text = description;
                    //StartCoroutine(llm.SendRequestToChatbot_translate(description));
                    tts.SpeakWithTencentTTS(description);
                }
                catch (Exception e)
                {
                    Debug.Log("解析响应时出错：" + e.ToString());
                    botResponseText.text = "解析响应时出错。";
                }
            }
            else
            {
                Debug.Log("获取响应内容时出错。");
                botResponseText.text = "获取响应内容时出错。";
            }
        }
        else
        {
            Debug.Log($"请求失败: {response?.StatusCode} {response?.ReasonPhrase}");
            botResponseText.text = "请求失败：" + response?.ReasonPhrase;
        }
    }

    /*
     * 加载并显示缩略图（通过 ThumbnailManager）
     */
    void LoadThumbnail(string imagePath)
    {
        // 加载图片为 Texture2D
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes))
        {
            // 创建 Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // 使用 ThumbnailManager 设置缩略图
            ThumbnailManager.Instance.SetThumbnail(sprite);
        }
        else
        {
            Debug.Log("无法加载图片作为缩略图。");
        }
    }

    /*
     * 显示返回的图片（图生文模块）
     */
    IEnumerator DisplayReturnedImage2TextImage(string imageBase64)
    {
        // 解码 Base64 图片
        byte[] imageBytes = Convert.FromBase64String(imageBase64);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageBytes))
        {
            // 创建 Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            // 使用 ThumbnailManager 设置缩略图
            ThumbnailManager.Instance.SetThumbnail(sprite);

            botResponseText.text = "Description Completed";
        }
        else
        {
            Debug.Log("无法加载返回的 Base64 图片。");
            botResponseText.text = "无法加载返回的图片。";
        }

        yield return null;
    }
}
