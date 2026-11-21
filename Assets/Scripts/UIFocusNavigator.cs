using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// gestiona la seleccion inicial y la recuperacion de foco en ui usando el new input system
public class UIFocusNavigator : MonoBehaviour
{
    [Header("seleccion inicial")]
    [SerializeField] private Selectable firstSelected;

    [Header("accion de navegacion (new input system)")]
    [Tooltip("opcional: asigna una accion de tipo value (vector2) o button desde tu input actions")]
    [SerializeField] private InputActionReference navigateAction;

    private void OnEnable()
    {
        if (navigateAction != null && navigateAction.action != null)
        {
            navigateAction.action.Enable();
            navigateAction.action.performed += OnNavigatePerformed;
        }

        FocusInitialSelectable();
    }

    private void OnDisable()
    {
        if (navigateAction != null && navigateAction.action != null)
        {
            navigateAction.action.performed -= OnNavigatePerformed;
            navigateAction.action.Disable();
        }
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        if (EventSystem.current == null)
            return;

        // si estamos escribiendo en un inputfield activo, no tocamos el foco
        GameObject currentObj = EventSystem.current.currentSelectedGameObject;
        if (currentObj != null)
        {
            var inputField = currentObj.GetComponent<TMPro.TMP_InputField>();
            if (inputField != null && inputField.isFocused)
                return;

            if (currentObj.activeInHierarchy)
                return;
        }

        FocusInitialSelectable();
    }

    public void FocusInitialSelectable()
    {
        if (EventSystem.current == null || firstSelected == null)
            return;

        if (!firstSelected.gameObject.activeInHierarchy || !firstSelected.interactable)
            return;

        EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }

    public void SetSelected(GameObject target)
    {
        if (EventSystem.current == null || target == null || !target.activeInHierarchy)
            return;

        EventSystem.current.SetSelectedGameObject(target);
    }
}