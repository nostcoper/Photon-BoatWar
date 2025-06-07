using UnityEngine;
using System.Collections.Generic;
public class PowerUp : MonoBehaviour
{
    public PowerUpBase[] powerUps;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("PowerUp detectado");
        
        BoatController boatController = other.GetComponent<BoatController>();
        if (boatController == null)
        {
            Debug.Log("No se encontró BoatController en el objeto colisionado");
            return;
        }

        if (powerUps == null || powerUps.Length == 0)
        {
            Debug.LogWarning("No hay powerups configurados");
            return;
        }

        Dictionary<float, List<PowerUpBase>> powerUpsByProbability = new Dictionary<float, List<PowerUpBase>>();
        
        foreach (var powerUp in powerUps)
        {
            if (!powerUpsByProbability.ContainsKey(powerUp.probability))
            {
                powerUpsByProbability[powerUp.probability] = new List<PowerUpBase>();
            }
            powerUpsByProbability[powerUp.probability].Add(powerUp);
        }

        // Calcular probabilidad total
        float totalProbability = 0f;
        foreach (var probGroup in powerUpsByProbability)
        {
            // Solo contamos una vez cada valor de probabilidad única
            totalProbability += probGroup.Key;
        }

        // Generar valor aleatorio
        float randomValue = Random.Range(0f, totalProbability);
        Debug.Log($"Total Probability: {totalProbability}");
        Debug.Log($"Random Value: {randomValue}");

        // Ordenar las probabilidades de mayor a menor para consistencia
        List<float> orderedProbabilities = new List<float>(powerUpsByProbability.Keys);
        orderedProbabilities.Sort((a, b) => b.CompareTo(a)); // Orden descendente

        // Seleccionar powerUp según probabilidad
        float cumulativeProbability = 0f;
        PowerUpBase selectedPowerUp = null;

        foreach (float probability in orderedProbabilities)
        {
            cumulativeProbability += probability;
            
            if (randomValue <= cumulativeProbability)
            {
                // Si hay múltiples powerUps con esta probabilidad, elegir uno al azar
                List<PowerUpBase> candidates = powerUpsByProbability[probability];
                int randomIndex = Random.Range(0, candidates.Count);
                selectedPowerUp = candidates[randomIndex];
                break;
            }
        }

        if (selectedPowerUp != null)
        {
            Debug.Log($"PowerUp seleccionado: {selectedPowerUp.name} (Probabilidad: {selectedPowerUp.probability})");
            boatController.CurrentPowerUp = selectedPowerUp;
        }
        else
        {
            Debug.LogWarning("No se pudo seleccionar ningún PowerUp");
        }

    }
}