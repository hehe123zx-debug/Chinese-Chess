using UnityEngine;
using UnityEngine.InputSystem;

public class BlackHorseMovement : MonoBehaviour
{
    [Header("Board Settings")]
    public float squareSize = 1f;
    public Vector2 boardOrigin = new Vector2(-4f, -4.5f);

    [Header("Click Detection")]
    public float clickRadius = 0.5f;

    [Header("Visual Feedback")]
    public GameObject moveIndicatorPrefab;
    public Color selectedColor = Color.yellow;
    public Color validMoveColor = Color.green;

    private bool isSelected = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector2Int currentGridPos;
    private GameObject[] moveIndicators;
    private Camera mainCam;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        originalColor = spriteRenderer.color;
        UpdateGridPosition();
        moveIndicators = new GameObject[0];
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        if (isSelected)
        {
            Vector2Int clickedGrid = WorldToGrid(mouseWorldPos);

            if (IsValidMove(clickedGrid))
            {
                MoveTo(clickedGrid);
                Deselect();
                return;
            }

            if (clickedGrid == currentGridPos)
            {
                Deselect();
                return;
            }
        }

        if (IsClickedOnPiece(mouseWorldPos))
        {
            Select();
        }
    }

    bool IsClickedOnPiece(Vector2 worldPos)
    {
        float dist = Vector2.Distance(worldPos, transform.position);
        return dist <= clickRadius;
    }

    void Select()
    {
        isSelected = true;
        spriteRenderer.color = selectedColor;
        ShowValidMoves();
    }

    void Deselect()
    {
        isSelected = false;
        spriteRenderer.color = originalColor;
        HideValidMoves();
    }

    void MoveTo(Vector2Int targetGrid)
    {
        currentGridPos = targetGrid;
        transform.position = GridToWorld(targetGrid);
    }

    bool IsValidMove(Vector2Int target)
    {
        if (target == currentGridPos) return false;
        if (!IsInsideBoard(target)) return false;

        Vector2Int diff = target - currentGridPos;

        // Xiangqi horse moves in L-shape: 2+1 or 1+2
        bool isLShape = (Mathf.Abs(diff.x) == 2 && Mathf.Abs(diff.y) == 1) ||
                        (Mathf.Abs(diff.x) == 1 && Mathf.Abs(diff.y) == 2);

        if (!isLShape) return false;

        // Check blocking piece ("hobble the horse's leg")
        Vector2Int leg = GetBlockingLeg(diff);
        if (IsPieceAt(currentGridPos + leg)) return false;

        return true;
    }

    Vector2Int GetBlockingLeg(Vector2Int diff)
    {
        if (Mathf.Abs(diff.x) == 2)
            return new Vector2Int((int)Mathf.Sign(diff.x), 0);
        else
            return new Vector2Int(0, (int)Mathf.Sign(diff.y));
    }

    bool IsPieceAt(Vector2Int gridPos)
    {
        Vector2 worldPos = GridToWorld(gridPos);
        SpriteRenderer[] allSprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < allSprites.Length; i++)
        {
            if (allSprites[i].gameObject == gameObject) continue;
            if (Vector2.Distance(allSprites[i].transform.position, worldPos) < 0.1f)
                return true;
        }
        return false;
    }

    bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x <= 8 && pos.y >= 0 && pos.y <= 9;
    }

    Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - boardOrigin.x) / squareSize);
        int y = Mathf.RoundToInt((worldPos.y - boardOrigin.y) / squareSize);
        return new Vector2Int(x, y);
    }

    Vector2 GridToWorld(Vector2Int gridPos)
    {
        float x = boardOrigin.x + gridPos.x * squareSize;
        float y = boardOrigin.y + gridPos.y * squareSize;
        return new Vector2(x, y);
    }

    void UpdateGridPosition()
    {
        currentGridPos = WorldToGrid(transform.position);
    }

    void ShowValidMoves()
    {
        HideValidMoves();
        System.Collections.Generic.List<Vector2Int> moves = GetValidMoves();
        moveIndicators = new GameObject[moves.Count];

        for (int i = 0; i < moves.Count; i++)
        {
            Vector2 worldPos = GridToWorld(moves[i]);

            if (moveIndicatorPrefab != null)
            {
                moveIndicators[i] = Instantiate(moveIndicatorPrefab, worldPos, Quaternion.identity);
            }
            else
            {
                moveIndicators[i] = CreateDefaultIndicator(worldPos);
            }

            SpriteRenderer sr = moveIndicators[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = validMoveColor;
        }
    }

    void HideValidMoves()
    {
        for (int i = 0; i < moveIndicators.Length; i++)
        {
            if (moveIndicators[i] != null) Destroy(moveIndicators[i]);
        }
        moveIndicators = new GameObject[0];
    }

    GameObject CreateDefaultIndicator(Vector2 position)
    {
        GameObject indicator = new GameObject("MoveIndicator");
        indicator.transform.position = position;
        indicator.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.color = validMoveColor;

        return indicator;
    }

    System.Collections.Generic.List<Vector2Int> GetValidMoves()
    {
        System.Collections.Generic.List<Vector2Int> moves = new System.Collections.Generic.List<Vector2Int>();

        int[] dx = { 2, 2, -2, -2, 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1, 2, -2, 2, -2 };

        for (int i = 0; i < 8; i++)
        {
            Vector2Int target = currentGridPos + new Vector2Int(dx[i], dy[i]);
            if (IsValidMove(target))
            {
                moves.Add(target);
            }
        }

        return moves;
    }
}