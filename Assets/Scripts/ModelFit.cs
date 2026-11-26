using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ModelFit : MonoBehaviour
{
    public float targetHeight = 1.4f;     
    public bool removeColliders = true;   

    [ContextMenu("Fit To Height")]
    public void Fit()
    {
#if UNITY_EDITOR
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("ModelFit: у модели нет Mesh Renderer", this);
            return;
        }

        Undo.RecordObject(transform, "Fit model");

        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        Bounds b = Measure(renderers);
        float k = targetHeight / Mathf.Max(0.001f, b.size.y);
        transform.localScale = Vector3.one * k;

        // Ноги на уровне корня игрока, модель по центру
        b = Measure(renderers);
        Vector3 anchor = transform.parent != null ? transform.parent.position : Vector3.zero;
        transform.position += new Vector3(
            anchor.x - b.center.x, anchor.y - b.min.y, anchor.z - b.center.z);

        if (removeColliders)
        {
            foreach (var c in GetComponentsInChildren<Collider>())
                Undo.DestroyObjectImmediate(c);
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("ModelFit: масштаб " + k.ToString("0.###") + ", высота " + targetHeight + " м", this);
#endif
    }

    static Bounds Measure(Renderer[] rs)
    {
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}