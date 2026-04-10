using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Modular Health System with optional knockback options.
/// </summary>
public class HealthHandler : MonoBehaviour
{
    [Header("Knockback-Switches")]
    [SerializeField] private bool charKnockback;
    [SerializeField] private bool regularKnockback;
    [SerializeField] private bool bounceOffEnemy;

    [Header("Health-Objects")]
    public Slider guiHealth;
    public Material thisMaterial;
    public ParticleSystem deathExplosion;
    public AudioSource audioSource;
    public AudioClip hurtSound;

    [Header("Health-Controls")]
    public float healthAmt;
    public int optionalCooldownFrames;
    [SerializeField] private string lossTag;
    [SerializeField] private string gainTag;

    private Collider userCollider;
    private bool isInvincible;
    private bool isDead;
    private bool deathStarted;
    private bool isPooled;
    private CharacterMovement mainChar;
    private NavMeshAgent enemyMesh;

    /// <summary>
    /// Checks for various bool states and healthAmt
    /// </summary>
    private void Awake()
    {
        if (bounceOffEnemy)
        {
            enemyMesh = GetComponent<NavMeshAgent>();
        }

        if (charKnockback)
        {
            mainChar = GetComponent<CharacterMovement>();
        }

        isDead = false;
        isInvincible = false;
        userCollider = GetComponent<Collider>();
        if (healthAmt < 0)
        {
            isInvincible = true;
        }
    }

    /// <summary>
    /// This is to allow outside scripts to cause 1 damage
    /// </summary>
    public void TakeDamage() 
    {
        Debug.LogWarning("Hurt");
        float DAMAGE_AMMOUNT = 1.0F;
        healthAmt -= DAMAGE_AMMOUNT;
    }
    /// <summary>
    /// This is to allow outside scripts to cause an ammount of damage
    /// </summary>
    public void TakeDamage(float ammount)
    {
        Debug.LogWarning("Hurt");
        healthAmt -= ammount;
    }

    /// <summary>
    /// This will set the object health to zero killing it.
    /// </summary>
    public void InstaKill()
    {
        Debug.LogWarning("Hurt");
        isDead = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    public void CheckDistance(Transform other) 
    {
        float dist = Vector3.Distance(transform.position, other.transform.position);

        if (dist < 0.5f)
        {
            enemyMesh.enabled = false; // ONLY disable when actually hit
        }
        else 
        {
            enemyMesh.enabled = true;
        }
    }

    /// <summary>
    /// Allows for control when a collider interacts 
    /// </summary>
    /// <param name="other"></param>
    /// 
    private void OnTriggerStay(Collider other)
    {
        if (isInvincible) return;

        if (!string.IsNullOrEmpty(lossTag) && other.CompareTag(lossTag))
        {
            Debug.LogWarning("Is Loss");

            if (healthAmt > 1.0f)
            {
                healthAmt -= 1.0f;

                if (charKnockback)
                {
                    StartCoroutine(mainChar.TemporarilyDisableMovement(0.1f));
                    KnockbackCharacter(other.transform);
                }

                if (regularKnockback)
                {
                    Knockback(other.transform);
                }

                StartCoroutine(HurtCooldown());
            }
            else
            {
                isDead = true;
            }
        }

        if (!string.IsNullOrEmpty(gainTag) && other.CompareTag(gainTag))
        {
            healthAmt += 1.0f;
        }
    }

    /// <summary>
    /// Knockback for object 'source'
    /// </summary>
    /// <param name="source"></param>
    private void Knockback(Transform source) 
    {
        Vector3 direction = (transform.position - source.position).normalized;
        StartCoroutine(Knockback(direction, 0.1f, 2f));
    }

    /// <summary>
    /// Knockback for object 'source' on everything but the y axis.
    /// </summary>
    /// <param name="source"></param>
    private void KnockbackCharacter(Transform source)
    {
        Vector3 direction = (transform.position - source.transform.position).normalized;
        direction.y = 0f;
        direction.Normalize();
        StartCoroutine(Knockback(direction, 0.3f, 10f));
    }

    /// <summary>
    /// Error Checking to fail silenetly if the userCollider is Null.
    /// </summary>
    void Start()
    {
        userCollider = GetComponent<Collider>();
        if (userCollider == null)
        {
            Debug.LogError("Collider not attached. This script requires one.");
        }

    }

    /// <summary>
    /// Observes guiHealth constantly and isDead bool
    /// </summary>
    void Update()
    {

        if (guiHealth != null)
        {
            guiHealth.value = healthAmt;
        }

        if (isDead && !deathStarted)
        {
            deathStarted = true;
            StartCoroutine(DeathAnimation());
        }
    }

    /// <summary>
    /// This allows us to utilze deterministic knockback data.
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="duration"></param>
    /// <param name="strength"></param>
    /// <returns></returns>
    private IEnumerator Knockback(Vector3 dir, float duration, float strength)
    {
        if (bounceOffEnemy && enemyMesh != null)
            enemyMesh.enabled = false;

        float timer = 0f;

        while (timer < duration)
        {
            transform.position += dir * strength * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        if (bounceOffEnemy && enemyMesh != null)
            enemyMesh.enabled = true;
    }

    /// <summary>
    /// Make the object un-interactable, play both the hurtSound and deathExplosion.
    /// </summary>
    /// <returns></returns>
    IEnumerator DeathAnimation() 
    {
        if (enemyMesh != null)
        {
            enemyMesh.enabled = false;
        }

        if (hurtSound != null)
        {
            Debug.LogWarning("Play Sound");
            audioSource.PlayOneShot(hurtSound);
        }

        if (deathExplosion != null)
            {
                deathExplosion.Play();
                yield return new WaitForSeconds(deathExplosion.main.duration);
            }



        if (isPooled)
        {
            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }
        else
        {
                Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// This gives the object invincibiliy frames after getting hit.
    /// </summary>
    /// <returns></returns>
    private IEnumerator HurtCooldown() 
    {
        Debug.LogWarning("CoolingDown");
        Color baseColor = thisMaterial.color;
        Color baseGreen = baseColor;
        baseGreen.g = 1f;

        isInvincible = true;
        for (int i = 0; i < optionalCooldownFrames; i++) 
        {
            thisMaterial.color = baseGreen;
            yield return new WaitForSeconds(0.15f);
            thisMaterial.color = baseColor;
            yield return new WaitForSeconds(0.15f);
            Debug.LogWarning("Cooling Down");

        }
        thisMaterial.color = baseColor;
        isInvincible = false;
    }

}
