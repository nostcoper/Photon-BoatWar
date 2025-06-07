using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    public float turnSpeed;
    public float accelerateSpeed;
    private Rigidbody rb;
    private Vector2 moveDirection;
    private bool powerUpInput;
    public PowerUpBase CurrentPowerUp;
    public int points;
    public bool winner = false;


    public GameObject flagMesh;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 0.5f;
        rb.maxAngularVelocity = 2f;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<Vector2>();
    }

    void Update(){
    }

    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (context.performed && CurrentPowerUp!= null)
        { 
            CurrentPowerUp.Activate(gameObject);
            if (CurrentPowerUp.IsFinished()){
                CurrentPowerUp = null;
            }
        }
    }

    void FixedUpdate()
    {
        rb.AddTorque(0f, moveDirection.x * turnSpeed * Time.deltaTime, 0f);

        if (Mathf.Abs(moveDirection.y) > 0.01f)
        {
            rb.AddForce(transform.forward * moveDirection.y * accelerateSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0;

            Vector3 brakeForce = -horizontalVelocity * 0.1f;
            rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    public void addPoint(){
        points++;
    }
}