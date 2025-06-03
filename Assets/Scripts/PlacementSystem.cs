using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    public GameObject[] placeablePrefabs;
    public GameObject placementIndicator;
    public Material validMaterial;
    public Material invalidMaterial;
    public Button[] buildButtons;

    [SerializeField] private GameObject conveyorPrefab;
    private InventoryMachine startMachine;
    private InventoryMachine endMachine;
    private bool conveyorMode = false;

    public float placementHeightOffset = 0.5f;
    private GameObject _selectedPrefab;
    private Vector3 _targetPosition;
    private bool _placementMode;
    private List<GameObject> _placedObjects = new List<GameObject>();
    private Renderer _indicatorRenderer;
    private List<Vector3> _hexPositions = new List<Vector3>();

    public float conveyorScale = 4f;

    private HashSet<Vector3> occupiedMineralVeins = new HashSet<Vector3>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _indicatorRenderer = placementIndicator.GetComponent<Renderer>();
        placementIndicator.SetActive(false);
        SetupBuildButtons();
        StartCoroutine(WaitForHexPositions());
    }

    IEnumerator WaitForHexPositions()
    {
        while (HexPoints.Instance == null || HexPoints.Instance.GetHexPositions().Count == 0)
        {
            yield return null;
        }

        _hexPositions = HexPoints.Instance.GetHexPositions();
        Debug.Log($"Loaded {_hexPositions.Count} hex positions.");
    }
    //Main update, handles placing conveyors and machines, also handles mode switching. done by calling functions
    void Update()
    {
        HandleModeSwitch();
        if (conveyorMode)
        {
            HandleConveyorPlacement();
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                TryPlaceObject();
            }
        }

        if (!_placementMode) return;

        UpdateTargetPosition();
        UpdatePlacementIndicator();

        if (Input.GetKeyDown(KeyCode.X))
            CancelPlacement();
    }
    //Handles switching between conveyor and machine placement modes, toggles the conveyor mode on and off
    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            conveyorMode = !conveyorMode;
            Debug.Log($"Conveyor Mode: {conveyorMode}");
        }
    }
    //Updates the target position based on the mouse position
    void UpdateTargetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            //Get the closest hex position to the hit point
            _targetPosition = GetNearestHexPosition(hit.point);
        }
    }
    //Gets the nearest hex position to the given position, using the hex positions list from HexPoints
    Vector3 GetNearestHexPosition(Vector3 position)
    {
        if (_hexPositions.Count == 0)
        {
            Debug.LogWarning("Hex positions list is empty.");
            return position;
        }
        return _hexPositions.OrderBy(hexPos => Vector3.Distance(position, hexPos)).First();
    }
    //Updates the placement indicator based on the target position, sets the position and rotation of the indicator
    void UpdatePlacementIndicator()
    {
        if (_selectedPrefab == null)
        {
            placementIndicator.SetActive(false);
            return;
        }

        placementIndicator.SetActive(true);
        placementIndicator.transform.position = _targetPosition;

        Vector3 normal = _targetPosition.normalized;
        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, normal).normalized;

        if (forward == Vector3.zero)
        {
            forward = Vector3.Cross(normal, Vector3.right).normalized;
        }

        Quaternion rotation = Quaternion.LookRotation(forward, normal);

        placementIndicator.transform.rotation = rotation;
        _indicatorRenderer.material = IsPositionValid() ? validMaterial : invalidMaterial;
    }
    //Checks if the position is valid for placing an object, checks for colliders in the area and checks if the object is a mining machine or conveyor
    bool IsPositionValid()
    {
        if (_selectedPrefab == null) return false;
        Collider[] colliders = Physics.OverlapSphere(_targetPosition, 0.5f);

        bool isMiningMachine = _selectedPrefab.GetComponent<MiningMachine>() != null;
        bool isConveyor = _selectedPrefab.GetComponent<ConveyorBelt>() != null;

        if (isMiningMachine)
        {
            //If its a mining machine, check if it's on a valid mineral vein
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("MineralVein"))
                {
                    Vector3 veinPosition = collider.transform.position;
                    if (occupiedMineralVeins.Contains(veinPosition))
                    {
                        return false; 
                    }
                    return true;
                }
            }
            return false;
        }
        else if (isConveyor)
        {
            // If it's a conveyor belt, check if its on another conveyor
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("ConveyorBelt"))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return !colliders.Any(collider => collider.CompareTag("PlacedObject"));
        }
    }
    //Tries to place the selected object at the target position, sets the rotation and position of the object, and initializes it if its a mining machine
    void TryPlaceObject()
    {
        if (!IsPositionValid()) return;

        Vector3 normal = _targetPosition.normalized;
        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, normal).normalized;

        if (forward == Vector3.zero)
        {
            forward = Vector3.Cross(normal, Vector3.right).normalized;
        }

        Quaternion rotation = Quaternion.LookRotation(forward, normal);
        rotation *= Quaternion.Euler(-90, 0, 0);

        Vector3 machinePosition = _targetPosition + Vector3.up * placementHeightOffset;
        GameObject newObject = Instantiate(_selectedPrefab, machinePosition, rotation);

        newObject.SetActive(true);
        _placedObjects.Add(newObject);

        MiningMachine miner = newObject.GetComponent<MiningMachine>();
        if (miner != null)
        {
            miner.Initialize();
        }

        if (_selectedPrefab.GetComponent<MiningMachine>() != null)
        {
            Collider[] colliders = Physics.OverlapSphere(machinePosition, 0.5f);
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("MineralVein"))
                {
                    occupiedMineralVeins.Add(collider.transform.position);
                    break;
                }
            }
        }

        newObject.tag = "PlacedObject";
    }
    //Handles the conveyor placement, checks if the clicked object is a machine or a hex point, then places the conveyor
    void HandleConveyorPlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                InventoryMachine clickedMachine = hit.collider.GetComponentInParent<InventoryMachine>();
                Vector3 clickedPosition = GetNearestHexPosition(hit.point);

                if (startMachine == null)
                {
                    if (clickedMachine != null)
                    {
                        startMachine = clickedMachine;
                        Debug.Log("Start machine selected.");
                    }
                    else
                    {
                        GameObject dummyStart = new GameObject("StartPoint");
                        dummyStart.transform.position = clickedPosition;
                        dummyStart.layer = LayerMask.NameToLayer("Conveyor");
                        startMachine = dummyStart.AddComponent<ConveyorPoint>();
                        Debug.Log("Start point set at hex.");
                    }
                }
                else if (endMachine == null)
                {
                    if (clickedMachine != null)
                    {
                        endMachine = clickedMachine;
                        Debug.Log("End machine selected.");
                    }
                    else
                    {
                        GameObject dummyEnd = new GameObject("EndPoint");
                        dummyEnd.transform.position = clickedPosition;
                        endMachine = dummyEnd.AddComponent<ConveyorPoint>();
                        Debug.Log("End point set at hex.");
                    }

                    if (endMachine == startMachine)
                    {
                        Debug.LogWarning("Can't connect a machine to itself.");
                        ResetConveyorSelection();
                        return;
                    }

                    PlaceConveyor(startMachine, endMachine);
                }
            }
        }
    }

    //places conveyors from the start machine to the end machine seelcted
    void PlaceConveyor(InventoryMachine from, InventoryMachine to)
    {
        Vector3 startPos = ((MonoBehaviour)from).transform.position;
        Vector3 endPos = ((MonoBehaviour)to).transform.position;

        List<Vector3> path = pathFinding.FindPath(startPos, endPos, pathFinding.GetAllHexPositions());

        if (path.Count == 0)
        {
            Debug.LogError("No valid path found between machines.");
            ResetConveyorSelection();
            return;
        }

        //Instead of placing scaled segments, build a chain of conveyors
        PlaceConveyorChain(from, to, path);

        Debug.Log("Conveyor placed along the path.");
        ResetConveyorSelection();
        conveyorMode = false; 
    }
    //turns the path into a chain of conveyors, placing them at the correct positions and rotations
    private void PlaceConveyorChain(InventoryMachine fromMachine, InventoryMachine toMachine, List<Vector3> path)
    {
        List<ConveyorBelt> spawnedConveyors = new List<ConveyorBelt>();
        bool placedFirstConveyor = false;

        for (int i = 0; i < path.Count; i++)
        {
            float distToStart = Vector3.Distance(path[i], ((MonoBehaviour)fromMachine).transform.position);
            if (i == 0 && distToStart < 0.01f) continue;

            float distToEnd = Vector3.Distance(path[i], ((MonoBehaviour)toMachine).transform.position);
            if (i == path.Count - 1 && distToEnd < 0.01f) continue;

            Vector3 pos = path[i];
            Vector3 upDir = pos.normalized;
            Vector3 forwardDir = i < path.Count - 1
                ? (path[i + 1] - pos).normalized
                : Vector3.forward;

            GameObject conveyorObj = Instantiate(conveyorPrefab, pos, Quaternion.identity);
            conveyorObj.transform.localScale = Vector3.one * conveyorScale;
            conveyorObj.transform.up = upDir;
            conveyorObj.transform.rotation = Quaternion.LookRotation(forwardDir, upDir) * Quaternion.Euler(-90, 0, 0);

            ConveyorBelt belt = conveyorObj.GetComponent<ConveyorBelt>();
            belt.outputBelts = new List<ConveyorBelt>();
            spawnedConveyors.Add(belt);

            if (!placedFirstConveyor)
            {
                belt.isFilterEnabled = true;
                belt.OnPlaced();
                placedFirstConveyor = true;
            }
            else
            {
                belt.isFilterEnabled = false;
            }
        }

        for (int i = 0; i < spawnedConveyors.Count; i++)
        {
            ConveyorBelt belt = spawnedConveyors[i];

            if (i == 0)
                belt.adjacentInputMachine = fromMachine;
            else
                spawnedConveyors[i - 1].outputBelts.Add(belt);

            if (i == spawnedConveyors.Count - 1)
                belt.adjacentOutputMachine = toMachine;
        }
    }
    //Resets conveyor selection
    void ResetConveyorSelection()
    {
        startMachine = null;
        endMachine = null;
    }
    //Sets up build buttons, adds listeners to them to call SelectPrefab with the correct index
    void SetupBuildButtons()
    {
        for (int i = 0; i < buildButtons.Length; i++)
        {
            int index = i;
            buildButtons[i].onClick.AddListener(() => SelectPrefab(index));
        }
    }
    //Selects the prefab to be placed, sets the selected prefab and enables placement mode
    public void SelectPrefab(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= placeablePrefabs.Length) return;

        _selectedPrefab = placeablePrefabs[prefabIndex];
        _placementMode = true;
        placementIndicator.SetActive(true);
    }
    //Cancels placement mode
    public void CancelPlacement()
    {
        _placementMode = false;
        _selectedPrefab = null;
        placementIndicator.SetActive(false);
    }
}
