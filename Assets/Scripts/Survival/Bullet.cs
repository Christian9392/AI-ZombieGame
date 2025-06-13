using UnityEngine;
using Unity.MLAgents;

public class Bullet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Zombie"))
        {
            PlayerAgent agent = GameObject.FindObjectOfType<PlayerAgent>();
            if (agent != null)
            {
                agent.AddReward(+1.0f);
            }

            Destroy(other.gameObject); 
            Destroy(gameObject);       
        }
    }
}
