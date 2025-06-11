using UnityEngine;
using UnityEngine.UI;

public class ZombieAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float attackRange = 1f;

    public float maxHealth = 100f;
    private float currentHealth;

    private Rigidbody2D rb;
    private Animator animator;
    private Slider healthSlider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        healthSlider = transform.Find("HealthBar")?.GetComponent<Slider>();
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);

        bool isMoving = distance > attackRange;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Debug.Log("Zombie attacking!");
        }

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");
        Destroy(gameObject);
    }
}
