using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KahootCreator : MonoBehaviour
{
    [Header("configuracion del kahoot")]
    [SerializeField] private string kahootsFolderName = "kahoots";
    [SerializeField] private TMP_InputField inputFieldKahootTitle;
    [SerializeField] private TMP_InputField inputFieldDuration;

    [Header("crear pregunta manual")]
    [SerializeField] private TMP_InputField inputQuestionStatement;
    [SerializeField] private TMP_InputField[] inputAnswerOptions = new TMP_InputField[4];
    [SerializeField] private Toggle[] optionCorrectToggles = new Toggle[4];

    [Header("contador de preguntas")]
    [SerializeField] private TextMeshProUGUI textQuestionsSummary;

    private readonly List<Question> questionsList = new List<Question>();
    private readonly Dictionary<Question, bool[]> questionCorrectFlagsMap = new Dictionary<Question, bool[]>();

    private void Start()
    {
        UpdateQuestionsCountUI();
    }

    // anade 1 pregunta manual, actualiza el contador y limpia el formulario
    public void AddQuestion()
    {
        string statement = inputQuestionStatement != null ? inputQuestionStatement.text.Trim() : "";
        if (string.IsNullOrEmpty(statement))
        {
            Debug.LogWarning("[KahootCreator] escribe el enunciado de la pregunta antes de anadirla.");
            return;
        }

        List<string> validOptions = new List<string>();
        List<bool> validCorrectFlags = new List<bool>();

        if (inputAnswerOptions != null)
        {
            for (int i = 0; i < inputAnswerOptions.Length; i++)
            {
                if (inputAnswerOptions[i] != null && !string.IsNullOrWhiteSpace(inputAnswerOptions[i].text))
                {
                    validOptions.Add(inputAnswerOptions[i].text.Trim());

                    bool isChecked = optionCorrectToggles != null
                        && i < optionCorrectToggles.Length
                        && optionCorrectToggles[i] != null
                        && optionCorrectToggles[i].isOn;

                    validCorrectFlags.Add(isChecked);
                }
            }
        }

        if (validOptions.Count < 2)
        {
            Debug.LogWarning("[KahootCreator] introduce al menos 2 respuestas posibles.");
            return;
        }

        List<int> correctIndices = new List<int>();
        for (int i = 0; i < validCorrectFlags.Count; i++)
        {
            if (validCorrectFlags[i])
            {
                correctIndices.Add(i);
            }
        }

        int maxAllowedCorrect = Mathf.Min(3, validOptions.Count - 1);

        if (correctIndices.Count < 1)
        {
            Debug.LogWarning("[KahootCreator] marca al menos 1 opcion como correcta.");
            return;
        }

        if (correctIndices.Count > maxAllowedCorrect)
        {
            Debug.LogWarning($"[KahootCreator] no pueden ser todas correctas ni mas de {maxAllowedCorrect}.");
            return;
        }

        float duration = GetConfiguredDuration();

        Question newQuestion = new Question
        {
            statement = statement,
            options = validOptions.ToArray(),
            correctOptionIndex = correctIndices[0],
            correctOptionIndices = correctIndices.ToArray(),
            duration = duration
        };

        questionsList.Add(newQuestion);
        questionCorrectFlagsMap[newQuestion] = validCorrectFlags.ToArray();

        ClearQuestionForm();
        UpdateQuestionsCountUI();
        Debug.Log($"[KahootCreator] pregunta anadida. total: {questionsList.Count}.");
    }

    // genera 10 preguntas preestablecidas y actualiza el contador
    public void GeneratePreset10Questions()
    {
        float duration = GetConfiguredDuration();

        List<Question> presetQuestions = new List<Question>
        {
            new Question { statement = "cual es el planeta mas grande del sistema solar?", options = new[] { "marte", "jupiter", "saturno", "venus" }, correctOptionIndex = 1, correctOptionIndices = new[] { 1 }, duration = duration },
            new Question { statement = "en que ano llego el ser humano a la luna?", options = new[] { "1965", "1969", "1972", "1959" }, correctOptionIndex = 1, correctOptionIndices = new[] { 1 }, duration = duration },
            new Question { statement = "que lenguaje se usa principalmente en unity para scripts?", options = new[] { "c#", "python", "php", "ruby" }, correctOptionIndex = 0, correctOptionIndices = new[] { 0 }, duration = duration },
            new Question { statement = "cual es la capital de cataluna?", options = new[] { "tarragona", "girona", "barcelona", "lleida" }, correctOptionIndex = 2, correctOptionIndices = new[] { 2 }, duration = duration },
            new Question { statement = "cuanto es 12 x 8?", options = new[] { "86", "92", "96", "104" }, correctOptionIndex = 2, correctOptionIndices = new[] { 2 }, duration = duration },
            new Question { statement = "que elemento quimico tiene el simbolo 'o'?", options = new[] { "oro", "oxigeno", "osmio", "oganeson" }, correctOptionIndex = 1, correctOptionIndices = new[] { 1 }, duration = duration },
            new Question { statement = "cual es el oceano mas grande de la tierra?", options = new[] { "atlantico", "indico", "artico", "pacifico" }, correctOptionIndex = 3, correctOptionIndices = new[] { 3 }, duration = duration },
            new Question { statement = "quien escribio 'don quijote de la mancha'?", options = new[] { "miguel de cervantes", "lope de vega", "federico garcia lorca", "perez galdos" }, correctOptionIndex = 0, correctOptionIndices = new[] { 0 }, duration = duration },
            new Question { statement = "cuantos dias tiene un ano bisiesto?", options = new[] { "365", "366", "364", "360" }, correctOptionIndex = 1, correctOptionIndices = new[] { 1 }, duration = duration },
            new Question { statement = "que componente ui de unity adapta el tamano de un contenedor?", options = new[] { "slider", "content size fitter", "toggle", "scrollbar" }, correctOptionIndex = 1, correctOptionIndices = new[] { 1 }, duration = duration }
        };

        questionsList.Clear();
        questionCorrectFlagsMap.Clear();

        for (int i = 0; i < presetQuestions.Count; i++)
        {
            Question q = presetQuestions[i];
            questionsList.Add(q);

            bool[] flags = new bool[q.options.Length];
            if (q.correctOptionIndex >= 0 && q.correctOptionIndex < flags.Length)
                flags[q.correctOptionIndex] = true;
            questionCorrectFlagsMap[q] = flags;
        }

        UpdateQuestionsCountUI();
        Debug.Log("[KahootCreator] se han cargado 10 preguntas preestablecidas.");
    }

    // guarda el kahoot con todas las preguntas en la carpeta configurada
    public void SaveKahoot()
    {
        if (questionsList.Count == 0)
        {
            Debug.LogWarning("[KahootCreator] anade al menos 1 pregunta antes de guardar.");
            return;
        }

        string kahootName = GetKahootTitle();
        float configuredDuration = GetConfiguredDuration();

        foreach (Question q in questionsList)
        {
            q.duration = configuredDuration;

            if (q.correctOptionIndices == null || q.correctOptionIndices.Length == 0)
            {
                q.correctOptionIndices = new[] { q.correctOptionIndex };
            }
        }

        KahootData newKahoot = new KahootData
        {
            title = kahootName,
            questions = new List<Question>(questionsList)
        };

        FileManager.SaveJson(kahootsFolderName, kahootName, newKahoot);
        Debug.Log($"[KahootCreator] kahoot '{kahootName}' guardado con {questionsList.Count} preguntas.");
    }

    public void ClearAllQuestions()
    {
        questionsList.Clear();
        questionCorrectFlagsMap.Clear();
        UpdateQuestionsCountUI();
        Debug.Log("[KahootCreator] preguntas borradas.");
    }

    private void UpdateQuestionsCountUI()
    {
        if (textQuestionsSummary != null)
            textQuestionsSummary.text = $"Questions quantity : {questionsList.Count}";
    }

    private string GetKahootTitle()
    {
        if (inputFieldKahootTitle != null && !string.IsNullOrWhiteSpace(inputFieldKahootTitle.text))
            return inputFieldKahootTitle.text.Trim();

        return "Kahoot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    private float GetConfiguredDuration()
    {
        if (inputFieldDuration != null && float.TryParse(inputFieldDuration.text.Trim(), out float parsed))
            return Mathf.Max(5f, parsed);

        return 15f;
    }

    private void ClearQuestionForm()
    {
        if (inputQuestionStatement != null) inputQuestionStatement.text = "";

        if (inputAnswerOptions != null)
        {
            for (int i = 0; i < inputAnswerOptions.Length; i++)
            {
                if (inputAnswerOptions[i] != null)
                    inputAnswerOptions[i].text = "";
            }
        }

        if (optionCorrectToggles != null)
        {
            for (int i = 0; i < optionCorrectToggles.Length; i++)
            {
                if (optionCorrectToggles[i] != null)
                    optionCorrectToggles[i].isOn = false;
            }
        }
    }
}