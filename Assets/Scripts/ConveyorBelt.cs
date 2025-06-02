using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public List<ConveyorBelt> outputBelts = new();
    public List<ConveyorBelt> inputBelts = new();

    public InventoryMachine adjacentInputMachine;
    public InventoryMachine adjacentOutputMachine;

    public GameObject itemVisualPrefab;
    public float itemVisualScale = 1f;
    public float beltLength = 1f;
    public float beltSpeed = 1f;

    [SerializeField] private List<Materials> allMaterials;
    public GameObject filterUIPrefab;
    public Materials allowedMaterial;
    public bool isFilterEnabled = false;

    private class ItemInstance
    {
        public Materials item;
        public GameObject visual;
        public float progress;
        public Vector3 startDir;
        public Vector3 endDir;
    }
    private List<ItemInstance> itemsOnBelt = new();

    void Start()
    {
        foreach (var outBelt in outputBelts)
            if (!outBelt.inputBelts.Contains(this))
                outBelt.inputBelts.Add(this);
    }

    void Update()
    {
        inputBelts.RemoveAll(b => b == null);
        outputBelts.RemoveAll(b => b == null);
        for (int i = 0; i < itemsOnBelt.Count; i++)
        {
            var inst = itemsOnBelt[i];
            inst.progress += Time.deltaTime * beltSpeed / beltLength;
            float radius = inst.visual.transform.position.magnitude;
            inst.visual.transform.position = Vector3.Slerp(inst.startDir, inst.endDir, inst.progress) * radius;

            if (inst.progress >= 1f)
            {
                if (TryDeliverToMachine(inst))
                {
                    itemsOnBelt.RemoveAt(i);
                    i--;
                    continue;
                }
                if (TryHandOffToBelt(inst))
                {
                    itemsOnBelt.RemoveAt(i);
                    i--;
                    continue;
                }
                inst.progress = 1f;
            }
        }
        if (itemsOnBelt.Count == 0)
            TryPullFromInput();
    }

    private void TryPullFromInput()
    {
        //Check if we have a filter enabled and if so, check if we have a material selected, this will prevent pulling from the machine without a filter on
        if (isFilterEnabled && allowedMaterial == null)
        {
            //Debug.Log($"[{name}] Filter enabled but no material selected – skipping pull.");
            return;
        }

        // 1) Pull-first from upstream belts
        foreach (var belt in inputBelts)
        {
            if (belt.HasReadyItem(out var mat, out var visual))
            {
                //Debug.Log($"[{name}] Belt '{belt.name}' has ready item '{mat.itemName}'");
                // Only accept if not filtering or it matches the selected filter
                if (!isFilterEnabled || mat == allowedMaterial)
                {
                    if (AddItem(mat, visual))
                    {
                        //Debug.Log($"[{name}] Added '{mat.itemName}' from belt '{belt.name}'");
                        belt.ConsumeReadyItem();
                        return;
                    }
                    else
                    {
                        //Debug.Log($"[{name}] Failed to add '{mat.itemName}' from belt");
                    }
                }
                else
                {
                    //Debug.Log($"[{name}] Rejected '{mat.itemName}' due to filter '{allowedMaterial.itemName}'");
                }
            }
        }
        if (adjacentInputMachine != null)
        {
            Materials mat = null;

            if (isFilterEnabled)
            {
                if (adjacentInputMachine is IFilteredOutputMachine filteredMachine)
                {
                    //Debug.Log($"[{name}] Pulling filtered from machine: {allowedMaterial.itemName}");
                    if (filteredMachine.CanOutputItem(allowedMaterial))
                    {
                        mat = filteredMachine.TakeItem(allowedMaterial);
                        //Debug.Log($"[{name}] Filtered pull returned '{(mat != null ? mat.itemName : "null")}'");
                    }
                    else
                    {
                        //Debug.Log($"[{name}] Machine cannot output '{allowedMaterial.itemName}' right now.");
                    }
                }
                else
                {
                    //Debug.LogWarning($"[{name}] Adjacent machine does not support filtered output.");
                }
            }
            else
            {
                if (adjacentInputMachine.CanOutputItem())
                {
                    //Debug.Log($"[{name}] Pulling unfiltered from machine");
                    mat = adjacentInputMachine.TakeItem();
                    //Debug.Log($"[{name}] Unfiltered pull returned '{(mat != null ? mat.itemName : "null")}'");
                }
            }

            if (mat != null)
            {
                //Debug.Log($"[{name}] Added '{mat.itemName}' from machine");
                AddItem(mat, null);
            }
        }
    }


    private bool TryDeliverToMachine(ItemInstance inst)
    {
        //Check if we have a filter enabled and if so, check if we have a material selected, this will prevent pulling from the machine without a filter on
        if (adjacentOutputMachine != null && adjacentOutputMachine.CanReceiveItem(inst.item))
        {
            if (adjacentOutputMachine.ReceiveItem(inst.item))
            {
                Destroy(inst.visual);
                return true;
            }
        }
        return false;
    }

    //Try to hand off to the next belt
    private bool TryHandOffToBelt(ItemInstance inst)
    {
        foreach (var next in outputBelts)
            if (next.AddItem(inst.item, inst.visual))
            {
                return true;
            }
        return false;
    }

    //Try to add an item to the belt
    public bool AddItem(Materials mat, GameObject visual = null)
    {
        if (itemsOnBelt.Count >= 1) return false;

        GameObject v = visual;
        //Check if the item is allowed by the filter
        if (v == null)
        {
            float radius = transform.position.magnitude;
            Vector3 spawnDir = transform.position.normalized;
            v = Instantiate(itemVisualPrefab, spawnDir * radius, Quaternion.identity);
            v.transform.localScale = Vector3.one * itemVisualScale;
            v.GetComponent<Container>()?.SetMaterial(mat);
        }
        v.transform.SetParent(null);
 
        var inst = new ItemInstance
        {
            item = mat,
            visual = v,
            progress = 0f,
            startDir = v.transform.position.normalized,
            endDir = (transform.position + transform.forward * beltLength).normalized
        };
        itemsOnBelt.Add(inst);
        return true;
    }
    //Check if the belt has a ready item
    public bool HasReadyItem(out Materials mat, out GameObject visual)
    {
        if (itemsOnBelt.Count > 0 && itemsOnBelt[0].progress >= 1f)
        {
            mat = itemsOnBelt[0].item;
            visual = itemsOnBelt[0].visual;
            return true;
        }
        //If we have no items on the belt, return null
        mat = null;
        visual = null;
        return false;
    }

    public void ConsumeReadyItem()
    {
        //Check if we have a ready item
        if (itemsOnBelt.Count > 0)
        {
            itemsOnBelt.RemoveAt(0);
        }
    }

    public void OnPlaced()
    {
        //for the filter UI, check if were filtering the conveyor belt, if so, show the filter UI
        if (!isFilterEnabled || filterUIPrefab == null)
        {
            return;
        }
        Canvas c = FindFirstObjectByType<Canvas>();
        if (c == null)
        {
            return;
        }
        var ui = Instantiate(filterUIPrefab, c.transform);
        var sel = ui.GetComponent<ConveyorFilter>();
        sel.Setup(allMaterials, selMat => allowedMaterial = selMat);
        ui.transform.position = Camera.main.WorldToScreenPoint(transform.position);
    }
    //draw gizmos to show the belt direction
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var o in outputBelts)
        {
            if (o != null)
            {
                Gizmos.DrawLine(transform.position, o.transform.position);
            }
        }
    }
}
