using UnityEngine;

public class GnomeAnimator : MonoBehaviour
{
    [Header("Бег")]
    public float legSwing = 45f;      // размах ног
    public float armSwing = 35f;      // размах рук
    public float strideRate = 2f;     // чем больше, тем чаще шаги
    public float bobHeight = 0.05f;   // подпрыгивание тела при беге 
    public float leanAngle = 8f;      // наклон вперёд при беге

    [Header("Воздух")]
    public float airArmAngle = 140f;  // руки вверх при прыжке
    public float airLegSpread = 35f;

    [Header("Сжатие и растяжение")]
    public float stretchAmount = 0.15f;  // растяжение в полёте
    public float landSquash = 0.22f;     // сплющивание при приземлении
    public float landTime = 0.2f;

    [Header("Плавность")]
    public float smooth = 16f;

    // Одна подвижная часть
    class Part
    {
        public Transform t;      // что вращаем (опора для рук и ног, сама часть для остальных)
        public Quaternion baseRot;
        public Vector3 axis;     // ось качания в пространстве родителя
        public float angle;      // текущий сглаженный угол
    }

    PlayerController player;
    CharacterController cc;
    Transform animRoot;
    Part legL, legR, armL, armR, head, hat, basket;
    Vector3 latWorld;
    Vector3 latPlayerLocal;
    float swingSign = 1f;

    float phase;
    float airTime;
    bool wasInAir;
    float fallSpeed;
    float landTimer;
    float landStrength;
    float stretchCur = 1f;
    float bobCur;
    float leanCur;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (player == null)
        {
            Debug.LogError("GnomeAnimator: у родителя нет PlayerController", this);
            enabled = false;
            return;
        }
        cc = player.GetComponent<CharacterController>();

        Transform tLegL = FindPart("Leg_L");
        Transform tLegR = FindPart("Leg_R");
        Transform tArmL = FindPart("Arm_L");
        Transform tArmR = FindPart("Arm_R");
        if (tLegL == null || tLegR == null || tArmL == null || tArmR == null)
        {
            Debug.LogWarning("GnomeAnimator: не найдены ноги или руки. В названиях частей должно быть Leg_L, Leg_R, Arm_L, Arm_R", this);
            enabled = false;
            return;
        }

        // Ось качания проходит от одной руки к другой
        latWorld = tArmL.position - tArmR.position;
        latWorld.y = 0f;
        if (latWorld.sqrMagnitude < 0.0001f) latWorld = transform.right;
        latWorld.Normalize();
        latPlayerLocal = player.transform.InverseTransformDirection(latWorld);

        // В какую сторону крутить, чтобы нога пошла вперёд 
        float side = Vector3.Dot(player.transform.forward, Vector3.Cross(latWorld, Vector3.down));
        swingSign = side >= 0f ? 1f : -1f;

        legL = MakePart(tLegL, true);
        legR = MakePart(tLegR, true);
        armL = MakePart(tArmL, true);
        armR = MakePart(tArmR, true);

        Transform tHead = FindPart("Head");
        Transform tHat = FindPart("Hat");
        Transform tBasket = FindPart("Basket");
        if (tHead != null) head = MakePart(tHead, false);
        if (tHat != null) hat = MakePart(tHat, false);
        if (tBasket != null) basket = MakePart(tBasket, false);

        // Обёртка в точке ног игрока: подпрыгивание, наклон и сжатие идут от земли
        animRoot = new GameObject("AnimRoot").transform;
        animRoot.SetParent(player.transform, false);
        transform.SetParent(animRoot, true);
    }

    Transform FindPart(string key)
    {
        foreach (var tr in GetComponentsInChildren<Transform>(true))
            if (tr.name.Contains(key)) return tr;
        return null;
    }

    Part MakePart(Transform part, bool pivotAtTop)
    {
        Transform t = part;

        if (pivotAtTop)
        {
            // Опора в верхней точке конечности: бедро или плечо
            var r = part.GetComponent<Renderer>();
            if (r == null) r = part.GetComponentInChildren<Renderer>();
            Bounds b = r.bounds;

            var pv = new GameObject(part.name + "_Pivot").transform;
            pv.SetParent(part.parent, false);
            pv.localRotation = Quaternion.identity;
            pv.position = new Vector3(b.center.x, b.max.y, b.center.z);
            part.SetParent(pv, true);
            t = pv;
        }

        return new Part
        {
            t = t,
            baseRot = t.localRotation,
            axis = t.parent.InverseTransformDirection(latWorld).normalized
        };
    }

    // Конечность: положительный угол двигает ногу или руку вперёд
    void ApplyLimb(Part p, float target, float k)
    {
        p.angle = Mathf.Lerp(p.angle, target, k);
        p.t.localRotation = Quaternion.AngleAxis(p.angle * swingSign, p.axis) * p.baseRot;
    }

    // Голова, шапка, корзина: положительный угол наклоняет верх вперёд
    void ApplyTop(Part p, float target, float k)
    {
        if (p == null) return;
        p.angle = Mathf.Lerp(p.angle, target, k);
        p.t.localRotation = Quaternion.AngleAxis(-p.angle * swingSign, p.axis) * p.baseRot;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        float k = 1f - Mathf.Exp(-smooth * dt);
        float time = Time.time;

        float speed = player.HorizontalSpeed;
        float moveT = Mathf.Clamp01(speed / 5f);
        float vy = cc != null ? cc.velocity.y : 0f;

        // Земля или воздух 
        if (player.IsGrounded) airTime = 0f; else airTime += dt;
        bool inAir = airTime > 0.08f;

        if (inAir) fallSpeed = Mathf.Max(fallSpeed, -vy);

        // Момент приземления
        if (wasInAir && !inAir)
        {
            landTimer = landTime;
            landStrength = Mathf.Clamp(fallSpeed / 15f, 0.25f, 1f);
            fallSpeed = 0f;
        }
        wasInAir = inAir;

        // Целевые углы
        float tLegL, tLegR, tArmL, tArmR, tHat, tBasket, tHead, tLean, tBob = 0f, tStretch = 1f;

        if (inAir)
        {
            float arms = vy > 0f ? airArmAngle : airArmAngle * 0.5f;
            tLegL = airLegSpread;
            tLegR = -airLegSpread * 0.6f;
            tArmL = arms;
            tArmR = arms;
            tHat = -Mathf.Clamp(vy, -10f, 10f) * 1.2f;
            tBasket = -Mathf.Clamp(vy, -10f, 10f) * 0.9f;
            tHead = 0f;
            tLean = 0f;
            tStretch = 1f + Mathf.Clamp01(Mathf.Abs(vy) / 14f) * stretchAmount;
        }
        else
        {
            // Фаза шага растёт только пока гном бежит по земле
            phase += speed * strideRate * dt;
            float s = Mathf.Sin(phase);
            float idle = 1f - moveT;
            float sway = Mathf.Sin(time * 1.6f) * 3f * idle;

            tLegL = s * legSwing * moveT;
            tLegR = -s * legSwing * moveT;
            tArmL = -s * armSwing * moveT + sway;
            tArmR = s * armSwing * moveT - sway;
            tHat = -10f * moveT + Mathf.Sin(phase * 2f) * 3f * moveT;
            tBasket = -8f * moveT + Mathf.Sin(phase * 2f + 1f) * 4f * moveT;
            tHead = Mathf.Sin(phase * 2f) * 2f * moveT;
            tLean = leanAngle * moveT;
            tBob = Mathf.Abs(s) * bobHeight * moveT;
        }

        ApplyLimb(legL, tLegL, k);
        ApplyLimb(legR, tLegR, k);
        ApplyLimb(armL, tArmL, k);
        ApplyLimb(armR, tArmR, k);
        ApplyTop(hat, tHat, k);
        ApplyTop(basket, tBasket, k);
        ApplyTop(head, tHead, k);

        // Всё тело: подпрыгивание, наклон, растяжение и сжатие от точки ног
        leanCur = Mathf.Lerp(leanCur, tLean, k);
        bobCur = Mathf.Lerp(bobCur, tBob, k);
        stretchCur = Mathf.Lerp(stretchCur, tStretch, k);

        float sy = stretchCur;
        if (landTimer > 0f)
        {
            landTimer -= dt;
            float u = 1f - Mathf.Max(0f, landTimer) / Mathf.Max(0.01f, landTime);
            sy *= 1f - landSquash * landStrength * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
        }
        sy *= 1f + Mathf.Sin(time * 2.2f) * 0.012f * (1f - moveT);   // дыхание
        float sxz = 1f / Mathf.Sqrt(sy);                              // объём сохраняется

        animRoot.localScale = new Vector3(sxz, sy, sxz);
        animRoot.localPosition = new Vector3(0f, bobCur, 0f);
        animRoot.localRotation = Quaternion.AngleAxis(-swingSign * leanCur, latPlayerLocal);
    }
}