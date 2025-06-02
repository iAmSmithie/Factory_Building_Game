using UnityEngine;
using System.Collections.Generic;


public class ConveyorPoint : MonoBehaviour, InventoryMachine
{
    //represents a point on the conveyor belt where items can be received or output.
    private List<Materials> items = new List<Materials>();
    [SerializeField] private int maxCapacity = 3;

    public bool HasItem() => items.Count > 0;

    public bool CanReceiveItem(Materials item) => items.Count < maxCapacity;

    public Materials TakeItem(Materials specificItem)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == specificItem)
            {
                var item = items[i];
                items.RemoveAt(i);
                return item;
            }
        }
        return null;
    }

    public Materials TakeItem()
    {
        if (items.Count == 0)
        {
            return null;
        }
        var item = items[0];
        items.RemoveAt(0);
        return item;
    }

    public bool ReceiveItem(Materials item)
    {
        if (!CanReceiveItem(item))
        {
            return false;
        }
        items.Add(item);
        return true;
    }

    public bool CanOutputItem()
    {
        return HasItem();
    }

    public bool OutputItem()
    {
        return false;
    }
}
