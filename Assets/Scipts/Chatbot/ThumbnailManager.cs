using UnityEngine;
using UnityEngine.UI;

public class ThumbnailManager : MonoBehaviour
{
    // Singleton ʵ��
    public static ThumbnailManager Instance { get; private set; }

    // UI���
    public Image thumbnailImage;       // ���������ͼ Image ���
    public Button thumbnailButton;     // ���������ͼ Button ���
    public GameObject imagePopupPanel; // ��ͼ���� Panel
    public Image fullImage;            // ��ͼ Image ���
    public Button closeButton;         // �رյ������ڵ� Button

    private Sprite currentPopupSprite; // ��ǰ���ڵ����� Sprite

    private void Awake()
    {
        // ʵ�� Singleton ģʽ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �����Ҫ�糡��ʹ��
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ��ʼ�� UI Ԫ��
        imagePopupPanel.SetActive(false); // ���ص�������

        // ȷ��û���ظ��ļ�����
        thumbnailButton.onClick.RemoveAllListeners();
        thumbnailButton.onClick.AddListener(OnThumbnailClick);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    /*
     * ��������ͼ����¼��ǰ���ڵ����� Sprite
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
            Debug.Log("�������ÿյ�����ͼ Sprite��");
        }
    }

    /*
     * ����ͼ��ť����¼���ͳһ����
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
            Debug.Log("��ǰû�пɹ���ʾ�Ĵ�ͼ Sprite��");
        }
    }

    /*
     * �رյ�������
     */
    private void OnCloseButtonClick()
    {
        imagePopupPanel.SetActive(false);
    }
}
