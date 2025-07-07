using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using LitJson;
using static DataStructure;
using TMPro;
using UnityEngine.Video;
using System.Net.NetworkInformation;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.SceneManagement;

public class WriteBoardManager : MonoBehaviour
{
    public GameObject panel;
    //笔的颜色
    public Color32 penColor = Color.black;
    //笔的宽度
    public int penWidth = 3;
    public Slider slider;//进度条
    public TextMeshProUGUI indexText;//当前题号
    public TextMeshProUGUI allIndex;//总题数
    public Button skipBtn;//跳过按钮
    public Button favoriteBtn;//收藏按钮
    public GameObject scorePanel;
    public TextMeshProUGUI chinese_character;
    public TextMeshProUGUI pinyin;
    public TextMeshProUGUI definition;
    public TextMeshProUGUI chinese_character_video;
    public GameObject videoImage;
    public Button checkBtn;
    public GameObject loading;
    public string jsonName;
    private Sprite drawSprite;
    private VideoPlayer videoPlayer;
    //画的纹理
    private Texture2D drawableTexture2D;
    //之前画的位置
    private Vector2 previousDragPosition = Vector2.zero;
    //存放最初的颜色数组
    private Color32[] orignalColorArray;
    //存放目前的颜色数组
    private Color32[] currentColorArray;
    //存放画的前一张图，用于撤回
    private Color32[] previousColorArray;
    //图像的宽高
    private int spriteHeight;
    private int spriteWidth;
    //图片实际大小
    private int transformHeight;
    private int transformWidth;
    //当前是否在滑动
    private bool isDraging = false;
    //保存画过的点坐标
    private List<List<Vector2>> drawLines;
    private List<Vector2> drawLine;
    //题目
    private Writing[] writings;
    private int currentIndex;
    private string curVideoPath;
    private int currentUnitNumber = 4;

    // Start is called before the first frame update
    void Start()
    {
        drawLines = new List<List<Vector2>>();
        drawSprite = GetComponent<Image>().sprite;
        drawableTexture2D = drawSprite.texture;
        orignalColorArray = drawableTexture2D.GetPixels32();
        //当前sprite各个像素的颜色
        currentColorArray = drawableTexture2D.GetPixels32();

        spriteHeight = (int)drawSprite.rect.height;
        spriteWidth = (int)drawSprite.rect.width;

        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        transformHeight = (int)rectTransform.rect.height;
        transformWidth = (int)rectTransform.rect.width;
        drawLine = new List<Vector2>();
        videoPlayer = videoImage.GetComponent<VideoPlayer>();
        ReadJson();
        LoadReading(currentIndex);
    }

    // Update is called once per frame
    void Update()
    {
        // 监听返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClick();
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
            if (hit.collider != null)
            {
                if (isDraging == false)
                {//刚按下鼠标左键，还没拖动
                    previousColorArray = drawableTexture2D.GetPixels32();
                }
                //正在滑动
                isDraging = true;
                Vector2 pixelPos = WorldToPixelCoordinates(mouseWorldPosition);
                //如果是0,0点就说明是重新开始画，就不用从之前的点lerp了
                if (previousDragPosition == Vector2.zero)
                {
                    previousDragPosition = pixelPos;
                }
                //计算当前的位置和上一次记录的位置之间的距离，然后平滑的画，这是为了防止鼠标移动太快，画的点不连续
                float distance = Vector2.Distance(previousDragPosition, pixelPos);
                float steps = 1 / distance;
                for (float lerp = 0; lerp <= 1; lerp += steps)
                {
                    //插值
                    Vector2 curPosition = Vector2.Lerp(previousDragPosition, pixelPos, lerp);
                    //保存坐标
                    if (!drawLine.Contains(curPosition)) 
                    {
                        drawLine.Add(curPosition);
                    }
                    //画
                    PenDraw(curPosition);
                }
                previousDragPosition = pixelPos;
                //设置当前显示图片
                drawableTexture2D.SetPixels32(currentColorArray);
                drawableTexture2D.Apply();
            }
        }
        else
        {   //鼠标没有按住
            if (drawLine.Count > 0) 
            {
                drawLines.Add(drawLine);
                drawLine = new List<Vector2>();
            }
            previousDragPosition = Vector2.zero;
        }
    }
    /// <summary>
    /// 读取JSON文件
    /// </summary>
    private void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>(jsonName);
        string jsonFileContent = jsonTextAsset.text;
        writings = JsonMapper.ToObject<Writing[]>(jsonFileContent);
        allIndex.text = writings.Length.ToString();
        currentIndex = 0;
    }
    private void LoadReading(int index)
    {
        indexText.text = (index + 1).ToString();
        slider.value = (index + 1) / (float)writings.Length;
        chinese_character.text = writings[index].chinese_character;
        chinese_character_video.text = writings[index].chinese_character;
        pinyin.text = writings[index].pinyin;
        definition.text = writings[index].definition;
        curVideoPath = "http://49.52.27.216" + writings[index].guide.Remove(0, 1);
        videoPlayer.url = curVideoPath;
    }
    private void PenDraw(Vector2 pixelPos)
    {
        //笔刷中心位置X
        int centerX = (int)pixelPos.x;
        //笔刷中心位置Y
        int centerY = (int)pixelPos.y;
        //遍历笔刷范围，边界外不绘制
        for (int x = centerX - penWidth; x <= centerX + penWidth; x++)
        {
            for (int y = centerY - penWidth; y <= centerY + penWidth; y++)
            {
                if (x >= spriteWidth || x < 0 ||
                    y >= spriteHeight || y < 0)
                    continue;
                int arrayPos = y * (int)drawSprite.rect.width + x;
                currentColorArray[arrayPos] = penColor;
            }
        }
    }
    /// <summary>
    /// 将世界坐标转换为图片上的像素坐标，要考虑到图片缩放
    /// </summary>
    /// <param name="mouseWorldPosition"></param>
    /// <returns></returns>
    private Vector2 WorldToPixelCoordinates(Vector3 mouseWorldPosition)
    {
        //localPos 为当前缩放程度的图片中鼠标点击的位置，是以图片中心为坐标原点的坐标，要转换成以原图大小并且以图片左下角为坐标原点的坐标
        Vector2 localPos = transform.InverseTransformPoint(mouseWorldPosition);
        //转换坐标系
        float transformX = localPos.x + transformWidth / 2;
        float transformY = localPos.y + transformHeight / 2;
        //计算缩放比例
        float scaleX = (float)spriteWidth / transformWidth;
        float scaleY = (float)spriteHeight / transformHeight;
        //得出最后计算的下笔位置
        float centeredX = transformX * scaleX;
        float centeredY = transformY * scaleY;
        Vector2 pixelPos = new Vector2(Mathf.RoundToInt(centeredX), Mathf.RoundToInt(centeredY));
        return pixelPos;
    }
    public void OnClickLearn()
    {
        panel.SetActive(true);
        videoPlayer.Play();
    }
    public void OnClickPost()
    {
        byte[] bytes = drawableTexture2D.EncodeToPNG();
        checkBtn.gameObject.SetActive(false);
        loading.SetActive(true);
        StartCoroutine(Post(bytes));

    }
    public void OnClickClear()
    {
        drawableTexture2D.SetPixels32(orignalColorArray);
        previousColorArray = drawableTexture2D.GetPixels32();
        currentColorArray = drawableTexture2D.GetPixels32();
        drawableTexture2D.Apply();
        drawLines.Clear();
    }
    public void OnClickVideoClose()
    {
        videoPlayer.Stop();
        panel.SetActive(false);
    }
    public void OnNextClick()
    {
        if (currentIndex == writings.Length - 1)
        {
            OnCompleteLearningButtonClicked();
        }
        else
        {
            scorePanel.SetActive(false);
            currentIndex = currentIndex + 1;
            OnClickClear();
            LoadReading(currentIndex);
        }
    }
    public void OnCompleteLearningButtonClicked()
    {
        PlayerPrefs.SetInt("lesson" + currentUnitNumber + "State", (int)UnitState.Completed);
        if (currentUnitNumber < 4)
        {
            PlayerPrefs.SetInt("lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        }
        PlayerPrefs.Save();
        Debug.Log("Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit");
    }
    /// <summary>
    /// 销毁前恢复为原来的图像并保存绘制完成的图像
    /// </summary>
    protected void OnDestroy()
    {
        drawableTexture2D.SetPixels32(orignalColorArray);
        drawableTexture2D.Apply();
    }

    /// <summary>
    /// 开启一个协程，发送请求
    /// </summary>
    /// <returns></returns>
    private IEnumerator Post(byte[] myData)
    {
        string linesJson = "{\"lines\": [";
        foreach (var line in drawLines)
        {
            linesJson += "[";
            foreach (var point in line)
            {
                linesJson += point.x + "," + point.y + ",";
            }
            linesJson = linesJson.TrimEnd(',');
            linesJson += "],";
        }
        linesJson = linesJson.TrimEnd(',');
        linesJson += "]}";
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("userid", "test"));
        formData.Add(new MultipartFormDataSection("lines_json", linesJson));
        formData.Add(new MultipartFormFileSection("png", myData, "myData.png", "image/png"));

        UnityWebRequest webRequest = UnityWebRequest.Post("http://49.52.27.216:5001/user/insert_character_img", formData);

        yield return webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
            checkBtn.gameObject.SetActive(true);
            loading.SetActive(false);
        }
        else
        {
            Debug.Log("发送成功");
             checkBtn.gameObject.SetActive(true);
            loading.SetActive(false);
            scorePanel.SetActive(true);
        }
    }
    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit");
    }
}
