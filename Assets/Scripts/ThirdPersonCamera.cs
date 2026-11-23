using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Расстояние")]
    public float distance = 7f;
    public float minDistance = 3f;
    public float maxDistance = 12f;

    [Header("Вращение")]
    public float mouseSensitivity = 3f;
    public float minPitch = -10f;
    public float maxPitch = 70f;
    public float followSmooth = 12f;

    float yaw;
    float pitch = 25f;
    Vector3 smoothFocus;

    void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            smoothFocus = target.position + targetOffset;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (Time.timeScale > 0f)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, minDistance, maxDistance);
        }

        Vector3 focus = target.position + targetOffset;
        smoothFocus = Vector3.Lerp(smoothFocus, focus, 1f - Mathf.Exp(-followSmooth * Time.unscaledDeltaTime));

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 dir = rot * Vector3.back;

        // Камера не уходит за стены и деревья
        float dist = distance;
        if (Physics.SphereCast(smoothFocus, 0.3f, dir, out RaycastHit hit, distance,
                ~0, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(0.8f, hit.distance);

        transform.position = smoothFocus + dir * dist;
        transform.rotation = rot;
    }
}