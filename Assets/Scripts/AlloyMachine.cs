using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class AlloyMachine : MonoBehaviour, IFilteredOutputMachine
{
    public float placementHeightOffset = 0.5f;

    [Header("Capacity Settings")]
    public int maxInputStorage = 50;
    public int maxOutputStorage = 50;

    [SerializeField] private List<Recipes> alloyRecipes;
    private Dictionary<Materials, int> _inputInventory = new Dictionary<Materials, int>();
    private Dictionary<Materials, int> _outputInventory = new Dictionary<Materials, int>();
    private Dictionary<Materials, Sprite> itemIcons = new Dictionary<Materials, Sprite>();

    private float alloyProgress = 0f;
    private Recipes activeRecipe;

    [Header("UI Settings")]
    public GameObject uiPanel;
    public Transform inputInventoryContainer;
    public Transform outputInventoryContainer;
    public GameObject slotTemplatePrefab;

    private bool isPlayerNearby = false;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        //Debug.Log("Initializing Alloy Machine…");
        //load ui features such as icons and slot prefabs, then initialize the UI and load the icons.
        if (uiPanel == null)
        {
            GameObject uiPrefab = Resources.Load<GameObject>("UI/AlloyUI/AlloyMachinePanel");
            if (uiPrefab == null)
            {
                Debug.LogError("AlloyMachine UI prefab missing!");
                return;
            }

            if (slotTemplatePrefab == null)
            {
                slotTemplatePrefab = Resources.Load<GameObject>("UI/AlloyMachineUI/SlotTemplate");
                if (slotTemplatePrefab == null)
                    Debug.LogError("SlotTemplate not found in Resources/UI/AlloyMachineUI!");
            }

            Canvas targetCanvas = FindFirstObjectByType<Canvas>();

            if (targetCanvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            uiPanel = Instantiate(uiPrefab, targetCanvas.transform);

            inputInventoryContainer = uiPanel.transform.Find("AInputInventoryContainer");
            outputInventoryContainer = uiPanel.transform.Find("AOutputInventoryContainer");
            if (inputInventoryContainer == null || outputInventoryContainer == null)
            {
                Debug.LogError("Couldn't find input/output inventory containers in UI hierarchy!");
                return;
            }

            uiPanel.SetActive(false);
        }

        LoadIcons();
        _UpdateInventoryUI();
    }

    void Update()
    {
        //check if the player is near the machine and if the UI is not already open, then check for input to toggle the UI.
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleUI();
        }
        //if the active recipe is null, run the FindMatchingRecipe function
        {
            activeRecipe = FindMatchingRecipe();
        }
        //if active recipe is found, check if the progress is complete and process the recipe
        if (activeRecipe != null)
        {
            alloyProgress += Time.deltaTime;
            if (alloyProgress >= activeRecipe.processingTime)
            {
                ProcessRecipe(activeRecipe);
                alloyProgress = 0f;
                activeRecipe = null;
            }
        }
    }

    private Recipes FindMatchingRecipe()
    {
        //check if the input inventory is empty
        foreach (var recipe in alloyRecipes)
        {
            //check if the recipe has exactly 2 inputs
            if (recipe.inputs.Count != 2)
            {
                continue;
            }
            //check if the recipe has exactly 1 output
            if (!recipe.inputs.All(i => _inputInventory.ContainsKey(i.item) && _inputInventory[i.item] >= i.quantity))
            {
                continue;
            }
            //check if the recipe has enough output storage
            int totalOutputs = recipe.outputs.Sum(o => o.quantity);
            //check if the recipe has enough input storage, if not, return null
            if (_outputInventory.Values.Sum() + totalOutputs <= maxOutputStorage)
            {
                return recipe;
            }
        }
        return null;
    }

    private void ProcessRecipe(Recipes recipe)
    {
        //process the recipe by removing the inputs and adding the outputs to the inventory
        foreach (var inputReq in recipe.inputs)
        {
            _inputInventory[inputReq.item] -= inputReq.quantity;
            if (_inputInventory[inputReq.item] <= 0)
            {
                _inputInventory.Remove(inputReq.item);
            }
        }

        foreach (var outputReq in recipe.outputs)
        {
            if (_outputInventory.ContainsKey(outputReq.item))
            {
                _outputInventory[outputReq.item] += outputReq.quantity;
            }
            else
            {
                _outputInventory.Add(outputReq.item, outputReq.quantity);
            }
        }

        _UpdateInventoryUI();
    }

    private void LoadIcons()
    {
        //clear the icon dictionary before loading new icons
        itemIcons.Clear();
        //lead all the icons from the folders and store them
        string[] folders = new string[] { "Recipes and Items/Minerals", "Recipes and Items/MetalBars", "Recipes and Items/Alloys" };
        foreach (var folder in folders)
        {
            //load all the materials
            Materials[] materials = Resources.LoadAll<Materials>(folder);
            foreach (var material in materials)
            {
                if (material.itemIcon != null)
                {
                    //add the material and its icon to the dictionary
                    itemIcons[material] = material.itemIcon;
                }
            }
        }
    }

    private void _UpdateInventoryUI()
    {
        //clear the inventory containers before building new slots
        foreach (Transform child in inputInventoryContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in outputInventoryContainer)
        {
            Destroy(child.gameObject);
        }

        //build the input and output slots
        void BuildSlots(Dictionary<Materials, int> inventory, Transform parent, int fontSize)
        {
            //create a new slot for each item in the inventory
            foreach (var kvp in inventory)
            {
                var mat = kvp.Key;
                var qty = kvp.Value;
                if (slotTemplatePrefab == null)
                {
                    continue;
                }

                var slot = Instantiate(slotTemplatePrefab, parent, false);
                slot.SetActive(true);

                var bg = slot.GetComponent<Image>();
                if (bg != null)
                {
                    bg.enabled = false;
                }

                Image icon = slot.transform.Find("Icon")?.GetComponent<Image>()
                             ?? slot.GetComponentsInChildren<Image>()
                                    .FirstOrDefault(i => i.gameObject != slot.gameObject);

                if (icon != null && itemIcons.TryGetValue(mat, out Sprite sprite))
                {
                    icon.enabled = true;
                    icon.sprite = sprite;
                }
                else if (icon != null)
                {
                    icon.enabled = false;
                }

                var text = slot.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = fontSize;
                    text.text = $"{mat.itemName}: {qty}";
                }
            }
        }

        BuildSlots(_inputInventory, inputInventoryContainer, 13);
        BuildSlots(_outputInventory, outputInventoryContainer, 13);
    }

    private void ToggleUI()
    {
        //check if the player is near the machine and if the UI is not already open, then toggle the UI
        if (uiPanel == null)
        {
            Debug.LogError("UI Panel is null! Did Initialize() run?");
            return;
        }
        uiPanel.SetActive(!uiPanel.activeSelf);
        if (uiPanel.activeSelf)
        {
            _UpdateInventoryUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //check if the player is near the machine and set the isPlayerNearby variable to true
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //check if the player is no longer near the machine and set the isPlayerNearby variable to false
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
        return _outputInventory.Values.Sum() > 0;
    }

    public bool CanOutputItem()
    {
        return HasItem();
    }

    public bool OutputItem()
    {
        //handles outputting items from the machine, removing them from the output inventory
        if (!HasItem())
        {
            return false;
        }
        var entry = _outputInventory.FirstOrDefault(kvp => kvp.Value > 0);
        if (entry.Key == null)
        {
            return false;
        }

        _outputInventory[entry.Key]--;
        if (_outputInventory[entry.Key] <= 0)
        {
            _outputInventory.Remove(entry.Key);
        }

        _UpdateInventoryUI();
        return true;
    }

    public Materials TakeItem()
    {
        //handles taking items from the machine, removing them from the output inventory
        if (!HasItem())
        {
            return null;
        }

        var entry = _outputInventory.FirstOrDefault(kvp => kvp.Value > 0);
        if (entry.Key == null)
        {
            return null;
        }

        _outputInventory[entry.Key]--;
        if (_outputInventory[entry.Key] <= 0)
        {
            _outputInventory.Remove(entry.Key);
        }

        _UpdateInventoryUI();
        return entry.Key;
    }

    public Materials TakeItem(Materials specificItem)
    {
        //handles taking a specific item from the machine, removing it from the output inventory, with a filter
        if (specificItem == null || !_outputInventory.ContainsKey(specificItem) || _outputInventory[specificItem] <= 0)
        {
            return null;
        }

        _outputInventory[specificItem]--;
        if (_outputInventory[specificItem] <= 0)
        {
            _outputInventory.Remove(specificItem);
        }

        _UpdateInventoryUI();
        return specificItem;
    }

    public bool CanOutputItem(Materials specificItem)
    {
        //check if the specific item is in the output inventory and has a quantity greater than 0
        return _outputInventory.ContainsKey(specificItem) && _outputInventory[specificItem] > 0;
    }

    public bool CanReceiveItem(Materials item)
    {
        //check if the item is null or if the input inventory is full, knowing that the max input storage is reached
        if (_inputInventory.Values.Sum() >= maxInputStorage)
        {
            return false;
        }

        if (_inputInventory.ContainsKey(item))
        {
            return true;
        }

        return _inputInventory.Keys.Count < 2;
    }

    public bool ReceiveItem(Materials item)
    {
        //handles receiving items into the machine, adding them to the input inventory, filtering the items
        if (!CanReceiveItem(item))
        {
            return false;
        }

        if (_inputInventory.ContainsKey(item))
        {
            _inputInventory[item]++;
        }
        else
        {
            _inputInventory.Add(item, 1);
        }

        _UpdateInventoryUI();
        return true;
    }
}
