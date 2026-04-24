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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveAction = InputSystem.actions.FindAction("Move", true);
        rb = GetComponent<Rigidbody2D>();
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
        rb.linearVelocity = new Vector2(moveValue.x * moveSpeed, rb.linearVelocity.y);
    }
}
