using UnityEngine;

public interface IFilteredOutputMachine : InventoryMachine
{
    //IFilteredOutputMachine Interface
    Materials TakeItem(Materials specificItem);
    bool CanOutputItem(Materials specificItem);
}

