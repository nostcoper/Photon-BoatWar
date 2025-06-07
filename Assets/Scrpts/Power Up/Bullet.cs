using System;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject owner;
    public float forwardSpeed = 15f;
    public int range = 5;
    private bool falling = false;
    private Rigidbody rb;
    private Vector3 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        rb.useGravity = false;
        rb.linearVelocity = transform.forward * forwardSpeed;
    }

    void Update()
    {
        if (!falling && Vector3.Distance(transform.position, startPos) >= range)
        {
            falling = true;
            rb.useGravity = true;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Ocean"))
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Colisión con: " + other.gameObject.name);
        }
    }

     private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        if (other == owner) return;

        if (other.CompareTag("Player"))
        {
            owner.GetComponent<BoatController>().addPoint();
        }
    }
}