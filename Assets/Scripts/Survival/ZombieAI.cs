using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    public Transform player;           
    public float moveSpeed = 2f;       
    public float attackRange = 1f;     

    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); 
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
}
