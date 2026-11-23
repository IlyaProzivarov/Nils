using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float walkSpeed = 6f;
    public float runSpeed = 9f;          // зажать Shift
    public float acceleration = 30f;
    public float turnSpeed = 720f;

    [Header("Прыжок")]
    public float jumpHeight = 2.2f;
    public float gravity = -25f;
    public bool allowDoubleJump = true;
    public float coyoteTime = 0.12f;     // можно прыгнуть сразу после схода с края
    public float jumpBuffer = 0.12f;     // нажатие чуть до приземления не пропадёт

    public bool InputLocked { get; set; }
    public bool IsGrounded { get; private set; }
    public float HorizontalSpeed { get; private set; }
    public float LastJumpPressTime => lastJumpPressTime;
    public bool JumpHeld => !InputLocked && Input.GetButton("Jump");

    CharacterController cc;
    Transform cam;
    Vector3 horizontalVel;
    float verticalVel;
    float lastGroundedTime = -10f;
    float lastJumpPressTime = -10f;
    int jumpsUsed;
    bool canCutJump;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        else Debug.LogError("PlayerController: на сцене нет камеры с тегом MainCamera", this);
    }

    void Update()
    {
        if (cam == null) return;

        IsGrounded = cc.isGrounded;
        if (IsGrounded && verticalVel <= 0.01f)
        {
            lastGroundedTime = Time.time;
            jumpsUsed = 0;
            canCutJump = false;
            if (verticalVel < 0f) verticalVel = -2f;
        }

        // Ввод
        Vector2 input = Vector2.zero;
        bool run = false;
        if (!InputLocked)
        {
            input = Vector2.ClampMagnitude(
                new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            run = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetButtonDown("Jump")) lastJumpPressTime = Time.time;

            // Отпустил прыжок рано: прыжок ниже
            if (Input.GetButtonUp("Jump") && canCutJump && verticalVel > 0f)
            {
                verticalVel *= 0.5f;
                canCutJump = false;
            }
        }

        // Движение относительно камеры
        Vector3 f = cam.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.right; r.y = 0f; r.Normalize();
        Vector3 wish = (f * input.y + r * input.x) * (run ? runSpeed : walkSpeed);
        horizontalVel = Vector3.MoveTowards(horizontalVel, wish, acceleration * Time.deltaTime);
        HorizontalSpeed = horizontalVel.magnitude;

        // Прыжок
        if (Time.time - lastJumpPressTime <= jumpBuffer)
        {
            if (Time.time - lastGroundedTime <= coyoteTime) Jump(1);
            else if (allowDoubleJump && jumpsUsed < 2) Jump(2);
        }

        // Гравитация и перемещение
        verticalVel += gravity * Time.deltaTime;
        verticalVel = Mathf.Max(verticalVel, -40f);
        cc.Move((horizontalVel + Vector3.up * verticalVel) * Time.deltaTime);

        // Ударился головой
        if ((cc.collisionFlags & CollisionFlags.Above) != 0 && verticalVel > 0f)
            verticalVel = 0f;

        // Поворот в сторону бега
        Vector3 flat = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
        if (flat.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
        }
    }

    void Jump(int used)
    {
        jumpsUsed = used;
        verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        canCutJump = true;
        lastJumpPressTime = -10f;
        lastGroundedTime = -10f;
    }

    // Герой коснулся чего-то при движении: проверяем, не батут ли это
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.3f) return;              // только сверху
        if (verticalVel > 1f) return;                 // уже летим вверх
        var bm = hit.collider.GetComponentInParent<BounceMushroom>();
        if (bm != null) bm.Launch(this);
    }

    // Вызывается батутами
    public void Bounce(float velocity)
    {
        verticalVel = velocity;
        jumpsUsed = 1;                 // после отскока остаётся один прыжок в воздухе
        canCutJump = false;
        lastGroundedTime = -10f;
        lastJumpPressTime = -10f;
    }
}