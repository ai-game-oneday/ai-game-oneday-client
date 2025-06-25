using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Transform respawnPoint;
  [SerializeField] private TextMeshProUGUI heightText;

  [Header("Movement Settings")]
  [SerializeField] private float moveSpeed = 4f;
  [SerializeField] private float jumpForce = 8f;

  [Header("Ground Detection")]
  [SerializeField] private float groundCheckDistance = 0.2f;
  [SerializeField] private LayerMask groundLayerMask = 1;
  [SerializeField] private Transform groundCheckPoint;

  [Header("Physics")]
  [SerializeField] private float fallMultiplier = 2.5f;
  [SerializeField] private float lowJumpMultiplier = 2f;

  [Header("Debug")]
  [SerializeField] private bool showGroundCheck = true;
  [SerializeField] private TMP_InputField inputField;

  // Components
  private Rigidbody2D rigid;
  private SpriteRenderer spriteRenderer;
  private CircleCollider2D playerCollider;
  private Animator anim;

  // Input and State
  private Vector2 moveInput;
  private bool jumpRequested;
  private bool isGrounded;
  private bool wasGrounded;

  // Ground check
  private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

  void Awake()
  {
    // Get components
    rigid = GetComponent<Rigidbody2D>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    playerCollider = GetComponent<CircleCollider2D>();
    anim = GetComponent<Animator>();

    // Create ground check point if it doesn't exist
    SetupGroundCheckPoint();

    // Configure Rigidbody2D
    rigid.gravityScale = 3f;
    rigid.freezeRotation = true;
  }

  void Update()
  {
    CheckHeight();
    CheckGrounded();
    HandleJump();
    ApplyBetterJump();
  }

  void FixedUpdate()
  {
    HandleMovement();
  }

  public void OnMove(InputAction.CallbackContext context)
  {
    if (inputField.isFocused) return;

    // Get movement input from Input System
    moveInput = context.ReadValue<Vector2>();
  }

  public void OnJump(InputAction.CallbackContext context)
  {
    if (inputField.isFocused) return;

    // Handle jump input
    if (context.performed) // When button is pressed
    {
      jumpRequested = true;
    }
  }

  private void CheckHeight()
  {
    heightText.text = $"{Mathf.RoundToInt(transform.position.y) + 3} M";

    if (transform.position.y < -10f)
    {
      transform.position = respawnPoint.position;
    }
  }

  private void SetupGroundCheckPoint()
  {
    if (groundCheckPoint == null)
    {
      GameObject groundCheck = new GameObject("GroundCheckPoint");
      groundCheck.transform.SetParent(transform);

      // Position it at the bottom of the player
      float colliderRadius = playerCollider != null ? playerCollider.radius : 0.5f;
      groundCheck.transform.localPosition = new Vector3(0, -colliderRadius - 0.1f, 0);
      groundCheckPoint = groundCheck.transform;
    }
  }

  private void CheckGrounded()
  {
    wasGrounded = isGrounded;

    if (groundCheckPoint == null) return;

    // Use BoxCast for reliable ground detection
    Vector2 boxCenter = (Vector2)groundCheckPoint.position;

    RaycastHit2D hit = Physics2D.BoxCast(
        boxCenter,
        groundCheckSize,
        0f,
        Vector2.down,
        groundCheckDistance,
        groundLayerMask
    );

    isGrounded = hit.collider != null;
  }

  private void HandleMovement()
  {
    // Apply horizontal movement
    Vector2 velocity = rigid.linearVelocity;
    velocity.x = moveInput.x * moveSpeed;
    rigid.linearVelocity = velocity;

    anim.SetFloat("speed", Mathf.Abs(velocity.x));

    // Flip sprite based on movement direction
    if (moveInput.x > 0.1f)
    {
      spriteRenderer.flipX = false;
    }
    else if (moveInput.x < -0.1f)
    {
      spriteRenderer.flipX = true;
    }
  }

  private void HandleJump()
  {
    if (jumpRequested && isGrounded)
    {
      // Reset Y velocity for consistent jump height
      Vector2 velocity = rigid.linearVelocity;
      velocity.y = 0f;
      rigid.linearVelocity = velocity;

      // Apply jump force
      rigid.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    jumpRequested = false;
  }

  private void ApplyBetterJump()
  {
    // Improved jump physics for better feel
    if (rigid.linearVelocity.y < 0)
    {
      // Falling - make it faster
      rigid.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
    }
    else if (rigid.linearVelocity.y > 0 && !jumpRequested)
    {
      // Rising but jump not held - make low jumps more responsive
      rigid.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }
  }

  // Public getter methods
  public bool IsGrounded() => isGrounded;
  public bool IsMoving() => Mathf.Abs(moveInput.x) > 0.1f;
  public Vector2 GetVelocity() => rigid.linearVelocity;
  public Vector2 GetMoveInput() => moveInput;

  // Public setter methods
  public void SetMoveSpeed(float speed) => moveSpeed = speed;
  public void SetJumpForce(float force) => jumpForce = force;

  // Debug visualization
  void OnDrawGizmosSelected()
  {
    if (!showGroundCheck || groundCheckPoint == null) return;

    // Draw ground check area
    Gizmos.color = isGrounded ? Color.green : Color.red;

    Vector3 boxCenter = groundCheckPoint.position + Vector3.down * groundCheckDistance;
    Gizmos.DrawWireCube(boxCenter, new Vector3(groundCheckSize.x, groundCheckSize.y, 0.1f));

    // Draw ground check ray
    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(groundCheckPoint.position,
                   groundCheckPoint.position + Vector3.down * groundCheckDistance);
  }
}