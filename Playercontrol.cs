using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    private float currentStamina;

    [Header("Camera")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float crouchHeight = 1f;
    public float standHeight = 2f;

    private CharacterController controller;
    private SanitySystem sanitySystem;
    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;
    private bool isCrouching = false;
    private bool isRunning = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        sanitySystem = GetComponent<SanitySystem>();
        currentStamina = maxStamina;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        HandleStamina();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        // WASD input
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W)) v =  1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h =  1f;

        isRunning = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0 && !isCrouching;

        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f) move.Normalize();

        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity -= 9.81f * Time.deltaTime;

        move = move * speed + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                if (Physics.SphereCast(transform.position, 0.5f, Vector3.up, out _, standHeight))
                    return;
            }

            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standHeight;
            controller.center = Vector3.up * (controller.height / 2f);
        }
    }

    void HandleStamina()
    {
        if (isRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        if (currentStamina <= 10f && sanitySystem != null)
            sanitySystem.DrainSanity(5f * Time.deltaTime);
    }

    public float GetStaminaPercent() => currentStamina / maxStamina;
    public bool IsRunning() => isRunning;
    public bool IsCrouching() => isCrouching;
}
