using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class KahootGameUI : MonoBehaviour
{
    [Header("ui elementos generales")]
    public TextMeshProUGUI textQuestionCount;
    public TextMeshProUGUI textQuestionTitle;
    public TextMeshProUGUI textTimer;
    public TextMeshProUGUI textScore;

    [Header("contenedor de botones")]
    public Transform optionsContainer;

    [Header("navegacion (new input system)")]
    [Tooltip("Control activo e interactuable al que pasar el foco mientras se bloquean las opciones (ej. Main Menu)")]
    public Selectable fallbackSelectableWhenLocked;
    [Tooltip("Numero de columnas de la cuadrícula de botones (2 para 2x2)")]
    public int gridColumns = 2;

    [Header("colores de feedback")]
    public Color colorCorrect = Color.green;
    public Color colorWrong = Color.red;
    public Color colorDefault = Color.white;

    private Button[] optionButtons;
    private TextMeshProUGUI[] optionButtonTexts;
    private Image[] optionButtonImages;

    // guardamos la pregunta activa para comprobar todas las opciones correctas
    private Question currentQuestion;

    void Awake()
    {
        CacheButtonReferences();
    }

    void Start()
    {
        SubscribeToManagerEvents();

        // sincronizacion manual por si el manager arranco antes que la ui
        if (KahootGameManager.Instance != null)
        {
            KahootGameManager.Instance.ForceRefreshUI();
        }
    }

    void OnDisable() => UnsubscribeFromManagerEvents();

    // cachea botones, textos e imagenes de las opciones
    void CacheButtonReferences()
    {
        if (optionsContainer == null) return;

        optionButtons = optionsContainer.GetComponentsInChildren<Button>(true);
        optionButtonTexts = new TextMeshProUGUI[optionButtons.Length];
        optionButtonImages = new Image[optionButtons.Length];

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtonTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            optionButtonImages[i] = optionButtons[i].GetComponent<Image>();
        }
    }

    // suscribe los eventos del game manager
    void SubscribeToManagerEvents()
    {
        if (KahootGameManager.Instance == null) return;

        KahootGameManager.Instance.OnQuestionLoaded -= HandleQuestionLoaded;
        KahootGameManager.Instance.OnQuestionLoaded += HandleQuestionLoaded;

        KahootGameManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
        KahootGameManager.Instance.OnScoreUpdated += HandleScoreUpdated;

        KahootGameManager.Instance.OnTimerUpdated -= HandleTimerUpdated;
        KahootGameManager.Instance.OnTimerUpdated += HandleTimerUpdated;

        KahootGameManager.Instance.OnAnswerEvaluated -= HandleAnswerEvaluated;
        KahootGameManager.Instance.OnAnswerEvaluated += HandleAnswerEvaluated;
    }

    // desuscribe los eventos al desactivar el objeto
    void UnsubscribeFromManagerEvents()
    {
        if (KahootGameManager.Instance == null) return;

        KahootGameManager.Instance.OnQuestionLoaded -= HandleQuestionLoaded;
        KahootGameManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
        KahootGameManager.Instance.OnTimerUpdated -= HandleTimerUpdated;
        KahootGameManager.Instance.OnAnswerEvaluated -= HandleAnswerEvaluated;
    }

    // actualiza enunciado, contador y botones al cargar una pregunta
    void HandleQuestionLoaded(Question question, int currentIndex, int totalCount)
    {
        currentQuestion = question;

        if (textQuestionTitle != null) textQuestionTitle.text = question.statement;
        if (textQuestionCount != null) textQuestionCount.text = $"Pregunta {currentIndex + 1} / {totalCount}";

        ConfigureOptionButtons(question);
    }

    // prepara los botones visibles con su texto, color inicial, listener y foco
    void ConfigureOptionButtons(Question question)
    {
        int activeCount = (question.options != null) ? Mathf.Min(question.options.Length, optionButtons.Length) : 0;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < activeCount)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;

                if (optionButtonImages[i] != null) optionButtonImages[i].color = colorDefault;
                if (optionButtonTexts[i] != null) optionButtonTexts[i].text = question.options[i];

                int choiceIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => KahootGameManager.Instance.AnswerQuestion(choiceIndex));
            }
            else
            {
                optionButtons[i].interactable = false;
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        ConfigureExplicitGridNavigation(activeCount);
        FocusFirstAvailableOption(activeCount);
    }

    // configura navegacion explicita entre los botones activos de la pregunta
    void ConfigureExplicitGridNavigation(int activeCount)
    {
        int cols = Mathf.Max(1, gridColumns);

        for (int i = 0; i < activeCount; i++)
        {
            Button current = optionButtons[i];
            if (current == null) continue;

            Navigation nav = current.navigation;
            nav.mode = Navigation.Mode.Explicit;

            int leftIndex = i - 1;
            int rightIndex = i + 1;
            int upIndex = i - cols;
            int downIndex = i + cols;

            nav.selectOnLeft = (leftIndex >= 0) ? optionButtons[leftIndex] : fallbackSelectableWhenLocked;
            nav.selectOnRight = (rightIndex < activeCount) ? optionButtons[rightIndex] : null;
            nav.selectOnUp = (upIndex >= 0) ? optionButtons[upIndex] : fallbackSelectableWhenLocked;
            nav.selectOnDown = (downIndex < activeCount) ? optionButtons[downIndex] : null;

            current.navigation = nav;
        }
    }

    // enfoca el primer boton de respuesta activo e interactuable
    void FocusFirstAvailableOption(int activeCount)
    {
        if (EventSystem.current == null) return;

        for (int i = 0; i < activeCount; i++)
        {
            Button btn = optionButtons[i];
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                return;
            }
        }

        EnsureFallbackFocus();
    }

    // mantiene el foco en un control activo cuando las opciones estan bloqueadas
    void EnsureFallbackFocus()
    {
        if (EventSystem.current == null) return;

        if (fallbackSelectableWhenLocked != null &&
            fallbackSelectableWhenLocked.gameObject.activeInHierarchy &&
            fallbackSelectableWhenLocked.interactable)
        {
            EventSystem.current.SetSelectedGameObject(fallbackSelectableWhenLocked.gameObject);
        }
    }

    // actualiza el marcador de puntuacion en pantalla
    void HandleScoreUpdated(int newScore)
    {
        if (textScore != null) textScore.text = $"Puntos: {newScore}";
    }

    // actualiza el contador de tiempo restante
    void HandleTimerUpdated(float remainingTime)
    {
        if (textTimer != null) textTimer.text = $"{remainingTime}s";
    }

    // bloquea botones, mantiene foco seguro y muestra el feedback visual al responder
    void HandleAnswerEvaluated(int selectedIndex, int correctIndex)
    {
        DisableAllButtons();
        EnsureFallbackFocus();
        HighlightAnswerFeedback(selectedIndex, correctIndex);
    }

    void DisableAllButtons()
    {
        foreach (var btn in optionButtons)
        {
            if (btn == null) continue;

            ColorBlock colors = btn.colors;
            Color disabled = colors.disabledColor;
            disabled.a = 1f;
            colors.disabledColor = disabled;
            btn.colors = colors;

            btn.interactable = false;
        }
    }

    // comprueba si un indice esta dentro de las respuestas correctas de la pregunta actual
    bool IsIndexCorrect(int index, int fallbackCorrectIndex)
    {
        if (currentQuestion != null && currentQuestion.correctOptionIndices != null && currentQuestion.correctOptionIndices.Length > 0)
        {
            return Array.IndexOf(currentQuestion.correctOptionIndices, index) >= 0;
        }

        return index == fallbackCorrectIndex;
    }

    // pinta todas las opciones correctas en verde y las incorrectas en rojo
    void HighlightAnswerFeedback(int selectedIndex, int correctIndex)
    {
        Color opaqueGreen = new Color(colorCorrect.r, colorCorrect.g, colorCorrect.b, 1f);
        Color opaqueRed = new Color(colorWrong.r, colorWrong.g, colorWrong.b, 1f);

        for (int i = 0; i < optionButtonImages.Length; i++)
        {
            if (optionButtonImages[i] == null || !optionButtonImages[i].gameObject.activeSelf) continue;

            bool isCorrect = IsIndexCorrect(i, correctIndex);
            optionButtonImages[i].color = isCorrect ? opaqueGreen : opaqueRed;
        }
    }
}