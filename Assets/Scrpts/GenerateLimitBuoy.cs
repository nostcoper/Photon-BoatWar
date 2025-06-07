using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;

public class GenerateLimitBoy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public SplineContainer spline;
    public int numBuoys;
    public Vector3 BuoyScale;

    public GameObject BuoyPrefab;

    void Start()
    {
        GenerateBuoys();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerateBuoys()
    {

        for (int i = 0; i < numBuoys; i++)
        {
            float t = i / (float)(numBuoys - 1);

            Vector3 position = spline.EvaluatePosition(t);
            position.y = 1.23f;
        

            GameObject BuoyObject = Instantiate(BuoyPrefab, position, Quaternion.Euler(-90f,0f,0f), transform );
            Debug.Log(BuoyObject.transform.localScale);
            BuoyObject.name = $"Bouy{i + 1}";
 

        }
    }
}
