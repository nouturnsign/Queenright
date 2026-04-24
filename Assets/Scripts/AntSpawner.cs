using UnityEngine;

public class AntSpawner : MonoBehaviour
{
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private int swarmSize = 10;
    [SerializeField] private float spawnRadius = 3f;

    void Start()
    {
        if (antPrefab == null)
        {
            Debug.LogError("AntSpawner: No ant prefab assigned! Assign it in the Inspector.");
            return;
        }

        SpawnSwarm();
    }

    private void SpawnSwarm()
    {
        for (int i = 0; i < swarmSize; i++)
        {
            // Pick a random X position around the spawner
            float randomOffsetX = Random.Range(-spawnRadius, spawnRadius);
            
            // Keep the Y and Z the same as the spawner's position
            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomOffsetX, 
                transform.position.y, 
                transform.position.z
            );

            // Instantiate the ant
            Instantiate(antPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
