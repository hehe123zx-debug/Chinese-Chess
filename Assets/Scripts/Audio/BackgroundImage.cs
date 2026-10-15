using UnityEngine;

public class BackgroundImage : MonoBehaviour
{
    [Header("Settings")]
    public bool autoShowOnStart = true;
    public int sortingOrder = -10;
    [Tooltip("If true, stretches image to fill the whole screen. If false, scales to the board size.")]
    public bool fillScreen = true;
    public Vector2 boardSize = new Vector2(8.64f, 9.64f);

    private SpriteRenderer spriteRenderer;
    private GameObject backgroundObject;
    private int lastWidth;
    private int lastHeight;

    void Start()
    {
        if (autoShowOnStart)
            ShowBackground();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ShowBackground();
        }
    }

    public void ShowBackground()
    {
        Sprite sprite = Resources.Load<Sprite>("Background_image");

        if (sprite == null)
        {
            Debug.LogWarning("BackgroundImage: Could not load Background_image sprite from Resources.");
            return;
        }

        if (backgroundObject != null)
            Destroy(backgroundObject);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(null);
        Vector3 camPos = Camera.main.transform.position;
        bg.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        backgroundObject = bg;

        spriteRenderer = bg.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = sortingOrder;

        float camHeight = 2f * Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        float targetWidth = fillScreen ? camWidth : boardSize.x;
        float targetHeight = fillScreen ? camHeight : boardSize.y;

        Vector2 bounds = sprite.bounds.size;
        float scaleX = targetWidth / bounds.x;
        float scaleY = targetHeight / bounds.y;

        bg.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    public void HideBackground()
    {
        if (backgroundObject != null)
            Destroy(backgroundObject);
    }
}