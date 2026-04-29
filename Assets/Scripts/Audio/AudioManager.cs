using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Listener")]
    [Tooltip("Опционально: назначь Transform игрока вручную. Если пусто — поиск по тегу Player.")]
    [SerializeField] private Transform listenerTransform;

    [Tooltip("Искать игрока по тегу Player, если listenerTransform не задан.")]
    [SerializeField] private bool autoFindPlayerByTag = true;

    [Tooltip("Тег объекта игрока для автопоиска.")]
    [SerializeField] private string playerTag = "Player";

    private readonly List<SoundZone> soundZones = new List<SoundZone>();

    public Vector3 ListenerPosition
    {
        get
        {
            if (listenerTransform != null)
                return listenerTransform.position;

            return Vector3.zero;
        }
    }

    private void Awake()
    {
        // Делаем менеджер единым на сцене (если случайно добавят второй — удаляем дубликат).
        AudioManager[] managers = FindObjectsOfType<AudioManager>();
        if (managers.Length > 1)
        {
            Debug.LogWarning("Найдено несколько AudioManager. Лишний экземпляр будет удалён.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        TryResolveListener();
    }

    private void Update()
    {
        if (listenerTransform == null)
        {
            TryResolveListener();
        }

        Vector3 listenerPos = ListenerPosition;

        for (int i = 0; i < soundZones.Count; i++)
        {
            SoundZone zone = soundZones[i];
            if (zone != null)
            {
                zone.UpdateVolume(listenerPos);
            }
        }
    }

    public void RegisterZone(SoundZone zone)
    {
        if (zone == null || soundZones.Contains(zone))
            return;

        soundZones.Add(zone);
    }

    public void UnregisterZone(SoundZone zone)
    {
        if (zone == null)
            return;

        soundZones.Remove(zone);
    }

    private void TryResolveListener()
    {
        if (listenerTransform != null || !autoFindPlayerByTag)
            return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            listenerTransform = player.transform;
        }
    }
}