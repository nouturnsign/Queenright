using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class QueenController : MonoBehaviour
{
    private struct TerrainAnchor
    {
        public Vector2 Point;
        public Vector2 Normal;

        public TerrainAnchor(Vector2 point, Vector2 normal)
        {
            Point = point;
            Normal = normal;
        }
    }

    // Unity animation and sprites
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Unity inputs
    private InputAction moveAction;
    private InputAction previousAction;
    private InputAction pointAction;
    private InputAction clickAction;
    private Vector2 moveValue;

    // Unity physics
    private Rigidbody2D rb;
    private Camera mainCamera;

    // custom logic
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject weaverAntPrefab;
    [SerializeField] private float weaverBridgeRadius = 6f;
    [SerializeField] private float weaverClickProbeDistance = 3f;
    [SerializeField] private float bridgeAnchorEmbedDepth = 0.15f;
    [SerializeField] private float bridgeAntSpacingMultiplier = 0.65f;

    private bool isWeaverModeActive;
    private bool hasFirstBridgeAnchor;
    private TerrainAnchor firstBridgeAnchor;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveAction = InputSystem.actions.FindAction("Move", true);
        previousAction = InputSystem.actions.FindAction("Previous", true);
        pointAction = InputSystem.actions.FindAction("Point", true);
        clickAction = InputSystem.actions.FindAction("Click", true);
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        mainCamera = Camera.main;
    }

    void OnEnable()
    {
        moveAction?.Enable();
        previousAction?.Enable();
        pointAction?.Enable();
        clickAction?.Enable();
    }

    void OnDisable()
    {
        clickAction?.Disable();
        pointAction?.Disable();
        previousAction?.Disable();
        moveAction?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        }

        HandleWeaverInput();

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

    private void HandleWeaverInput()
    {
        if (previousAction.WasPressedThisFrame())
        {
            isWeaverModeActive = !isWeaverModeActive;
            hasFirstBridgeAnchor = false;
            Debug.Log(isWeaverModeActive
                ? "QueenController: Weaver bridge mode enabled. Click two terrain points."
                : "QueenController: Weaver bridge mode disabled.");
        }

        if (!isWeaverModeActive || !clickAction.WasPressedThisFrame())
        {
            return;
        }

        if (weaverAntPrefab == null)
        {
            Debug.LogWarning("QueenController: Weaver bridge mode needs a WeaverAnt prefab assigned.");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("QueenController: Could not find a camera for Weaver bridge placement.");
            return;
        }

        Vector2 screenPosition = pointAction.ReadValue<Vector2>();
        Vector3 worldPosition3D = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        Vector2 worldPosition = new Vector2(worldPosition3D.x, worldPosition3D.y);

        if (!TryGetTerrainAnchor(worldPosition, out TerrainAnchor clickedAnchor))
        {
            Debug.Log("QueenController: Click a valid terrain surface to place a Weaver bridge.");
            return;
        }

        if (!hasFirstBridgeAnchor)
        {
            firstBridgeAnchor = clickedAnchor;
            hasFirstBridgeAnchor = true;
            Debug.Log("QueenController: Weaver bridge start point selected.");
            return;
        }

        float bridgeDistance = Vector2.Distance(firstBridgeAnchor.Point, clickedAnchor.Point);
        if (bridgeDistance > weaverBridgeRadius)
        {
            firstBridgeAnchor = clickedAnchor;
            Debug.Log($"QueenController: Bridge points must be within {weaverBridgeRadius:F1} units. Start point reset.");
            return;
        }

        CreateWeaverBridge(firstBridgeAnchor, clickedAnchor);
        hasFirstBridgeAnchor = false;
    }

    private bool TryGetTerrainAnchor(Vector2 clickedPoint, out TerrainAnchor terrainAnchor)
    {
        Vector2 probeOrigin = clickedPoint + Vector2.up * weaverClickProbeDistance;
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, weaverClickProbeDistance * 2f, groundLayer);
        if (hit.collider == null)
        {
            terrainAnchor = default;
            return false;
        }

        terrainAnchor = new TerrainAnchor(hit.point, hit.normal);
        return true;
    }

    private void CreateWeaverBridge(TerrainAnchor startAnchor, TerrainAnchor endAnchor)
    {
        Vector2 embeddedStart = startAnchor.Point - startAnchor.Normal * bridgeAnchorEmbedDepth;
        Vector2 embeddedEnd = endAnchor.Point - endAnchor.Normal * bridgeAnchorEmbedDepth;
        Vector2 bridgeVector = embeddedEnd - embeddedStart;
        float bridgeLength = bridgeVector.magnitude;
        if (bridgeLength <= Mathf.Epsilon)
        {
            Debug.Log("QueenController: Bridge points are too close together.");
            return;
        }

        float antLength = GetWeaverAntLength();
        float spacing = Mathf.Max(antLength * bridgeAntSpacingMultiplier, 0.05f);
        int antCount = Mathf.Max(2, Mathf.CeilToInt(bridgeLength / spacing) + 1);
        Vector2 bridgeDirection = bridgeVector.normalized;
        float bridgeAngle = Mathf.Atan2(bridgeVector.y, bridgeVector.x) * Mathf.Rad2Deg;

        GameObject bridgeRoot = new GameObject("WeaverBridge");
        bridgeRoot.transform.position = embeddedStart;

        for (int i = 0; i < antCount; i++)
        {
            float t = antCount == 1 ? 0f : (float)i / (antCount - 1);
            Vector2 antPosition = Vector2.Lerp(embeddedStart, embeddedEnd, t);
            GameObject ant = Instantiate(weaverAntPrefab, antPosition, Quaternion.Euler(0f, 0f, bridgeAngle), bridgeRoot.transform);
            SetLayerRecursively(ant, GetPrimaryGroundLayer());

            if (ant.TryGetComponent<FollowQueen>(out var followQueen))
            {
                followQueen.enabled = false;
            }

            if (ant.TryGetComponent<Animator>(out var antAnimator))
            {
                antAnimator.SetBool("isWalking", false);
            }

            if (ant.TryGetComponent<Rigidbody2D>(out var antBody))
            {
                antBody.linearVelocity = Vector2.zero;
                antBody.angularVelocity = 0f;
                antBody.bodyType = RigidbodyType2D.Static;
            }

            if (ant.TryGetComponent<SpriteRenderer>(out var antSprite))
            {
                antSprite.flipX = bridgeDirection.x >= 0f;
                antSprite.sortingLayerID = spriteRenderer.sortingLayerID;
                antSprite.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, antSprite.sortingOrder + 2);
            }
        }

        Debug.Log($"QueenController: Created Weaver bridge with {antCount} ants.");
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

        return weaverAntPrefab.layer;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private float GetWeaverAntLength()
    {
        if (weaverAntPrefab.TryGetComponent<BoxCollider2D>(out var boxCollider))
        {
            float localScaleX = weaverAntPrefab.transform.localScale.x;
            return Mathf.Abs(boxCollider.size.x * localScaleX);
        }

        if (weaverAntPrefab.TryGetComponent<Collider2D>(out var collider2D))
        {
            return Mathf.Max(collider2D.bounds.size.x, 0.5f);
        }

        return 0.5f;
    }
}
