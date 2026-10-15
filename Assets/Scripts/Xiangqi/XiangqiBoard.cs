using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class XiangqiBoard : MonoBehaviour
{
    public enum AIDifficulty { Easy, Medium, Hard }

    public const int GridWidth = 9;
    public const int GridHeight = 10;

    [Header("Board Grid")]
    public float squareSize = 1f;
    public Vector2 boardOrigin = new Vector2(-4f, -4.5f);

    [Header("Piece Sprites (drag the piece sprites here)")]
    public Sprite redGeneral;
    public Sprite redAdvisor;
    public Sprite redElephant;
    public Sprite redHorse;
    public Sprite redChariot;
    public Sprite redCannon;
    public Sprite redSoldier;
    public Sprite blackGeneral;
    public Sprite blackAdvisor;
    public Sprite blackElephant;
    public Sprite blackHorse;
    public Sprite blackChariot;
    public Sprite blackCannon;
    public Sprite blackSoldier;

    [Header("Visual Feedback")]
    public Color selectedColor = new Color(1f, 0.92f, 0.3f, 1f);
    public Color moveIndicatorColor = new Color(0f, 1f, 0f, 0.55f);
    public Color captureIndicatorColor = new Color(1f, 0.25f, 0.15f, 0.65f);
    public float indicatorScale = 0.6f;
    public float pieceScale = 0.8f;
    public bool showStatusText = true;

    [Header("Game Mode")]
    public bool enablePVE = true;
    public bool playerIsRed = true;
    public bool autoStart = true;
    public AIDifficulty aiDifficulty = AIDifficulty.Medium;
    [Range(1, 4)]
    public int aiDepthEasy = 1;
    [Range(1, 4)]
    public int aiDepthMedium = 2;
    [Range(1, 5)]
    public int aiDepthHard = 4;
    public float aiMoveDelay = 0.6f;

    private readonly Piece[,] grid = new Piece[GridWidth, GridHeight];
    private readonly List<GameObject> indicators = new List<GameObject>();
    private readonly List<GameObject> trailMarkers = new List<GameObject>();
    private Sprite moveDotSprite;
    private Sprite captureRingSprite;
    private XiangqiAI ai = new XiangqiAI();

    private Piece selected;
    private PieceColor currentTurn = PieceColor.Red;
    private bool gameOver;
    private string statusMessage = "Red to move";
    private Camera mainCam;
    private bool aiThinking;
    private float aiMoveTimer;

    [Header("Move History")]
    public bool recordMoves = true;
    public bool showHistory = true;

    [Header("Move Trail")]
    public bool showMoveTrail = true;
    public Color redMoveColor = new Color(1f, 0.4f, 0.35f, 0.9f);
    public Color blackMoveColor = new Color(0.4f, 0.4f, 0.45f, 0.9f);
    public float trailSize = 1f;

    private readonly List<string> moveHistory = new List<string>();
    private string pieceNotationChars = "KAEHCRS";

    [Header("Announcement")]
    public bool showAnnouncement = true;
    public int announcementFontSize = 64;
    public Color announcementColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color winnerColor = new Color(0.2f, 1f, 0.4f, 1f);
    public bool useWinImages = true;

    private string announcementText = "";
    private float announcementTimer = 0f;
    private bool announcementIsEnd = false;
    private Sprite redWinSprite;
    private Sprite blackWinSprite;
    private Sprite activeWinSprite = null;

    void Awake()
    {
        AutoLoadSprites();
    }

    void AutoLoadSprites()
    {
        if (redGeneral == null) redGeneral = Resources.Load<Sprite>("pieces/red_general");
        if (redAdvisor == null) redAdvisor = Resources.Load<Sprite>("pieces/red_advisor");
        if (redElephant == null) redElephant = Resources.Load<Sprite>("pieces/red_elephant");
        if (redHorse == null) redHorse = Resources.Load<Sprite>("pieces/red_horse");
        if (redChariot == null) redChariot = Resources.Load<Sprite>("pieces/red_chariot");
        if (redCannon == null) redCannon = Resources.Load<Sprite>("pieces/red_cannon");
        if (redSoldier == null) redSoldier = Resources.Load<Sprite>("pieces/red_soldier");
        if (blackGeneral == null) blackGeneral = Resources.Load<Sprite>("pieces/black_general");
        if (blackAdvisor == null) blackAdvisor = Resources.Load<Sprite>("pieces/black_advisor");
        if (blackElephant == null) blackElephant = Resources.Load<Sprite>("pieces/black_elephant");
        if (blackHorse == null) blackHorse = Resources.Load<Sprite>("pieces/black_horse");
        if (blackChariot == null) blackChariot = Resources.Load<Sprite>("pieces/black_chariot");
        if (blackCannon == null) blackCannon = Resources.Load<Sprite>("pieces/black_cannon");
        if (blackSoldier == null) blackSoldier = Resources.Load<Sprite>("pieces/black_soldier");
        if (redWinSprite == null) redWinSprite = Resources.Load<Sprite>("MenuAssets/Red_win");
        if (blackWinSprite == null) blackWinSprite = Resources.Load<Sprite>("MenuAssets/Black_win");
    }

    void Start()
    {
        mainCam = Camera.main;
        if (autoStart && IsBoardEmpty())
            ResetBoard();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            CopyHistoryToClipboard();
        }

        if (gameOver && (PointerPressedThisFrame() || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)))
        {
            ResetBoard();
            return;
        }

        if (!gameOver && enablePVE && IsAITurn())
        {
            if (!aiThinking)
            {
                aiThinking = true;
                aiMoveTimer = aiMoveDelay;
            }

            aiMoveTimer -= Time.deltaTime;
            if (aiMoveTimer <= 0f)
            {
                aiThinking = false;
                MakeAIMove();
            }
            return;
        }

        Vector2? tapWorld = GetTapWorldPosition();
        if (!tapWorld.HasValue) return;

        HandleClick(WorldToGrid(tapWorld.Value));
    }

    Vector2? GetTapWorldPosition()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return null;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return (Vector2)mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch.ReadValue();
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return (Vector2)mainCam.ScreenToWorldPoint(touch.position);
        }

        return null;
    }

    bool PointerPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    bool IsAITurn()
    {
        return enablePVE && currentTurn != (playerIsRed ? PieceColor.Red : PieceColor.Black);
    }

    void MakeAIMove()
    {
        int depth = GetAIDepth();
        var move = ai.GetBestMove(this, currentTurn, depth);
        if (move == null) return;

        Piece piece = GetPieceAt(move.Value.Item1);
        if (piece == null) return;

        ExecuteMove(piece, move.Value.Item2);
        ClearSelection();
        CleanupOrphans();
    }

    int GetAIDepth()
    {
        switch (aiDifficulty)
        {
            case AIDifficulty.Easy: return aiDepthEasy;
            case AIDifficulty.Hard: return aiDepthHard;
            default: return aiDepthMedium;
        }
    }

    void HandleClick(Vector2Int cell)
    {
        if (gameOver)
        {
            ClearSelection();
            return;
        }

        if (enablePVE && IsAITurn())
        {
            ClearSelection();
            return;
        }

        Piece pieceAt = GetPieceAt(cell);

        if (selected != null)
        {
            if (cell == selected.GridPos)
            {
                ClearSelection();
                return;
            }

            if (pieceAt != null && pieceAt.Color == currentTurn)
            {
                SetSelection(pieceAt);
                return;
            }

            if (IsLegalMove(selected, cell))
            {
                ExecuteMove(selected, cell);
                ClearSelection();
                CleanupOrphans();
                return;
            }

            ClearSelection();
            return;
        }

        if (pieceAt != null && pieceAt.Color == currentTurn)
            SetSelection(pieceAt);
    }

    void SetSelection(Piece piece)
    {
        if (selected != null) selected.OnDeselect();
        selected = piece;
        selected.OnSelect(selectedColor);
        ShowValidMoves(piece);
    }

    void ClearSelection()
    {
        if (selected != null) selected.OnDeselect();
        selected = null;
        DestroyIndicators();
    }

    void ExecuteMove(Piece piece, Vector2Int target)
    {
        Piece captured = GetPieceAt(target);
        Vector2Int from = piece.GridPos;

        grid[from.x, from.y] = null;
        grid[target.x, target.y] = piece;
        piece.MoveTo(target);

        if (recordMoves)
            RecordMove(piece, from, target, captured);

        if (showMoveTrail)
            AddTrailMarker(from, piece.Color);

        bool isGeneralCapture = captured != null && captured.Type == PieceType.General;

        if (captured != null)
        {
            if (isGeneralCapture)
            {
                gameOver = true;
                statusMessage = currentTurn + " wins! The " + captured.Color + " General has been captured.";
                Debug.Log(statusMessage);
                ShowAnnouncement(currentTurn + " WINS!", true, currentTurn);
                return;
            }
            Destroy(captured.gameObject);
        }

        PieceColor opponent = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
        bool opponentInCheck = IsInCheck(opponent);

        if (!isGeneralCapture && !HasAnyLegalMove(opponent))
        {
            gameOver = true;
            statusMessage = "Checkmate! " + currentTurn + " wins.";
            Debug.Log(statusMessage);
            ShowAnnouncement("CHECKMATE! " + currentTurn + " WINS!", true, currentTurn);
        }
        else
        {
            currentTurn = opponent;
            aiThinking = false;
            statusMessage = BuildStatus(opponentInCheck);
            if (opponentInCheck && !gameOver)
                ShowAnnouncement("CHECK!", false);
        }
    }

    void AddTrailMarker(Vector2Int from, PieceColor color)
    {
        DestroyTrailMarkers();

        GameObject marker = new GameObject("MoveTrailMarker");
        marker.transform.SetParent(transform);
        marker.transform.localPosition = (Vector3)GridToWorld(from);

        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = GetTrailSprite();
        sr.color = color == PieceColor.Red ? redMoveColor : blackMoveColor;
        sr.sortingLayerName = "Pieces";
        sr.sortingOrder = 3;
        marker.transform.localScale = Vector3.one * (trailSize * 0.5f);

        trailMarkers.Add(marker);
    }

    Sprite GetTrailSprite()
    {
        if (moveDotSprite == null)
            moveDotSprite = CreateDotSprite(Color.white, false);
        return moveDotSprite;
    }

    void DestroyTrailMarkers()
    {
        for (int i = trailMarkers.Count - 1; i >= 0; i--)
        {
            if (trailMarkers[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(trailMarkers[i].gameObject);
                else
                    DestroyImmediate(trailMarkers[i].gameObject);
            }
        }
        trailMarkers.Clear();
    }

    void RecordMove(Piece piece, Vector2Int from, Vector2Int to, Piece captured)
    {
        char typeChar = pieceNotationChars[(int)piece.Type];
        char fileChar = (char)('a' + from.x);
        char capture = captured != null ? 'x' : '-';
        string notation = string.Format("{0}: {1}{2}{3}{4}{5}{6}",
            piece.Color == PieceColor.Red ? "R" : "B",
            typeChar,
            fileChar,
            (from.y + 1),
            capture,
            (char)('a' + to.x),
            (to.y + 1));

        moveHistory.Add(notation);
    }

    string BuildStatus(bool inCheck)
    {
        string who = currentTurn.ToString();
        if (enablePVE)
        {
            bool isPlayer = currentTurn == (playerIsRed ? PieceColor.Red : PieceColor.Black);
            who = isPlayer ? "You" : "Computer";
        }
        string check = inCheck ? " (check!)" : "";

        if (enablePVE && currentTurn != (playerIsRed ? PieceColor.Red : PieceColor.Black))
            return "Computer is thinking..." + check;

        return (enablePVE && currentTurn == (playerIsRed ? PieceColor.Red : PieceColor.Black) ? "Your turn" : who + " to move") + check;
    }

    public bool IsLegalMove(Piece piece, Vector2Int target)
    {
        if (!InBounds(target)) return false;
        if (target == piece.GridPos) return false;

        Piece occupant = GetPieceAt(target);
        if (occupant != null && occupant.Color == piece.Color) return false;

        Vector2Int d = target - piece.GridPos;
        int adx = Mathf.Abs(d.x);
        int ady = Mathf.Abs(d.y);

        switch (piece.Type)
        {
            case PieceType.General:
                if (!InPalace(target, piece.Color)) return false;
                if (adx + ady != 1) return false;
                break;

            case PieceType.Advisor:
                if (!InPalace(target, piece.Color)) return false;
                if (!(adx == 1 && ady == 1)) return false;
                break;

            case PieceType.Elephant:
                if (piece.Color == PieceColor.Red && target.y > 4) return false;
                if (piece.Color == PieceColor.Black && target.y < 5) return false;
                if (!(adx == 2 && ady == 2)) return false;
                {
                    Vector2Int eye = piece.GridPos + new Vector2Int(d.x / 2, d.y / 2);
                    if (GetPieceAt(eye) != null) return false;
                }
                break;

            case PieceType.Horse:
                Vector2Int leg;
                if (adx == 2 && ady == 1)
                    leg = new Vector2Int((int)Mathf.Sign(d.x), 0);
                else if (adx == 1 && ady == 2)
                    leg = new Vector2Int(0, (int)Mathf.Sign(d.y));
                else return false;
                if (GetPieceAt(piece.GridPos + leg) != null) return false;
                break;

            case PieceType.Chariot:
                if (!IsOrthogonal(d)) return false;
                if (CountBetween(piece.GridPos, target) != 0) return false;
                break;

            case PieceType.Cannon:
                if (!IsOrthogonal(d)) return false;
                int screens = CountBetween(piece.GridPos, target);
                if (occupant == null)
                {
                    if (screens != 0) return false;
                }
                else
                {
                    if (screens != 1) return false;
                }
                break;

            case PieceType.Soldier:
                int forward = piece.Color == PieceColor.Red ? 1 : -1;
                if (d.y == forward && d.x == 0) break;
                bool crossedRiver = piece.Color == PieceColor.Red
                    ? piece.GridPos.y >= 5
                    : piece.GridPos.y <= 4;
                if (!crossedRiver) return false;
                if (!(d.y == 0 && adx == 1)) return false;
                break;
        }

        Vector2Int from = piece.GridPos;
        grid[from.x, from.y] = null;
        grid[target.x, target.y] = piece;
        bool leavesKingInCheck = IsInCheck(piece.Color);
        grid[from.x, from.y] = piece;
        grid[target.x, target.y] = occupant;

        return !leavesKingInCheck;
    }

    List<Vector2Int> GetLegalMoves(Piece piece)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Vector2Int target = new Vector2Int(x, y);
                if (IsLegalMove(piece, target))
                    moves.Add(target);
            }
        }
        return moves;
    }

    bool HasAnyLegalMove(PieceColor color)
    {
        foreach (Piece piece in GetActivePieces())
        {
            if (piece.Color == color && GetLegalMoves(piece).Count > 0)
                return true;
        }
        return false;
    }

    bool IsInCheck(PieceColor color)
    {
        Piece general = FindGeneral(color);
        if (general == null) return true;

        foreach (Piece piece in GetActivePieces())
        {
            if (piece.Color != color && CanAttack(piece, general.GridPos))
                return true;
        }
        return false;
    }

    bool CanAttack(Piece attacker, Vector2Int target)
    {
        Vector2Int d = target - attacker.GridPos;
        int adx = Mathf.Abs(d.x);
        int ady = Mathf.Abs(d.y);

        switch (attacker.Type)
        {
            case PieceType.Chariot:
                return IsOrthogonal(d) && CountBetween(attacker.GridPos, target) == 0;

            case PieceType.Cannon:
                return IsOrthogonal(d) && CountBetween(attacker.GridPos, target) == 1;

            case PieceType.Horse:
                Vector2Int leg;
                if (adx == 2 && ady == 1)
                    leg = new Vector2Int((int)Mathf.Sign(d.x), 0);
                else if (adx == 1 && ady == 2)
                    leg = new Vector2Int(0, (int)Mathf.Sign(d.y));
                else return false;
                return GetPieceAt(attacker.GridPos + leg) == null;

            case PieceType.Elephant:
                if (!(adx == 2 && ady == 2)) return false;
                {
                    Vector2Int eye = attacker.GridPos + new Vector2Int(d.x / 2, d.y / 2);
                    return GetPieceAt(eye) == null;
                }

            case PieceType.Advisor:
                return adx == 1 && ady == 1;

            case PieceType.General:
                if (d.x == 0 && CountBetween(attacker.GridPos, target) == 0) return true;
                return adx + ady == 1;

            case PieceType.Soldier:
                int forward = attacker.Color == PieceColor.Red ? 1 : -1;
                if (d.y == forward && d.x == 0) return true;
                bool crossedRiver = attacker.Color == PieceColor.Red
                    ? attacker.GridPos.y >= 5
                    : attacker.GridPos.y <= 4;
                return crossedRiver && d.y == 0 && adx == 1;

            default:
                return false;
        }
    }

    void ShowValidMoves(Piece piece)
    {
        DestroyIndicators();

        foreach (Vector2Int move in GetLegalMoves(piece))
        {
            if (move.x < 0 || move.x >= GridWidth || move.y < 0 || move.y >= GridHeight)
                continue;

            GameObject indicator = new GameObject("MoveIndicator");
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = (Vector3)GridToWorld(move);

            SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Pieces";
            sr.sortingOrder = 4;

            if (GetPieceAt(move) != null)
            {
                sr.sprite = GetCaptureRingSprite();
                sr.color = captureIndicatorColor;
                sr.transform.localScale = Vector3.one * indicatorScale;
            }
            else
            {
                sr.sprite = GetMoveDotSprite();
                sr.color = moveIndicatorColor;
                sr.transform.localScale = Vector3.one * indicatorScale * 0.5f;
            }

            indicators.Add(indicator);
        }
    }

    void DestroyIndicators()
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] != null) Destroy(indicators[i]);
        }
        indicators.Clear();
    }

    Sprite GetMoveDotSprite()
    {
        if (moveDotSprite == null)
            moveDotSprite = CreateDotSprite(moveIndicatorColor, false);
        return moveDotSprite;
    }

    Sprite GetCaptureRingSprite()
    {
        if (captureRingSprite == null)
            captureRingSprite = CreateDotSprite(captureIndicatorColor, true);
        return captureRingSprite;
    }

    Sprite CreateDotSprite(Color color, bool ring)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float outer = size / 2f - 1f;
        float inner = ring ? size * 0.30f : 0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = ring ? (dist <= outer && dist >= inner) : dist <= outer;
                texture.SetPixel(x, y, inside ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    Piece FindGeneral(PieceColor color)
    {
        foreach (Piece piece in GetActivePieces())
        {
            if (piece.Color == color && piece.Type == PieceType.General)
                return piece;
        }
        return null;
    }

    public List<Piece> GetActivePieces()
    {
        List<Piece> pieces = new List<Piece>();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (grid[x, y] != null)
                    pieces.Add(grid[x, y]);
            }
        }
        return pieces;
    }

    bool IsBoardEmpty()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (grid[x, y] != null) return false;
            }
        }
        return true;
    }

    Piece GetPieceAt(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= GridWidth || cell.y < 0 || cell.y >= GridHeight)
            return null;
        return grid[cell.x, cell.y];
    }

    void CleanupOrphans()
    {
        Piece[] all = FindObjectsByType<Piece>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Piece piece = all[i];
            if (piece == null) continue;
            Vector2Int p = piece.GridPos;
            if (p.x < 0 || p.x >= GridWidth || p.y < 0 || p.y >= GridHeight)
            {
                Destroy(piece.gameObject);
                continue;
            }
            if (grid[p.x, p.y] != piece)
            {
                Destroy(piece.gameObject);
            }
        }
    }

    static bool IsOrthogonal(Vector2Int d)
    {
        return (d.x == 0) ^ (d.y == 0);
    }

    bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < GridWidth && p.y >= 0 && p.y < GridHeight;
    }

    bool InPalace(Vector2Int p, PieceColor color)
    {
        if (p.x < 3 || p.x > 5) return false;
        return color == PieceColor.Red ? p.y >= 0 && p.y <= 2 : p.y >= 7 && p.y <= 9;
    }

    int CountBetween(Vector2Int a, Vector2Int b)
    {
        int count = 0;

        if (a.x == b.x)
        {
            int step = (int)Mathf.Sign(b.y - a.y);
            for (int y = a.y + step; y != b.y; y += step)
            {
                if (grid[a.x, y] != null) count++;
            }
        }
        else if (a.y == b.y)
        {
            int step = (int)Mathf.Sign(b.x - a.x);
            for (int x = a.x + step; x != b.x; x += step)
            {
                if (grid[x, a.y] != null) count++;
            }
        }

        return count;
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector2 local = transform.InverseTransformPoint(worldPos);
        int x = Mathf.RoundToInt((local.x - boardOrigin.x) / squareSize);
        int y = Mathf.RoundToInt((local.y - boardOrigin.y) / squareSize);
        return new Vector2Int(x, y);
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        float x = boardOrigin.x + gridPos.x * squareSize;
        float y = boardOrigin.y + gridPos.y * squareSize;
        return new Vector2(x, y);
    }

    [ContextMenu("Reset Board")]
    public void ResetBoard()
    {
        DestroyIndicators();
        DestroyTrailMarkers();

        foreach (Piece piece in GetActivePieces())
        {
            if (piece == null) continue;
            if (Application.isPlaying)
                Destroy(piece.gameObject);
            else
                DestroyImmediate(piece.gameObject);
        }

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                grid[x, y] = null;
            }
        }

        selected = null;
        gameOver = false;
        aiThinking = false;
        aiMoveTimer = 0f;
        currentTurn = PieceColor.Red;
        moveHistory.Clear();
        statusMessage = enablePVE && !playerIsRed ? "Computer is thinking..." : "Red to move";
        ClearAnnouncement();

        SpawnRank(PieceColor.Red, 0);
        SpawnRank(PieceColor.Black, 9);

        for (int i = 1; i < 8; i += 6)
        {
            SpawnPiece(GetSprite(PieceColor.Red, PieceType.Cannon), PieceType.Cannon, PieceColor.Red, new Vector2Int(i, 2));
            SpawnPiece(GetSprite(PieceColor.Black, PieceType.Cannon), PieceType.Cannon, PieceColor.Black, new Vector2Int(i, 7));
        }

        for (int i = 0; i < 9; i += 2)
        {
            SpawnPiece(GetSprite(PieceColor.Red, PieceType.Soldier), PieceType.Soldier, PieceColor.Red, new Vector2Int(i, 3));
            SpawnPiece(GetSprite(PieceColor.Black, PieceType.Soldier), PieceType.Soldier, PieceColor.Black, new Vector2Int(i, 6));
        }
    }

    void SpawnRank(PieceColor color, int y)
    {
        PieceType[] order =
        {
            PieceType.Chariot, PieceType.Horse, PieceType.Elephant, PieceType.Advisor,
            PieceType.General,
            PieceType.Advisor, PieceType.Elephant, PieceType.Horse, PieceType.Chariot
        };

        for (int x = 0; x < GridWidth; x++)
        {
            SpawnPiece(GetSprite(color, order[x]), order[x], color, new Vector2Int(x, y));
        }
    }

    void SpawnPiece(Sprite sprite, PieceType type, PieceColor color, Vector2Int gridPos)
    {
        GameObject pieceObject = new GameObject("Piece_" + color + "_" + type);
        pieceObject.transform.SetParent(transform);
        pieceObject.transform.localPosition = (Vector3)GridToWorld(gridPos);

        SpriteRenderer sr = pieceObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Pieces";
        sr.sortingOrder = 5;

        float scale = squareSize * pieceScale;
        if (sprite != null && sprite.bounds.size.x > 0.001f)
            scale = squareSize * pieceScale / sprite.bounds.size.x;
        pieceObject.transform.localScale = Vector3.one * scale;

        Piece piece = pieceObject.AddComponent<Piece>();
        piece.Init(this, type, color, gridPos);
        grid[gridPos.x, gridPos.y] = piece;
    }

    Sprite GetSprite(PieceColor color, PieceType type)
    {
        switch (color)
        {
            case PieceColor.Red:
                switch (type)
                {
                    case PieceType.General: return redGeneral;
                    case PieceType.Advisor: return redAdvisor;
                    case PieceType.Elephant: return redElephant;
                    case PieceType.Horse: return redHorse;
                    case PieceType.Chariot: return redChariot;
                    case PieceType.Cannon: return redCannon;
                    case PieceType.Soldier: return redSoldier;
                }
                break;

            case PieceColor.Black:
                switch (type)
                {
                    case PieceType.General: return blackGeneral;
                    case PieceType.Advisor: return blackAdvisor;
                    case PieceType.Elephant: return blackElephant;
                    case PieceType.Horse: return blackHorse;
                    case PieceType.Chariot: return blackChariot;
                    case PieceType.Cannon: return blackCannon;
                    case PieceType.Soldier: return blackSoldier;
                }
                break;
        }

        return null;
    }

    void CopyHistoryToClipboard()
    {
        if (moveHistory.Count == 0) return;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < moveHistory.Count; i++)
        {
            int number = (i / 2) + 1;
            bool isRed = (i % 2) == 0;
            sb.Append(isRed ? number + ". " : "     ");
            sb.AppendLine(moveHistory[i]);
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Move history copied to clipboard.");
    }

    public void ShowAnnouncement(string text, bool isEnd, PieceColor? winColor = null)
    {
        announcementText = text;
        announcementIsEnd = isEnd;
        announcementTimer = isEnd ? 0f : 2f;
        activeWinSprite = null;
        if (isEnd && winColor.HasValue)
            activeWinSprite = winColor.Value == PieceColor.Red ? redWinSprite : blackWinSprite;
    }

    void ClearAnnouncement()
    {
        announcementText = "";
        announcementIsEnd = false;
        announcementTimer = 0f;
        activeWinSprite = null;
    }

    void DrawAnnouncement()
    {
        if (!showAnnouncement) return;
        if (string.IsNullOrEmpty(announcementText)) return;

        if (announcementTimer > 0f)
        {
            announcementTimer -= Time.deltaTime;
            if (announcementTimer <= 0f && !announcementIsEnd)
                ClearAnnouncement();
        }

        Color color = announcementIsEnd ? winnerColor : announcementColor;

        if (announcementIsEnd && useWinImages && activeWinSprite != null)
        {
            Rect imgRect = GetWinImageRect(activeWinSprite);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(imgRect, activeWinSprite.texture, ScaleMode.ScaleToFit, true);
            GUI.color = prev;

            GUIStyle hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.width * 0.02f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                active = { textColor = Color.white },
                hover = { textColor = Color.white }
            };
            GUI.Label(new Rect(0f, imgRect.yMax + 10f, Screen.width, 60f), "Tap or press R to play again", hint);
            return;
        }

        int size = announcementFontSize;
        if (announcementTimer > 0f && announcementTimer < 0.5f && !announcementIsEnd)
            size = Mathf.RoundToInt(size * (announcementTimer / 0.5f));

        string text = announcementIsEnd ? announcementText + "\n\n" + "Tap or press R to play again" : announcementText;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = color },
            active = { textColor = color },
            hover = { textColor = color }
        };
        style.richText = true;

        Vector2 textSize = style.CalcSize(new GUIContent(text));
        float w = Mathf.Min(Screen.width * 0.9f, textSize.x + 80f);
        float h = textSize.y + 80f;
        Rect box = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        Rect inner = new Rect(box.x, box.y, box.width, box.height);
        GUI.Box(inner, "", GetAnnouncementBoxStyle());
        GUI.Label(inner, text, style);
    }

    Rect GetWinImageRect(Sprite sprite)
    {
        float aspect = sprite.bounds.size.x / sprite.bounds.size.y;
        float w = Screen.width * 0.6f;
        float h = w / aspect;
        float maxH = Screen.height * 0.5f;
        if (h > maxH)
        {
            h = maxH;
            w = h * aspect;
        }
        return new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
    }

    GUIStyle announcementBoxStyle;
    Texture2D announcementBgTexture;

    GUIStyle GetAnnouncementBoxStyle()
    {
        if (announcementBoxStyle != null) return announcementBoxStyle;
        if (announcementBgTexture == null)
        {
            announcementBgTexture = new Texture2D(1, 1);
            announcementBgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            announcementBgTexture.Apply();
        }
        announcementBoxStyle = new GUIStyle(GUI.skin.box);
        announcementBoxStyle.normal.background = announcementBgTexture;
        announcementBoxStyle.border = new RectOffset(6, 6, 6, 6);
        return announcementBoxStyle;
    }

    void OnGUI()
    {
        if (showStatusText)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 80));
            GUILayout.Label(statusMessage);
            if (gameOver)
                GUILayout.Label("Press R to start a new game.");
            GUILayout.EndArea();
        }

        DrawAnnouncement();
    }

}