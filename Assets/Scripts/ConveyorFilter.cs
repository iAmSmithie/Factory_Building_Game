using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConveyorFilter : MonoBehaviour
{
    public GameObject buttonPrefab; 
    public Transform buttonContainer;
    public Button dropDownButton;

    private Action<Materials> onSelectCallback;

    //setup the filter with a list of materials and a callback for when a material is selected
    public void Setup(List<Materials> materials, Action<Materials> onSelect)
    {
        onSelectCallback = onSelect;

        buttonContainer.gameObject.SetActive(false);

        //check for the button to be clicked
        if (dropDownButton != null)
        {
            dropDownButton.onClick.RemoveAllListeners();
            dropDownButton.onClick.AddListener(() =>
            {
                Debug.Log("Dropdown button clicked!");
                bool isActive = buttonContainer.gameObject.activeSelf;
                buttonContainer.gameObject.SetActive(!isActive);
            });
        }

        //clear the button container before adding new buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        //create buttons for each material
        foreach (var mat in materials)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);

            var text = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            {
                if (text != null) text.text = mat.itemName;
            }

            var icon = buttonObj.transform.Find("ButtonIcon")?.GetComponent<Image>();
            if (icon != null && mat.itemIcon != null)
            {
                icon.sprite = mat.itemIcon;
            }

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                onSelectCallback?.Invoke(mat);
                Destroy(gameObject);
            });
        }
    }

}
