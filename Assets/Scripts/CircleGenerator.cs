using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CircleGenerator : MonoBehaviour
{
    [Header("Circle Settings")]
    public float radius = 1f;
    public Color color = Color.white;
    [Range(32, 512)]
    public int resolution = 256;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GenerateCircle();
    }

    [ContextMenu("Generate")]
    public void GenerateCircle()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        int size = resolution;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= center - 0.5f)
                    texture.SetPixel(x, y, color);
                else
                    texture.SetPixel(x, y, Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / (radius * 2f));
        spriteRenderer.sprite = sprite;
    }
}
