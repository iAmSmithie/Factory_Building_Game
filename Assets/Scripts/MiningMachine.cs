using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class MiningMachine : MonoBehaviour, IFilteredOutputMachine
{
    public float miningSpeed = 1f;
    public int maxStorage = 50;
    public float placementHeightOffset = 0.5f;

    [SerializeField] private Materials _currentOreType;
    [SerializeField] private int _currentYield;
    private Dictionary<Materials, int> _inventory = new();
    private float _miningProgress;
    private MineralVein _targetVein;

    [Header("UI Settings")]
    public GameObject uiPanel;
    public Transform inventoryContainer;
    public GameObject slotTemplatePrefab;
    public Dictionary<Materials, Sprite> itemIcons = new();

    private bool isPlayerNearby = false;
    private float lastTriggerTime;

    // initialisation of the ui and starts update inventory UI and loads icons
    public void Initialize()
    {
        if (uiPanel != null)
        {
            return;
        }

        GameObject uiPrefab = Resources.Load<GameObject>("UI/MiningUI/MiningMachineUI");
        if (uiPrefab == null)
        {
            Debug.LogError("UI prefab missing from Resources/UI/!");
            return;
        }

        if (slotTemplatePrefab == null)
        {
            Debug.LogError("SlotTemplatePrefab is not assigned in Inspector!");
            slotTemplatePrefab = Resources.Load<GameObject>("UI/MiningUI/SlotTemplate");
            if (slotTemplatePrefab == null)
            {
                Debug.LogError("SlotTemplate not found in Resources/UI/!");
            }
        }

        Canvas targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        uiPanel = Instantiate(uiPrefab, targetCanvas.transform);

        inventoryContainer = uiPanel.transform.Find("inventorySlot");
        if (inventoryContainer == null)
        {
            Debug.LogError("inventorySlot not found in UI hierarchy!");
        }

        uiPanel.SetActive(false);
        _UpdateInventoryUI();
        LoadIcons();
    }
    //check for the E key to be pressed and if the player is nearby, toggle the UI. also mines runs MineResrource() to mine the resource
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            //Debug.Log($"[Miner] E key pressed. Player is nearby: {isPlayerNearby}");
        }

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleUI();
        }

        if (_targetVein == null)
        {
            FindNearestVein();
            return;
        }

        _miningProgress += miningSpeed * Time.deltaTime;

        if (_miningProgress >= 1f)
        {
            MineResource();
            _miningProgress = 0f;
        }
    }
    //finds the nearest vein and sets the target vein to it, uses the FindNearestVein() function
    void MineResource()
    {
        if (_targetVein == null || _targetVein.currentYield <= 0)
        {
            FindNearestVein();
            return;
        }

        if (GetTotalStored() >= maxStorage)
        {
            return;
        }

        int mined = _targetVein.Mine(1);
        if (mined <= 0)
        {
            return;
        }

        Materials justGot = _targetVein.oreType;
        _currentOreType = justGot;

        if (_inventory.ContainsKey(justGot))
        {
            _inventory[justGot] += mined;
        }
        else
        {
            _inventory.Add(justGot, mined);
        }

        _UpdateInventoryUI();
    }
    //loads the icons for the items in the inventory
    void LoadIcons()
    {
        Materials[] materials = Resources.LoadAll<Materials>("Recipes and Items/Minerals");

        if (materials.Length == 0)
        {
        }

        foreach (Materials material in materials)
        {
            if (material.itemIcon != null)
            {
                itemIcons[material] = material.itemIcon;
            }
        }
    }
    //updates the inventory UI, destroys the old slots and creates new ones for each item in the inventory
    void _UpdateInventoryUI()
    {
        if (inventoryContainer == null)
        {
            return;
        }

        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in _inventory)
        {
            if (slotTemplatePrefab == null)
            {
                continue;
            }

            GameObject slot = Instantiate(slotTemplatePrefab, inventoryContainer, false);
            slot.SetActive(true);
            slot.transform.localScale = Vector3.one;

            Image icon = slot.GetComponentInChildren<Image>();
            TextMeshProUGUI text = slot.GetComponentInChildren<TextMeshProUGUI>();

            if (icon == null || text == null)
            {
                continue;
            }

            if (itemIcons.TryGetValue(item.Key, out Sprite iconSprite))
            {
                icon.sprite = iconSprite;
            }
            else
            {
                icon.enabled = false;
            }

            text.fontSize = 14;
            text.text = $"{item.Key.itemName}: {item.Value}";
        }
    }
    //finds the nrearest vein by getting the components in the area of the player and setting the target vein to it
    void FindNearestVein()
    {
        Vector3 groundPosition = transform.position;
        var nearbyVeins = MineralSpawner.Instance.GetVeinComponentsInArea(groundPosition, 2.0f);

        if (nearbyVeins.Count == 0)
        {
            return;
        }

        _targetVein = nearbyVeins[0];
        _currentOreType = _targetVein.oreType;
    }
    //returns the total amount of items stored in the inventory
    int GetTotalStored()
    {
        return _inventory.Values.Sum();
    }
    //toggles the UI on and off, if the UI is active it updates the inventory UI
    void ToggleUI()
    {
        if (uiPanel == null)
        {
            return;
        }

        uiPanel.SetActive(!uiPanel.activeSelf);

        if (uiPanel.activeSelf)
        {
            _UpdateInventoryUI();
        }
    }
    //checks if the player is nearby, returns isPlayerNearby
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }
    //checks if the player is not nearby, sets isPlayerNearby to false and hides the UI
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (uiPanel != null)
            {
                uiPanel.SetActive(false);
            }
        }
    }

    ///////////// IFilteredOutputMachine Interface ////////////////////////////////////

    public bool HasItem()
    {
        return _inventory.TryGetValue(_currentOreType, out int count) && count > 0;
    }

    public bool CanOutputItem()
    {
        return _inventory.ContainsKey(_currentOreType) && _inventory[_currentOreType] > 0;
    }

    public bool CanOutputItem(Materials specificItem)
    {
        return specificItem != null
            && _inventory.TryGetValue(specificItem, out int count)
            && count > 0;
    }
    //returns true if the item can be outputted, false otherwise
    public bool OutputItem()
    {
        if (CanOutputItem())
        {
            _inventory[_currentOreType]--;

            if (_inventory[_currentOreType] <= 0)
            {
                _inventory.Remove(_currentOreType);
            }

            _UpdateInventoryUI();
            return true;
        }

        return false;
    }

    public bool ReceiveItem(Materials item)
    {
        return false;
    }

    public bool CanReceiveItem(Materials item)
    {
        return false;
    }

    public Materials TakeItem()
    {
        if (!HasItem())
        {
            return null;
        }

        _inventory[_currentOreType]--;

        if (_inventory[_currentOreType] <= 0)
        {
            _inventory.Remove(_currentOreType);
        }

        _UpdateInventoryUI();
        return _currentOreType;
    }
    //takes a specific item from the inventory and returns it, if the item is not in the inventory it returns null
    public Materials TakeItem(Materials specificItem)
    {
        //Debug.Log($"[Miner] Filtered TakeItem({specificItem?.itemName ?? "null"}) called");

        foreach (var kv in _inventory)
        {
            //Debug.Log($"[Miner]   Inventory contains {kv.Key.itemName}: {kv.Value}");
        }

        if (specificItem == null)
        {
            return null;
        }

        if (_inventory.TryGetValue(specificItem, out int count) && count > 0)
        {
            _inventory[specificItem]--;

            if (_inventory[specificItem] <= 0)
            {
                _inventory.Remove(specificItem);
            }

            _UpdateInventoryUI();
            return specificItem;
        }

        return null;
    }
}
