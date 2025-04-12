using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using FMODUnity;

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
    public Renderer otherrenderer, otherrenderer2, selfrenderer;

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

    public NetworkMic voiceNetworker;

    private bool isTransformed = false;
    private bool canAttack = true;
    private float staminaRegenTimer;
    private float attackTimer;                  // Tracks time left before we can attack again
    public bool isGrounded;
    public bool debugOffline = false;
    public bool canMove = true;
    public bool useAnimator = true;

    public override void OnNetworkSpawn()
    {
        playerInput = GetComponent<PlayerInput>();
        camMovement = cam.GetComponent<CameraMovement>();
        StudioListener sl = cam.GetComponent<StudioListener>();

        if (IsOwner)
        {
            otherrenderer.enabled = false;
            otherrenderer2.enabled = false;
            selfrenderer.enabled = true;
            useAnimator = true;
            playerInput.enabled = true;
            cam.enabled = true;
            camMovement.enabled = true;
            sl.enabled = true;
        }
        else
        {
            otherrenderer.enabled = true;
            otherrenderer2.enabled = true;
            selfrenderer.enabled = false;
            useAnimator = false;
            playerInput.enabled = false;
            cam.enabled = false;
            camMovement.enabled = false;
            sl.enabled = false;
        }

    }
    [ClientRpc]
    public void SetDoppleClientRpc(bool newIsDopple)
    {
        isDopple = newIsDopple;
        if (isDopple)
            TeamDeclaration.text = "You are a : Dopple";
        else
            TeamDeclaration.text = "You are a : Scientist";

        Debug.Log($"[ClientRpc] Player {OwnerClientId} => isDopple={isDopple}");
    }
    private void Start()
    {
        rgd = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        currentStamina = maxStamina;

        if (isDopple)
            TeamDeclaration.text = "You are a : Dopple";
        else
            TeamDeclaration.text = "You are a : Scientist";
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
        if (useAnimator)
        {
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Transformed", isTransformed);
            animator.SetFloat("YVelocity", rgd.linearVelocity.y);
        }

        // Handle movement (if allowed)
        if (canMove)
        {
            // Calculate forward/right movement based on camera's transform
            Vector3 forwardMovement = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z) * z * velocity;
            Vector3 rightMovement = new Vector3(cam.transform.right.x, 0, cam.transform.right.z) * x * velocity;
            Vector3 linearVelocity = forwardMovement + rightMovement - rgd.linearVelocity;

            // Apply horizontal movement (ignore existing vertical velocity to prevent messing up jumps)
            rgd.linearVelocity += new Vector3(linearVelocity.x, 0f, linearVelocity.z);

            // Update animator (walking state)
            if (useAnimator) animator.SetFloat("Speed", new Vector3(rgd.linearVelocity.x, 0, rgd.linearVelocity.z).magnitude);

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
            if (useAnimator)
                animator.SetFloat("Speed", 0);
        }

        // Handle attack cooldown
        if (!canAttack)
        {
            if (useAnimator)
                animator.SetBool("Attacking", attackTimer > AttackDelay - 0.3f);
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
    public void Morph(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (isDopple && !isTransformed)
            {
                isTransformed = true;
                StartCoroutine(Morph());
            }
            else isTransformed = false;
            if (useAnimator)
                animator.SetBool("Transformed", isTransformed);
        }
    }

    public IEnumerator Morph()
    {
        canMove = false;
        canAttack = false;
        yield return new WaitForSeconds(1.1f);
        canMove = true;
        canAttack = true;
    }

    public void Attack(InputAction.CallbackContext context)
    {
        // Client side input
        // If you want only Dopples to do this, check isDopple here:
        // if (!isDopple) return;

        if (context.performed && canAttack)
        {
            // Start the cooldown
            canAttack = false;
            attackTimer = AttackDelay;

            // We gather necessary info for the server to do the raycast:
            Vector3 origin = shootingPoint.position;
            Vector3 direction = shootingPoint.forward;

            // Call a ServerRpc, passing in the needed data
            Debug.Log("Local Attack => calling AttackServerRpc");
            AttackServerRpc(origin, direction);
        }
    }
    [ServerRpc]
    private void AttackServerRpc(Vector3 origin, Vector3 direction)
    {
        Debug.Log("[Server] AttackServerRpc triggered by " + OwnerClientId);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, AttackDistance, playerLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            var targetPlayer = hitObject.GetComponent<PlayerController>();
            if (targetPlayer != null && !targetPlayer.isDead)
            {
                Debug.Log($"[Server] Player {OwnerClientId} killed {targetPlayer.OwnerClientId}");
                // We kill them via a ClientRpc call to the victim
                targetPlayer.KillClientRpc();
            }
        }
    }

    // ------------------------------------------------
    // Called on the victim to set isDead = true,
    // show the death screen, etc.
    // ------------------------------------------------
    [ClientRpc]
    private void KillClientRpc()
    {
        isDead = true;
        Debug.Log($"[ClientRpc] KillClientRpc => Player {OwnerClientId} is now dead.");
        // The next Update() will show the death screen and disable movement.
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

    public void UpdateTeam(bool Dopple)
    {
        isDopple = Dopple;
        if (isDopple)
            TeamDeclaration.text = "You are a : Dopple";
        else
            TeamDeclaration.text = "You are a : Scientist";
    }
}
