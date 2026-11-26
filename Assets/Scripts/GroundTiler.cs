using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GroundTiler : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public float halfSize = 30f;      
    public float tileSize = 10f;      
    public float overlap = 1.5f;      
    public float maxHeight = 0.3f;    
    public int seed = 7;

    [ContextMenu("Generate")]
    public void Generate()
    {
#if UNITY_EDITOR
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogWarning("GroundTiler: не заполнен список Tile Prefabs", this);
            return;
        }

        Clear();
        Random.InitState(seed);

        var root = new GameObject("GeneratedTiles").transform;
        root.SetParent(transform, false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate ground tiles");

        int count = Mathf.CeilToInt(halfSize * 2f / tileSize);
        float start = -halfSize + tileSize * 0.5f;
        float size = tileSize + overlap;
        int index = 0;

        for (int ix = 0; ix < count; ix++)
        {
            for (int iz = 0; iz < count; iz++)
            {
                var prefab = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
                if (prefab == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.transform.position = Vector3.zero;

                // Подгоняем ширину и глубину под нужный размер
                Bounds b = GetBounds(go);
                go.transform.localScale = new Vector3(
                    size / Mathf.Max(0.001f, b.size.x), 1f,
                    size / Mathf.Max(0.001f, b.size.z));

                // Сплющиваем неровности
                b = GetBounds(go);
                if (b.size.y > maxHeight)
                {
                    Vector3 s = go.transform.localScale;
                    s.y = maxHeight / b.size.y;
                    go.transform.localScale = s;
                }

                go.transform.rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

                // Каждая плитка на долю миллиметра выше предыдущей, чтобы края не мерцали
                b = GetBounds(go);
                Vector3 want = new Vector3(
                    start + ix * tileSize, 0.01f + index * 0.0015f, start + iz * tileSize);
                go.transform.position += new Vector3(
                    want.x - b.center.x, want.y - b.min.y, want.z - b.center.z);

                // Коллайдеры не нужны: ходим по плоскости Ground
                foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;

                index++;
            }
        }
        Debug.Log("GroundTiler: выложено плиток: " + index);
#endif
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
#if UNITY_EDITOR
        var old = transform.Find("GeneratedTiles");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
#endif
    }

#if UNITY_EDITOR
    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
#endif
}