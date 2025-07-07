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

public class WriteBoardManager2 : MonoBehaviour
{
    public GameObject panel;
    //�ʵ���ɫ
    public Color32 penColor = Color.black;
    //�ʵĿ���
    public int penWidth = 3;
    public Slider slider;//������
    public TextMeshProUGUI indexText;//��ǰ���
    public TextMeshProUGUI allIndex;//������
    public Button skipBtn;//������ť
    public Button favoriteBtn;//�ղذ�ť
    public GameObject scorePanel;
    public TextMeshProUGUI chinese_character;
    public TextMeshProUGUI pinyin;
    public TextMeshProUGUI definition;
    public TextMeshProUGUI chinese_character_video;
    public GameObject videoImage;
    public Button checkBtn;
    public GameObject loading;
    private Sprite drawSprite;
    private VideoPlayer videoPlayer;
    //��������
    private Texture2D drawableTexture2D;
    //֮ǰ����λ��
    private Vector2 previousDragPosition = Vector2.zero;
    //����������ɫ����
    private Color32[] orignalColorArray;
    //���Ŀǰ����ɫ����
    private Color32[] currentColorArray;
    //��Ż���ǰһ��ͼ�����ڳ���
    private Color32[] previousColorArray;
    //ͼ��Ŀ���
    private int spriteHeight;
    private int spriteWidth;
    //ͼƬʵ�ʴ�С
    private int transformHeight;
    private int transformWidth;
    //��ǰ�Ƿ��ڻ���
    private bool isDraging = false;
    //���滭���ĵ�����
    private List<List<Vector2>> drawLines;
    private List<Vector2> drawLine;
    //��Ŀ
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
        //��ǰsprite�������ص���ɫ
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
        // �������ؼ�
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
                {//�հ�������������û�϶�
                    previousColorArray = drawableTexture2D.GetPixels32();
                }
                //���ڻ���
                isDraging = true;
                Vector2 pixelPos = WorldToPixelCoordinates(mouseWorldPosition);
                //�����0,0���˵�������¿�ʼ�����Ͳ��ô�֮ǰ�ĵ�lerp��
                if (previousDragPosition == Vector2.zero)
                {
                    previousDragPosition = pixelPos;
                }
                //���㵱ǰ��λ�ú���һ�μ�¼��λ��֮��ľ��룬Ȼ��ƽ���Ļ�������Ϊ�˷�ֹ����ƶ�̫�죬���ĵ㲻����
                float distance = Vector2.Distance(previousDragPosition, pixelPos);
                float steps = 1 / distance;
                for (float lerp = 0; lerp <= 1; lerp += steps)
                {
                    //��ֵ
                    Vector2 curPosition = Vector2.Lerp(previousDragPosition, pixelPos, lerp);
                    //��������
                    if (!drawLine.Contains(curPosition)) 
                    {
                        drawLine.Add(curPosition);
                    }
                    //��
                    PenDraw(curPosition);
                }
                previousDragPosition = pixelPos;
                //���õ�ǰ��ʾͼƬ
                drawableTexture2D.SetPixels32(currentColorArray);
                drawableTexture2D.Apply();
            }
        }
        else
        {   //���û�а�ס
            if (drawLine.Count > 0) 
            {
                drawLines.Add(drawLine);
                drawLine = new List<Vector2>();
            }
            previousDragPosition = Vector2.zero;
        }
    }
    /// <summary>
    /// ��ȡJSON�ļ�
    /// </summary>
    private void ReadJson()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("writeFood");
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
        //��ˢ����λ��X
        int centerX = (int)pixelPos.x;
        //��ˢ����λ��Y
        int centerY = (int)pixelPos.y;
        //������ˢ��Χ���߽��ⲻ����
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
    /// ����������ת��ΪͼƬ�ϵ��������꣬Ҫ���ǵ�ͼƬ����
    /// </summary>
    /// <param name="mouseWorldPosition"></param>
    /// <returns></returns>
    private Vector2 WorldToPixelCoordinates(Vector3 mouseWorldPosition)
    {
        //localPos Ϊ��ǰ���ų̶ȵ�ͼƬ���������λ�ã�����ͼƬ����Ϊ����ԭ������꣬Ҫת������ԭͼ��С������ͼƬ���½�Ϊ����ԭ�������
        Vector2 localPos = transform.InverseTransformPoint(mouseWorldPosition);
        //ת������ϵ
        float transformX = localPos.x + transformWidth / 2;
        float transformY = localPos.y + transformHeight / 2;
        //�������ű���
        float scaleX = (float)spriteWidth / transformWidth;
        float scaleY = (float)spriteHeight / transformHeight;
        //�ó���������±�λ��
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
        PlayerPrefs.SetInt("travelUnit2State", (int)UnitState.Completed);
        //if (currentUnitNumber < 4)
        //{
        //    PlayerPrefs.SetInt("lesson" + (currentUnitNumber + 1).ToString() + "State", (int)UnitState.InProgress);
        //}
        PlayerPrefs.SetInt("travelUnit3State", (int)UnitState.InProgress);
        PlayerPrefs.Save();
        Debug.Log("Lesson " + currentUnitNumber + " state saved: " + UnitState.Completed);
        Debug.Log("Lesson " + (currentUnitNumber + 1).ToString() + " state saved: " + UnitState.InProgress);
        SceneManager.LoadScene("Unit Food");
    }
    /// <summary>
    /// ����ǰ�ָ�Ϊԭ����ͼ�񲢱��������ɵ�ͼ��
    /// </summary>
    protected void OnDestroy()
    {
        drawableTexture2D.SetPixels32(orignalColorArray);
        drawableTexture2D.Apply();
    }

    /// <summary>
    /// ����һ��Э�̣���������
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
            Debug.Log("���ͳɹ�");
             checkBtn.gameObject.SetActive(true);
            loading.SetActive(false);
            scorePanel.SetActive(true);
        }
    }
    public void OnBackClick()
    {
        SceneManager.LoadScene("Unit Food");
    }
}
