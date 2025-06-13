using UnityEngine;
using Unity.MLAgents;

public class Bullet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Zombie") || other.CompareTag("BruteZombie"))
        {
            PlayerAgent agent = GameObject.FindObjectOfType<PlayerAgent>();
            if (agent != null)
            {
                agent.AddReward(+1.0f);
            }

            other.GetComponent<ZombieAI>()?.TakeDamage(20f);
            other.GetComponent<ZombieAgent>()?.TakeDamage(20f);
            other.GetComponent<BruteZombieAgent>()?.TakeDamage(20f);
            Destroy(gameObject);
        }
    }
}
