using UnityEngine;

public class SmoothZoneCamera2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody2D targetRb;

    [Header("Camera")]
    [SerializeField] private float cameraZ = -10f;

    [Tooltip("سالب = رؤية أكثر لما تحت اللاعب")]
    [SerializeField] private Vector2 baseOffset = new Vector2(0f, -0.6f);

    [Header("Zones Around Camera Center")]
    [SerializeField] private Vector2 innerRange = new Vector2(0.7f, 0.35f);
    [SerializeField] private Vector2 outerRange = new Vector2(2.0f, 1.1f);

    [Header("Center Movement Smoothing")]
    [SerializeField] private float centerSmoothTimeX = 0.16f;
    [SerializeField] private float centerSmoothTimeY = 0.20f;
    [SerializeField] private float maxCenterSpeedX = 50f;
    [SerializeField] private float maxCenterSpeedY = 50f;

    [Header("Camera Transform Smoothing")]
    [SerializeField] private float cameraSmoothTime = 0.05f;

    [Header("Look Down While Falling")]
    [SerializeField] private bool lookDownWhenFalling = true;
    [SerializeField] private float fallVelocityThreshold = -0.5f;
    [SerializeField] private float extraDownOffset = -0.9f;
    [SerializeField] private float offsetSmoothSpeed = 6f;

    private Vector2 focusCenter;
    private Vector2 focusVelocity;
    private Vector3 cameraVelocity;
    private float currentOffsetY;

    private void Start()
    {
        if (target == null)
            return;

        if (targetRb == null)
            targetRb = target.GetComponent<Rigidbody2D>();

        focusCenter = target.position;
        currentOffsetY = baseOffset.y;

        Vector3 startPos = new Vector3(
            focusCenter.x + baseOffset.x,
            focusCenter.y + currentOffsetY,
            cameraZ
        );

        transform.position = startPos;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector2 targetPos = target.position;
        Vector2 desiredCenter = focusCenter;

        float dx = targetPos.x - focusCenter.x;
        float dy = targetPos.y - focusCenter.y;

        // X: إذا خرج اللاعب عن المجال الخارجي، ننقل مركز الكاميرا حتى يرجعه للحافة الخارجية.
        // وإذا بقي بين الداخلي والخارجي، نعيده تدريجيًا نحو الحافة الداخلية.
        if (dx > outerRange.x)
            desiredCenter.x = targetPos.x - outerRange.x;
        else if (dx < -outerRange.x)
            desiredCenter.x = targetPos.x + outerRange.x;
        else if (dx > innerRange.x)
            desiredCenter.x = targetPos.x - innerRange.x;
        else if (dx < -innerRange.x)
            desiredCenter.x = targetPos.x + innerRange.x;

        // Y
        if (dy > outerRange.y)
            desiredCenter.y = targetPos.y - outerRange.y;
        else if (dy < -outerRange.y)
            desiredCenter.y = targetPos.y + outerRange.y;
        else if (dy > innerRange.y)
            desiredCenter.y = targetPos.y - innerRange.y;
        else if (dy < -innerRange.y)
            desiredCenter.y = targetPos.y + innerRange.y;

        focusCenter.x = Mathf.SmoothDamp(
            focusCenter.x,
            desiredCenter.x,
            ref focusVelocity.x,
            centerSmoothTimeX,
            maxCenterSpeedX
        );

        focusCenter.y = Mathf.SmoothDamp(
            focusCenter.y,
            desiredCenter.y,
            ref focusVelocity.y,
            centerSmoothTimeY,
            maxCenterSpeedY
        );

        float targetOffsetY = baseOffset.y;

        if (lookDownWhenFalling && targetRb != null && targetRb.velocity.y < fallVelocityThreshold)
            targetOffsetY += extraDownOffset;

        currentOffsetY = Mathf.Lerp(
            currentOffsetY,
            targetOffsetY,
            1f - Mathf.Exp(-offsetSmoothSpeed * Time.deltaTime)
        );

        Vector3 desiredCameraPos = new Vector3(
            focusCenter.x + baseOffset.x,
            focusCenter.y + currentOffsetY,
            cameraZ
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredCameraPos,
            ref cameraVelocity,
            cameraSmoothTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 innerCenter = Application.isPlaying
            ? new Vector3(focusCenter.x, focusCenter.y, 0f)
            : transform.position - new Vector3(baseOffset.x, baseOffset.y, 0f);

        Gizmos.DrawWireCube(innerCenter, new Vector3(innerRange.x * 2f, innerRange.y * 2f, 0f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(innerCenter, new Vector3(outerRange.x * 2f, outerRange.y * 2f, 0f));
    }
}