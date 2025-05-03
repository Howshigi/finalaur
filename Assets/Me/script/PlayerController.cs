using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public GameObject focalPoint;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction smashAction;
    private InputAction breakAction;

    private Coroutine runningSmashRoutine = null;
    private bool hasPowerUp = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // Get InputActions from the InputActionAsset
        moveAction = playerInput.actions["Move"];
        smashAction = playerInput.actions["Smash"];
        breakAction = playerInput.actions["Break"];
    }

    void OnEnable()
    {
        moveAction.Enable();
        smashAction.Enable();
        breakAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        smashAction.Disable();
        breakAction.Disable();
    }

    void Update()
    {
        // Read movement input
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float verticalInput = moveInput.y;

        if (focalPoint != null)
        {
            rb.AddForce(focalPoint.transform.forward * verticalInput * speed);
        }

        if (breakAction.IsPressed())
        {
            rb.velocity = Vector3.zero; // ใช้ velocity แทน linearVelocity
        }

        if (smashAction.triggered && hasPowerUp)
        {
            if (runningSmashRoutine == null)
            {
                runningSmashRoutine = StartCoroutine(SmashRoutine());
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PowerUp"))
        {
            Destroy(other.gameObject);
            hasPowerUp = true;
            StartCoroutine(PowerUpCooldownRoutine(60));
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy") && hasPowerUp)
        {
            Vector3 dir = collision.transform.position - transform.position;
            dir.Normalize();
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                enemyRb.AddForce(dir * 10f, ForceMode.Impulse);
            }
        }
    }

    IEnumerator PowerUpCooldownRoutine(float cooldownTime)
    {
        yield return new WaitForSeconds(cooldownTime);
        hasPowerUp = false;
        if (runningSmashRoutine != null)
        {
            StopCoroutine(runningSmashRoutine);
            runningSmashRoutine = null;
        }
    }

    IEnumerator SmashRoutine()
    {
        float chargeTime = 0f;

        while (smashAction.IsPressed())
        {
            chargeTime += Time.deltaTime;
            yield return null;

            if (chargeTime >= 2f)
            {
                Debug.Log("Smash activated!!");
                break;
            }
        }

        if (chargeTime < 2f)
        {
            Debug.Log("Smash canceled");
            yield break;
        }

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                enemyRb.AddExplosionForce(10f, transform.position, 100f, 0f, ForceMode.Impulse);
            }
        }

        runningSmashRoutine = null;
        yield return null;
    }
}
