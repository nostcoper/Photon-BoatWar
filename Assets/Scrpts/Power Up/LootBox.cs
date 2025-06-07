using UnityEngine;

public class LootBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Collider BC; //MeshCollider
    private Renderer MR; //MeshRenderer
    
    public float reaparitionTimer  = 5f;
    public bool isTouch = false;

    void Start()
    {
        BC = GetComponent<Collider>();
        MR = GetComponent<Renderer>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other){
        
        if(other.CompareTag("Player")){
            isTouch = true;
            BC.enabled = false;
            MR.enabled = false;

            Invoke("Respawn", reaparitionTimer);
            Debug.Log("Toque la caja");
        }
    }

    void Respawn(){
        BC.enabled = true;
        MR.enabled =true;
        Debug.Log("Caja reaparecio");
    }
}
