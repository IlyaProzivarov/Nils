using System.Collections;
using UnityEngine;

public class BounceMushroom : MonoBehaviour
{
    [Header("Отскок")]
    public float bounceVelocity = 14f;    // обычный
    public float boostedVelocity = 20f;   // если держишь пробел
    public float boostWindow = 0.2f;      // нажатие чуть до касания тоже считается

    [Header("Анимация")]
    public float squashAmount = 0.3f;
    public float squashTime = 0.3f;

    Vector3 baseScale;
    Coroutine squashRoutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Launch(PlayerController player)
    {
        bool boosted = player.JumpHeld ||
                       Time.time - player.LastJumpPressTime <= boostWindow;

        player.Bounce(boosted ? boostedVelocity : bounceVelocity);

        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(Squash());
    }

    // Гриб приседает и пружинит обратно
    IEnumerator Squash()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, squashTime);
            float k = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);  
            transform.localScale = new Vector3(
                baseScale.x * (1f + k * squashAmount * 0.5f),
                baseScale.y * (1f - k * squashAmount),
                baseScale.z * (1f + k * squashAmount * 0.5f));
            yield return null;
        }
        transform.localScale = baseScale;
        squashRoutine = null;
    }
}