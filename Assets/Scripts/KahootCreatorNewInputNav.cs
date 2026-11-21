using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class KahootCreatorNewInputNav : MonoBehaviour
{
    [Header("botones principales")]
    [SerializeField] private Selectable mainMenuButton;
    [SerializeField] private Selectable addQuestionButton;
    [SerializeField] private Selectable generatePresetButton;
    [SerializeField] private Selectable saveButton;

    [Header("campos de texto")]
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField durationInput;
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private List<TMP_InputField> answerInputs = new List<TMP_InputField>();
    [SerializeField] private List<Selectable> correctCheckSelectables = new List<Selectable>();

    // lista con todos los input fields para comprobar si alguno esta escribiendo
    private readonly List<TMP_InputField> allInputs = new List<TMP_InputField>();
    // guarda el ultimo texto escrito (con saltos de linea incluidos) para no perderlo con escape
    private readonly Dictionary<TMP_InputField, string> lastWrittenTexts = new Dictionary<TMP_InputField, string>();

    private InputSystemUIInputModule uiInputModule;
    private bool wasEditingInput;

    void Awake()
    {
        CacheUIInputModule();
    }

    void Start()
    {
        InitializeNavigation();
    }

    void Update()
    {
        TickNavigationState();
    }

    void OnDisable()
    {
        ResetNavigationStateOnDisable();
    }

    // busca y guarda el inputsystemuiinputmodule del eventsystem actual
    void CacheUIInputModule()
    {
        if (EventSystem.current != null)
        {
            uiInputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        }
    }

    // prepara listas, listeners, enlaces explicit y foco inicial
    void InitializeNavigation()
    {
        CollectInputs();
        BindInputListeners();
        ConfigureExplicitNavigation();
        FocusFirst();
    }

    // gestiona escape para salir de escribir (dejando enter libre para saltar de linea)
    void TickNavigationState()
    {
        if (EventSystem.current == null) return;

        TMP_InputField focusedField = GetFocusedInputField();
        bool isEditing = focusedField != null;

        if (isEditing && WasEscapePressed())
        {
            ExitEditing(focusedField);
            isEditing = false;
        }

        SyncInputModuleActions(isEditing);
        EnsureValidSelection(isEditing);
    }

    // restaura las acciones al desactivar el objeto o cambiar de escena
    void ResetNavigationStateOnDisable()
    {
        SetNavigationActionsEnabled(true);
        wasEditingInput = false;
    }

    // comprueba si se ha pulsado escape en este frame
    bool WasEscapePressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    // activa o desactiva move y cancel segun estemos escribiendo o navegando
    void SyncInputModuleActions(bool isEditing)
    {
        if (isEditing == wasEditingInput) return;

        wasEditingInput = isEditing;
        SetNavigationActionsEnabled(!isEditing);
    }

    // si no estamos escribiendo y no hay ningun objeto seleccionado, recupera foco
    void EnsureValidSelection(bool isEditing)
    {
        if (!isEditing && EventSystem.current.currentSelectedGameObject == null)
        {
            FocusFirst();
        }
    }

    // activa o desactiva move y cancel del inputsystemuiinputmodule
    void SetNavigationActionsEnabled(bool enabled)
    {
        if (uiInputModule == null)
        {
            CacheUIInputModule();
        }

        if (uiInputModule == null) return;

        if (uiInputModule.move != null && uiInputModule.move.action != null)
        {
            if (enabled)
                uiInputModule.move.action.Enable();
            else
                uiInputModule.move.action.Disable();
        }

        if (uiInputModule.cancel != null && uiInputModule.cancel.action != null)
        {
            if (enabled)
                uiInputModule.cancel.action.Enable();
            else
                uiInputModule.cancel.action.Disable();
        }
    }

    // restaura el texto si al salir de edicion se vacio o revirtio teniendo contenido escrito
    void RestoreWrittenTextIfReverted(TMP_InputField field, string finalText)
    {
        if (field == null) return;

        if (lastWrittenTexts.TryGetValue(field, out string writtenText))
        {
            if (!string.IsNullOrEmpty(writtenText) && string.IsNullOrEmpty(finalText))
            {
                field.SetTextWithoutNotify(writtenText);
                field.ForceLabelUpdate();
            }
        }
    }

    // sale del modo escritura con escape manteniendo lo escrito y devolviendo foco al campo
    void ExitEditing(TMP_InputField field)
    {
        if (field == null) return;

        string textToKeep = lastWrittenTexts.TryGetValue(field, out string cached) && !string.IsNullOrEmpty(cached)
            ? cached
            : field.text;

        field.DeactivateInputField();
        field.SetTextWithoutNotify(textToKeep);
        field.ForceLabelUpdate();

        SetNavigationActionsEnabled(true);
        wasEditingInput = false;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(field.gameObject);
        }
    }

    // junta todos los tmp_inputfield de la pantalla en una lista
    void CollectInputs()
    {
        allInputs.Clear();
        lastWrittenTexts.Clear();

        if (titleInput != null) allInputs.Add(titleInput);
        if (durationInput != null) allInputs.Add(durationInput);
        if (questionInput != null) allInputs.Add(questionInput);

        foreach (var ans in answerInputs)
        {
            if (ans != null) allInputs.Add(ans);
        }
    }

    // registra cambios de texto y salida de edicion sin bloquear el salto de linea con enter
    void BindInputListeners()
    {
        foreach (var field in allInputs)
        {
            if (field == null) continue;

            lastWrittenTexts[field] = field.text;

            field.onValueChanged.AddListener(newValue =>
            {
                lastWrittenTexts[field] = newValue;
            });

            field.onEndEdit.AddListener(finalValue =>
            {
                RestoreWrittenTextIfReverted(field, finalValue);
            });
        }
    }

    // devuelve el input field que esta actualmente en modo escritura, o null si ninguno lo esta
    TMP_InputField GetFocusedInputField()
    {
        foreach (var field in allInputs)
        {
            if (field != null && field.isFocused)
                return field;
        }
        return null;
    }

    // configura toda la navegacion explicit entre botones, campos y checks
    void ConfigureExplicitNavigation()
    {
        SetExplicit(mainMenuButton, up: null, down: addQuestionButton, left: null, right: titleInput);
        SetExplicit(addQuestionButton, up: mainMenuButton, down: generatePresetButton, left: null, right: questionInput);
        SetExplicit(generatePresetButton, up: addQuestionButton, down: saveButton, left: null, right: answerInputs.Count > 0 ? answerInputs[0] : null);
        SetExplicit(saveButton, up: generatePresetButton, down: null, left: null, right: null);

        SetExplicit(titleInput, up: mainMenuButton, down: questionInput, left: mainMenuButton, right: durationInput);
        SetExplicit(durationInput, up: mainMenuButton, down: questionInput, left: titleInput, right: null);
        SetExplicit(questionInput, up: titleInput, down: answerInputs.Count > 0 ? answerInputs[0] : saveButton, left: addQuestionButton, right: null);

        for (int i = 0; i < answerInputs.Count; i++)
        {
            TMP_InputField ans = answerInputs[i];
            Selectable check = i < correctCheckSelectables.Count ? correctCheckSelectables[i] : null;
            Selectable prev = i > 0 ? answerInputs[i - 1] : questionInput;
            Selectable next = i < answerInputs.Count - 1 ? answerInputs[i + 1] : saveButton;

            SetExplicit(ans, up: prev, down: next, left: i == 0 ? generatePresetButton : null, right: check);
            if (check != null)
                SetExplicit(check, up: prev, down: next, left: ans, right: null);
        }
    }

    // helper para aplicar navigation.mode.explicit sin repetir codigo
    void SetExplicit(Selectable target, Selectable up, Selectable down, Selectable left, Selectable right)
    {
        if (target == null) return;

        Navigation nav = target.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = up;
        nav.selectOnDown = down;
        nav.selectOnLeft = left;
        nav.selectOnRight = right;
        target.navigation = nav;
    }

    // pone el foco inicial al entrar en la escena
    void FocusFirst()
    {
        if (EventSystem.current == null) return;

        if (mainMenuButton != null && mainMenuButton.gameObject.activeInHierarchy && mainMenuButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(mainMenuButton.gameObject);
        }
    }
}