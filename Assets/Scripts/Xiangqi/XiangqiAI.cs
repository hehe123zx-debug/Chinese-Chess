using System.Collections.Generic;
using UnityEngine;

public class XiangqiAI
{
    private const int EMPTY = 0;

    private int[,] grid = new int[9, 10];
    private int maxDepth;
    private PieceColor aiColor;

    private static readonly int[] PieceValue =
        { 10000, 200, 200, 400, 900, 450, 100 };

    static int Encode(PieceType t, PieceColor c) =>
        ((int)t + 1) * (c == PieceColor.Red ? 1 : -1);

    static PieceType DecodeType(int v) => (PieceType)(Mathf.Abs(v) - 1);
    static PieceColor DecodeColor(int v) => v > 0 ? PieceColor.Red : PieceColor.Black;
    static bool IsEmpty(int v) => v == 0;
    static bool IsSameSide(int a, int b) => a != 0 && b != 0 && ((a > 0) == (b > 0));

    static PieceColor Opponent(PieceColor c) =>
        c == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

    static bool InBounds(int x, int y) => x >= 0 && x < 9 && y >= 0 && y < 10;
    static bool IsOrthogonal(int dx, int dy) => (dx == 0) ^ (dy == 0);
    static bool InPalace(int x, int y, PieceColor side)
    {
        if (x < 3 || x > 5) return false;
        return side == PieceColor.Red ? y >= 0 && y <= 2 : y >= 7 && y <= 9;
    }

    static int Sign(int v) => v > 0 ? 1 : v < 0 ? -1 : 0;

    public void LoadFrom(XiangqiBoard board)
    {
        System.Array.Clear(grid, 0, grid.Length);
        foreach (Piece piece in board.GetActivePieces())
        {
            Vector2Int p = piece.GridPos;
            grid[p.x, p.y] = Encode(piece.Type, piece.Color);
        }
    }

    public (Vector2Int from, Vector2Int to)? GetBestMove(XiangqiBoard board, PieceColor color, int depth)
    {
        LoadFrom(board);
        aiColor = color;
        maxDepth = depth;

        if (depth <= 0)
            return GetRandomMove(color);

        List<(Vector2Int, Vector2Int)> moves = GenerateAllMoves(color);
        if (moves.Count == 0) return null;

        OrderMoves(moves);

        float bestScore = float.NegativeInfinity;
        var bestMoves = new List<(Vector2Int, Vector2Int)>();

        foreach (var move in moves)
        {
            int captured = SimMakeMove(move.Item1, move.Item2);
            float score = -NegaMax(depth - 1, float.NegativeInfinity, float.PositiveInfinity, Opponent(color), 1);
            SimUndoMove(move.Item1, move.Item2, captured);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (Mathf.Approximately(score, bestScore))
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves[Random.Range(0, bestMoves.Count)];
    }

    private float NegaMax(int depth, float alpha, float beta, PieceColor side, int ply)
    {
        if (depth == 0) return EvaluateFor(side);

        List<(Vector2Int, Vector2Int)> moves = GenerateAllMoves(side);
        if (moves.Count == 0)
        {
            if (SimIsInCheck(side))
                return -100000 + ply;
            return -90000 + ply;
        }

        OrderMoves(moves);

        float best = float.NegativeInfinity;
        foreach (var move in moves)
        {
            int captured = SimMakeMove(move.Item1, move.Item2);
            float score = -NegaMax(depth - 1, -beta, -alpha, Opponent(side), ply + 1);
            SimUndoMove(move.Item1, move.Item2, captured);

            if (score > best) best = score;
            if (score > alpha) alpha = score;
            if (alpha >= beta) break;
        }

        return best;
    }

    private float EvaluateFor(PieceColor side)
    {
        float score = 0f;
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                int v = grid[x, y];
                if (v == 0) continue;
                PieceType type = DecodeType(v);
                PieceColor color = DecodeColor(v);
                float value = PieceValue[(int)type] + PositionalBonus(type, x, y, color);
                score += (color == side) ? value : -value;
            }
        }
        return score;
    }

    private float PositionalBonus(PieceType type, int x, int y, PieceColor color)
    {
        switch (type)
        {
            case PieceType.Horse:
            {
                float center = (4f - Mathf.Abs(x - 4)) * 4f;
                float edge = (x == 0 || x == 8) ? -40f : (x == 1 || x == 7) ? -15f : 0f;
                return center + edge;
            }
            case PieceType.Chariot:
                return (4f - Mathf.Abs(x - 4)) * 3f;
            case PieceType.Cannon:
                return (4f - Mathf.Abs(x - 4)) * 2f;
            case PieceType.Soldier:
            {
                bool crossed = color == PieceColor.Red ? y >= 5 : y <= 4;
                if (!crossed) return 0f;
                float river = 50f;
                float center = (4f - Mathf.Abs(x - 4)) * 8f;
                return river + center;
            }
            default:
                return 0f;
        }
    }

    private void OrderMoves(List<(Vector2Int, Vector2Int)> moves)
    {
        moves.Sort((a, b) =>
        {
            int valA = GetCaptureValue(a.Item2);
            int valB = GetCaptureValue(b.Item2);
            return valB.CompareTo(valA);
        });
    }

    private int GetCaptureValue(Vector2Int to)
    {
        int v = grid[to.x, to.y];
        if (v == EMPTY) return 0;
        return PieceValue[Mathf.Abs(v) - 1];
    }

    private (Vector2Int, Vector2Int)? GetRandomMove(PieceColor side)
    {
        List<(Vector2Int, Vector2Int)> moves = GenerateAllMoves(side);
        if (moves.Count == 0) return null;
        return moves[Random.Range(0, moves.Count)];
    }

    private int SimMakeMove(Vector2Int from, Vector2Int to)
    {
        int captured = grid[to.x, to.y];
        grid[to.x, to.y] = grid[from.x, from.y];
        grid[from.x, from.y] = EMPTY;
        return captured;
    }

    private void SimUndoMove(Vector2Int from, Vector2Int to, int captured)
    {
        grid[from.x, from.y] = grid[to.x, to.y];
        grid[to.x, to.y] = captured;
    }

    private bool SimIsInCheck(PieceColor side)
    {
        Vector2Int gen = FindGeneral(side);
        if (gen.x < 0) return true;
        PieceColor opp = Opponent(side);
        for (int x = 0; x < 9; x++)
            for (int y = 0; y < 10; y++)
            {
                int v = grid[x, y];
                if (v == 0 || DecodeColor(v) != opp) continue;
                if (SimCanAttack(x, y, gen.x, gen.y))
                    return true;
            }
        return false;
    }

    private bool SimCanAttack(int fx, int fy, int tx, int ty)
    {
        int v = grid[fx, fy];
        PieceType type = DecodeType(v);
        int dx = tx - fx;
        int dy = ty - fy;
        int adx = Mathf.Abs(dx);
        int ady = Mathf.Abs(dy);

        switch (type)
        {
            case PieceType.Chariot:
                return IsOrthogonal(dx, dy) && SimCountBetween(fx, fy, tx, ty) == 0;

            case PieceType.Cannon:
                return IsOrthogonal(dx, dy) && SimCountBetween(fx, fy, tx, ty) == 1;

            case PieceType.Horse:
            {
                if (adx == 2 && ady == 1)
                {
                    int lx = fx + Sign(dx);
                    return grid[lx, fy] == EMPTY;
                }
                if (adx == 1 && ady == 2)
                {
                    int ly = fy + Sign(dy);
                    return grid[fx, ly] == EMPTY;
                }
                return false;
            }

            case PieceType.Elephant:
            {
                if (adx != 2 || ady != 2) return false;
                int ex = fx + dx / 2;
                int ey = fy + dy / 2;
                PieceColor color = DecodeColor(v);
                if (color == PieceColor.Red && ty > 4) return false;
                if (color == PieceColor.Black && ty < 5) return false;
                return grid[ex, ey] == EMPTY;
            }

            case PieceType.Advisor:
                return adx == 1 && ady == 1;

            case PieceType.General:
            {
                if (dx == 0 && SimCountBetween(fx, fy, tx, ty) == 0) return true;
                return adx + ady == 1;
            }

            case PieceType.Soldier:
            {
                PieceColor color = DecodeColor(v);
                int fwd = color == PieceColor.Red ? 1 : -1;
                if (dy == fwd && dx == 0) return true;
                bool crossed = color == PieceColor.Red ? fy >= 5 : fy <= 4;
                return crossed && dy == 0 && adx == 1;
            }

            default:
                return false;
        }
    }

    private int SimCountBetween(int ax, int ay, int bx, int by)
    {
        int count = 0;
        if (ax == bx)
        {
            int step = Sign(by - ay);
            for (int y = ay + step; y != by; y += step)
                if (grid[ax, y] != EMPTY) count++;
        }
        else if (ay == by)
        {
            int step = Sign(bx - ax);
            for (int x = ax + step; x != bx; x += step)
                if (grid[x, ay] != EMPTY) count++;
        }
        return count;
    }

    private Vector2Int FindGeneral(PieceColor side)
    {
        int target = Encode(PieceType.General, side);
        for (int x = 0; x < 9; x++)
            for (int y = 0; y < 10; y++)
                if (grid[x, y] == target) return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }

    private List<(Vector2Int, Vector2Int)> GenerateAllMoves(PieceColor side)
    {
        var moves = new List<(Vector2Int, Vector2Int)>();

        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                int v = grid[x, y];
                if (v == EMPTY || DecodeColor(v) != side) continue;
                Vector2Int from = new Vector2Int(x, y);

                switch (DecodeType(v))
                {
                    case PieceType.General:
                        AddStepMoves(from, side, moves, 4, new[] { 0, 0, 1, -1 }, new[] { 1, -1, 0, 0 });
                        break;
                    case PieceType.Advisor:
                        AddStepMoves(from, side, moves, 4, new[] { 1, 1, -1, -1 }, new[] { 1, -1, 1, -1 });
                        break;
                    case PieceType.Elephant:
                        AddElephantMoves(from, side, moves);
                        break;
                    case PieceType.Horse:
                        AddHorseMoves(from, side, moves);
                        break;
                    case PieceType.Chariot:
                        AddLineMoves(from, side, moves, false);
                        break;
                    case PieceType.Cannon:
                        AddLineMoves(from, side, moves, true);
                        break;
                    case PieceType.Soldier:
                        AddSoldierMoves(from, side, moves);
                        break;
                }
            }
        }

        var legal = new List<(Vector2Int, Vector2Int)>();
        foreach (var move in moves)
        {
            int captured = SimMakeMove(move.Item1, move.Item2);
            bool inCheck = SimIsInCheck(side);
            SimUndoMove(move.Item1, move.Item2, captured);
            if (!inCheck)
                legal.Add(move);
        }

        return legal;
    }

    private void AddStepMoves(Vector2Int from, PieceColor side, List<(Vector2Int, Vector2Int)> moves,
        int count, int[] dx, int[] dy)
    {
        for (int i = 0; i < count; i++)
        {
            int tx = from.x + dx[i];
            int ty = from.y + dy[i];
            if (!InBounds(tx, ty)) continue;
            if (!InPalace(tx, ty, side)) continue;
            if (grid[tx, ty] != EMPTY && IsSameSide(grid[from.x, from.y], grid[tx, ty])) continue;
            moves.Add((from, new Vector2Int(tx, ty)));
        }
    }

    private void AddElephantMoves(Vector2Int from, PieceColor side, List<(Vector2Int, Vector2Int)> moves)
    {
        int[] dx = { 2, 2, -2, -2 };
        int[] dy = { 2, -2, 2, -2 };
        for (int i = 0; i < 4; i++)
        {
            int tx = from.x + dx[i];
            int ty = from.y + dy[i];
            if (!InBounds(tx, ty)) continue;
            if (side == PieceColor.Red && ty > 4) continue;
            if (side == PieceColor.Black && ty < 5) continue;
            int ex = from.x + dx[i] / 2;
            int ey = from.y + dy[i] / 2;
            if (grid[ex, ey] != EMPTY) continue;
            if (grid[tx, ty] != EMPTY && IsSameSide(grid[from.x, from.y], grid[tx, ty])) continue;
            moves.Add((from, new Vector2Int(tx, ty)));
        }
    }

    private void AddHorseMoves(Vector2Int from, PieceColor side, List<(Vector2Int, Vector2Int)> moves)
    {
        int[] dx = { 1, 1, -1, -1, 2, 2, -2, -2 };
        int[] dy = { 2, -2, 2, -2, 1, -1, 1, -1 };
        int[] legX = { 0, 0, 0, 0, 1, 1, -1, -1 };
        int[] legY = { 1, -1, 1, -1, 0, 0, 0, 0 };

        for (int i = 0; i < 8; i++)
        {
            int tx = from.x + dx[i];
            int ty = from.y + dy[i];
            if (!InBounds(tx, ty)) continue;
            if (grid[from.x + legX[i], from.y + legY[i]] != EMPTY) continue;
            if (grid[tx, ty] != EMPTY && IsSameSide(grid[from.x, from.y], grid[tx, ty])) continue;
            moves.Add((from, new Vector2Int(tx, ty)));
        }
    }

    private void AddLineMoves(Vector2Int from, PieceColor side, List<(Vector2Int, Vector2Int)> moves, bool isCannon)
    {
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int d = 0; d < 4; d++)
        {
            bool foundScreen = false;
            for (int step = 1; step < 10; step++)
            {
                int tx = from.x + dx[d] * step;
                int ty = from.y + dy[d] * step;
                if (!InBounds(tx, ty)) break;

                if (!foundScreen)
                {
                    if (grid[tx, ty] != EMPTY)
                    {
                        if (isCannon)
                            foundScreen = true;
                        else
                        {
                            if (!IsSameSide(grid[from.x, from.y], grid[tx, ty]))
                                moves.Add((from, new Vector2Int(tx, ty)));
                            break;
                        }
                    }
                    else
                    {
                        moves.Add((from, new Vector2Int(tx, ty)));
                    }
                }
                else
                {
                    if (grid[tx, ty] != EMPTY)
                    {
                        if (!IsSameSide(grid[from.x, from.y], grid[tx, ty]))
                            moves.Add((from, new Vector2Int(tx, ty)));
                        break;
                    }
                }
            }
        }
    }

    private void AddSoldierMoves(Vector2Int from, PieceColor side, List<(Vector2Int, Vector2Int)> moves)
    {
        int fwd = side == PieceColor.Red ? 1 : -1;
        int tx = from.x;
        int ty = from.y + fwd;
        if (InBounds(tx, ty) && (grid[tx, ty] == EMPTY || !IsSameSide(grid[from.x, from.y], grid[tx, ty])))
            moves.Add((from, new Vector2Int(tx, ty)));

        bool crossed = side == PieceColor.Red ? from.y >= 5 : from.y <= 4;
        if (crossed)
        {
            for (int dir = -1; dir <= 1; dir += 2)
            {
                int sx = from.x + dir;
                if (InBounds(sx, from.y) && (grid[sx, from.y] == EMPTY || !IsSameSide(grid[from.x, from.y], grid[sx, from.y])))
                    moves.Add((from, new Vector2Int(sx, from.y)));
            }
        }
    }
}