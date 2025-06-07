using UnityEngine;
using UnityEngine.Rendering;

public class LootBoxSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject LootBoxPrefab;
    public int numBoxes = 5;

    public Vector3 mapCenter = Vector3.zero;
    public float radioSpawn = 50f;
    void Start()
    {
        for(int i = 0; i<numBoxes; i++){
            SpawnBox();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnBox(){
        
        Vector3 randomPosition = mapCenter + Random.insideUnitSphere * radioSpawn;
        randomPosition.y = 0f;

        Instantiate(LootBoxPrefab, randomPosition, Quaternion.identity);

    }
}
