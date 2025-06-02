using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipes", menuName = "Factory/Recipes")]
public class Recipes : ScriptableObject
{
    //Used to create new recipes in the inspector
    public string recipeName;
    public Sprite recipeIcon;
    public List<ItemQuantity> inputs;
    public List<ItemQuantity> outputs;
    public float processingTime; 
}

[System.Serializable]
public class ItemQuantity
{
    public Materials item;
    public int quantity;
}
