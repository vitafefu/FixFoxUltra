using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Min(0.01f)]
    [SerializeField] private float radius = 8f;

    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 1f;

    [Tooltip("Кривая затухания: X=нормализованная дистанция (0..1), Y=множитель громкости (0..1).")]
    [SerializeField]
    private AnimationCurve falloffCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0f)
    );

    [Tooltip("Игнорировать разницу по Z (для 2D почти всегда true).")]
    [SerializeField] private bool ignoreZ = true;

    private AudioSource audioSource;
    private AudioManager audioManager;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Рекомендуемые параметры для зоны окружения.
        audioSource.playOnAwake = true;
        audioSource.loop = true;

        // Управляем громкостью сами (по дистанции), а не встроенным 3D-rolloff.
        audioSource.spatialBlend = 0f; // 2D звук
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void OnEnable()
    {
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.RegisterZone(this);
        }
        else
        {
            Debug.LogWarning($"SoundZone на объекте '{name}' не нашёл AudioManager на сцене.");
        }
    }

    private void OnDisable()
    {
        if (audioManager != null)
        {
            audioManager.UnregisterZone(this);
        }
    }

    public void UpdateVolume(Vector3 listenerPosition)
    {
        Vector3 zonePosition = transform.position;

        if (ignoreZ)
        {
            listenerPosition.z = 0f;
            zonePosition.z = 0f;
        }

        float distance = Vector3.Distance(listenerPosition, zonePosition);

        if (distance >= radius)
        {
            if (audioSource.volume != 0f)
                audioSource.volume = 0f;
            return;
        }

        float normalizedDistance = Mathf.Clamp01(distance / radius); // 0 в центре, 1 на границе
        float volumeMultiplier = Mathf.Clamp01(falloffCurve.Evaluate(normalizedDistance));
        float targetVolume = maxVolume * volumeMultiplier;

        audioSource.volume = targetVolume;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.75f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}