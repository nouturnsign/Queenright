using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class FollowQueen : MonoBehaviour
{
    // Unity animation and sprites
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // custom logic
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float flockSpread = 0f;
    [SerializeField] private float speedVariance = 0f;
    [SerializeField] private float millingDistance = 0f;
    [SerializeField] private float millingSpeed = 0f;
    [SerializeField] private float catchUpDistance = 0f;
    [SerializeField] private float catchUpSpeed = 0f;
    [SerializeField] private float teleportCatchUpDistance = 0f;
    [SerializeField] private float irreparableLagDistance = 0f;
    [SerializeField] private float queenPullStrength = 0f;
    
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;

    private Transform queenTransform;
    private float myFlockOffset;
    private float myActualSpeed;
    private float myRandomPhaseOffset;
    private float lastQueenX;
    private bool isCatchingUp;
    private static readonly float MinSpeed = 1f;
    private static readonly float CloseEnough = 0.05f;
    private static readonly float QueenMovingThreshold = 0.01f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Find the queen. 
        QueenController queen = FindAnyObjectByType<QueenController>();
        if (queen != null)
        {
            queenTransform = queen.transform;
            lastQueenX = queenTransform.position.x;
        }
        else
        {
            Debug.LogWarning("FollowQueen: Could not find a QueenController in the scene!");
        }
        myFlockOffset = Random.Range(-flockSpread, flockSpread);
        // Ensure baseline speed is at least 1f so ants don't spawn with 0 or negative speed
        myActualSpeed = Mathf.Max(MinSpeed, moveSpeed + Random.Range(-speedVariance, speedVariance));
        myRandomPhaseOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        if (queenTransform == null) return;

        float queenX = queenTransform.position.x;
        float queenDeltaX = queenX - lastQueenX;

        // Calculate where this specific ant wants to stand on the X axis
        float dynamicOffset = myFlockOffset + (Mathf.Sin(Time.time * millingSpeed + myRandomPhaseOffset) * millingDistance);
        float targetX = queenX + dynamicOffset;
        float currentX = transform.position.x;
        float distanceFromQueen = Mathf.Abs(queenX - currentX);

        // calculate distance and check if we need to move
        float distanceToTarget = Mathf.Abs(targetX - currentX);

        if (teleportCatchUpDistance > 0f && distanceFromQueen > teleportCatchUpDistance)
        {
            TeleportToTarget(targetX, queenTransform.position.y);
            currentX = transform.position.x;
            distanceToTarget = Mathf.Abs(targetX - currentX);
            isCatchingUp = false;
        }

        // Hysteresis for catch-up mode: turn on when too far, stay on until we reach the target
        if (distanceToTarget > catchUpDistance)
        {
            isCatchingUp = true;
        }
        else if (distanceToTarget <= CloseEnough)
        {
            isCatchingUp = false;
        }

        bool isWalking = distanceToTarget > CloseEnough;

        if (isWalking)
        {
            // Move strictly along the X axis using Time.deltaTime
            float currentSpeed = isCatchingUp ? catchUpSpeed : myActualSpeed;
            currentSpeed += CalculateQueenPullBoost(currentX, queenX, queenDeltaX);
            float step = currentSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(currentX, targetX, step);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            // handle sprite flipping
            if (newX > currentX)
                spriteRenderer.flipX = true;
            else if (newX < currentX)
                spriteRenderer.flipX = false;
        }

        animator.SetBool(IsWalkingHash, isWalking);

        // handle ground rotation mapping
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            float targetAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = transform.eulerAngles.z;
            float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, smoothedAngle);
        }

        lastQueenX = queenX;
    }

    private float CalculateQueenPullBoost(float antX, float queenX, float queenDeltaX)
    {
        if (Mathf.Abs(queenDeltaX) <= QueenMovingThreshold)
        {
            return 0f;
        }

        bool queenMovingRight = queenDeltaX > 0f;
        bool isTrailingBehindQueen = queenMovingRight ? antX < queenX : antX > queenX;
        if (!isTrailingBehindQueen)
        {
            return 0f;
        }

        float distanceBehindQueen = Mathf.Abs(queenX - antX);
        if (distanceBehindQueen <= irreparableLagDistance)
        {
            return 0f;
        }

        float excessLag = distanceBehindQueen - irreparableLagDistance;
        return excessLag * queenPullStrength;
    }

    private void TeleportToTarget(float targetX, float queenY)
    {
        transform.position = new Vector3(targetX, queenY, transform.position.z);
    }
}
