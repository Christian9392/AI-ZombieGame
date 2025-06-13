using UnityEngine;

public class Projectile : MonoBehaviour
{
    public BruteZombieAgent originAgent; // Reference to the zombie that fired it
    public float damage = 10f; // Damage the projectile deals

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            // Inform the originating agent about the hit
            if (originAgent != null)
            {
                originAgent.OnProjectileHitPlayer();
            }
            Destroy(gameObject); // Destroy the projectile on impact
        }
    }

    // Use OnCollisionEnter2D if your projectile is a Rigidbody2D and you're using physics collisions
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            if (originAgent != null)
            {
                originAgent.OnProjectileHitPlayer();
            }
            Destroy(gameObject);
        }
    }
}