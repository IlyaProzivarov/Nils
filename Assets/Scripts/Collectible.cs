using System;
using System.Collections;
using UnityEngine;

public enum CollectibleType { Points, ExtraTime }

public class Collectible : MonoBehaviour
{
    [Header("Что даёт")]
    public CollectibleType type = CollectibleType.Points;
    public int pointsValue = 10;
    public float timeBonus = 5f;

    [Header("Анимация")]
    public Transform model;           // дочерний объект с видом гриба
    public float spinSpeed = 90f;     // градусов в секунду
    public float hoverHeight = 0.3f;  // на какой высоте парит
    public float bobHeight = 0.15f;   // амплитуда покачивания
    public float bobSpeed = 2f;

    public static event Action<Collectible> Collected;

    bool collected;
    float phase;

    void Start()
    {
        phase = UnityEngine.Random.value * 10f;   // чтобы грибы качались не синхронно
    }

    void Update()
    {
        if (collected || model == null) return;

        model.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        Vector3 p = model.localPosition;
        p.y = hoverHeight + Mathf.Sin((Time.time + phase) * bobSpeed) * bobHeight;
        model.localPosition = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        collected = true;
        Debug.Log("Собран гриб: " + type + " (очки " + pointsValue + ", время +" + timeBonus + ")");
        Collected?.Invoke(this);
        StartCoroutine(PopAndDestroy());
    }

    // Гриб надувается и лопается, во все стороны летят споры
    IEnumerator PopAndDestroy()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Material mat = null;
        if (model != null)
        {
            var r = model.GetComponentInChildren<Renderer>();
            if (r != null) mat = r.sharedMaterial;
        }

        const int n = 8;
        var spores = new Transform[n];
        var vel = new Vector3[n];
        Vector3 center = transform.position + Vector3.up * 0.7f;

        for (int i = 0; i < n; i++)
        {
            var sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sp.GetComponent<Collider>());
            sp.transform.position = center;
            sp.transform.localScale = Vector3.one * 0.15f;

            var rend = sp.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mat != null) rend.sharedMaterial = mat;

            Vector2 d = UnityEngine.Random.insideUnitCircle.normalized;
            vel[i] = new Vector3(d.x * 2.5f, UnityEngine.Random.Range(3f, 5f), d.y * 2.5f);
            spores[i] = sp.transform;
        }

        Vector3 startScale = model != null ? model.localScale : Vector3.one;
        float t = 0f;
        const float duration = 0.45f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);

            if (model != null)
            {
                // сначала чуть вырастает, потом исчезает
                float s = k < 0.25f
                    ? Mathf.Lerp(1f, 1.3f, k / 0.25f)
                    : Mathf.Lerp(1.3f, 0f, (k - 0.25f) / 0.75f);
                model.localScale = startScale * s;
            }

            for (int i = 0; i < n; i++)
            {
                vel[i] += Physics.gravity * Time.deltaTime;
                spores[i].position += vel[i] * Time.deltaTime;
                spores[i].localScale = Vector3.one * 0.15f * (1f - k);
            }
            yield return null;
        }

        for (int i = 0; i < n; i++) Destroy(spores[i].gameObject);
        Destroy(gameObject);
    }
}