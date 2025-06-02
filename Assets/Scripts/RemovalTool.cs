using UnityEngine;

public class RemovalTool : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.R;
    public float rayDistance = 10f;
    public LayerMask removableLayer;

    [Header("Optional Feedback")]
    public GameObject deleteEffectPrefab;

    private bool removalModeEnabled = false;

    //handles the removal of objects in the game
    void Update()
    {
        //Toggle removal mode
        if (Input.GetKeyDown(toggleKey))
        {
            removalModeEnabled = !removalModeEnabled;
            Debug.Log("Removal Mode: " + (removalModeEnabled ? "ENABLED" : "DISABLED"));
        }

        //Handle click-to-remove
        if (removalModeEnabled && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, removableLayer);
            //Filter out hits that are not on the removable layer to avoid deleting important assets
            foreach (var hit in hits)
            {
                GameObject hitObj = hit.collider.gameObject;
                GameObject rootObj = hitObj.transform.root.gameObject;

                string tag = rootObj.tag;

                Debug.Log($"Hit object: {hitObj.name}, Root: {rootObj.name}, Tag: {tag}, Layer: {LayerMask.LayerToName(rootObj.layer)}");
                //Check if the object is tagged as removable, if yes, remove it
                if (tag == "PlacedObject" || tag == "Conveyor")
                {
                    Debug.Log("Removed object: " + rootObj.name);

                    if (deleteEffectPrefab)
                        Instantiate(deleteEffectPrefab, rootObj.transform.position, Quaternion.identity);

                    Destroy(rootObj);
                    break;
                }
                else
                {
                    Debug.Log("Hit object is not tagged as removable: " + rootObj.name);
                }
            }
        }
    }

    public bool IsRemovalModeEnabled() => removalModeEnabled;
}
