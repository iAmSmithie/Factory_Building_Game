using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Factory/Items")]
public class Materials : ScriptableObject
{
    //makes it easier to create new items in the inspector
    public string itemName;
    public Sprite itemIcon;
}
