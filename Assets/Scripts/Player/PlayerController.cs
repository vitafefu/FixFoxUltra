using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    private enum PredictedHitType
    {
        None,
        Ground,
        Water,
        Wall
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Double Tap Run")]
    [SerializeField] private float doubleTapTime = 0.25f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private LayerMask waterZoneLayer;
    [SerializeField] private LayerMask ladderLayer;

    [Header("Ladder Movement")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private Tilemap ladderTilemap;

    [Header("Instant Water Jump Prediction")]
    [SerializeField] private float predictionStepTime = 0.015f;
    [SerializeField] private float maxPredictionTime = 2.4f;
    [SerializeField] private float predictionIgnoreTime = 0.08f;

    [Header("Predicted Body")]
    [SerializeField] private Vector2 predictedBodyScale = new Vector2(0.82f, 0.90f);

    [Header("Water Probes")]
    [SerializeField] private Vector2 waterProbeBoxSize = new Vector2(0.32f, 0.32f);
    [SerializeField] private float lowerProbeYOffset = -0.22f;
    [SerializeField] private float middleProbeYOffset = 0.02f;

    [Header("Prediction Start Offset")]
    [SerializeField] private float predictionStartForwardFactor = 0.55f;
    [SerializeField] private float predictionStartUpFactor = 0.15f;

    [Header("Water Movement")]
    [SerializeField] private float waterGravityScale = 1f;
    [SerializeField] private float swimHorizontalSpeed = 3f;

    [SerializeField] private float swimUpSpeed = 4.5f;
    [SerializeField] private float swimDownAcceleration = 10f;
    [SerializeField] private float maxSwimFallSpeed = 7f;

    [SerializeField] private float waterEntryFallDamping = 0.35f;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCol;

    private float moveInputX;
    private float moveInputY;
    private bool jumpRequested;

    private float lastLeftTapTime = -10f;
    private float lastRightTapTime = -10f;

    private bool isRunning;
    private int runDirection;

    private float defaultGravityScale;
    private bool wasInWaterLastFrame;

    private bool waterJumpLocked;
    private float lockedWaterJumpXVelocity;
    private bool facingRight = true;

    private bool instantPredictionResult;
    private bool willLandInWaterLatched;

    public float MoveInputX => moveInputX;
    public float MoveInputY => moveInputY;
    public bool FacingRight => facingRight;
    public bool IsWaterJumpLocked => waterJumpLocked;

    public float AnimationSpeed => IsInWater
        ? Mathf.Max(Mathf.Abs(moveInputX), Mathf.Abs(moveInputY))
        : Mathf.Abs(moveInputX);

    public bool IsGrounded { get; private set; }
    public float VerticalSpeed => rb.velocity.y;
    public bool IsInWater { get; private set; }
    public bool IsInWaterZone { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsClimbing { get; private set; }
    public bool WillLandInWater => willLandInWaterLatched;

    private void Awake()
    {
        Physics2D.queriesHitTriggers = true;

        rb = GetComponent<Rigidbody2D>();
        capsuleCol = GetComponent<CapsuleCollider2D>();
        defaultGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputY = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveInputY = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveInputY = -1f;

        if (!waterJumpLocked)
        {
            if (moveInputX > 0.01f)
                facingRight = true;
            else if (moveInputX < -0.01f)
                facingRight = false;
        }

        HandleRunInput();
        StopRunIfNeeded();

        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        RefreshEnvironmentState();

        if (!wasInWaterLastFrame && IsInWater)
        {
            ApplyWaterEntryDamping();
            ClearWaterJumpState();
        }

        if (IsInWater)
        {
            IsClimbing = false;
            HandleSwimming();
        }
        else if (IsOnLadder)
        {
            HandleLadderClimbing();
        }
        else
        {
            if (IsClimbing)
            {
                IsClimbing = false;
                rb.gravityScale = defaultGravityScale;
            }

            HandleGroundAndAir();
        }

        wasInWaterLastFrame = IsInWater;
        jumpRequested = false;
    }

    private void RefreshEnvironmentState()
    {
        IsInWaterZone = OverlapsLayerWithCapsule(waterZoneLayer, 1f);
        IsInWater = HasRealWaterSubmersion(transform.position);

        bool ladderOverlap = OverlapsLayerWithCapsule(ladderLayer, 1f);
        bool ladderByTilemap = IsInsideLadderTilemap();

        IsOnLadder = ladderOverlap || ladderByTilemap;

        IsGrounded = !IsInWater && !IsClimbing && capsuleCol.IsTouchingLayers(groundLayer);
    }

    private bool OverlapsLayerWithCapsule(LayerMask mask, float scale)
    {
        Bounds b = capsuleCol.bounds;
        Vector2 size = new Vector2(b.size.x * scale, b.size.y * scale);

        Collider2D hit = Physics2D.OverlapCapsule(
            b.center,
            size,
            CapsuleDirection2D.Vertical,
            0f,
            mask
        );

        return hit != null;
    }

    private bool IsInsideLadderTilemap()
    {
        if (ladderTilemap == null)
            return false;

        Bounds b = capsuleCol.bounds;

        Vector3 center = b.center;
        Vector3 lower = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
        Vector3 upper = new Vector3(b.center.x, b.max.y - 0.05f, b.center.z);

        return HasLadderTileAtWorldPosition(center)
            || HasLadderTileAtWorldPosition(lower)
            || HasLadderTileAtWorldPosition(upper);
    }

    private bool HasLadderTileAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cellPosition = ladderTilemap.WorldToCell(worldPosition);
        return ladderTilemap.HasTile(cellPosition);
    }

    private void ApplyWaterEntryDamping()
    {
        Vector2 velocity = rb.velocity;

        if (velocity.y < 0f)
            velocity.y *= waterEntryFallDamping;

        rb.velocity = velocity;
    }

    private void HandleGroundAndAir()
    {
        rb.gravityScale = defaultGravityScale;

        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector2 velocity = rb.velocity;

        if (waterJumpLocked)
        {
            velocity.x = lockedWaterJumpXVelocity;
            rb.velocity = velocity;
            return;
        }

        velocity.x = moveInputX * currentSpeed;

        if (jumpRequested && IsGrounded)
        {
            velocity.y = jumpForce;

            instantPredictionResult = false;

            if (IsInWaterZone)
            {
                Vector2 predictionStart = GetJumpPredictionStartPosition(velocity.x);
                Vector2 predictionVelocity = new Vector2(velocity.x, jumpForce);

                instantPredictionResult = EvaluateWaterLandingInstant(predictionStart, predictionVelocity);

                if (instantPredictionResult)
                    LatchWaterLanding(velocity.x);
            }

            rb.velocity = velocity;
            IsGrounded = false;
            return;
        }

        rb.velocity = velocity;
    }

    private void HandleSwimming()
    {
        rb.gravityScale = waterGravityScale;

        Vector2 velocity = rb.velocity;
        velocity.x = moveInputX * swimHorizontalSpeed;

        if (moveInputY > 0.01f)
        {
            velocity.y = moveInputY * swimUpSpeed;
        }
        else if (moveInputY < -0.01f)
        {
            velocity.y -= Mathf.Abs(moveInputY) * swimDownAcceleration * Time.fixedDeltaTime;

            if (velocity.y < -maxSwimFallSpeed)
                velocity.y = -maxSwimFallSpeed;
        }
        else
        {
            if (velocity.y < -maxSwimFallSpeed)
                velocity.y = -maxSwimFallSpeed;
        }

        rb.velocity = velocity;
    }

    private void HandleLadderClimbing()
    {
        ClearWaterJumpState();

        rb.gravityScale = 0f;

        Vector2 velocity = rb.velocity;

        velocity.x = moveInputX * moveSpeed;

        if (Mathf.Abs(moveInputY) > 0.01f)
        {
            IsClimbing = true;
            velocity.y = moveInputY * climbSpeed;
        }
        else
        {
            velocity.y = 0f;
        }

        rb.velocity = velocity;
    }

    private Vector2 GetJumpPredictionStartPosition(float xVelocity)
    {
        Bounds b = capsuleCol.bounds;

        float dir;
        if (Mathf.Abs(xVelocity) > 0.01f)
            dir = Mathf.Sign(xVelocity);
        else
            dir = facingRight ? 1f : -1f;

        float x = b.center.x + dir * (b.extents.x * predictionStartForwardFactor);
        float y = b.center.y + b.extents.y * predictionStartUpFactor;

        return new Vector2(x, y);
    }

    private bool EvaluateWaterLandingInstant(Vector2 startCenter, Vector2 startVelocity)
    {
        PredictedHitType hitType = PredictLandingByBodySimulation(startCenter, startVelocity);
        return hitType == PredictedHitType.Water;
    }

    private PredictedHitType PredictLandingByBodySimulation(Vector2 startCenter, Vector2 startVelocity)
    {
        Vector2 gravity = new Vector2(0f, Physics2D.gravity.y * defaultGravityScale);
        Bounds b = capsuleCol.bounds;

        Vector2 predictedBodySize = new Vector2(
            b.size.x * predictedBodyScale.x,
            b.size.y * predictedBodyScale.y
        );

        int totalSteps = Mathf.CeilToInt(maxPredictionTime / predictionStepTime);

        for (int i = 1; i <= totalSteps; i++)
        {
            float t = i * predictionStepTime;

            if (t < predictionIgnoreTime)
                continue;

            Vector2 simulatedCenter =
                startCenter +
                startVelocity * t +
                0.5f * gravity * t * t;

            WaterProbeResult probes = GetWaterProbeResult(simulatedCenter);

            if (probes.lowerInWater && probes.middleInWater)
                return PredictedHitType.Water;

            Collider2D groundHit = Physics2D.OverlapCapsule(
                simulatedCenter,
                predictedBodySize,
                CapsuleDirection2D.Vertical,
                0f,
                groundLayer
            );

            if (groundHit != null)
            {
                Vector2 toHit = (Vector2)groundHit.bounds.center - simulatedCenter;

                if (Mathf.Abs(toHit.x) > predictedBodySize.x * 0.35f)
                    return PredictedHitType.Wall;

                return PredictedHitType.Ground;
            }
        }

        return PredictedHitType.None;
    }

    private bool HasRealWaterSubmersion(Vector2 worldCenter)
    {
        WaterProbeResult probes = GetWaterProbeResult(worldCenter);
        return probes.lowerInWater && probes.middleInWater;
    }

    private WaterProbeResult GetWaterProbeResult(Vector2 center)
    {
        Bounds b = capsuleCol.bounds;

        float bodyWidth = b.size.x * predictedBodyScale.x;
        float bodyHeight = b.size.y * predictedBodyScale.y;

        Vector2 lowerCenter = center + new Vector2(0f, bodyHeight * lowerProbeYOffset);
        Vector2 middleCenter = center + new Vector2(0f, bodyHeight * middleProbeYOffset);

        Vector2 boxSize = new Vector2(
            Mathf.Min(bodyWidth * 0.75f, waterProbeBoxSize.x),
            Mathf.Min(bodyHeight * 0.35f, waterProbeBoxSize.y)
        );

        bool lower = Physics2D.OverlapBox(lowerCenter, boxSize, 0f, waterLayer) != null;
        bool middle = Physics2D.OverlapBox(middleCenter, boxSize, 0f, waterLayer) != null;

        return new WaterProbeResult(lowerCenter, middleCenter, boxSize, lower, middle);
    }

    private void LatchWaterLanding(float xVelocity)
    {
        willLandInWaterLatched = true;
        waterJumpLocked = true;
        lockedWaterJumpXVelocity = xVelocity;
    }

    private void ClearWaterJumpState()
    {
        instantPredictionResult = false;
        willLandInWaterLatched = false;
        waterJumpLocked = false;
        lockedWaterJumpXVelocity = 0f;
    }

    private void HandleRunInput()
    {
        if (waterJumpLocked)
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftTapTime <= doubleTapTime)
            {
                isRunning = true;
                runDirection = -1;
            }

            lastLeftTapTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightTapTime <= doubleTapTime)
            {
                isRunning = true;
                runDirection = 1;
            }

            lastRightTapTime = Time.time;
        }
    }

    private void StopRunIfNeeded()
    {
        if (waterJumpLocked)
            return;

        if (Mathf.Abs(moveInputX) < 0.01f)
        {
            isRunning = false;
            runDirection = 0;
            return;
        }

        if ((runDirection == 1 && moveInputX < 0f) || (runDirection == -1 && moveInputX > 0f))
        {
            isRunning = false;
            runDirection = 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (capsuleCol == null)
            capsuleCol = GetComponent<CapsuleCollider2D>();

        if (capsuleCol == null)
            return;

        DrawCurrentWaterProbes();

        if (!Application.isPlaying)
            return;

        DrawPredictedPathAndWaterChecks();
    }

    private void DrawCurrentWaterProbes()
    {
        WaterProbeResult probes = GetWaterProbeResult(transform.position);

        Gizmos.color = probes.lowerInWater ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(probes.lowerCenter, probes.boxSize);

        Gizmos.color = probes.middleInWater ? Color.green : Color.cyan;
        Gizmos.DrawWireCube(probes.middleCenter, probes.boxSize);
    }

    private void DrawPredictedPathAndWaterChecks()
    {
        Bounds b = capsuleCol.bounds;
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        Vector2 predictedVelocity;
        if (waterJumpLocked)
            predictedVelocity = new Vector2(lockedWaterJumpXVelocity, rb.velocity.y);
        else if (IsGrounded)
            predictedVelocity = new Vector2(moveInputX * currentSpeed, jumpForce);
        else
            predictedVelocity = rb.velocity;

        Vector2 startPosition = GetJumpPredictionStartPosition(predictedVelocity.x);
        Vector2 gravity = new Vector2(0f, Physics2D.gravity.y * defaultGravityScale);
        Vector2 predictedBodySize = new Vector2(
            b.size.x * predictedBodyScale.x,
            b.size.y * predictedBodyScale.y
        );

        Vector2 prev = startPosition;
        int totalSteps = Mathf.CeilToInt(maxPredictionTime / predictionStepTime);

        for (int i = 1; i <= totalSteps; i++)
        {
            float t = i * predictionStepTime;
            Vector2 current =
                startPosition +
                predictedVelocity * t +
                0.5f * gravity * t * t;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(prev, current);

            if (t >= predictionIgnoreTime)
            {
                WaterProbeResult probes = GetWaterProbeResult(current);

                Gizmos.color = probes.lowerInWater ? Color.green : new Color(1f, 0.8f, 0f, 1f);
                Gizmos.DrawWireCube(probes.lowerCenter, probes.boxSize);

                Gizmos.color = probes.middleInWater ? Color.green : Color.cyan;
                Gizmos.DrawWireCube(probes.middleCenter, probes.boxSize);

                Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
                Gizmos.DrawWireCube(current, predictedBodySize);
            }

            prev = current;
        }
    }

    private struct WaterProbeResult
    {
        public Vector2 lowerCenter;
        public Vector2 middleCenter;
        public Vector2 boxSize;
        public bool lowerInWater;
        public bool middleInWater;

        public WaterProbeResult(
            Vector2 lowerCenter,
            Vector2 middleCenter,
            Vector2 boxSize,
            bool lowerInWater,
            bool middleInWater)
        {
            this.lowerCenter = lowerCenter;
            this.middleCenter = middleCenter;
            this.boxSize = boxSize;
            this.lowerInWater = lowerInWater;
            this.middleInWater = middleInWater;
        }
    }
}