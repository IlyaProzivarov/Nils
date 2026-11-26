using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PlatformFit : MonoBehaviour
{
    [Tooltip("Ширина (X), высота (Y), глубина (Z) в метрах")]
    public Vector3 size = new Vector3(4f, 2f, 4f);
    public float sink = 0.1f;            // насколько камень утоплен в землю
    public bool useMeshCollider = true;  // точный коллайдер по форме модели

    [ContextMenu("Fit To Size")]
    public void Fit()
    {
#if UNITY_EDITOR
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("PlatformFit: у объекта нет Mesh Renderer", this);
            return;
        }

        Undo.RecordObject(transform, "Fit platform");

        Vector3 want = transform.position;      // здесь должен оказаться центр
        Quaternion oldRot = transform.rotation;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        Bounds b = Measure(renderers);
        transform.localScale = new Vector3(
            size.x / Mathf.Max(0.001f, b.size.x),
            size.y / Mathf.Max(0.001f, b.size.y),
            size.z / Mathf.Max(0.001f, b.size.z));

        b = Measure(renderers);
        transform.position += new Vector3(
            want.x - b.center.x, -sink - b.min.y, want.z - b.center.z);
        transform.rotation = oldRot;

        if (useMeshCollider)
        {
            foreach (var c in GetComponentsInChildren<Collider>())
                Undo.DestroyObjectImmediate(c);

            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                if (mf.name.EndsWith("LOD1")) continue;
                Undo.AddComponent<MeshCollider>(mf.gameObject);
                break;
            }
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log(name + ": размер " + size + ", позиция " + transform.position, this);
#endif
    }

    static Bounds Measure(Renderer[] rs)
    {
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}