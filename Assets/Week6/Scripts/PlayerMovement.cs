using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float stealthSpeed = 1.6f;
    [SerializeField] private float soundRadius = 6.0f;
    [SerializeField] private float rotationSpeed = 12.0f;

    private NavMeshAgent agent;
    private Camera mainCamera;

    public bool IsStealthMode { get; private set; }
    public bool IsMakingSound { get; private set; }
    public float SoundRadius => soundRadius;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = normalSpeed;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        HandleInput();
        HandleMovementSpeed();
        HandleRotation();
        EvaluateSound();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    agent.SetDestination(hit.point);
                }
            }
        }

        IsStealthMode = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private void HandleMovementSpeed()
    {
        agent.speed = IsStealthMode ? stealthSpeed : normalSpeed;
    }

    private void HandleRotation()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void EvaluateSound()
    {
        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        IsMakingSound = isMoving && !IsStealthMode;
    }

    private void OnDrawGizmosSelected()
    {
        if (IsMakingSound)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, soundRadius);
        }
    }
}
