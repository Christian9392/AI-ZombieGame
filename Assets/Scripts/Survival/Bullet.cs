using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Zombie"))
        {
            other.GetComponent<ZombieAI>()?.TakeDamage(20f);
            other.GetComponent<ZombieAgent>()?.TakeDamage(20f);
            Destroy(gameObject); 
        }
    }
}
