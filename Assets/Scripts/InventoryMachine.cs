using UnityEngine;

public interface InventoryMachine
{
    //Check if the machine currently has an item
    bool HasItem();

    //Check if the machine can receive a specific item type
    bool CanReceiveItem(Materials item);

    //Take an item from the machine
    Materials TakeItem();

    //Receive an item into the machine
    bool ReceiveItem(Materials item);

    //Check if the machine can output an item
    bool CanOutputItem();

    //Output an item from the machine
    bool OutputItem();
}
