using UnityEngine;

public enum PieceColor
{
    Red,
    Black
}

public enum PieceType
{
    General,
    Advisor,
    Elephant,
    Horse,
    Chariot,
    Cannon,
    Soldier
}

public class Piece : MonoBehaviour
{
    public PieceColor Color { get; private set; }
    public PieceType Type { get; private set; }
    public Vector2Int GridPos { get; private set; }
    public XiangqiBoard Board { get; private set; }

    private SpriteRenderer spriteRenderer;
    private UnityEngine.Color defaultColor = UnityEngine.Color.white;

    public void Init(XiangqiBoard board, PieceType type, PieceColor color, Vector2Int gridPos)
    {
        Board = board;
        Type = type;
        Color = color;
        GridPos = gridPos;
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultColor = spriteRenderer != null ? spriteRenderer.color : UnityEngine.Color.white;
    }

    public void MoveTo(Vector2Int gridPos)
    {
        GridPos = gridPos;
        transform.localPosition = (Vector3)Board.GridToWorld(gridPos);
    }

    public void OnSelect(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void OnDeselect()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }
}