using System.Collections.Generic;
using UnityEngine;

public class HexPoints : MonoBehaviour
{
    public static HexPoints Instance { get; private set; }
    public Mesh sphereMesh;
    public Material sphereMaterial;
    public GameObject hexFillPrefab;
    public Transform player;
    public float hexSpawnRadius = 5f;

    private List<Vector3> hexPositions = new List<Vector3>();
    private Dictionary<Vector3, GameObject> activeHexFills = new Dictionary<Vector3, GameObject>();
    private bool isHexFillActive = false;

    //calculates the hex positions based on the mesh and spawns a sphere at each position
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isHexFillActive = !isHexFillActive;
            if (!isHexFillActive)
            {
                ClearHexFills();
            }
        }

        if (isHexFillActive)
        {
            UpdateHexFills();
        }
    }

    public void Initialize()
    {
        //Extract hex positions from the mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("HexPoints: No mesh found on object.");
            return;
        }

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        HashSet<Vector3> uniqueCenters = new HashSet<Vector3>();

        for (int i = 0; i < triangles.Length; i += 12)
        {
            HashSet<Vector3> hexVertices = new HashSet<Vector3>();
            for (int j = i; j < i + 12; j++)
            {
                hexVertices.Add(vertices[triangles[j]]);
            }

            Vector3 center = Vector3.zero;
            foreach (Vector3 v in hexVertices) center += v;
            center /= hexVertices.Count;
            center = transform.TransformPoint(center);
            center = new Vector3(
                Mathf.Round(center.x * 1000f) / 1000f,
                Mathf.Round(center.y * 1000f) / 1000f,
                Mathf.Round(center.z * 1000f) / 1000f
            );

            if (uniqueCenters.Add(center))
            {
                hexPositions.Add(center);
            }
        }

        Debug.Log($"Hex Centers Extracted: {hexPositions.Count}");
    }

    public List<Vector3> GetHexPositions()
    {
        return new List<Vector3>(hexPositions);
    }

    private void UpdateHexFills()
    {
        //update the hex points shown based on the players location
        if (player == null) return;

        Vector3 playerPosition = player.position;

        foreach (Vector3 hexPos in hexPositions)
        {
            if (!activeHexFills.ContainsKey(hexPos) && Vector3.Distance(playerPosition, hexPos) <= hexSpawnRadius)
            {
                GameObject hexObject = Instantiate(hexFillPrefab, hexPos, Quaternion.identity);
                activeHexFills[hexPos] = hexObject;
            }
        }

        List<Vector3> toRemove = new List<Vector3>();
        foreach (var hex in activeHexFills)
        {
            if (Vector3.Distance(playerPosition, hex.Key) > hexSpawnRadius)
            {
                Destroy(hex.Value);
                toRemove.Add(hex.Key);
            }
        }

        foreach (Vector3 key in toRemove)
        {
            activeHexFills.Remove(key);
        }
    }

    private void ClearHexFills()
    {
        foreach (var hex in activeHexFills.Values)
        {
            Destroy(hex);
        }
        activeHexFills.Clear();
    }

}
