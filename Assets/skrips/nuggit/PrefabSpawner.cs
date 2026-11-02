using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawnerTerrain : MonoBehaviour
{
    public enum AreaShape
    {
        Rechteck,
        Kreis
    }

    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Prefabs")]
    public List<SpawnEntry> prefabs = new List<SpawnEntry>();

    [Min(0)]
    public int amount = 20;

    public bool spawnOnStart = true;

    [Header("Dauerhaftes Spawnen")]
    public bool continuousSpawn = true;

    [Min(0.01f)]
    public float spawnInterval = 2f;

    [Header("Terrain & Bereich")]
    public Terrain terrain;

    public bool useWholeTerrain = true;

    public AreaShape areaShape = AreaShape.Rechteck;

    public Vector2 areaSize = new Vector2(50f, 50f);

    public Transform centerPoint;

    public Vector2 centerOffset = Vector2.zero;

    [Header("Filter: Höhe")]
    public bool useHeightFilter = false;

    public bool autoDetectBase = true;

    public float manualBaseHeight = 0f;

    public float minAboveBase = 0.5f;

    public float maxAboveBase = 10000f;

    [Header("Filter: Neigung")]
    public bool useSteepnessFilter = false;

    [Range(0f, 90f)]
    public float minSteepness = 0f;

    [Range(0f, 90f)]
    public float maxSteepness = 90f;

    [Header("Filter: Terrain-Textur")]
    public bool useTextureFilter = false;

    public int textureLayerIndex = 0;

    [Range(0f, 1f)]
    public float minTextureWeight = 0.5f;

    [Header("Platzierung")]
    public float heightAboveGround = 1.26f;

    public float heightRandom = 0f;

    [Range(0f, 1f)]
    public float alignToSurface = 0f;

    [Header("Rotation")]
    public Vector3 baseRotation = new Vector3(90f, 0f, 0f);

    public Vector3 randomRotation = Vector3.zero;

    [Header("Skalierung")]
    public bool randomScale = false;

    public float minScale = 0.8f;

    public float maxScale = 1.2f;

    [Header("Abstand")]
    public float minDistanceBetween = 0f;

    [Min(1)]
    public int maxTriesPerSpawn = 1000;

    [Header("Zufall")]
    public bool useSeed = false;

    public int seed = 12345;

    private readonly List<Vector3> spawned = new List<Vector3>();

    private float baseHeight;

    private string ContainerName
    {
        get
        {
            return "Spawned (" + name + ")";
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }

        if (continuousSpawn)
        {
            InvokeRepeating(
                nameof(SpawnOneContinuous),
                spawnInterval,
                spawnInterval
            );
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(SpawnOneContinuous));
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        if (!HasValidPrefab())
        {
            Debug.LogError(
                "PrefabSpawnerTerrain: KEIN PREFAB EINGETRAGEN!"
            );
            return;
        }

        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
        {
            Debug.LogError(
                "PrefabSpawnerTerrain: KEIN TERRAIN GEFUNDEN!"
            );
            return;
        }

        ClearSpawned();

        spawned.Clear();

        if (autoDetectBase)
        {
            baseHeight = DetectBaseHeight(terrain.terrainData);
        }
        else
        {
            baseHeight = manualBaseHeight;
        }

        Random.State oldState = Random.state;

        if (useSeed)
        {
            Random.InitState(seed);
        }

        int success = 0;

        for (int i = 0; i < amount; i++)
        {
            if (TrySpawnOne())
            {
                success++;
            }
        }

        if (useSeed)
        {
            Random.state = oldState;
        }

        Debug.Log(
            "PrefabSpawnerTerrain: " +
            success +
            " von " +
            amount +
            " Prefabs gespawnt."
        );
    }

    private void SpawnOneContinuous()
    {
        if (!HasValidPrefab())
            return;

        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
            return;

        TrySpawnOne();
    }

    private bool TrySpawnOne()
    {
        TerrainData td = terrain.terrainData;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = td.size;

        Transform container = GetOrCreateContainer();

        for (int attempt = 0; attempt < maxTriesPerSpawn; attempt++)
        {
            float x;
            float z;

            Vector3 center = GetCenter();

            if (useWholeTerrain)
            {
                x = Random.Range(
                    terrainPosition.x,
                    terrainPosition.x + terrainSize.x
                );

                z = Random.Range(
                    terrainPosition.z,
                    terrainPosition.z + terrainSize.z
                );
            }
            else
            {
                if (areaShape == AreaShape.Kreis)
                {
                    Vector2 random =
                        Random.insideUnitCircle *
                        (areaSize.x * 0.5f);

                    x = center.x + random.x;
                    z = center.z + random.y;
                }
                else
                {
                    x =
                        center.x +
                        Random.Range(
                            -areaSize.x * 0.5f,
                            areaSize.x * 0.5f
                        );

                    z =
                        center.z +
                        Random.Range(
                            -areaSize.y * 0.5f,
                            areaSize.y * 0.5f
                        );
                }
            }

            // Prüfen, ob Position innerhalb des Terrains liegt
            if (
                x < terrainPosition.x ||
                x > terrainPosition.x + terrainSize.x ||
                z < terrainPosition.z ||
                z > terrainPosition.z + terrainSize.z
            )
            {
                continue;
            }

            float nx =
                Mathf.InverseLerp(
                    terrainPosition.x,
                    terrainPosition.x + terrainSize.x,
                    x
                );

            float nz =
                Mathf.InverseLerp(
                    terrainPosition.z,
                    terrainPosition.z + terrainSize.z,
                    z
                );

            // WICHTIG:
            // Terrain.SampleHeight gibt die lokale Terrainhöhe zurück.
            float terrainHeight =
                terrain.SampleHeight(
                    new Vector3(x, 0f, z)
                );

            float worldY =
                terrainHeight +
                terrainPosition.y +
                heightAboveGround;

            if (heightRandom != 0f)
            {
                worldY +=
                    Random.Range(
                        -heightRandom,
                        heightRandom
                    );
            }

            if (useHeightFilter)
            {
                float aboveBase =
                    terrainHeight - baseHeight;

                if (
                    aboveBase < minAboveBase ||
                    aboveBase > maxAboveBase
                )
                {
                    continue;
                }
            }

            if (useSteepnessFilter)
            {
                float steepness =
                    td.GetSteepness(nx, nz);

                if (
                    steepness < minSteepness ||
                    steepness > maxSteepness
                )
                {
                    continue;
                }
            }

            if (useTextureFilter)
            {
                if (!PassesTextureFilter(td, nx, nz))
                {
                    continue;
                }
            }

            Vector3 spawnPosition =
                new Vector3(
                    x,
                    worldY,
                    z
                );

            if (
                minDistanceBetween > 0f &&
                TooClose(spawnPosition)
            )
            {
                continue;
            }

            GameObject prefab = PickPrefab();

            if (prefab == null)
            {
                Debug.LogError(
                    "PrefabSpawnerTerrain: PickPrefab() hat kein Prefab gefunden!"
                );

                return false;
            }

            Quaternion rotation =
                Quaternion.Euler(
                    baseRotation
                );

            if (alignToSurface > 0f)
            {
                Vector3 normal =
                    td.GetInterpolatedNormal(
                        nx,
                        nz
                    );

                Quaternion surfaceRotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        normal
                    );

                rotation =
                    Quaternion.Slerp(
                        Quaternion.identity,
                        surfaceRotation,
                        alignToSurface
                    ) *
                    rotation;
            }

            Vector3 randomRot =
                new Vector3(
                    Random.Range(
                        -randomRotation.x,
                        randomRotation.x
                    ),
                    Random.Range(
                        -randomRotation.y,
                        randomRotation.y
                    ),
                    Random.Range(
                        -randomRotation.z,
                        randomRotation.z
                    )
                );

            rotation =
                rotation *
                Quaternion.Euler(randomRot);

            // HIER wird das echte Prefab erzeugt
            GameObject spawnedObject =
                Instantiate(
                    prefab,
                    spawnPosition,
                    rotation,
                    container
                );

            if (spawnedObject == null)
            {
                Debug.LogError(
                    "PrefabSpawnerTerrain: Instantiate fehlgeschlagen!"
                );

                return false;
            }

            if (randomScale)
            {
                float scale =
                    Random.Range(
                        minScale,
                        maxScale
                    );

                spawnedObject.transform.localScale *= scale;
            }

            spawned.Add(spawnPosition);

            return true;
        }

        return false;
    }

    private Transform GetOrCreateContainer()
    {
        GameObject container =
            GameObject.Find(ContainerName);

        if (container == null)
        {
            container =
                new GameObject(ContainerName);
        }

        return container.transform;
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        GameObject container =
            GameObject.Find(ContainerName);

        if (container != null)
        {
            if (Application.isPlaying)
            {
                Destroy(container);
            }
            else
            {
                DestroyImmediate(container);
            }
        }

        spawned.Clear();
    }

    private Vector3 GetCenter()
    {
        Vector3 center =
            centerPoint != null
                ? centerPoint.position
                : transform.position;

        center +=
            new Vector3(
                centerOffset.x,
                0f,
                centerOffset.y
            );

        return center;
    }

    private bool HasValidPrefab()
    {
        foreach (SpawnEntry entry in prefabs)
        {
            if (
                entry != null &&
                entry.prefab != null &&
                entry.weight > 0f
            )
            {
                return true;
            }
        }

        return false;
    }

    private GameObject PickPrefab()
    {
        float totalWeight = 0f;

        foreach (SpawnEntry entry in prefabs)
        {
            if (
                entry != null &&
                entry.prefab != null &&
                entry.weight > 0f
            )
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
            return null;

        float random =
            Random.Range(
                0f,
                totalWeight
            );

        foreach (SpawnEntry entry in prefabs)
        {
            if (
                entry == null ||
                entry.prefab == null ||
                entry.weight <= 0f
            )
            {
                continue;
            }

            random -= entry.weight;

            if (random <= 0f)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    private bool TooClose(Vector3 position)
    {
        float distance =
            minDistanceBetween *
            minDistanceBetween;

        foreach (Vector3 oldPosition in spawned)
        {
            if (
                (oldPosition - position).sqrMagnitude <
                distance
            )
            {
                return true;
            }
        }

        return false;
    }

    private bool PassesTextureFilter(
        TerrainData td,
        float nx,
        float nz
    )
    {
        if (
            textureLayerIndex < 0 ||
            textureLayerIndex >= td.alphamapLayers
        )
        {
            return true;
        }

        int x =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    nx * td.alphamapWidth
                ),
                0,
                td.alphamapWidth - 1
            );

        int z =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    nz * td.alphamapHeight
                ),
                0,
                td.alphamapHeight - 1
            );

        float[,,] alpha =
            td.GetAlphamaps(
                x,
                z,
                1,
                1
            );

        return
            alpha[0, 0, textureLayerIndex] >=
            minTextureWeight;
    }

    private float DetectBaseHeight(TerrainData td)
    {
        int resolution =
            td.heightmapResolution;

        float[,] heights =
            td.GetHeights(
                0,
                0,
                resolution,
                resolution
            );

        float lowest = 1f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (heights[z, x] < lowest)
                {
                    lowest = heights[z, x];
                }
            }
        }

        return lowest * td.size.y;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (useWholeTerrain)
        {
            Terrain t =
                terrain != null
                    ? terrain
                    : Terrain.activeTerrain;

            if (t == null)
                return;

            Vector3 size =
                t.terrainData.size;

            Gizmos.DrawWireCube(
                t.transform.position +
                new Vector3(
                    size.x * 0.5f,
                    0f,
                    size.z * 0.5f
                ),
                new Vector3(
                    size.x,
                    0.1f,
                    size.z
                )
            );
        }
        else
        {
            Vector3 center =
                GetCenter();

            if (areaShape == AreaShape.Kreis)
            {
                float radius =
                    areaSize.x * 0.5f;

                const int steps = 48;

                Vector3 previous =
                    center +
                    new Vector3(
                        radius,
                        0f,
                        0f
                    );

                for (int i = 1; i <= steps; i++)
                {
                    float angle =
                        i *
                        Mathf.PI *
                        2f /
                        steps;

                    Vector3 next =
                        center +
                        new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius
                        );

                    Gizmos.DrawLine(
                        previous,
                        next
                    );

                    previous = next;
                }
            }
            else
            {
                Gizmos.DrawWireCube(
                    center,
                    new Vector3(
                        areaSize.x,
                        0.1f,
                        areaSize.y
                    )
                );
            }

            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                center,
                0.5f
            );
        }
    }
}