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

    private Transform queenTransform;
    private float myFlockOffset;
    private float myActualSpeed;
    private float myRandomPhaseOffset;
    private bool isCatchingUp;
    private static readonly float MinSpeed = 1f;
    private static readonly float CloseEnough = 0.05f;

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

        // Calculate where this specific ant wants to stand on the X axis
        float dynamicOffset = myFlockOffset + (Mathf.Sin(Time.time * millingSpeed + myRandomPhaseOffset) * millingDistance);
        float targetX = queenTransform.position.x + dynamicOffset;
        float currentX = transform.position.x;

        // calculate distance and check if we need to move
        float distanceToTarget = Mathf.Abs(targetX - currentX);

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
    }
}
