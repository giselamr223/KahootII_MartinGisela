using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class ErrorReportsViewer : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Transform listContainer;              // El Content del ScrollView
    public GameObject reportButtonPrefab;        // El Prefab del Botón
    public GameObject viewerPanel;               // El Panel del visor (empieza desactivado)
    public TextMeshProUGUI viewerTextContent;    // El TextMeshProUGUI dentro del Panel

    [Header("Navegación Explicit")]
    public ScrollRect scrollRect;                // ScrollRect horizontal de los errores (opcional)
    public Selectable mainMenuButton;            // Botón Main Menu arriba a la izquierda (opcional)

    [Header("Estilo de Selección")]
    public Color selectedButtonColor = new Color(1f, 0.45f, 0.75f, 1f); // Rosa al estar seleccionado

    private readonly List<Selectable> spawnedSelectables = new List<Selectable>();
    private readonly Dictionary<Graphic, Color> originalGraphicColors = new Dictionary<Graphic, Color>();

    private GameObject lastSelectedObject;
    private Graphic currentlyTintedGraphic;
    private GameObject buttonBeforeOpenPanel;

    void OnEnable()
    {
        LoadErrorReports();
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // Si el visor está abierto, permitimos cerrarlo con tecla (Escape o Backspace)
        if (viewerPanel != null && viewerPanel.activeInHierarchy)
        {
            Keyboard kb = Keyboard.current;
            bool closePressed = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame);

            if (closePressed)
            {
                CloseErrorDetails();
            }
            return;
        }

        // Si se pierde la selección, recuperamos un foco válido
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
        {
            ClearSelectionTint();
            FocusFirstAvailable();
            return;
        }

        // Cuando cambia el objeto seleccionado, actualizamos tinte rosa y scroll horizontal
        if (selected != lastSelectedObject)
        {
            lastSelectedObject = selected;
            UpdateSelectionTint(selected);

            RectTransform selectedRect = selected.GetComponent<RectTransform>();
            RectTransform containerRect = listContainer as RectTransform;

            if (selectedRect != null && containerRect != null && selectedRect.IsChildOf(containerRect))
            {
                ScrollToTarget(selectedRect, containerRect);
            }
        }
    }

    public void LoadErrorReports()
    {
        if (listContainer == null || reportButtonPrefab == null || viewerPanel == null || viewerTextContent == null)
        {
            Debug.LogError("ErrorReportsViewer: Faltan referencias en el Inspector.");
            return;
        }

        ClearSelectionTint();
        spawnedSelectables.Clear();
        originalGraphicColors.Clear();
        buttonBeforeOpenPanel = null;

        // 1. El panel de detalles empieza oculto
        viewerPanel.SetActive(false);

        // 2. Limpiar botones anteriores
        foreach (Transform child in listContainer)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // 3. Obtener los archivos de error
        string reportsFolderPath = Path.Combine(Application.persistentDataPath, "ErrorReports");
        if (!Directory.Exists(reportsFolderPath))
        {
            Directory.CreateDirectory(reportsFolderPath);
        }

        string[] reportFiles = Directory.GetFiles(reportsFolderPath, "*.txt");

        if (reportFiles.Length == 0)
        {
            Debug.Log("No hay informes de error registrados.");
            ConfigureExplicitNavigation();
            FocusFirstAvailable();
            return;
        }

        // 4. Instanciar un botón numerado por cada archivo de error
        int reportIndex = 1;
        foreach (string file in reportFiles)
        {
            string fileContent = File.ReadAllText(file);

            // Instanciar el botón
            GameObject btnObj = Instantiate(reportButtonPrefab, listContainer);

            // Asegurar que el objeto del botón y su RectTransform tengan un tamaño mínimo
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.sizeDelta = new Vector2(btnRect.sizeDelta.x > 0 ? btnRect.sizeDelta.x : 250f, 40f);
            }

            // Buscar y configurar el TextMeshPro del botón
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                // Numerar el texto del botón de forma limpia
                btnText.text = $"Error #{reportIndex}";

                // Actualizado para evitar el aviso CS0618 en versiones recientes de TextMeshPro
                btnText.textWrappingMode = TextWrappingModes.NoWrap;
                btnText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform textRect = btnText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }
            }

            // Configurar Selectable / Button y colores de selección en rosa
            Button btn = btnObj.GetComponent<Button>();
            Selectable sel = btn != null ? (Selectable)btn : btnObj.GetComponent<Selectable>();

            if (sel != null)
            {
                ConfigureSelectablePink(sel);
                spawnedSelectables.Add(sel);
            }

            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    ShowErrorDetails(fileContent);
                });
            }

            reportIndex++;
        }

        // 5. Configurar navegación Explicit y foco inicial tras crear los botones
        ConfigureExplicitNavigation();
        RebuildLayoutAndFocusFirst();
    }

    private void ConfigureSelectablePink(Selectable sel)
    {
        if (sel == null) return;

        if (sel.transition == Selectable.Transition.ColorTint)
        {
            ColorBlock colors = sel.colors;
            colors.highlightedColor = selectedButtonColor;
            colors.selectedColor = selectedButtonColor;
            sel.colors = colors;
        }

        Graphic targetGraphic = sel.targetGraphic != null ? sel.targetGraphic : sel.GetComponent<Graphic>();
        if (targetGraphic != null && !originalGraphicColors.ContainsKey(targetGraphic))
        {
            originalGraphicColors[targetGraphic] = targetGraphic.color;
        }
    }

    private void UpdateSelectionTint(GameObject selectedObj)
    {
        ClearSelectionTint();

        if (selectedObj == null) return;

        Selectable sel = selectedObj.GetComponent<Selectable>();
        if (sel == null || !spawnedSelectables.Contains(sel)) return;

        Graphic targetGraphic = sel.targetGraphic != null ? sel.targetGraphic : sel.GetComponent<Graphic>();
        if (targetGraphic != null)
        {
            if (!originalGraphicColors.ContainsKey(targetGraphic))
            {
                originalGraphicColors[targetGraphic] = targetGraphic.color;
            }

            currentlyTintedGraphic = targetGraphic;
            currentlyTintedGraphic.color = selectedButtonColor;
        }
    }

    private void ClearSelectionTint()
    {
        if (currentlyTintedGraphic != null)
        {
            if (originalGraphicColors.TryGetValue(currentlyTintedGraphic, out Color baseColor))
            {
                currentlyTintedGraphic.color = baseColor;
            }
            currentlyTintedGraphic = null;
        }
    }

    private void ConfigureExplicitNavigation()
    {
        Selectable firstError = spawnedSelectables.Count > 0 ? spawnedSelectables[0] : null;

        if (mainMenuButton != null)
        {
            Navigation menuNav = mainMenuButton.navigation;
            menuNav.mode = Navigation.Mode.Explicit;
            menuNav.selectOnDown = firstError;
            menuNav.selectOnRight = firstError;
            mainMenuButton.navigation = menuNav;
        }

        for (int i = 0; i < spawnedSelectables.Count; i++)
        {
            Selectable current = spawnedSelectables[i];
            if (current == null) continue;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;

            nav.selectOnLeft = i > 0 ? spawnedSelectables[i - 1] : mainMenuButton;
            nav.selectOnRight = i < spawnedSelectables.Count - 1 ? spawnedSelectables[i + 1] : null;
            nav.selectOnUp = mainMenuButton;
            nav.selectOnDown = null;

            current.navigation = nav;
        }
    }

    private void RebuildLayoutAndFocusFirst()
    {
        RectTransform containerRect = listContainer as RectTransform;
        if (containerRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        FocusFirstAvailable();
    }

    private void FocusFirstAvailable()
    {
        if (EventSystem.current == null) return;

        Selectable target = null;
        for (int i = 0; i < spawnedSelectables.Count; i++)
        {
            if (spawnedSelectables[i] != null &&
                spawnedSelectables[i].gameObject.activeInHierarchy &&
                spawnedSelectables[i].interactable)
            {
                target = spawnedSelectables[i];
                break;
            }
        }

        if (target == null && mainMenuButton != null &&
            mainMenuButton.gameObject.activeInHierarchy &&
            mainMenuButton.interactable)
        {
            target = mainMenuButton;
        }

        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            lastSelectedObject = target.gameObject;
            UpdateSelectionTint(target.gameObject);

            RectTransform targetRect = target.GetComponent<RectTransform>();
            RectTransform containerRect = listContainer as RectTransform;
            if (targetRect != null && containerRect != null && targetRect.IsChildOf(containerRect))
            {
                ScrollToTarget(targetRect, containerRect);
            }
        }
    }

    private void ScrollToTarget(RectTransform targetRect, RectTransform containerRect)
    {
        if (scrollRect == null || scrollRect.viewport == null || targetRect == null || containerRect == null)
            return;

        float contentWidth = containerRect.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;
        float scrollableWidth = contentWidth - viewportWidth;

        if (scrollableWidth <= 0.01f)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            return;
        }

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(containerRect, targetRect);
        float targetCenterX = targetBounds.center.x - containerRect.rect.xMin;
        float desiredLeft = targetCenterX - (viewportWidth * 0.5f);

        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(desiredLeft / scrollableWidth);
    }

    void ShowErrorDetails(string details)
    {
        if (EventSystem.current != null)
        {
            buttonBeforeOpenPanel = EventSystem.current.currentSelectedGameObject;
        }

        if (viewerPanel != null)
        {
            viewerPanel.SetActive(true);
        }

        if (viewerTextContent != null)
        {
            viewerTextContent.text = details;
        }
    }

    public void CloseErrorDetails()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            if (buttonBeforeOpenPanel != null && buttonBeforeOpenPanel.activeInHierarchy)
            {
                Selectable sel = buttonBeforeOpenPanel.GetComponent<Selectable>();
                if (sel == null || sel.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(buttonBeforeOpenPanel);
                    lastSelectedObject = buttonBeforeOpenPanel;
                    UpdateSelectionTint(buttonBeforeOpenPanel);
                    return;
                }
            }

            FocusFirstAvailable();
        }
    }
}