using UnityEngine;
using System.Collections.Generic;

public class PlanetSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform ship;
    public GameObject[] planetPrefabs;

    [Header("Spawn Timing")]
    public float spawnIntervalDistance = 1500f;

    [Header("Placement Rules")]
    public float minSpawnRadius = 2000f;   // Closest a planet can spawn
    public float maxSpawnRadius = 7000f;   // Furthest a planet can spawn
    public float minBufferDistance = 3000f; // Minimum distance BETWEEN two planets
    public int maxSpawnAttempts = 10;      // How many times to try finding a spot

    [Header("Optimization")]
    public float activationDistance = 10000f;
    public float cleanupDistance = 20000f; // Destroy planets left far behind

    private Vector3 lastSpawnPosition;
    private List<GameObject> activePlanets = new List<GameObject>();

    void Start() => lastSpawnPosition = ship.position;

    void Update()
    {
        if (Vector3.Distance(ship.position, lastSpawnPosition) >= spawnIntervalDistance)
        {
            TrySpawnPlanet();
            lastSpawnPosition = ship.position;
        }

        OptimizeAndCleanup();
    }

    void TrySpawnPlanet()
    {
        Vector3 potentialPos = Vector3.zero;
        bool validPointFound = false;

        // Try several times to find a spot that isn't near another planet
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Create a random point in a large sphere around the ship
            Vector3 randomDir = Random.onUnitSphere;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);

            // Bias the spawn towards the direction the ship is actually flying
            Vector3 spawnOffset = Vector3.Lerp(randomDir, ship.forward, 0.5f) * randomDist;
            potentialPos = ship.position + spawnOffset;

            if (IsPositionValid(potentialPos))
            {
                validPointFound = true;
                break;
            }
        }

        if (validPointFound)
        {
            SpawnPlanet(potentialPos);
        }
    }

    bool IsPositionValid(Vector3 pos)
    {
        foreach (GameObject p in activePlanets)
        {
            if (p == null) continue;
            // Check if this new spot is too close to an existing planet
            if (Vector3.Distance(pos, p.transform.position) < minBufferDistance)
                return false;
        }
        return true;
    }

    void SpawnPlanet(Vector3 pos)
    {
        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        GameObject newPlanet = Instantiate(prefab, pos, Random.rotation);

        // Randomize scale to make the same models look like different planets
        float scale = Random.Range(50f, 150f);
        newPlanet.transform.localScale = new Vector3(scale, scale, scale);

        activePlanets.Add(newPlanet);
    }

    void OptimizeAndCleanup()
    {
        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            GameObject planet = activePlanets[i];
            if (planet == null) continue;

            float dist = Vector3.Distance(ship.position, planet.transform.position);

            // 1. View Distance Optimization
            planet.SetActive(dist < activationDistance);

            // 2. Memory Cleanup (Remove if left way behind)
            if (dist > cleanupDistance)
            {
                activePlanets.RemoveAt(i);
                Destroy(planet);
            }
        }
    }
}