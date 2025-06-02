using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MineralSpawner : MonoBehaviour
{
    public static MineralSpawner Instance { get; private set; }

    [System.Serializable]
    public class MineralSpawnSettings
    {
        //normal veins
        public Materials oreType;
        [Range(0f, 1f)] public float spawnChance = 0.5f;
        public Vector2Int yieldRange = new Vector2Int(10, 20);
        public Color oreColor = Color.white;

        //special veins
        public bool isRandomSpecial = false;
        public List<Materials> randomOreList = new List<Materials>();
        public float randomTickInterval = 3f;
        public Vector2Int randomAmountRange = new Vector2Int(1, 5);
    }

    public class MineralVeinData
    {
        //mineral vein data
        public Materials oreType;
        public Vector3 position;
        public int remainingYield;
        public GameObject veinObject;
    }

    public GameObject mineralVeinPrefab;
    public Transform spawnParent;
    public List<MineralSpawnSettings> spawnSettings = new();
    public int totalVeinsToSpawn = 50;
    public float minDistanceBetweenVeins = 2f;

    private List<MineralVeinData> allVeinData = new();
    private List<GameObject> spawnedVeins = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Start()
    {
        //wait for hex points
        yield return StartCoroutine(WaitForHexPointsCoroutine());
        GenerateMineralVeins();
    }

    IEnumerator WaitForHexPointsCoroutine()
    {
        //wait for HexPoints to be initialized
        while (HexPoints.Instance == null || HexPoints.Instance.GetHexPositions().Count == 0)
        {
            yield return null;
        }
    }

    public void GenerateMineralVeins()
    {
        //generates the mineral veins, sets their rotation to be flat and ensures theyre on the hex points
        ClearExistingVeins();
        List<Vector3> spawnPoints = HexPoints.Instance.GetHexPositions();

        for (int i = 0; i < totalVeinsToSpawn && spawnPoints.Count > 0; i++)
        {
            Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
            spawnPoints.Remove(spawnPos);

            MineralSpawnSettings settings = GetRandomOreType();
            if (settings == null)
            {
                continue;
            }

            Vector3 normal = spawnPos.normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, normal).normalized;
            if (forward == Vector3.zero)
            {
                forward = Vector3.Cross(normal, Vector3.right).normalized;
            }

            Quaternion rotation = Quaternion.LookRotation(forward, normal) * Quaternion.Euler(-90, 0, 0);

            GameObject newVein = Instantiate(mineralVeinPrefab, spawnPos, rotation, spawnParent);
            MineralVein vein = newVein.GetComponent<MineralVein>();
            if (vein == null)
            {
                Debug.LogError("Prefab missing MineralVein script!", newVein);
                continue;
            }

            int initialYield = 0;
            if (settings.isRandomSpecial)
            {
                vein.InitializeRandom(
                    settings.randomOreList,
                    settings.randomTickInterval,
                    settings.randomAmountRange
                );
            }
            else
            {
                initialYield = Random.Range(settings.yieldRange.x, settings.yieldRange.y + 1);
                vein.Initialize(settings.oreType, initialYield);
            }

            MineralVeinData data = new MineralVeinData()
            {
                oreType = settings.oreType,
                position = spawnPos,
                remainingYield = initialYield,
                veinObject = newVein
            };
            allVeinData.Add(data);
            spawnedVeins.Add(newVein);
        }
    }

    public List<MineralVeinData> GetVeinsInArea(Vector3 position, float radius)
    {
        //returns a list of all veins data within a certain radius of the given position
        List<MineralVeinData> veinsInRange = new();
        foreach (var vein in allVeinData)
        {
            if (Vector3.Distance(position, vein.position) <= radius)
            {
                veinsInRange.Add(vein);
            }
        }
        return veinsInRange;
    }

    public void UpdateVeinYield(GameObject veinObject, int newYield)
    {
        //updates the yield of a vein, if the yield is 0 or less, remove it from the list and destroy the object
        MineralVeinData data = allVeinData.Find(d => d.veinObject == veinObject);
        if (data != null)
        {
            data.remainingYield = newYield;
            if (newYield <= 0)
            {
                allVeinData.Remove(data);
                spawnedVeins.Remove(veinObject);
                Destroy(veinObject);
            }
        }
    }

    public MineralSpawnSettings GetOreData(Materials oreType)
    {
        return spawnSettings.Find(s => s.oreType == oreType);
    }

    MineralSpawnSettings GetRandomOreType()
    {
        //gets a random ore type based on the spawn settings
        float totalWeight = spawnSettings.Sum(s => s.spawnChance);
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var setting in spawnSettings)
        {
            cumulativeWeight += setting.spawnChance;
            if (randomValue <= cumulativeWeight)
            {
                return setting;
            }
        }
        return null;
    }

    public void ClearExistingVeins()
    {
        //clear exsiting vein function
        foreach (var vein in spawnedVeins)
        {
            if (vein != null)
            {
                Destroy(vein);
            }
        }
        spawnedVeins.Clear();
        allVeinData.Clear();
    }

    public List<MineralVein> GetVeinComponentsInArea(Vector3 position, float radius)
    {
        //returns a list of all vein components within a certain radius of the given position
        List<MineralVein> veinsInRange = new();
        foreach (var veinObject in spawnedVeins)
        {
            if (veinObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(position, veinObject.transform.position);
            if (distance <= radius)
            {
                MineralVein veinComponent = veinObject.GetComponent<MineralVein>();
                if (veinComponent != null)
                {
                    veinsInRange.Add(veinComponent);
                }
            }
        }
        return veinsInRange;
    }
}
