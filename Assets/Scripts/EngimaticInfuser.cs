using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class EnigmaticInfuser : MonoBehaviour, IFilteredOutputMachine
{
    public float placementHeightOffset = 0.5f;

    [Header("Capacity Settings")]
    public int maxInputStorage = 100;
    public int maxOutputStorage = 50;

    [SerializeField] private List<Recipes> infusionRecipes;
    private Dictionary<Materials, int> _inputInventory = new Dictionary<Materials, int>();
    private Dictionary<Materials, int> _outputInventory = new Dictionary<Materials, int>();
    private Dictionary<Materials, Sprite> itemIcons = new Dictionary<Materials, Sprite>();

    private float infusionProgress = 0f;
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
    //load ui features such as icons and slot prefabs, then initialize the UI.
    public void Initialize()
    {
        Debug.Log("Initializing Enigmatic Infuser…");

        if (uiPanel == null)
        {
            GameObject uiPrefab = Resources.Load<GameObject>("UI/EnigmaticUI/EnigmaticInfuserPanel");
            if (uiPrefab == null)
            {
                Debug.LogError("EnigmaticInfuser UI prefab missing from Resources/UI/EnigmaticUI!");
                return;
            }

            if (slotTemplatePrefab == null)
            {
                slotTemplatePrefab = Resources.Load<GameObject>("UI/EnigmaticInfuserUI/SlotTemplate");
                if (slotTemplatePrefab == null)
                {
                    Debug.LogError("SlotTemplate not found in Resources/UI/EnigmaticInfuserUI!");
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
            }

            uiPanel = Instantiate(uiPrefab, targetCanvas.transform);

            inputInventoryContainer = uiPanel.transform.Find("EInputInventoryContainer");
            outputInventoryContainer = uiPanel.transform.Find("EOutputInventoryContainer");

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
    //checks for player input to open the UI, and checks for recipes to process
    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleUI();
        }

        if (activeRecipe == null)
        {
            activeRecipe = FindMatchingRecipe();
        }

        if (activeRecipe != null)
        {
            infusionProgress += Time.deltaTime;
            if (infusionProgress >= activeRecipe.processingTime)
            {
                ProcessRecipe(activeRecipe);
                infusionProgress = 0f;
                activeRecipe = null;
            }
        }
    }
    //checks for recipes to process
    private Recipes FindMatchingRecipe()
    {
        foreach (var recipe in infusionRecipes)
        {
            if (recipe.inputs.Count != 5)
            {
                continue;
            }

            if (!recipe.inputs.All(i => _inputInventory.ContainsKey(i.item) && _inputInventory[i.item] >= i.quantity))
            {
                continue;
            }

            int totalOutputs = recipe.outputs.Sum(o => o.quantity);
            if (_outputInventory.Values.Sum() + totalOutputs <= maxOutputStorage)
            {
                return recipe;
            }
        }
        return null;
    }
    //processes the recipe by removing the input items and adding the output items to the inventory
    private void ProcessRecipe(Recipes recipe)
    {
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
    //loads the icons for the items in the inventory
    private void LoadIcons()
    {
        itemIcons.Clear();

        string[] folders = new string[] { "Recipes and Items/Minerals", "Recipes and Items/MetalBars", "Recipes and Items/Alloys" };
        foreach (var folder in folders)
        {
            Materials[] materials = Resources.LoadAll<Materials>(folder);
            foreach (var material in materials)
            {
                if (material.itemIcon != null)
                {
                    itemIcons[material] = material.itemIcon;
                }
            }
        }
    }
    //update the inventory UI by clearing the current slots and creating new ones based on the current inventory
    private void _UpdateInventoryUI()
    {
        foreach (Transform child in inputInventoryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in outputInventoryContainer)
        {
            Destroy(child.gameObject);
        }

        void BuildSlots(Dictionary<Materials, int> inventory, Transform parent, int fontSize)
        {
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
                             ?? slot.GetComponentsInChildren<Image>().FirstOrDefault(i => i.gameObject != slot.gameObject);
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
        BuildSlots(_outputInventory, outputInventoryContainer, 16);
    }
    //toggle the UI on and off when the player is nearby and presses the interact key
    private void ToggleUI()
    {
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
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

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
    // comments in alloy machine script as they function the same way

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
        return _outputInventory.ContainsKey(specificItem) && _outputInventory[specificItem] > 0;
    }

    public bool CanReceiveItem(Materials item)
    {
        if (_inputInventory.Values.Sum() >= maxInputStorage)
        {
            return false;
        }

        if (_inputInventory.ContainsKey(item))
        {
            return true;
        }

        return _inputInventory.Keys.Count < 5;
    }

    public bool ReceiveItem(Materials item)
    {
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
