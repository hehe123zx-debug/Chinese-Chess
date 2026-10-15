using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [Tooltip("Width of the play area in world units that must always be visible.")]
    public float targetWidth = 8.64f;
    [Tooltip("Height of the play area in world units that must always be visible.")]
    public float targetHeight = 9.64f;

    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Start()
    {
        Fit();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Fit();
        }
    }

    void Fit()
    {
        if (cam == null) cam = GetComponent<Camera>();

        float screenAspect = Screen.width / (float)Screen.height;

        float orthoForHeight = targetHeight * 0.5f;
        float orthoForWidth = targetWidth * 0.5f / screenAspect;

        cam.orthographicSize = Mathf.Max(orthoForHeight, orthoForWidth);
    }
}
