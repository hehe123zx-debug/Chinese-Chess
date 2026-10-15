using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
[Header("Game Reference")]
    public XiangqiBoard board;

    [Header("Settings")]
    public BackgroundMusic music;

    private enum MenuScreen { Main, Difficulty, Settings }
    private MenuScreen currentScreen = MenuScreen.Main;
    private bool menuOpen = true;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle sliderStyle;
    private GUIStyle thumbsStyle;
    private bool stylesBuilt;
    private float musicVolume = 0.6f;

    private Sprite menuFrameSprite;
    private Sprite logoSprite;
    private Sprite pvpSprite;
    private Sprite pveSprite;
    private Sprite settingSprite;
    private Sprite settingBottomSprite;
    private Sprite onVolumeSprite;
    private Sprite muteVolumeSprite;
    private bool spritesLoaded;

    void LoadMenuSprites()
    {
        if (spritesLoaded) return;
        menuFrameSprite = Resources.Load<Sprite>("MenuAssets/Menu_frame");
        logoSprite = Resources.Load<Sprite>("MenuAssets/Logo");
        pvpSprite = Resources.Load<Sprite>("MenuAssets/PvP");
        pveSprite = Resources.Load<Sprite>("MenuAssets/PvE");
        settingSprite = Resources.Load<Sprite>("MenuAssets/Setting");
        settingBottomSprite = Resources.Load<Sprite>("MenuAssets/setting_buttom");
        onVolumeSprite = Resources.Load<Sprite>("MenuAssets/On_volume");
        muteVolumeSprite = Resources.Load<Sprite>("MenuAssets/Mute_volume");
        spritesLoaded = true;
    }

    void Awake()
    {
        LoadMenuSprites();

        if (board == null)
            board = Object.FindAnyObjectByType<XiangqiBoard>();

        if (music == null)
            music = Object.FindAnyObjectByType<BackgroundMusic>();

        if (music != null)
            musicVolume = music.GetVolume();
        else
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);

        if (board != null)
        {
            board.autoStart = false;
            HideBoard();
        }
    }

    void Start()
    {
        if (board == null)
            board = Object.FindAnyObjectByType<XiangqiBoard>();

        if (board != null)
        {
            board.autoStart = false;
            HideBoard();
        }
    }

    void Update()
    {
        if (!menuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            OpenMenu();
    }

    void HideBoard()
    {
        board.gameObject.SetActive(false);
    }

    void ShowBoard()
    {
        board.gameObject.SetActive(true);
    }

    void OpenMenu()
    {
        menuOpen = true;
        currentScreen = MenuScreen.Main;
        if (board != null) HideBoard();
    }

    void LaunchPVP()
    {
        if (board == null) return;
        board.enablePVE = false;
        StartGame();
    }

    void LaunchPVE(XiangqiBoard.AIDifficulty diff)
    {
        if (board == null) return;
        board.enablePVE = true;
        board.aiDifficulty = diff;
        StartGame();
    }

    void StartGame()
    {
        menuOpen = false;
        if (board != null)
        {
            ShowBoard();
            board.ResetBoard();
        }
    }

    void OnGUI()
    {
        BuildStyles();

        if (board == null)
            return;

        if (!menuOpen)
        {
            float bw = 120f;
            float bh = 44f;
            if (GUI.Button(new Rect(10f, Screen.height - bh - 10f, bw, bh), "Menu"))
                OpenMenu();
            return;
        }

        float frameAspect = 1254f / 1260f;
        float frameW = Screen.height * 0.85f * frameAspect;
        if (frameW > Screen.width * 0.9f)
            frameW = Screen.width * 0.9f;
        float frameH = frameW / frameAspect;
        float left = (Screen.width - frameW) / 2f;
        float top = (Screen.height - frameH) / 2f;

        DrawSpriteBackground(menuFrameSprite, new Rect(left, top, frameW, frameH));

        if (currentScreen == MenuScreen.Main)
        {
            DrawMainScreen(left, top, frameW, frameH);
        }
        else if (currentScreen == MenuScreen.Difficulty)
        {
            DrawDifficultyScreen(left, top, frameW, frameH);
        }
        else
        {
            DrawSettings(left, top, frameW, frameH);
        }
    }

    void DrawMainScreen(float left, float top, float frameW, float frameH)
    {
        float logoH = frameH * 0.36f;
        float logoW = logoH * (451f / 193f);
        Rect logoRect = new Rect(left + (frameW - logoW) / 2f, top + frameH * 0.03f, logoW, logoH);
        DrawSpriteBackground(logoSprite, logoRect);

        float btnW = frameW * 0.55f;
        float btnH = frameH * 0.10f;
        float bx = left + (frameW - btnW) / 2f;
        float by = top + frameH * 0.42f;
        float gap = frameH * 0.11f;

        if (SpriteButton(pvpSprite, new Rect(bx, by, btnW, btnH)))
            LaunchPVP();
        if (SpriteButton(pveSprite, new Rect(bx, by + gap, btnW, btnH)))
            currentScreen = MenuScreen.Difficulty;
        if (SpriteButton(settingSprite, new Rect(bx, by + gap * 2f, btnW, btnH)))
            currentScreen = MenuScreen.Settings;
    }

    void DrawDifficultyScreen(float left, float top, float frameW, float frameH)
    {
        float btnW = frameW * 0.55f;
        float btnH = frameH * 0.10f;
        float bx = left + (frameW - btnW) / 2f;
        float by = top + frameH * 0.40f;
        float gap = frameH * 0.12f;

        if (GUI.Button(new Rect(bx, by, btnW, btnH), "Easy"))
            LaunchPVE(XiangqiBoard.AIDifficulty.Easy);
        if (GUI.Button(new Rect(bx, by + gap, btnW, btnH), "Medium"))
            LaunchPVE(XiangqiBoard.AIDifficulty.Medium);
        if (GUI.Button(new Rect(bx, by + gap * 2f, btnW, btnH), "Hard"))
            LaunchPVE(XiangqiBoard.AIDifficulty.Hard);
        if (GUI.Button(new Rect(bx, by + gap * 2.95f, btnW * 0.45f, btnH * 0.6f), "< Back"))
            currentScreen = MenuScreen.Main;
    }

    void DrawSpriteBackground(Sprite sprite, Rect rect)
    {
        if (sprite == null) return;
        Color prev = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, sprite.texture, ScaleMode.StretchToFill, true);
        GUI.color = prev;
    }

    bool SpriteButton(Sprite sprite, Rect rect, string label = "")
    {
        if (sprite == null)
            return GUI.Button(rect, label);

        GUIStyle style = new GUIStyle("button");
        style.normal.background = sprite.texture;
        style.hover.background = sprite.texture;
        style.active.background = sprite.texture;
        style.border = new RectOffset(0, 0, 0, 0);
        style.padding = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(0, 0, 0, 0);
        style.stretchWidth = false;
        style.stretchHeight = false;
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = Mathf.Max(16, Mathf.RoundToInt(rect.height * 0.32f));
        style.alignment = TextAnchor.MiddleCenter;
        return GUI.Button(rect, label, style);
    }

    void DrawSettings(float left, float top, float panelW, float panelH)
    {
        float cy = top + panelH * 0.42f;

        float iconSize = panelH * 0.08f;
        Rect iconRect = new Rect(left + panelW * 0.16f, cy - iconSize / 2f, iconSize, iconSize);
        DrawSpriteBackground(musicVolume > 0f ? onVolumeSprite : muteVolumeSprite, iconRect);

        float sliderW = panelW * 0.46f;
        Rect sliderRect = new Rect(left + panelW * 0.28f, cy, sliderW, panelH * 0.06f);
        float newVolume = GUI.HorizontalSlider(sliderRect, musicVolume, 0f, 1f, sliderStyle, thumbsStyle);
        if (Mathf.Abs(newVolume - musicVolume) > 0.0001f)
        {
            musicVolume = newVolume;
            if (music != null)
                music.SetVolume(musicVolume);
            else
                PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }
        GUI.Label(new Rect(left + panelW * 0.28f + sliderW + panelW * 0.02f, cy - panelH * 0.015f, panelW * 0.12f, panelH * 0.06f),
            Mathf.RoundToInt(musicVolume * 100f) + "%", labelStyle);

        GUI.Label(new Rect(left + panelW * 0.16f, top + panelH * 0.58f, panelW * 0.68f, panelH * 0.07f),
            "Author : Tran Hoa Thien", labelStyle);

        if (SpriteButton(settingBottomSprite, new Rect(left + panelW * 0.30f, top + panelH * 0.78f, panelW * 0.40f, panelH * 0.12f)))
            currentScreen = MenuScreen.Main;
    }

    void BuildStyles()
    {
        if (stylesBuilt) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 44,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
        sliderStyle.fixedHeight = 16;
        thumbsStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
        thumbsStyle.fixedWidth = 24;
        thumbsStyle.fixedHeight = 24;

        GUI.skin.button = buttonStyle;

        stylesBuilt = true;
    }
}