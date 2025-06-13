using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayerAgent : Unity.MLAgents.Agent
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public float shootCooldown = 1.0f;

    private float lastShootTime;

    [Header("Target Info")]
    public Transform nearestZombie;

    private Transform FindNearestZombie()
    {
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject zombie in zombies)
        {
            if (zombie == null) continue;
            float dist = Vector2.Distance(transform.position, zombie.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = zombie.transform;
            }
        }

        return closest;
    }

    public override void OnEpisodeBegin()
    {
        lastShootTime = -shootCooldown;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        nearestZombie = FindNearestZombie();

        if (nearestZombie == null)
        {
            sensor.AddObservation(0f);  
            sensor.AddObservation(0f);  
        }
        else
        {
            Vector2 offset = (Vector2)(nearestZombie.position - transform.position);
            sensor.AddObservation(offset.x);
            sensor.AddObservation(offset.y);

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int shoot = actions.DiscreteActions[0]; 

        if (shoot == 1 && Time.time - lastShootTime > shootCooldown)
        {
            Shoot();
            lastShootTime = Time.time;
            AddReward(+0.1f); 
        }

        AddReward(-0.001f); 
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Debug.Log("AI SHOOT");

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = firePoint.up * bulletSpeed;
            }
        }
        else
        {
            Debug.LogWarning("Missing bulletPrefab or firePoint!");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = Input.GetMouseButton(0) ? 1 : 0;
    }

    void FixedUpdate()
    {
        RequestDecision(); 
    }
}

