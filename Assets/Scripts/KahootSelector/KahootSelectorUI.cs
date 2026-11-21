using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// gestiona la seleccion de kahoots disponibles en persistentDataPath,
// genera los botones en el scrollview, enlaza navegacion explicita + scroll y guarda usuario + kahoot elegido
public class KahootSelectorUI : MonoBehaviour
{
    [Header("elementos de UI")]
    public TMP_InputField usernameInputField; // campo para escribir el nombre de usuario
    public Transform contentScrollView;       // contenedor donde se instancian los botones
    public Button mainMenuButton;             // boton de volver al menu principal (opcional)

    [Header("navegacion (new input system)")]
    public SelectorExplicitNav explicitNavigation; // gestor de navegacion explicita (opcional)

    [Header("configuracion de carpeta y escenas")]
    public string kahootsFolderName = "kahoots";
    public string gameSceneName = "03_KahootGame";

    // lista de kahoots cargados correctamente desde los json
    private List<KahootData> availableKahoots = new List<KahootData>();

    // botones generados en el scrollview
    private List<Button> spawnedKahootButtons = new List<Button>();

    // scrollrect padre del content para mover el scroll al navegar
    private ScrollRect parentScrollRect;

    void Start()
    {
        LoadAndDisplayKahoots();
    }

    void Update()
    {
        ReleaseUsernameFocusOnNavigate();

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame))
        {
            ScrollToSelectedKahootButton();
        }
    }

    // suelta el foco de escritura del inputfield al pulsar flecha abajo, arriba o tab para poder navegar
    void ReleaseUsernameFocusOnNavigate()
    {
        if (usernameInputField == null || !usernameInputField.isFocused || EventSystem.current == null)
            return;

        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        Selectable nextTarget = null;

        if (kb.downArrowKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
        {
            nextTarget = spawnedKahootButtons.Count > 0 && spawnedKahootButtons[0] != null
                ? (Selectable)spawnedKahootButtons[0]
                : mainMenuButton;
        }
        else if (kb.upArrowKey.wasPressedThisFrame)
        {
            nextTarget = mainMenuButton;
        }

        if (nextTarget != null)
        {
            usernameInputField.DeactivateInputField();

            if (nextTarget.gameObject.activeInHierarchy && nextTarget.interactable)
            {
                EventSystem.current.SetSelectedGameObject(nextTarget.gameObject);
            }
        }
    }

    // lee los archivos .json de la carpeta de kahoots y prepara la lista
    void LoadAndDisplayKahoots()
    {
        availableKahoots.Clear();

        string persistentPath = Path.Combine(Application.persistentDataPath, kahootsFolderName);
        if (!Directory.Exists(persistentPath))
        {
            Directory.CreateDirectory(persistentPath);
        }

        string[] jsonFiles = Directory.GetFiles(persistentPath, "*.json");

        foreach (string filePath in jsonFiles)
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    throw new FormatException("El archivo JSON esta vacio.");
                }

                KahootData kahoot = JsonUtility.FromJson<KahootData>(jsonContent);

                if (kahoot == null || kahoot.questions == null || kahoot.questions.Count == 0)
                {
                    throw new FormatException("El JSON no tiene una estructura valida de KahootData o no contiene preguntas.");
                }

                // si no tiene titulo definido, usamos el nombre del archivo sin extension
                if (string.IsNullOrWhiteSpace(kahoot.title))
                {
                    kahoot.title = Path.GetFileNameWithoutExtension(filePath);
                }

                availableKahoots.Add(kahoot);
            }
            catch (Exception e)
            {
                // registramos un error incluyendo el archivo para que quede reflejado en los informes
                Debug.LogError($"Error de formato/lectura en JSON '{fileName}' ({DateTime.Now}): {e.Message}");
            }
        }

        CreateKahootButtons();
    }

    // crea un boton por cada kahoot valido dentro del scrollview
    void CreateKahootButtons()
    {
        if (contentScrollView == null) return;

        spawnedKahootButtons.Clear();

        // limpiamos botones anteriores para evitar duplicados al recargar
        foreach (Transform child in contentScrollView)
        {
            Destroy(child.gameObject);
        }

        // ajustamos el tamano del content para que el scroll horizontal funcione bien
        RectTransform contentRect = contentScrollView.GetComponent<RectTransform>();
        HorizontalLayoutGroup hLayoutGroup = contentScrollView.GetComponent<HorizontalLayoutGroup>();
        VerticalLayoutGroup vLayoutGroup = contentScrollView.GetComponent<VerticalLayoutGroup>();

        if (contentRect != null && hLayoutGroup != null)
        {
            float spacing = hLayoutGroup.spacing;
            float buttonWidth = 250f;
            float totalWidth = hLayoutGroup.padding.left + hLayoutGroup.padding.right;
            if (availableKahoots.Count > 0)
            {
                totalWidth += availableKahoots.Count * buttonWidth + (availableKahoots.Count - 1) * spacing;
            }
            contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
        }
        else if (contentRect != null && vLayoutGroup != null)
        {
            float spacing = vLayoutGroup.spacing;
            float buttonHeight = 80f;
            float totalHeight = vLayoutGroup.padding.top + vLayoutGroup.padding.bottom;
            if (availableKahoots.Count > 0)
            {
                totalHeight += availableKahoots.Count * buttonHeight + (availableKahoots.Count - 1) * spacing;
            }
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
        }

        foreach (KahootData kahoot in availableKahoots)
        {
            // creamos el objeto del boton con sus componentes basicos de ui
            GameObject newButtonObj = new GameObject(kahoot.title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            newButtonObj.transform.SetParent(contentScrollView, false);

            RectTransform rect = newButtonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 80);

            // creamos el texto centrado dentro del boton
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(newButtonObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = kahoot.title;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.fontSize = 22;

            // guardamos referencia local para evitar problemas de cierre en el listener
            KahootData selectedKahoot = kahoot;
            Button btn = newButtonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnKahootSelected(selectedKahoot));

            spawnedKahootButtons.Add(btn);
        }

        ConfigureExplicitNavigation();
    }

    // enlaza main menu, usernameInputField y los botones creados en fila horizontal dentro del scrollview
    void ConfigureExplicitNavigation()
    {
        if (parentScrollRect == null && contentScrollView != null)
        {
            parentScrollRect = contentScrollView.GetComponentInParent<ScrollRect>();
        }

        Button firstKahootButton = spawnedKahootButtons.Count > 0 ? spawnedKahootButtons[0] : null;

        if (explicitNavigation != null)
        {
            explicitNavigation.ApplyNavigation(spawnedKahootButtons, focusFirstDynamicIfAvailable: false);
        }

        if (mainMenuButton != null)
        {
            Navigation menuNav = mainMenuButton.navigation;
            menuNav.mode = Navigation.Mode.Explicit;
            menuNav.selectOnDown = usernameInputField != null ? (Selectable)usernameInputField : firstKahootButton;
            menuNav.selectOnRight = usernameInputField != null ? (Selectable)usernameInputField : firstKahootButton;
            mainMenuButton.navigation = menuNav;
        }

        if (usernameInputField != null)
        {
            Navigation inputNav = usernameInputField.navigation;
            inputNav.mode = Navigation.Mode.Explicit;
            inputNav.selectOnUp = mainMenuButton;
            inputNav.selectOnDown = firstKahootButton;
            inputNav.selectOnLeft = mainMenuButton;
            usernameInputField.navigation = inputNav;
        }

        for (int i = 0; i < spawnedKahootButtons.Count; i++)
        {
            Button current = spawnedKahootButtons[i];
            if (current == null) continue;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = i > 0
                ? (Selectable)spawnedKahootButtons[i - 1]
                : (usernameInputField != null ? (Selectable)usernameInputField : mainMenuButton);
            nav.selectOnRight = i < spawnedKahootButtons.Count - 1
                ? (Selectable)spawnedKahootButtons[i + 1]
                : spawnedKahootButtons[0];
            nav.selectOnUp = usernameInputField != null ? (Selectable)usernameInputField : mainMenuButton;
            nav.selectOnDown = null;
            current.navigation = nav;
        }
    }

    // desplaza el scroll horizontal para mostrar el boton seleccionado
    void ScrollToSelectedKahootButton()
    {
        if (parentScrollRect == null || contentScrollView == null || EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;

        RectTransform targetRect = selected.GetComponent<RectTransform>();
        if (targetRect == null || targetRect.parent != contentScrollView)
            return;

        RectTransform contentRect = contentScrollView.GetComponent<RectTransform>();
        RectTransform viewportRect = parentScrollRect.viewport != null
            ? parentScrollRect.viewport
            : parentScrollRect.GetComponent<RectTransform>();

        if (contentRect == null || viewportRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        float contentWidth = contentRect.rect.width;
        float viewportWidth = viewportRect.rect.width;
        if (contentWidth <= viewportWidth)
            return;

        float buttonCenterX = Mathf.Abs(targetRect.localPosition.x) + (targetRect.rect.width * 0.5f);
        float targetScrollX = (buttonCenterX - (viewportWidth * 0.5f)) / (contentWidth - viewportWidth);
        parentScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(targetScrollX);
    }

    // guarda el nombre de usuario y el kahoot seleccionado y pasa a la escena de juego
    void OnKahootSelected(KahootData kahoot)
    {
        string username = usernameInputField != null ? usernameInputField.text : "";
        string trimmed = username.Trim();
        string lower = trimmed.ToLower();

        // si esta vacio o coincide con el placeholder por defecto, asignamos anonymous
        if (string.IsNullOrWhiteSpace(trimmed) || lower == "write your name..." || lower == "write your name")
        {
            username = "Anonymous";
        }
        else
        {
            username = trimmed;
        }

        PlayerPrefs.SetString("CurrentUsername", username);
        PlayerPrefs.SetString("CurrentKahootTitle", kahoot.title);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }
}