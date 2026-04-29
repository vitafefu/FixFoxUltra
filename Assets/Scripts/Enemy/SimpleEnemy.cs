using UnityEngine;

/// <summary>
/// Простой враг, который двигается вправо и влево в пределах заданных зон относительно стартовой позиции
/// </summary>
public class SimpleEnemy : Enemy
{
    [Header("Движение влево-вправо")]
    [SerializeField] private float moveDistance = 5f; // Расстояние движения от центра
    [SerializeField] private float startOffset = 0f;  // Смещение центра движения
    
    private bool movingRight = true;
    private Vector3 startPosition;
    private float leftBoundary;
    private float rightBoundary;
    
    protected override void Start()
    {
        base.Start();
        
        // Запоминаем стартовую позицию
        startPosition = transform.position;
        
        // Вычисляем границы относительно стартовой позиции
        leftBoundary = startPosition.x - moveDistance + startOffset;
        rightBoundary = startPosition.x + moveDistance + startOffset;
    }
    
    private void Update()
    {
        Move();
        CheckBoundaries();
    }
    
    /// <summary>
    /// Движение врага влево-вправо
    /// </summary>
    private void Move()
    {
        float direction = movingRight ? 1f : -1f;
        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Проверка границ и смена направления
    /// </summary>
    private void CheckBoundaries()
    {
        if (movingRight && transform.position.x >= rightBoundary)
        {
            movingRight = false;
        }
        else if (!movingRight && transform.position.x <= leftBoundary)
        {
            movingRight = true;
        }
    }
    
    /// <summary>
    /// Визуализация границ движения в редакторе
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // В режиме редактора используем текущую позицию
            Vector3 pos = transform.position;
            float left = pos.x - moveDistance + startOffset;
            float right = pos.x + moveDistance + startOffset;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(left, pos.y - 0.5f, pos.z), new Vector3(left, pos.y + 0.5f, pos.z));
            Gizmos.DrawLine(new Vector3(right, pos.y - 0.5f, pos.z), new Vector3(right, pos.y + 0.5f, pos.z));
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(pos.x + startOffset, pos.y, pos.z), new Vector3(moveDistance * 2, 1f, 0));
        }
        else
        {
            // В режиме игры используем вычисленные границы
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(leftBoundary, transform.position.y - 0.5f, transform.position.z), 
                           new Vector3(leftBoundary, transform.position.y + 0.5f, transform.position.z));
            Gizmos.DrawLine(new Vector3(rightBoundary, transform.position.y - 0.5f, transform.position.z), 
                           new Vector3(rightBoundary, transform.position.y + 0.5f, transform.position.z));
        }
    }
}