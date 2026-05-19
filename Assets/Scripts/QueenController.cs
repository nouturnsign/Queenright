using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class QueenController : MonoBehaviour
{
    // Unity animation and sprites
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Unity inputs
    private InputAction moveAction;
    private Vector2 moveValue;

    // Unity physics
    private Rigidbody2D rb;

    // custom logic
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveAction = InputSystem.actions.FindAction("Move", true);
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // Update is called once per frame
    void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        bool isWalking = Mathf.Abs(moveValue.x) > 0.01f;
        if (moveValue.x > 0)
            spriteRenderer.flipX = true;
        else if (moveValue.x < 0)
            spriteRenderer.flipX = false;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    void FixedUpdate()
    {
        float targetSpeed = moveValue.x * moveSpeed;
        float currentAccel = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            float targetAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float smoothedAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothedAngle);
        }
    }
}
