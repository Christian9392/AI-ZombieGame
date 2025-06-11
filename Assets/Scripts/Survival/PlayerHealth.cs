using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public Slider healthBarSlider;
    public GameObject gameOverTextObject; 
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        if (gameOverTextObject != null)
        {
            gameOverTextObject.SetActive(false);
        }
    }

    void Update()
    {
        // just for decoding-press H and player loses life(ill delete in the end)
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10f);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");

        if (animator != null)
        {
            animator.SetTrigger("player_dead");
        }

        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerShooting>().enabled = false;

        StartCoroutine(HandleDeathSequence());
    }

    System.Collections.IEnumerator HandleDeathSequence()
    {
        yield return new WaitForSeconds(1.2f); 

        if (gameOverTextObject != null)
        {
            gameOverTextObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}
