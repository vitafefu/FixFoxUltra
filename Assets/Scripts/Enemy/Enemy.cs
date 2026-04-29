using UnityEngine;

/// <summary>
/// Базовый класс для всех врагов
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Общие свойства врага")]
    [SerializeField] protected int health = 100;
    [SerializeField] protected int damage = 1; // Урон в сердечках (1 сердечко)
    [SerializeField] protected float moveSpeed = 3f;
    
    [Header("Атака")]
    [SerializeField] protected float damageCooldown = 0.5f; // Задержка между ударами
    
    protected Rigidbody2D rb;
    private float lastDamageTime;
    
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log($"{gameObject.name} создан, урон: {damage} сердечка");
    }
    
    /// <summary>
    /// Метод получения урона
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"{gameObject.name} получил урон {amount}. Осталось здоровья: {health}");
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Метод смерти врага
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} умер!");
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Метод нанесения урона игроку (в сердечках)
    /// </summary>
    protected virtual void DealDamage(PlayerHealth playerHealth)
    {
        if (playerHealth != null && !playerHealth.IsInvincible)
        {
            Debug.Log($"{gameObject.name} наносит урон {damage} сердечко(а) игроку");
            playerHealth.TakeDamage(damage);
        }
        else if (playerHealth != null && playerHealth.IsInvincible)
        {
            Debug.Log($"{gameObject.name} пытается атаковать, но игрок неуязвим!");
        }
    }
    
    /// <summary>
    /// Обработка столкновения
    /// </summary>
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && Time.time - lastDamageTime >= damageCooldown)
        {
            lastDamageTime = Time.time;
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            DealDamage(playerHealth);
        }
    }
    
    /// <summary>
    /// Обработка непрерывного столкновения (если враг касается игрока)
    /// </summary>
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && Time.time - lastDamageTime >= damageCooldown)
        {
            lastDamageTime = Time.time;
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            DealDamage(playerHealth);
        }
    }
}