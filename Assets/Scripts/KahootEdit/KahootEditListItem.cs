using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KahootEditListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textKahootName;
    [SerializeField] private Button buttonEdit;

    private string fileName;
    private Action<string> onEditClicked;

    public void Setup(string kahootFileName, Action<string> editCallback)
    {
        fileName = kahootFileName;
        onEditClicked = editCallback;

        if (textKahootName != null)
            textKahootName.text = kahootFileName;

        if (buttonEdit != null)
        {
            buttonEdit.onClick.RemoveAllListeners();
            buttonEdit.onClick.AddListener(() => onEditClicked?.Invoke(fileName));
        }
    }
}