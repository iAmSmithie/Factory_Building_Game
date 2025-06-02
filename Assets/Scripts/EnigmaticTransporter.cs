using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class EnigmaticTransporter : MonoBehaviour, IFilteredOutputMachine
{
    public Materials requiredMaterial;
    public int requiredAmount = 50;
    public float placementHeightOffset = 0.5f;

    private Dictionary<Materials, int> _inputInventory = new Dictionary<Materials, int>();
    private Dictionary<Materials, Sprite> itemIcons = new Dictionary<Materials, Sprite>();
    private bool gameCompleted = false;
    private bool isPlayerNearby = false;

    [Header("UI Settings")]
    public GameObject uiPanel;
    public Transform inputInventoryContainer;
    public GameObject slotTemplatePrefab;

    void Awake()
    {
        Initialize();
    }
    //enigmatic transporter, functions similarly to all other machines, but has a unique UI and game completion mechanic.
    public void Initialize()
    {
        Debug.Log("Initializing Enigmatic Transporter…");

        if (uiPanel == null)
        {
            GameObject uiPrefab = Resources.Load<GameObject>("UI/TransporterUI/EnigmaticTransporterPanel");
            if (uiPrefab == null)
            {
                Debug.LogError("Transporter UI prefab missing from Resources/UI/EnigmaticTransporterUI!");
                return;
            }

            if (slotTemplatePrefab == null)
            {
                slotTemplatePrefab = Resources.Load<GameObject>("UI/EnigmaticTransporterUI/SlotTemplate");
                if (slotTemplatePrefab == null)
                {
                    Debug.LogError("SlotTemplate not found in Resources/UI/EnigmaticTransporterUI!");
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
            inputInventoryContainer = uiPanel.transform.Find("TInputInventoryContainer");
            if (inputInventoryContainer == null)
            {
                Debug.LogError("Couldn't find input inventory container in UI hierarchy!");
                return;
            }

            uiPanel.SetActive(false);
        }

        LoadIcons();
        _UpdateInventoryUI();
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleUI();
        }

        if (gameCompleted)
        {
            return;
        }

        if (_inputInventory.TryGetValue(requiredMaterial, out int count) && count >= requiredAmount)
        {
            gameCompleted = true;
            Debug.Log("Game Complete!");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void LoadIcons()
    {
        itemIcons.Clear();

        string[] folders = new string[] { "Recipes and Items/Minerals", "Recipes and Items/MetalBars" };
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

    private void _UpdateInventoryUI()
    {
        foreach (Transform child in inputInventoryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in _inputInventory)
        {
            var mat = kvp.Key;
            var qty = kvp.Value;
            if (slotTemplatePrefab == null)
            {
                continue;
            }

            var slot = Instantiate(slotTemplatePrefab, inputInventoryContainer, false);
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
                text.fontSize = 20;
                text.text = $"{mat.itemName}: {qty}";
            }
        }
    }

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

    public bool HasItem()
    {
        return false;
    }

    public bool CanOutputItem()
    {
        return false;
    }

    public bool CanOutputItem(Materials specificItem)
    {
        return false;
    }

    public bool OutputItem()
    {
        return false;
    }

    public bool OutputItem(Materials specificItem)
    {
        return false;
    }

    public Materials TakeItem()
    {
        return null;
    }

    public Materials TakeItem(Materials specificItem)
    {
        return null;
    }

    public bool CanReceiveItem(Materials item)
    {
        return item == requiredMaterial;
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
            _inputInventory[item] = 1;
        }

        _UpdateInventoryUI();
        return true;
    }

    public float GetPlacementHeightOffset()
    {
        return placementHeightOffset;
    }
}
