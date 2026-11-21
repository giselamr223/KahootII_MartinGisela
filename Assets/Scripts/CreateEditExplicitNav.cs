using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// configura la navegacion explicita por teclado y el feedback visual rosa en los botones generados de content
public class CreateEditExplicitNav : MonoBehaviour
{
    [Header("controles fijos")]
    [SerializeField] private Selectable mainMenuButton;
    [SerializeField] private Selectable goToCreateButton;
    [SerializeField] private Selectable firstSelectedOnStart;

    [Header("contenedor de botones generados")]
    [SerializeField] private Transform generatedButtonsParent;

    [Header("feedback visual en botones generados")]
    [SerializeField] private Color selectedPinkColor = new Color(1f, 0.41f, 0.71f, 1f);

    [Header("accion de teclado (new input system)")]
    [SerializeField] private InputActionReference navigateAction;

    // guarda los botones edit generados en orden y su color original para no perderlo
    private readonly List<Selectable> editButtons = new List<Selectable>();
    private readonly Dictionary<Selectable, ColorBlock> originalColors = new Dictionary<Selectable, ColorBlock>();

    void OnEnable()
    {
        HookNavigationAction();
        StartCoroutine(RefreshAfterSpawnRoutine());
    }

    void OnDisable()
    {
        UnhookNavigationAction();
        RestoreGeneratedButtonColors();
    }

    // activa y enlaza la accion de teclado del new input system
    void HookNavigationAction()
    {
        if (navigateAction != null && navigateAction.action != null)
        {
            navigateAction.action.Enable();
            navigateAction.action.performed += OnNavigatePerformed;
        }
    }

    // desconecta y desactiva la accion de teclado al salir
    void UnhookNavigationAction()
    {
        if (navigateAction != null && navigateAction.action != null)
        {
            navigateAction.action.performed -= OnNavigatePerformed;
            navigateAction.action.Disable();
        }
    }

    // espera un frame para que se generen los botones edit al entrar
    IEnumerator RefreshAfterSpawnRoutine()
    {
        yield return null;
        ApplyNavigation(focusFirstEditIfAvailable: true);
    }

    // si pulsas tecla de navegacion y no hay foco valido, lo recupera
    void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        if (EventSystem.current == null)
            return;

        GameObject currentObj = EventSystem.current.currentSelectedGameObject;
        if (currentObj != null && currentObj.activeInHierarchy)
            return;

        FocusFallback();
    }

    // enlaza main menu -> botones edit -> go to create y aplica el color rosa a los botones de content
    public void ApplyNavigation(bool focusFirstEditIfAvailable = false)
    {
        RestoreGeneratedButtonColors();
        CollectGeneratedEditButtons();
        ApplyPinkFeedbackToGeneratedButtons();

        Selectable firstEdit = editButtons.Count > 0 ? editButtons[0] : null;
        Selectable lastEdit = editButtons.Count > 0 ? editButtons[editButtons.Count - 1] : null;

        ConfigureMainMenu(firstEdit);
        ConfigureEditButtons(firstEdit);
        ConfigureGoToCreate(lastEdit);

        if (focusFirstEditIfAvailable && firstEdit != null)
        {
            SetFocus(firstEdit);
        }
        else
        {
            FocusFallback();
        }
    }

    // busca los botones edit creados dentro del scrollview (content)
    void CollectGeneratedEditButtons()
    {
        editButtons.Clear();
        if (generatedButtonsParent == null) return;

        Button[] buttons = generatedButtonsParent.GetComponentsInChildren<Button>(false);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].interactable)
            {
                editButtons.Add(buttons[i]);
            }
        }
    }

    // guarda el color original y pone rosa al seleccionar/resaltar cada boton generado en content
    void ApplyPinkFeedbackToGeneratedButtons()
    {
        for (int i = 0; i < editButtons.Count; i++)
        {
            Selectable btn = editButtons[i];
            if (btn == null) continue;

            if (!originalColors.ContainsKey(btn))
            {
                originalColors[btn] = btn.colors;
            }

            ColorBlock colors = btn.colors;
            colors.highlightedColor = selectedPinkColor;
            colors.selectedColor = selectedPinkColor;
            btn.colors = colors;
        }
    }

    // restaura los colores originales de los botones generados si existen
    void RestoreGeneratedButtonColors()
    {
        foreach (var pair in originalColors)
        {
            if (pair.Key != null)
            {
                pair.Key.colors = pair.Value;
            }
        }
        originalColors.Clear();
    }

    // enlaces explicitos de main menu
    void ConfigureMainMenu(Selectable firstEdit)
    {
        if (mainMenuButton == null) return;

        Navigation nav = mainMenuButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = null;
        nav.selectOnDown = firstEdit != null ? firstEdit : goToCreateButton;
        nav.selectOnLeft = null;
        nav.selectOnRight = firstEdit;
        mainMenuButton.navigation = nav;
    }

    // enlaces explicitos de cada boton edit generado
    void ConfigureEditButtons(Selectable firstEdit)
    {
        for (int i = 0; i < editButtons.Count; i++)
        {
            Selectable current = editButtons[i];
            if (current == null) continue;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = i > 0 ? editButtons[i - 1] : mainMenuButton;
            nav.selectOnDown = i < editButtons.Count - 1 ? editButtons[i + 1] : goToCreateButton;
            nav.selectOnLeft = mainMenuButton;
            nav.selectOnRight = goToCreateButton;
            current.navigation = nav;
        }
    }

    // enlaces explicitos de go to create
    void ConfigureGoToCreate(Selectable lastEdit)
    {
        if (goToCreateButton == null) return;

        Navigation nav = goToCreateButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = lastEdit != null ? lastEdit : mainMenuButton;
        nav.selectOnDown = null;
        nav.selectOnLeft = editButtons.Count > 0 ? editButtons[0] : mainMenuButton;
        nav.selectOnRight = null;
        goToCreateButton.navigation = nav;
    }

    // pone foco en el primer control disponible si no hay seleccion
    void FocusFallback()
    {
        if (firstSelectedOnStart != null && firstSelectedOnStart.gameObject.activeInHierarchy && firstSelectedOnStart.interactable)
        {
            SetFocus(firstSelectedOnStart);
            return;
        }

        if (editButtons.Count > 0)
        {
            SetFocus(editButtons[0]);
            return;
        }

        if (mainMenuButton != null)
        {
            SetFocus(mainMenuButton);
            return;
        }

        if (goToCreateButton != null)
        {
            SetFocus(goToCreateButton);
        }
    }

    // asigna el foco de forma segura si el objeto esta activo y se puede pulsar
    public void SetFocus(Selectable target)
    {
        if (EventSystem.current == null || target == null) return;
        if (!target.gameObject.activeInHierarchy || !target.interactable) return;

        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}