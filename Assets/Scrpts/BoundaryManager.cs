using UnityEngine;

public class Boundary : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform centerZone;
    public float bouncyForce = 50f;
    void Start()
    {
       centerZone = GameObject.FindWithTag("Center").transform;
    }

    // Update is called once per frame
    void Update()
    {
       
        
    }

    private void OnTriggerExit(Collider other){
        
        if(other.CompareTag("Limit")){
            Debug.Log("Intento salir");
            
            Rigidbody RB = GetComponent<Rigidbody>();
            if(RB != null && centerZone != null){
                Vector3 directionToCenter = (centerZone.position - transform.position).normalized;

                // Aplicar fuerza como rebote
                RB.AddForce(directionToCenter * bouncyForce, ForceMode.Impulse);
            }
        }

    }
}
