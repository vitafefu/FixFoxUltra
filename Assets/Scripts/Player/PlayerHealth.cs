using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3; // Максимум сердечек
    [SerializeField] private float invincibilityDuration = 1f;
    
    private int currentHealth;
    private float invincibilityTimer;
    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => invincibilityTimer > 0f;
    
    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        Debug.Log($"Здоровье игрока: {currentHealth}/{maxHealth} сердечек");
    }
    
    private void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
            
            // Визуальный эффект мигания при неуязвимости
            if (spriteRenderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * 15f, 0.5f) + 0.5f;
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
        }
        else if (spriteRenderer != null && spriteRenderer.color != originalColor)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    private void OnGUI()
    {
        // Отображение сердечек
        string hearts = "";
        for (int i = 0; i < currentHealth; i++)
        {
            hearts += "❤️ ";
        }
        for (int i = currentHealth; i < maxHealth; i++)
        {
            hearts += "🖤 ";
        }
        GUI.Box(new Rect(10, 10, 200, 30), $"Здоровье: {hearts}");
    }
    
    public void TakeDamage(int damage)
    {
        // Проверка на неуязвимость
        if (IsInvincible)
        {
            Debug.Log($"Игрок неуязвим! Осталось времени: {invincibilityTimer:F2}");
            return;
        }
        
        if (currentHealth <= 0)
        {
            Debug.Log("Игрок уже мертв!");
            return;
        }
        
        // Урон в сердечках
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        Debug.Log($"Игрок потерял {damage} сердечко(а). Осталось сердечек: {currentHealth}/{maxHealth}");
        
        if (currentHealth > 0)
        {
            // Включаем неуязвимость
            invincibilityTimer = invincibilityDuration;
            Debug.Log($"Неуязвимость активирована на {invincibilityDuration} секунд");
        }
        else
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        int healed = currentHealth - oldHealth;
        Debug.Log($"Игрок восстановил {healed} сердечко(а). Теперь сердечек: {currentHealth}/{maxHealth}");
    }
    
    private void Die()
    {
        Debug.Log("Игрок умер! Все сердечки потеряны");
        
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Отключаем коллайдер
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
    
    public void ResetInvincibility()
    {
        invincibilityTimer = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}