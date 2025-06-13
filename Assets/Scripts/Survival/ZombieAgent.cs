using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;

public class ZombieAgent : Unity.MLAgents.Agent
{
    // Controls the zombie's movement
    [Header("Agent Movement")]
    public float speed = 5f;
    public float turnSpeed = 10f;

    // Implement the zombie's health
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private Slider healthSlider;
    private Animator animator;
    private SpriteRenderer sr;
    public GameObject bloodStainPrefab;

    // To spawn zombie when episode begins
    [Header("Episode Settings")]
    public Vector2 spawnRangeX = new Vector2(-6f, 6f);
    public Vector2 spawnRangeY = new Vector2(-3f, 3f);

    // Add a reference to the player to chase
    [Header("References (optional)")]
    public Transform player;

    Rigidbody2D rb;
    Vector2 lastPos;

    // Initialize the agent
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Set Rigidbody2D properties
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;

        // Find the player if not assigned by reference
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("ZombieAgent: no Player tagged in scene!");
        }

        // Health slider setup
        healthSlider = GetComponentInChildren<Slider>();
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    // Smooth movement: rotate the zombie to face the direction of movement
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

    // Reset the agent at the start of each episode
    public override void OnEpisodeBegin()
    {
        // Reset the health
        currentHealth = maxHealth;
        if (healthSlider != null) healthSlider.value = currentHealth;
        lastPos = transform.position;

        // Randomize positions when training
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

        // Kick off the first decision
        RequestDecision();
    }

    // Collect observations for the ML Agent
    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("CollectObservations called");

        // Feed the agent's position with 0s
        if (player == null)
        {
            sensor.AddObservation(0f); // x-offset
            sensor.AddObservation(0f); // y-offset
            sensor.AddObservation(0f); // distance
            return;
        }

        // Calculate the offset from the zombie to the player
        Vector2 offset = (Vector2)(player.position - transform.position);

        // 3 floats
        sensor.AddObservation(offset.x); // x-offset
        sensor.AddObservation(offset.y); // y-offset
        sensor.AddObservation(offset.magnitude); // distance
    }

    // Handle actions received from the ML Agent
    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived called");

        // Discrete actions:
        int act = actions.DiscreteActions[0];
        Vector2 dir = Vector2.zero;
        switch (act)
        {
            case 1: dir = Vector2.left; break;
            case 2: dir = Vector2.right; break;
            case 3: dir = Vector2.up; break;
            case 4: dir = Vector2.down; break;
        }

        // Move the zombie agent
        rb.linearVelocity = dir * speed;

        Debug.Log($"Moved {dir} to {transform.position}");

        // Animate the zombie agent to move
        bool isMoving = dir != Vector2.zero;
        animator?.SetBool("isMoving", isMoving);

        // Reward by distance change
        if (player != null)
        {
            // Calculate the distance change
            float oldDist = Vector2.Distance(lastPos, player.position);
            float newDist = Vector2.Distance(transform.position, player.position);
            float delta = oldDist - newDist;

            // Reward:
            if (delta > 0f)
            {
                AddReward(delta * 0.2f);
            }
            else if (delta < 0f)
            {
                AddReward(delta * 0.5f);
            }

            // Per-step penalty
            AddReward(-0.001f);

            lastPos = transform.position;

            // Bonus + end if close enough
            if (newDist < 1.5f)
            {
                AddReward(+1f);
                EndEpisode();
            }
        }

        RequestDecision();
    }

    // Handle manual control for testing purposes
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;
        if (Input.GetKey(KeyCode.A)) discrete[0] = 1;
        if (Input.GetKey(KeyCode.D)) discrete[0] = 2;
        if (Input.GetKey(KeyCode.W)) discrete[0] = 3;
        if (Input.GetKey(KeyCode.S)) discrete[0] = 4;
    }

    // Method to take damage
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthSlider != null) healthSlider.value = currentHealth;

        AddReward(-0.5f); // penalty for getting hit

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Method to handle the zombie's death
    private void Die()
    {
        AddReward(-1f); // big penalty for dying
        if (bloodStainPrefab != null)
        {
            GameObject blood = Instantiate(bloodStainPrefab, transform.position, Random.rotation);
            Destroy(blood, 6f);
        }
        EndEpisode();
        Destroy(gameObject);
    }

    // Handle collision with the player or other zombies
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.GetComponent<PlayerHealth>()?.TakeDamage(10f);

            if (animator != null)
            {
                animator.SetTrigger("attack");
            }

            AddReward(+0.5f);
        }
        if (collision.collider.CompareTag("Zombie"))
        {
            AddReward(-0.2f); // penalty for colliding with another zombie
        }
    }
}
