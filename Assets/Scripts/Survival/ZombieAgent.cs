using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;

public class ZombieAgent : Unity.MLAgents.Agent
{
    [Header("Agent Movement")]
    public float speed = 5f;
    public float turnSpeed = 10f;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private Slider healthSlider;
    private Animator animator;
    private SpriteRenderer sr;
    public GameObject bloodStainPrefab;

    [Header("Episode Settings")]
    public Vector2 spawnRangeX = new Vector2(-6f, 6f);
    public Vector2 spawnRangeY = new Vector2(-3f, 3f);

    [Header("References (optional)")]
    public Transform player;

    Rigidbody2D rb;
    Vector2 lastPos;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        // Force kinematic for MovePosition
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;

        // Auto-find player if not set
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("ZombieAgent: no Player tagged in scene!");
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

        // Kick off the first decision
        RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1) Always log that we're in here
        Debug.Log("CollectObservations called");

        // 2) If we don't know where the player is, just feed zeros
        if (player == null)
        {
            sensor.AddObservation(0f); // x-offset
            sensor.AddObservation(0f); // y-offset
            sensor.AddObservation(0f); // distance
            return;
        }

        // 3) Compute relative position
        Vector2 offset = (Vector2)(player.position - transform.position);

        // 4) Add exactly 3 floats:
        //    a) X offset
        sensor.AddObservation(offset.x);
        //    b) Y offset
        sensor.AddObservation(offset.y);
        //    c) Straight‐line distance
        sensor.AddObservation(offset.magnitude);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived called");
        int act = actions.DiscreteActions[0];
        Vector2 dir = Vector2.zero;
        switch (act)
        {
            case 1: dir = Vector2.left; break;
            case 2: dir = Vector2.right; break;
            case 3: dir = Vector2.up; break;
            case 4: dir = Vector2.down; break;
        }

        // Move agent
        rb.linearVelocity = dir * speed;

        Debug.Log($"Moved {dir} to {transform.position}");

        bool isMoving = dir != Vector2.zero;
        animator?.SetBool("isMoving", isMoving);

        // Flip sprite on X when moving left/right
        if (dir.x > 0) sr.flipX = false;
        else if (dir.x < 0) sr.flipX = true;

        // Reward by distance change
        if (player != null)
        {
            // 3) Compute distance change for shaped reward
            float oldDist = Vector2.Distance(lastPos, player.position);
            float newDist = Vector2.Distance(transform.position, player.position);
            float delta = oldDist - newDist;

            // Reward shaping
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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;
        if (Input.GetKey(KeyCode.A)) discrete[0] = 1;
        if (Input.GetKey(KeyCode.D)) discrete[0] = 2;
        if (Input.GetKey(KeyCode.W)) discrete[0] = 3;
        if (Input.GetKey(KeyCode.S)) discrete[0] = 4;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (healthSlider != null) healthSlider.value = currentHealth;

        AddReward(-0.5f);  // penalty for getting hit

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        AddReward(-1f);      // big penalty for dying
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
            collision.collider.GetComponent<PlayerHealth>()?.TakeDamage(10f);

            if (animator != null)
            {
                animator.SetTrigger("attack");
            }

            AddReward(+0.5f);
        }
        if (collision.collider.CompareTag("Zombie"))
        {
            AddReward(-0.2f);
        }
    }
}
