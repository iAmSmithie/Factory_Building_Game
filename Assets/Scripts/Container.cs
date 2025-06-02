using UnityEngine;

public class Container : MonoBehaviour
{
    public Materials containedMaterial; //The material inside the container

    public void SetMaterial(Materials material)
    {
        containedMaterial = material;
    }
}
