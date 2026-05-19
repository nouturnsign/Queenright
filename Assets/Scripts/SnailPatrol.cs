using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class SnailPatrol : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsHidingHash = Animator.StringToHash("IsHiding");
    private static readonly float CloseEnough = 0.05f;

    [SerializeField] private float patrolRadius = 2f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float walkSegmentDistance = 0.75f;
    [SerializeField] private Vector2 pauseDurationRange = new Vector2(0.5f, 1.2f);
    [SerializeField] private float turnAroundChanceAtPause = 0.5f;
    [SerializeField] private float hideRadius = 3f;
    [SerializeField] private float hideFreezeDelay = 0.35f;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float groundContactOffset = 0f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform playerTransform;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D snailCollider;
    private Vector2 homePosition;
    private float pivotToGroundDistance;
    private float segmentTargetX;
    private float pauseTimer;
    private float hideFreezeTimer = -1f;
    private bool wasHiding;
    private bool hasHiddenPermanently;
    private int originalLayer;
    private RigidbodyType2D originalBodyType;
    private int patrolDirection = 1;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        snailCollider = GetComponent<Collider2D>();
        originalLayer = gameObject.layer;
        originalBodyType = rb.bodyType;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        homePosition = transform.position;
        pivotToGroundDistance = transform.position.y - snailCollider.bounds.min.y;
        ChooseNextSegment();

        if (playerTransform == null)
        {
            QueenController queen = FindAnyObjectByType<QueenController>();
            if (queen != null)
            {
                playerTransform = queen.transform;
            }
        }
    }

    void FixedUpdate()
    {
        bool isHiding = hasHiddenPermanently || ShouldHide();
        if (isHiding != wasHiding)
        {
            if (isHiding)
            {
                hasHiddenPermanently = true;
            }

            SetHidingState(isHiding);
            wasHiding = isHiding;
        }

        animator.SetBool(IsHidingHash, isHiding);

        if (isHiding)
        {
            animator.SetBool(IsWalkingHash, false);
            SnapToGround();
            UpdateHideFreezeTimer();
            return;
        }

        float currentX = rb.position.x;
        float leftBound = homePosition.x - patrolRadius;
        float rightBound = homePosition.x + patrolRadius;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            StopMotion();
            animator.SetBool(IsWalkingHash, false);
            SnapToGround();

            if (pauseTimer <= 0f)
            {
                if (Random.value < turnAroundChanceAtPause)
                {
                    patrolDirection *= -1;
                }

                ChooseNextSegment();
            }

            return;
        }

        if (patrolDirection > 0 && currentX >= rightBound - CloseEnough)
        {
            patrolDirection = -1;
            BeginPause();
            SnapToGround();
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        if (patrolDirection < 0 && currentX <= leftBound + CloseEnough)
        {
            patrolDirection = 1;
            BeginPause();
            SnapToGround();
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        bool isWalking = Mathf.Abs(segmentTargetX - currentX) > CloseEnough;
        animator.SetBool(IsWalkingHash, isWalking);

        if (!isWalking)
        {
            BeginPause();
            SnapToGround();
            return;
        }

        float newX = Mathf.MoveTowards(currentX, segmentTargetX, moveSpeed * Time.fixedDeltaTime);
        Vector2 currentPosition = rb.position;
        Vector2 probeOrigin = new(newX, currentPosition.y + groundCheckDistance);
        RaycastHit2D hit = RaycastGround(probeOrigin);

        float newY = currentPosition.y;
        if (hit.collider != null)
        {
            newY = hit.point.y + pivotToGroundDistance + groundContactOffset;
            RotateToGroundNormal(hit.normal);
        }

        StopMotion();
        rb.MovePosition(new Vector2(newX, newY));
        spriteRenderer.flipX = patrolDirection > 0;
    }

    private bool ShouldHide()
    {
        if (playerTransform == null || hideRadius <= 0f)
        {
            return false;
        }

        return Vector2.Distance(transform.position, playerTransform.position) <= hideRadius;
    }

    private void SnapToGround()
    {
        Vector2 currentPosition = rb.position;
        Vector2 probeOrigin = new(currentPosition.x, currentPosition.y + groundCheckDistance);
        RaycastHit2D hit = RaycastGround(probeOrigin);
        if (hit.collider == null)
        {
            return;
        }

        rb.MovePosition(new Vector2(currentPosition.x, hit.point.y + pivotToGroundDistance + groundContactOffset));
        RotateToGroundNormal(hit.normal);
    }

    private void BeginPause()
    {
        pauseTimer = Random.Range(pauseDurationRange.x, pauseDurationRange.y);
    }

    private void ChooseNextSegment()
    {
        float leftBound = homePosition.x - patrolRadius;
        float rightBound = homePosition.x + patrolRadius;
        float currentX = rb.position.x;
        float desiredTargetX = currentX + (walkSegmentDistance * patrolDirection);
        segmentTargetX = Mathf.Clamp(desiredTargetX, leftBound, rightBound);
        spriteRenderer.flipX = patrolDirection > 0;
    }

    private void RotateToGroundNormal(Vector2 groundNormal)
    {
        float targetAngle = Mathf.Atan2(groundNormal.y, groundNormal.x) * Mathf.Rad2Deg - 90f;
        float smoothedAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothedAngle);
    }

    private void SetHidingState(bool isHiding)
    {
        if (isHiding)
        {
            pauseTimer = 0f;
            StopMotion();
            animator.SetBool(IsWalkingHash, false);
            animator.SetBool(IsHidingHash, true);
            animator.speed = 1f;
            hideFreezeTimer = hideFreezeDelay;
            SnapToGround();
            gameObject.layer = GetPrimaryGroundLayer();
            rb.bodyType = RigidbodyType2D.Static;
            return;
        }
    }

    private int GetPrimaryGroundLayer()
    {
        int layerMask = groundLayer.value;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((layerMask & (1 << layer)) != 0)
            {
                return layer;
            }
        }

        return originalLayer;
    }

    private RaycastHit2D RaycastGround(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, groundCheckDistance * 2f, groundLayer);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider != snailCollider)
            {
                return hit;
            }
        }

        return default;
    }

    private void StopMotion()
    {
        if (rb.bodyType == RigidbodyType2D.Static)
        {
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void UpdateHideFreezeTimer()
    {
        if (animator.speed <= 0f)
        {
            return;
        }

        if (hideFreezeTimer > 0f)
        {
            hideFreezeTimer -= Time.fixedDeltaTime;
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("SnailHide"))
        {
            animator.speed = 0f;
            hideFreezeTimer = -1f;
        }
    }
}
