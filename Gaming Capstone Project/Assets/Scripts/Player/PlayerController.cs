using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Netcode.Components;
using System.Drawing;

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

    private bool isTransformed = false;
    private bool canAttack = true;
    private float staminaRegenTimer;
    private float attackTimer;                  // Tracks time left before we can attack again
    public bool isGrounded;
    public bool debugOffline = false;
    public bool canMove = true;
    public bool useAnimator = true;
    public GameObject PlayerDisplay;
    public NetworkVariable<Vector3> LastAssignedSpawnPos = new NetworkVariable<Vector3>();
    public NetworkVariable<int> ColorID = new NetworkVariable<int>(-1);

    [Header("Death Visuals")]
    public GameObject playerModel; // Assign in Inspector: this should be the mesh or object representing the visible character


    public CanvasGroup VotingScreen;
    bool point, wave, thumbsUp, peaceSign;

    public override void OnNetworkSpawn()
    {
        playerInput = GetComponent<PlayerInput>();
        camMovement = cam.GetComponent<CameraMovement>();
        AudioListener al = cam.GetComponent<AudioListener>();        

        if (IsOwner)
        {
            otherrenderer.enabled = false;
            otherrenderer2.enabled = false;
            selfrenderer.enabled = true;
            useAnimator = true;
            playerInput.enabled = true;
            cam.gameObject.SetActive(true);
            camMovement.enabled = true;
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
            al.enabled = false;
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        if (IsOwner)
        {
            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(position, rotation, Vector3.one * 0.75f);
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }

            LastAssignedSpawnPos.Value = position;
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
    private void ApplyColor(int colorIndex)
    {

        if (gameObject.GetComponent<ColorManager>() != null && colorIndex >= 1 && colorIndex < 13)
        {
            ColorManager colormanage = gameObject.GetComponent<ColorManager>();
            colormanage.ChangeSuitColor(colorIndex);
        }
    }


    private void OnColorChanged(int previous, int current)
    {
        ApplyColor(current);

        if (IsOwner)
        {
            ColorSelectionUIManager uiManager = FindFirstObjectByType<ColorSelectionUIManager>() ;
            if (uiManager != null)
            {
                uiManager.RefreshAll();
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
public void ForceSetColorServerRpc(int colorIndex)
{
    if (!GameController.Instance.usedColors.Contains(colorIndex))
    {
        GameController.Instance.LockColor(colorIndex);
    }

    if (ColorID.Value > 0)
    {
        GameController.Instance.UnlockColor(ColorID.Value);
    }

    ColorID.Value = colorIndex;
}



    public void RequestColorSelection(int colorIndex)
    {
        if (IsOwner)
        {
            TrySetColorServerRpc(colorIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TrySetColorServerRpc(int colorIndex, ServerRpcParams rpcParams = default)
    {
        if (!GameController.Instance.IsColorAvailable(colorIndex)) return;

        GameController.Instance.LockColor(colorIndex);

        // Release previous color (if changing)
        if (ColorID.Value > 0)
        {
            GameController.Instance.UnlockColor(ColorID.Value);
        }

        ColorID.Value = colorIndex;
    }
    private void Start()
    {
        rgd = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        ColorID.OnValueChanged += OnColorChanged;

        if (ColorID.Value >= 0)
        {
            ApplyColor(ColorID.Value);
        }
        currentStamina = maxStamina;

        if (isDopple)
            TeamDeclaration.text = "You are a : Dopple";
        else
            TeamDeclaration.text = "You are a : Scientist";
    }


    public void StartVote()
    {
        Cursor.lockState = CursorLockMode.None;
        VotingScreen.gameObject.SetActive(true);
        VotingScreen.DOFade(1, 3);
        VotingScreen.GetComponent<VoteManager>().CreateColorButtons();
        //make color buttons.
        
        //start voting timer
    }

    public void EndVote()
    {

        VotingScreen.DOFade(0, 3);
        VotingScreen.GetComponent<VoteManager>().ClearButtons();

        VotingScreen.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;



    }

    [ServerRpc(RequireOwnership = false)]
    public void CastVoteServerRpc(int colorIndex, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[ServerRpc] Received vote for color {colorIndex} from {OwnerClientId}");
        GameController.Instance.ReceiveVote(OwnerClientId, colorIndex);
    }

    private void Update()
    {
        // Network checks (ignore if you're testing offline)
        if (!debugOffline)
        {
            if (!IsOwner || !IsSpawned) return;
        }

        // Check if on the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (useAnimator)
        {
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Transformed", isTransformed);
            animator.SetFloat("YVelocity", rgd.linearVelocity.y);
            
            if (wave)
            {
                animator.SetLayerWeight(4, 1);//set emote layer weight to 1
                animator.SetFloat("currentEmote", 3);
            }
            else if (peaceSign)
            {
                animator.SetLayerWeight(4, 1);//set emote layer weight to 1
                animator.SetFloat("currentEmote", 2);
            }
            else if (thumbsUp)
            {
                animator.SetLayerWeight(4, 1);//set emote layer weight to 1
                animator.SetFloat("currentEmote", 1);
            }
            else if (point)
            {
                animator.SetLayerWeight(4, 1);//set emote layer weight to 1
                animator.SetFloat("currentEmote", 0);
            }
            else
            {
                animator.SetLayerWeight(4, 0);
            }
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
        if(isDead)
        {
            return;
        }
        else {
            // If not grounded and character is moving downward, apply extra downward force
            if (!isGrounded && rgd.linearVelocity.y < 0)
            {
                // Multiply the default gravity effect to make the character fall faster
                rgd.linearVelocity += Vector3.up * (Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
            }
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
        if (canMove)
        {
            if (context.performed && IsOwner)
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
    }

    public IEnumerator Morph()
    {
        canMove = false;
        canAttack = false;
        yield return new WaitForSeconds(1.1f);
        canMove = true;
        canAttack = true;
    }


    public void Point(InputAction.CallbackContext context)
    {
        point = context.ReadValueAsButton();
    }
    public void Wave(InputAction.CallbackContext context)
    {
        wave = context.ReadValueAsButton();
    }
    public void ThumbsUp(InputAction.CallbackContext context)
    {
        thumbsUp = context.ReadValueAsButton();
    }
    public void PeaceSign(InputAction.CallbackContext context)
    {
        peaceSign = context.ReadValueAsButton();
    }

    public void Attack(InputAction.CallbackContext context)
    {
        // Client side input
        // If you want only Dopples to do this, check isDopple here:
        // if (!isDopple) return;

        if (context.performed && canAttack && IsOwner)
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
    public void KillClientRpc()
    {
        isDead = true;
        Debug.Log($"[ClientRpc] KillClientRpc => Player {OwnerClientId} is now dead.");
        StartCoroutine(EnterGhostMode());
        // The next Update() will show the death screen and disable movement.
    }

private IEnumerator EnterGhostMode()
{
    isDead = true;
    canMove = false;

    Debug.Log($"[Client] Entering ghost mode for Player {OwnerClientId}");

    // Fade in the death screen
    if (DeathScreen != null && IsOwner)
        DeathScreen.DOFade(1, 2f);

    yield return new WaitForSeconds(3f); // time to show death screen

    // Fade out the death screen
    if (DeathScreen != null && IsOwner)
        DeathScreen.DOFade(0, 2f);

    // Make the player invisible
    if (playerModel != null)
        playerModel.SetActive(false);

    // Disable the collider so they can walk through stuff
    Collider col = GetComponent<Collider>();
    if (col != null)
        col.enabled = false;

    // Disable attacking if you want
    canAttack = false;

    // Now allow movement again (ghost mode)
    canMove = true;

    Debug.Log($"[Client] Player {OwnerClientId} is now a ghost.");
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
    public void ToggleInput(bool input)
    {
        playerInput.enabled = input;    
    }
    public void UpdateTeam(bool Dopple)
    {
        isDopple = Dopple;
        if (isDopple)
            TeamDeclaration.text = "You are a : Dopple";
        else
            TeamDeclaration.text = "You are a : Scientist";
    }
    private void OnDestroy()
    {
        ColorID.OnValueChanged -= OnColorChanged;

    }
}
