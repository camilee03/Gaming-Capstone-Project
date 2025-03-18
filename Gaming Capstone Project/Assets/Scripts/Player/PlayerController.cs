using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Player Settings")]
    public float velocity = 10f;
    public float jumpForce = 7.5f;
    public float fallMultiplier = 2f;

    [Header("Mouse Sensitivity")]
    public float HorizontalSensitivity = 10f;
    public float VerticalSensitivity = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; // set this in the Inspector to the layer(s) considered "ground"

    [Header("References")]
    public Camera cam;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 5f;
    public float staminaRegenDelay = 2f;   // Delay time before stamina can begin regenerating
    public StaminaTracker staminaTracker;

    [Header("Health and Team Settings")]
    public bool isDopple = false;
    public TMP_Text TeamDeclaration;
    public bool isDead = false;
    public CanvasGroup DeathScreen;
    public float AttackDistance = 3f;
    public float AttackDelay = 20f;        // Cooldown time (in seconds) before you can attack again
    public Transform shootingPoint;
    public LayerMask playerLayerMask;

    // Private variables
    private float currentStamina;
    private float yaw = 0.0f;
    private float x;
    private float z;
    private bool isSprinting;
    private Rigidbody rgd;
    private Animator animator;
    private PlayerInput playerInput;
    private CameraMovement camMovement;

    private bool canAttack = true;
    private float staminaRegenTimer;
    private float attackTimer;                  // Tracks time left before we can attack again
    public bool isGrounded;
    public bool debugOffline = false;
    public bool canMove = true;

    private void Start()
    {
        rgd = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        camMovement = cam.GetComponent<CameraMovement>();
        camMovement.SetPlayerController(this);

        // Initialize current stamina
        currentStamina = maxStamina;

        if (isDopple)
        {
            TeamDeclaration.text = "You are a : Dopple";
        }
        else TeamDeclaration.text = "You are a : Scientist";
    }

    private void Update()
    {
        // Network checks (ignore if you're testing offline)
        if (!debugOffline)
        {
            if (!IsOwner || !IsSpawned) return;
        }

        // If we're dead, show death screen, disable movement, then exit
        if (isDead)
        {
            canMove = false;
            if (DeathScreen.alpha == 0)
            {
                Debug.Log("dead as hell");
                DeathScreen.DOFade(1, 3);
            }
            return;
        }

        // Check if on the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Handle movement (if allowed)
        if (canMove)
        {
            // Calculate forward/right movement based on camera's transform
            Vector3 forwardMovement = cam.transform.forward * z * velocity;
            Vector3 rightMovement = cam.transform.right * x * velocity;
            Vector3 linearVelocity = forwardMovement + rightMovement - rgd.linearVelocity;

            // Apply horizontal movement (ignore existing vertical velocity to prevent messing up jumps)
            rgd.linearVelocity += new Vector3(linearVelocity.x, 0f, linearVelocity.z);

            // Update animator (walking state)

            animator.SetBool("isWalking", Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0);

            // Handle rotation with mouse movement
            yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
            transform.eulerAngles = new Vector3(0f, yaw, 0f);

            #region Sprinting
            if (isSprinting)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                animator.speed = 1.25f;
                // Out of stamina? Stop sprinting
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    StopSprinting();
                }

                staminaTracker.UpdateStamina(currentStamina);
            }
            else
            {
                animator.speed = 1;
                // If we're not sprinting, decrement our regeneration timer first
                if (staminaRegenTimer > 0f)
                {
                    // Count down before allowing regen
                    staminaRegenTimer -= Time.deltaTime;
                }
                else
                {
                    // Regenerate stamina if timer has expired
                    if (currentStamina < maxStamina)
                    {
                        currentStamina += staminaRegenRate * Time.deltaTime;
                        currentStamina = Mathf.Min(currentStamina, maxStamina);
                        staminaTracker.UpdateStamina(currentStamina);
                    }
                }
            }
            #endregion
        }
        else
        {
            // Stop all movement if can't move
            rgd.linearVelocity = Vector3.zero;
            rgd.angularVelocity = Vector3.zero;
            animator.SetBool("isWalking", false);
        }

        // Handle attack cooldown
        if (!canAttack)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                canAttack = true;
                attackTimer = 0f;
            }
        }
    }

    private void FixedUpdate()
    {
        // If not grounded and character is moving downward, apply extra downward force
        if (!isGrounded && rgd.linearVelocity.y < 0)
        {
            // Multiply the default gravity effect to make the character fall faster
            rgd.linearVelocity += Vector3.up * (Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    public void MovePlayer(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        x = input.x;
        z = input.y;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            // Launch the character upward
            rgd.linearVelocity = new Vector3(rgd.linearVelocity.x, jumpForce, rgd.linearVelocity.z);
            animator.SetBool("isJumping", true);
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed && currentStamina > 0f)
        {
            isSprinting = true;
            velocity = 20f;
        }
        else if (context.canceled)
        {
            StopSprinting();
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        // Only proceed if the action was performed (button fully pressed),
        // the player is a dopple, and we can still attack
        if (context.performed && isDopple && canAttack)
        {
            Debug.Log("Attacking");
            RaycastHit hit;
            // Attempt to hit something within AttackDistance
            if (Physics.Raycast(new Ray(shootingPoint.position,shootingPoint.forward),
                                out hit, AttackDistance, playerLayerMask))
            {
                GameObject hitObject = hit.collider.gameObject;
                Debug.Log("hit player: " + hitObject.name);
                // If we hit another player, mark them as dead
                if (hitObject.GetComponent<PlayerController>() != null)
                {
                    if (!hitObject.GetComponent<PlayerController>().isDopple)
                    {
                        hitObject.GetComponent<PlayerController>().isDead = true;
                    }
                }
            }

            // Begin our attack cooldown
            canAttack = false;
            attackTimer = AttackDelay;
        }
    }

    // Helper method to cleanly stop sprinting logic
    private void StopSprinting()
    {
        isSprinting = false;
        velocity = 10f;
        // Every time we stop sprinting, reset the regeneration delay
        staminaRegenTimer = staminaRegenDelay;
    }

    public void ToggleMovement(bool input)
    {
        canMove = input;
    }
}
