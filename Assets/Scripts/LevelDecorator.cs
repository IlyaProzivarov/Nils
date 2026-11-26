using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ScatterGroup
{
    public string name = "Группа";
    public GameObject[] prefabs;
    public int count = 20;
    public Vector2 heightRange = new Vector2(1f, 2f);   
    public float freeRadius = 1f;                        
    public bool removeColliders = false;                 
}

public class LevelDecorator : MonoBehaviour
{
    [Header("Арена")]
    public float halfSize = 30f;            
    public int seed = 12345;                
    public Transform keepClearCenter;       
    public float keepClearRadius = 6f;

    [Header("Лес по краю")]
    public GameObject[] edgeTrees;
    public Vector2 edgeHeightRange = new Vector2(7f, 11f);
    public float edgeInset = 1.5f;
    public float edgeSpacing = 3f;
    public int edgeRows = 2;

    [Header("Разбросанные объекты внутри арены")]
    public ScatterGroup[] groups;

    [ContextMenu("Generate")]
    public void Generate()
    {
#if UNITY_EDITOR
        Clear();
        Random.InitState(seed);
        Physics.SyncTransforms();

        var root = new GameObject("Generated").transform;
        root.SetParent(transform, false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate level decor");

        // Лес по краю
        if (edgeTrees != null && edgeTrees.Length > 0)
        {
            var edge = new GameObject("EdgeForest").transform;
            edge.SetParent(root, false);

            for (int row = 0; row < edgeRows; row++)
            {
                float d = halfSize - edgeInset - row * 2.5f;
                float start = -halfSize + row * edgeSpacing * 0.5f;
                for (float t = start; t <= halfSize; t += edgeSpacing)
                {
                    float j1 = Random.Range(-0.7f, 0.7f);
                    float j2 = Random.Range(-0.7f, 0.7f);
                    Spawn(edgeTrees, new Vector3(t + j1, 0f, d + j2), edgeHeightRange, edge, false);
                    Spawn(edgeTrees, new Vector3(t + j1, 0f, -d + j2), edgeHeightRange, edge, false);
                    Spawn(edgeTrees, new Vector3(d + j2, 0f, t + j1), edgeHeightRange, edge, false);
                    Spawn(edgeTrees, new Vector3(-d + j2, 0f, t + j1), edgeHeightRange, edge, false);
                }
            }
        }

        // Разбросанные группы
        if (groups != null)
        {
            foreach (var g in groups)
            {
                if (g.prefabs == null || g.prefabs.Length == 0) continue;

                var parent = new GameObject(g.name).transform;
                parent.SetParent(root, false);

                int placed = 0, tries = 0;
                float m = halfSize - 4f;
                while (placed < g.count && tries < g.count * 40)
                {
                    tries++;
                    Vector3 pos = new Vector3(Random.Range(-m, m), 0f, Random.Range(-m, m));

                    if (keepClearCenter != null)
                    {
                        Vector3 c = keepClearCenter.position;
                        if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(c.x, c.z)) < keepClearRadius)
                            continue;
                    }

                    // Место занято платформой, грибом или другим деревом
                    Physics.SyncTransforms();
                    Vector3 probe = pos + Vector3.up * (g.freeRadius + 0.5f);
                    if (Physics.CheckSphere(probe, g.freeRadius)) continue;

                    Spawn(g.prefabs, pos, g.heightRange, parent, g.removeColliders);
                    placed++;
                }
                Debug.Log("LevelDecorator: " + g.name + " - поставлено " + placed + " из " + g.count);
            }
        }
#endif
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
#if UNITY_EDITOR
        var old = transform.Find("Generated");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
#endif
    }

#if UNITY_EDITOR
    void Spawn(GameObject[] prefabs, Vector3 pos, Vector2 heightRange, Transform parent, bool removeColliders)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // Подгоняем размер: итоговая высота в метрах
        Bounds b = GetBounds(go);
        float h = Random.Range(heightRange.x, heightRange.y);
        go.transform.localScale = Vector3.one * (h / Mathf.Max(0.001f, b.size.y));
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Ставим на землю
        b = GetBounds(go);
        go.transform.position += Vector3.up * (-0.05f - b.min.y);

        if (removeColliders)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                DestroyImmediate(c);
        }
    }

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