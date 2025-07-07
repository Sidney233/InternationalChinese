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
    // UI组件
    public Button compositionButton;        // 作文批改按钮
    public Image botResponseImage;          // 引用气泡图的 Image 组件
    public TMP_Text botResponseText;        // 大模型的回复文本框

    // 后端接口地址
    private string apiUrl = "http://49.52.27.216:5000/composition/get";

    void Start()
    {
        // 隐藏初始UI元素
        botResponseImage.gameObject.SetActive(false);  // 隐藏气泡图
        botResponseText.gameObject.SetActive(false);    // 隐藏回复文本框

        // 添加监听事件
        compositionButton.onClick.AddListener(OnCompositionButtonClick);
    }

    /*
     * 点击作文批改按钮后的逻辑
     */
    void OnCompositionButtonClick()
    {
#if UNITY_EDITOR
        // 编辑器中使用 EditorUtility 选择图片
        string imagePath = UnityEditor.EditorUtility.OpenFilePanel("选择一张图片", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(imagePath))
        {
            // 调用作文批改接口
            StartCoroutine(UploadImageForComposition(imagePath));
            LoadCompositionThumbnail(imagePath);
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
                // 调用作文批改接口
                StartCoroutine(UploadImageForComposition(path));
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
     * 上传图片并调用作文批改接口
     */
    IEnumerator UploadImageForComposition(string imagePath)
    {
        // 显示气泡图和文本框，提示用户等待回复
        botResponseImage.gameObject.SetActive(true);
        botResponseText.gameObject.SetActive(true);
        botResponseText.text = "Upload Image and Await Essay Correction";

        // 将图片转换为 Base64 编码
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string imageBase64 = Convert.ToBase64String(imageBytes);

        // 创建 JSON 数据
        var jsonData = new JObject
        {
            { "image_base64", imageBase64 }
        };

        string responseBody = null;

        // 使用 HttpClient 发送 POST 请求
        using (HttpClient client = new HttpClient())
        {
            var jsonContent = new StringContent(jsonData.ToString(), Encoding.UTF8, "application/json");

            // 发送异步 POST 请求
            Task<HttpResponseMessage> task = client.PostAsync(apiUrl, jsonContent);

            // 等待请求完成
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
                        Debug.Log("作文批改接口响应：" + responseBody);

                        try
                        {
                            var responseJson = JObject.Parse(responseBody);
                            string returnedImageBase64 = responseJson["image_base64"]?.ToString();
                            string message = responseJson["message"]?.ToString();

                            if (!string.IsNullOrEmpty(returnedImageBase64))
                            {
                                // 显示返回的图片
                                StartCoroutine(DisplayReturnedCompositionImage(returnedImageBase64));
                            }
                            else
                            {
                                Debug.Log("返回的图片 Base64 为空。");
                                botResponseText.text = "未收到处理后的图片。";
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log("解析响应时出错：" + e.ToString());
                            botResponseText.text = "解析响应时出错。";
                        }
                    }
                    else
                    {
                        Debug.Log("读取响应内容时出错。");
                        botResponseText.text = "读取响应内容时出错。";
                    }
                }
                else
                {
                    Debug.Log($"作文批改请求失败: {response.StatusCode} {response.ReasonPhrase}");
                    botResponseText.text = $"请求失败：{response.ReasonPhrase}";
                }
            }
            else
            {
                Debug.Log("作文批改请求失败：任务未成功完成。");
                botResponseText.text = "请求失败：无法连接到服务器。";
            }
        }

        yield return null;
    }

    /*
     * 加载并显示上传的缩略图（通过 ThumbnailManager）
     */
    void LoadCompositionThumbnail(string imagePath)
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
     * 显示返回的图片（作文批改模块）
     */
    IEnumerator DisplayReturnedCompositionImage(string imageBase64)
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

            botResponseText.text = "Essay Correction Completed. Please check the thumbnail in the top left corner.";
        }
        else
        {
            Debug.Log("无法加载返回的 Base64 图片。");
            botResponseText.text = "无法加载返回的图片。";
        }

        yield return null;
    }
}
