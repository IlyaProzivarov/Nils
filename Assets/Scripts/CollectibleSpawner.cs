using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Префабы")]
    public GameObject pointsPrefab;
    public GameObject timePrefab;

    [Header("Сколько держать на уровне")]
    public int pointsCount = 8;
    public int timeCount = 3;
    public float respawnDelay = 1.5f;      

    [Header("Где появляться")]
    public float halfSize = 30f;         
    public float edgeMargin = 7f;         
    public float groundLevel = 0.15f;      
    public Transform platformsRoot;        
    [Range(0f, 1f)]
    public float platformChance = 0.35f;   

    [Header("Свободное место")]
    public float minDistanceBetween = 4f;
    public float minDistanceFromPlayer = 3f;
    public float clearRadius = 1f;

    [Header("Бонус очков за высоту (жёлтые грибы)")]
    public bool heightBonus = true;
    public float midHeight = 2f;           
    public float highHeight = 6f;          

    readonly List<Collectible> active = new List<Collectible>();
    readonly List<Collider> platformColliders = new List<Collider>();
    Transform container;
    Transform player;
    float nextSpawnTime;

    void Start()
    {
        container = new GameObject("Collectibles").transform;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;

        if (platformsRoot != null)
            platformColliders.AddRange(platformsRoot.GetComponentsInChildren<Collider>());
        else
            Debug.LogWarning("CollectibleSpawner: не назначен Platforms Root, грибы будут только на земле", this);

        Collectible.Collected += OnCollected;

        for (int i = 0; i < pointsCount; i++) Spawn(pointsPrefab);
        for (int i = 0; i < timeCount; i++) Spawn(timePrefab);

        int elevated = 0;
        foreach (var c in active)
            if (c != null && c.transform.position.y > 1f) elevated++;
        Debug.Log("CollectibleSpawner: появилось грибов " + active.Count +
                  ", из них на платформах: " + elevated);
    }

    void OnDestroy()
    {
        Collectible.Collected -= OnCollected;
    }

    void Update()
    {
        if (Time.time < nextSpawnTime) return;

        if (Count(CollectibleType.Points) < pointsCount) Spawn(pointsPrefab);
        else if (Count(CollectibleType.ExtraTime) < timeCount) Spawn(timePrefab);
        else return;

        nextSpawnTime = Time.time + respawnDelay;
    }

    void OnCollected(Collectible c)
    {
        active.Remove(c);
        nextSpawnTime = Mathf.Max(nextSpawnTime, Time.time + respawnDelay);
    }

    int Count(CollectibleType type)
    {
        active.RemoveAll(c => c == null);
        int n = 0;
        foreach (var c in active)
            if (c.type == type) n++;
        return n;
    }

    bool Spawn(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("CollectibleSpawner: не назначен префаб гриба", this);
            return false;
        }

        for (int attempt = 0; attempt < 60; attempt++)
        {
            if (!TryFindPoint(out Vector3 p)) continue;

            var go = Instantiate(prefab, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), container);
            var c = go.GetComponent<Collectible>();
            if (c == null)
            {
                Debug.LogError("CollectibleSpawner: у префаба нет компонента Collectible", prefab);
                Destroy(go);
                return false;
            }

            // Чем выше гриб, тем больше очков
            if (heightBonus && c.type == CollectibleType.Points)
            {
                if (p.y >= highHeight) c.pointsValue *= 3;
                else if (p.y >= midHeight) c.pointsValue *= 2;
            }

            active.Add(c);
            return true;
        }

        Debug.LogWarning("CollectibleSpawner: не нашлось свободного места для гриба", this);
        return false;
    }

    bool TryFindPoint(out Vector3 p)
    {
        p = Vector3.zero;
        bool onPlatform = platformColliders.Count > 0 && Random.value < platformChance;

        if (onPlatform)
        {
            // Случайная точка над платформой, луч вниз только по её коллайдеру
            var col = platformColliders[Random.Range(0, platformColliders.Count)];
            if (col == null) return false;

            Bounds b = col.bounds;
            float mx = Mathf.Min(0.8f, b.extents.x * 0.5f);
            float mz = Mathf.Min(0.8f, b.extents.z * 0.5f);
            Vector3 origin = new Vector3(
                Random.Range(b.min.x + mx, b.max.x - mx),
                b.max.y + 1f,
                Random.Range(b.min.z + mz, b.max.z - mz));

            if (!col.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, b.size.y + 3f))
                return false;
            if (hit.normal.y < 0.75f) return false;   // крутой склон: гриб скатился бы

            p = hit.point;
        }
        else
        {
            float m = halfSize - edgeMargin;
            p = new Vector3(Random.Range(-m, m), groundLevel, Random.Range(-m, m));
        }

        if (IsBlocked(p, onPlatform)) return false;

        if (player != null && Vector3.Distance(p, player.position) < minDistanceFromPlayer)
            return false;

        foreach (var c in active)
        {
            if (c != null && Vector3.Distance(p, c.transform.position) < minDistanceBetween)
                return false;
        }
        return true;
    }

    // Не торчит ли в этом месте дерево, камень или другой предмет
    bool IsBlocked(Vector3 p, bool ignorePlatforms)
    {
        Vector3 center = p + Vector3.up * (clearRadius + 0.3f);
        var hits = Physics.OverlapSphere(center, clearRadius, ~(1 << 2), QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (ignorePlatforms && platformColliders.Contains(h)) continue;
            return true;
        }
        return false;
    }
}