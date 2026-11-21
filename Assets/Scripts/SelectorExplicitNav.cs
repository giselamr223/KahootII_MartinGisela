using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// configura la navegacion explicita y el foco inicial sin alterar estilos visuales
public class SelectorExplicitNav : MonoBehaviour
{
    [Header("controles fijos")]
    [SerializeField] private Selectable mainMenuButton;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Selectable firstSelectedOnStart;

    // enlaza la navegacion explicita entre main menu, inputfield y los botones creados en el scrollview
    public void ApplyNavigation(List<Button> dynamicButtons, bool focusFirstDynamicIfAvailable = false)
    {
        if (mainMenuButton != null)
        {
            Navigation menuNav = mainMenuButton.navigation;
            menuNav.mode = Navigation.Mode.Explicit;
            menuNav.selectOnDown = usernameInputField != null
                ? (Selectable)usernameInputField
                : (dynamicButtons != null && dynamicButtons.Count > 0 ? dynamicButtons[0] : null);
            menuNav.selectOnRight = usernameInputField != null ? (Selectable)usernameInputField : null;
            mainMenuButton.navigation = menuNav;
        }

        if (usernameInputField != null)
        {
            Navigation inputNav = usernameInputField.navigation;
            inputNav.mode = Navigation.Mode.Explicit;
            inputNav.selectOnUp = mainMenuButton;
            inputNav.selectOnDown = (dynamicButtons != null && dynamicButtons.Count > 0) ? dynamicButtons[0] : null;
            inputNav.selectOnLeft = mainMenuButton;
            usernameInputField.navigation = inputNav;
        }

        if (dynamicButtons != null && dynamicButtons.Count > 0)
        {
            for (int i = 0; i < dynamicButtons.Count; i++)
            {
                Button current = dynamicButtons[i];
                if (current == null) continue;

                Navigation nav = current.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = i > 0
                    ? (Selectable)dynamicButtons[i - 1]
                    : (usernameInputField != null ? (Selectable)usernameInputField : mainMenuButton);
                nav.selectOnDown = i < dynamicButtons.Count - 1
                    ? (Selectable)dynamicButtons[i + 1]
                    : dynamicButtons[0];
                nav.selectOnLeft = mainMenuButton;
                nav.selectOnRight = null;
                current.navigation = nav;
            }
        }

        if (focusFirstDynamicIfAvailable && dynamicButtons != null && dynamicButtons.Count > 0 && dynamicButtons[0] != null)
        {
            SetFocus(dynamicButtons[0]);
        }
        else if (firstSelectedOnStart != null)
        {
            SetFocus(firstSelectedOnStart);
        }
    }

    public void SetFocus(Selectable target)
    {
        if (EventSystem.current == null || target == null) return;
        if (!target.gameObject.activeInHierarchy || !target.interactable) return;

        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}