using UnityEngine;
using UnityEditor;

public class CreateCircle
{
    [MenuItem("GameObject/2D Object/Circle", false, 10)]
    static void CreateCircleObject()
    {
        GameObject go = new GameObject("Circle");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<CircleGenerator>();
        Undo.RegisterCreatedObjectUndo(go, "Create Circle");
        Selection.activeGameObject = go;
    }
}
