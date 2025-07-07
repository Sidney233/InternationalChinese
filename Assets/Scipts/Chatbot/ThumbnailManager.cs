using UnityEngine;
using UnityEngine.UI;

public class ThumbnailManager : MonoBehaviour
{
    // Singleton 实例
    public static ThumbnailManager Instance { get; private set; }

    // UI组件
    public Image thumbnailImage;       // 共享的缩略图 Image 组件
    public Button thumbnailButton;     // 共享的缩略图 Button 组件
    public GameObject imagePopupPanel; // 大图弹出 Panel
    public Image fullImage;            // 大图 Image 组件
    public Button closeButton;         // 关闭弹出窗口的 Button

    private Sprite currentPopupSprite; // 当前用于弹出的 Sprite

    private void Awake()
    {
        // 实现 Singleton 模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 如果需要跨场景使用
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化 UI 元素
        imagePopupPanel.SetActive(false); // 隐藏弹出窗口

        // 确保没有重复的监听器
        thumbnailButton.onClick.RemoveAllListeners();
        thumbnailButton.onClick.AddListener(OnThumbnailClick);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    /*
     * 设置缩略图并记录当前用于弹出的 Sprite
     */
    public void SetThumbnail(Sprite sprite)
    {
        if (sprite != null)
        {
            thumbnailImage.sprite = sprite;
            thumbnailImage.gameObject.SetActive(true);
            thumbnailButton.gameObject.SetActive(true);
            currentPopupSprite = sprite;
        }
        else
        {
            Debug.Log("尝试设置空的缩略图 Sprite。");
        }
    }

    /*
     * 缩略图按钮点击事件的统一处理
     */
    private void OnThumbnailClick()
    {
        if (currentPopupSprite != null)
        {
            fullImage.sprite = currentPopupSprite;
            imagePopupPanel.SetActive(true);
        }
        else
        {
            Debug.Log("当前没有可供显示的大图 Sprite。");
        }
    }

    /*
     * 关闭弹出窗口
     */
    private void OnCloseButtonClick()
    {
        imagePopupPanel.SetActive(false);
    }
}
