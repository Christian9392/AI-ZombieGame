using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;

public class BruteZombieAgent : Unity.MLAgents.Agent
{
    [Header("Agent Movement")]
    public float speed = 4f;  
    public float turnSpeed = 7f;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private Slider healthSlider;
    private Animator animator;
    private SpriteRenderer sr;
    public GameObject bloodStainPrefab;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;  
    public float projectileSpeed = 8f;   
    public Transform projectileSpawnPoint;  
    public float timeBetweenShots = 2f; 
    private float nextShootTime;

    public float spawnOffsetDistance = 0.6f; 

    [Header("Episode Settings")]
    public Vector2 spawnRangeX = new Vector2(-6f, 6f);
    public Vector2 spawnRangeY = new Vector2(-3f, 3f);
    public float optimalShootingDistance = 5f; 

    [Header("References (optional)")]
    public Transform player;

    Rigidbody2D rb;
    Vector2 lastPos;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;
        nextShootTime = Time.time; // Initialize cooldown

        // Auto-find player if not set
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("BruteZombieAgent: no Player tagged in scene!");
        }

        healthSlider = GetComponentInChildren<Slider>();
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                turnSpeed * Time.fixedDeltaTime
            );
        }
    }

    public override void OnEpisodeBegin()
    {
        currentHealth = maxHealth;
        if (healthSlider != null) healthSlider.value = currentHealth;
        lastPos = transform.position;
        nextShootTime = Time.time + timeBetweenShots; 

        // Only randomize positions when training
        if (Academy.Instance.IsCommunicatorOn)
        {
            transform.position = new Vector2(
                Random.Range(spawnRangeX.x, spawnRangeX.y),
                Random.Range(spawnRangeY.x, spawnRangeY.y)
            );
            if (player != null)
            {
                player.position = new Vector2(
                    Random.Range(spawnRangeX.x, spawnRangeX.y),
                    Random.Range(spawnRangeY.x, spawnRangeY.y)
                );
            }
        }

        lastPos = transform.position;
        RequestDecision(); 
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1) Log that we're in here (can remove for release)
        // Debug.Log("CollectObservations called");

        // 2) If we don't know where the player is, feed zeros
        if (player == null)
        {
            sensor.AddObservation(0f); // x-offset
            sensor.AddObservation(0f); // y-offset
            sensor.AddObservation(0f); // distance
            sensor.AddObservation(0f); // Can shoot (0 or 1)
            return;
        }

        // 3) Compute relative position
        Vector2 offset = (Vector2)(player.position - transform.position);

        // 4) Add observations:
        sensor.AddObservation(offset.x);
        sensor.AddObservation(offset.y);
        sensor.AddObservation(offset.magnitude);

        // 5) Add observation about whether the agent can shoot
        sensor.AddObservation(Time.time >= nextShootTime ? 1f : 0f); // 1 if can shoot, 0 otherwise
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        if (actions.DiscreteActions.Length == 0)
        {
            Debug.LogError("No actions received or action length is incorrect!");
            RequestDecision();
            return;
        }

        int act = actions.DiscreteActions[0];
        Vector2 dir = Vector2.zero;

        // Handle movement actions (1-4)
        switch (act)
        {
            case 1: dir = Vector2.left; break;
            case 2: dir = Vector2.right; break;
            case 3: dir = Vector2.up; break;
            case 4: dir = Vector2.down; break;
            case 5: // Action for shooting
                Shoot();
                break;
            default:
                // Debug.LogWarning("Received invalid action: " + act); // Can remove for release
                break;
        }

        // Apply movement
        rb.linearVelocity = dir * speed;
        bool isMoving = dir != Vector2.zero;
        animator?.SetBool("isMoving", isMoving);

        // Flip sprite based on direction
        if (dir.x > 0) sr.flipX = false;
        else if (dir.x < 0) sr.flipX = true;

        // Reward shaping based on strategy (shooting vs. moving closer/melee)
        if (player != null)
        {
            float currentDist = Vector2.Distance(transform.position, player.position);
            float meleeDistanceThreshold = 2.0f; 

            if (currentDist <= meleeDistanceThreshold)
            {
                // Reward for being in melee range (small positive)
                AddReward(0.003f); 
            }
            else if (Mathf.Abs(currentDist - optimalShootingDistance) < 1f)
            {
                // Reward staying within optimal shooting distance
                AddReward(0.005f); 
            }
            else if (currentDist > optimalShootingDistance)
            {
                // Reward moving closer if too far for shooting
                AddReward(0.002f);
            }

            // Per-step penalty to encourage efficient behavior
            AddReward(-0.001f);
        }

        RequestDecision();
    }

    private void Shoot()
    {
        if (Time.time >= nextShootTime && projectilePrefab != null && projectileSpawnPoint != null)
        {
            // Debug.Log("Shooting projectile!"); // Can remove for release
            FireProjectile();
            nextShootTime = Time.time + timeBetweenShots; 
            AddReward(0.1f); 
        }
        else if (Time.time < nextShootTime)
        {
            // Penalize trying to shoot when on cooldown
            AddReward(-0.01f);
        }
        else
        {
            // Penalize trying to shoot if no projectile prefab or spawn point
            AddReward(-0.05f);
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            // Calculate the direction towards the player
            Vector2 direction = (player.position - projectileSpawnPoint.position).normalized;
            Vector3 actualSpawnPosition = projectileSpawnPoint.position + (Vector3)direction * spawnOffsetDistance;

            GameObject projectile = Instantiate(projectilePrefab, actualSpawnPosition, Quaternion.identity);
            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();

            if (projRb != null && player != null)
            {
                // Apply the calculated velocity
                projRb.linearVelocity = direction * projectileSpeed;
                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj == null)
                {
                    proj = projectile.AddComponent<Projectile>();
                }
                proj.originAgent = this; 
                proj.damage = 20f; 
                Destroy(projectile, 8f);
            }
            else
            {
                Destroy(projectile);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0; // Default action (no movement, no shoot)

        if (Input.GetKey(KeyCode.A)) discrete[0] = 1;  // Move Left
        else if (Input.GetKey(KeyCode.D)) discrete[0] = 2;  // Move Right
        else if (Input.GetKey(KeyCode.W)) discrete[0] = 3;  // Move Up
        else if (Input.GetKey(KeyCode.S)) discrete[0] = 4;  // Move Down
        else if (Input.GetKey(KeyCode.Space)) discrete[0] = 5;  // Shoot
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthSlider != null) healthSlider.value = currentHealth;

        AddReward(-0.5f);  // penalty for getting hit

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        AddReward(-1f);      // big penalty for dying
        if (bloodStainPrefab != null)
        {
            GameObject blood = Instantiate(bloodStainPrefab, transform.position, Random.rotation);
            Destroy(blood, 6f);
        }
        EndEpisode();
        Destroy(gameObject);
    }

void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.collider.CompareTag("Player"))
    {
        collision.collider.GetComponent<PlayerHealth>()?.TakeDamage(5f);
        AddReward(-1.0f); 
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
        EndEpisode();
    }
    else if (collision.collider.CompareTag("Zombie"))
    {
        AddReward(-0.2f);
    }
}

    public void OnProjectileHitPlayer()
    {
        AddReward(+5.0f); 
        EndEpisode(); 
    }
}