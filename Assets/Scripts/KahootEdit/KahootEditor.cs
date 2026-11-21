using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KahootEditor : MonoBehaviour
{
    [Header("carpeta de datos")]
    [SerializeField] private string kahootsFolderName = "kahoots";

    [Header("datos generales del kahoot")]
    [SerializeField] private TMP_InputField inputKahootTitle;
    [SerializeField] private TMP_InputField inputDurationSeconds;
    [SerializeField] private TextMeshProUGUI textQuestionsCount;
    [SerializeField] private TextMeshProUGUI textQuestionIndex;

    [Header("formulario de pregunta")]
    [SerializeField] private TMP_InputField inputStatement;
    [SerializeField] private TMP_InputField[] inputOptions = new TMP_InputField[4];
    [SerializeField] private Toggle[] correctToggles = new Toggle[4];

    private readonly List<Question> editableQuestions = new List<Question>();
    private readonly Dictionary<Question, bool[]> correctFlagsByQuestion = new Dictionary<Question, bool[]>();

    private string originalFileName;
    private int currentQuestionIndex = -1;

    private void Start()
    {
        if (!KahootEditTransfer.HasKahootToEdit)
        {
            Debug.LogWarning("[KahootEditor] no se ha recibido ningun kahoot para editar.");
            UpdateCountersUI();
            return;
        }

        KahootData loadedKahoot = KahootEditTransfer.KahootToEdit;
        originalFileName = KahootEditTransfer.FileName;
        KahootEditTransfer.Clear();

        if (inputKahootTitle != null)
        {
            inputKahootTitle.text = !string.IsNullOrWhiteSpace(loadedKahoot.title)
                ? loadedKahoot.title
                : originalFileName;
        }

        editableQuestions.Clear();
        correctFlagsByQuestion.Clear();

        if (loadedKahoot.questions != null && loadedKahoot.questions.Count > 0)
        {
            if (inputDurationSeconds != null && loadedKahoot.questions[0].duration > 0f)
            {
                inputDurationSeconds.text = loadedKahoot.questions[0].duration.ToString();
            }

            foreach (Question q in loadedKahoot.questions)
            {
                editableQuestions.Add(q);

                int optionCount = q.options != null ? q.options.Length : 0;
                bool[] flags = new bool[optionCount];

                // cargamos todos los indices correctos si existen, o usamos correctOptionIndex como respaldo
                if (q.correctOptionIndices != null && q.correctOptionIndices.Length > 0)
                {
                    foreach (int idx in q.correctOptionIndices)
                    {
                        if (idx >= 0 && idx < optionCount)
                        {
                            flags[idx] = true;
                        }
                    }
                }
                else if (q.correctOptionIndex >= 0 && q.correctOptionIndex < optionCount)
                {
                    flags[q.correctOptionIndex] = true;
                }

                correctFlagsByQuestion[q] = flags;
            }

            currentQuestionIndex = 0;
            LoadQuestionIntoUI(0);
        }
        else
        {
            currentQuestionIndex = -1;
            ClearQuestionFields();
        }

        UpdateCountersUI();
        Debug.Log($"[KahootEditor] editando '{originalFileName}' ({editableQuestions.Count} preguntas).");
    }

    // pasa a la pregunta anterior de forma circular
    public void PreviousQuestion()
    {
        if (editableQuestions.Count == 0) return;

        int nextIndex = currentQuestionIndex - 1;
        if (nextIndex < 0)
            nextIndex = editableQuestions.Count - 1;

        LoadQuestionIntoUI(nextIndex);
        UpdateCountersUI();
    }

    // pasa a la siguiente pregunta de forma circular
    public void NextQuestion()
    {
        if (editableQuestions.Count == 0) return;

        int nextIndex = (currentQuestionIndex + 1) % editableQuestions.Count;
        LoadQuestionIntoUI(nextIndex);
        UpdateCountersUI();
    }

    // actualiza en memoria la pregunta activa con los datos del formulario
    public void UpdateCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= editableQuestions.Count)
        {
            Debug.LogWarning("[KahootEditor] no hay ninguna pregunta seleccionada para actualizar.");
            return;
        }

        if (!TryReadQuestionFromUI(out Question updatedQuestion, out bool[] flags, out string error))
        {
            Debug.LogWarning("[KahootEditor] " + error);
            return;
        }

        Question oldQuestion = editableQuestions[currentQuestionIndex];
        correctFlagsByQuestion.Remove(oldQuestion);

        editableQuestions[currentQuestionIndex] = updatedQuestion;
        correctFlagsByQuestion[updatedQuestion] = flags;

        UpdateCountersUI();
        Debug.Log($"[KahootEditor] pregunta {currentQuestionIndex + 1} actualizada en memoria.");
    }

    // anade la pregunta del formulario como una nueva pregunta al final de la lista
    public void AddAsNewQuestion()
    {
        if (!TryReadQuestionFromUI(out Question newQuestion, out bool[] flags, out string error))
        {
            Debug.LogWarning("[KahootEditor] " + error);
            return;
        }

        editableQuestions.Add(newQuestion);
        correctFlagsByQuestion[newQuestion] = flags;
        currentQuestionIndex = editableQuestions.Count - 1;

        ClearQuestionFields();
        UpdateCountersUI();
        Debug.Log($"[KahootEditor] pregunta anadida. total actual: {editableQuestions.Count}.");
    }

    // elimina la pregunta actual de la lista en memoria
    public void RemoveCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= editableQuestions.Count)
        {
            Debug.LogWarning("[KahootEditor] no hay ninguna pregunta para eliminar.");
            return;
        }

        Question toRemove = editableQuestions[currentQuestionIndex];
        correctFlagsByQuestion.Remove(toRemove);
        editableQuestions.RemoveAt(currentQuestionIndex);

        if (editableQuestions.Count > 0)
        {
            currentQuestionIndex = Mathf.Clamp(currentQuestionIndex, 0, editableQuestions.Count - 1);
            LoadQuestionIntoUI(currentQuestionIndex);
        }
        else
        {
            currentQuestionIndex = -1;
            ClearQuestionFields();
        }

        UpdateCountersUI();
        Debug.Log("[KahootEditor] pregunta eliminada. guarda el kahoot para conservar los cambios.");
    }

    // guarda el kahoot editado en su archivo json dentro de persistentDataPath/kahoots
    public void SaveEditedKahoot()
    {
        if (editableQuestions.Count == 0)
        {
            Debug.LogWarning("[KahootEditor] el kahoot debe tener al menos 1 pregunta antes de guardar.");
            return;
        }

        // volcamos la pregunta visible en pantalla por si se edito sin pulsar actualizar antes
        if (currentQuestionIndex >= 0 && currentQuestionIndex < editableQuestions.Count)
        {
            if (TryReadQuestionFromUI(out Question currentEdited, out bool[] flags, out _))
            {
                Question oldQuestion = editableQuestions[currentQuestionIndex];
                correctFlagsByQuestion.Remove(oldQuestion);
                editableQuestions[currentQuestionIndex] = currentEdited;
                correctFlagsByQuestion[currentEdited] = flags;
            }
        }

        float duration = GetConfiguredDuration();
        for (int i = 0; i < editableQuestions.Count; i++)
        {
            Question q = editableQuestions[i];
            q.duration = duration;

            if (q.correctOptionIndices == null || q.correctOptionIndices.Length == 0)
            {
                q.correctOptionIndices = new[] { q.correctOptionIndex };
            }
        }

        string newTitle = (inputKahootTitle != null && !string.IsNullOrWhiteSpace(inputKahootTitle.text))
            ? inputKahootTitle.text.Trim()
            : (!string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : "Kahoot_Editado");

        string targetFileName = !string.IsNullOrWhiteSpace(newTitle) ? newTitle : originalFileName;

        KahootData updatedData = new KahootData
        {
            title = newTitle,
            questions = new List<Question>(editableQuestions)
        };

        FileManager.SaveJson(kahootsFolderName, targetFileName, updatedData);
        originalFileName = targetFileName;

        Debug.Log($"[KahootEditor] kahoot '{targetFileName}' guardado correctamente en '{kahootsFolderName}'.");
    }

    // carga una pregunta concreta en los campos y toggles de la ui
    private void LoadQuestionIntoUI(int index)
    {
        currentQuestionIndex = Mathf.Clamp(index, 0, editableQuestions.Count - 1);
        Question q = editableQuestions[currentQuestionIndex];
        correctFlagsByQuestion.TryGetValue(q, out bool[] savedFlags);

        if (inputStatement != null)
        {
            inputStatement.text = q.statement ?? string.Empty;
        }

        for (int i = 0; i < inputOptions.Length; i++)
        {
            if (inputOptions[i] != null)
            {
                inputOptions[i].text = (q.options != null && i < q.options.Length)
                    ? q.options[i]
                    : string.Empty;
            }

            if (correctToggles != null && i < correctToggles.Length && correctToggles[i] != null)
            {
                bool isCorrect = false;

                if (savedFlags != null && i < savedFlags.Length)
                {
                    isCorrect = savedFlags[i];
                }
                else if (q.correctOptionIndices != null && q.correctOptionIndices.Length > 0)
                {
                    isCorrect = Array.IndexOf(q.correctOptionIndices, i) >= 0;
                }
                else
                {
                    isCorrect = (i == q.correctOptionIndex);
                }

                correctToggles[i].isOn = isCorrect;
            }
        }

        Debug.Log($"[KahootEditor] mostrando pregunta {currentQuestionIndex + 1} de {editableQuestions.Count}.");
    }

    // lee y valida enunciado, opciones y respuestas correctas desde la ui
    private bool TryReadQuestionFromUI(out Question question, out bool[] flags, out string errorMessage)
    {
        question = null;
        flags = null;
        errorMessage = string.Empty;

        string statement = inputStatement != null ? inputStatement.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(statement))
        {
            errorMessage = "el enunciado de la pregunta no puede estar vacio.";
            return false;
        }

        List<string> validOptions = new List<string>();
        List<bool> validFlags = new List<bool>();

        for (int i = 0; i < inputOptions.Length; i++)
        {
            if (inputOptions[i] != null && !string.IsNullOrWhiteSpace(inputOptions[i].text))
            {
                validOptions.Add(inputOptions[i].text.Trim());
                bool isChecked = correctToggles != null
                    && i < correctToggles.Length
                    && correctToggles[i] != null
                    && correctToggles[i].isOn;
                validFlags.Add(isChecked);
            }
        }

        if (validOptions.Count < 2)
        {
            errorMessage = "introduce al menos 2 respuestas posibles.";
            return false;
        }

        int correctCount = 0;
        List<int> selectedCorrectIndices = new List<int>();

        for (int i = 0; i < validFlags.Count; i++)
        {
            if (validFlags[i])
            {
                correctCount++;
                selectedCorrectIndices.Add(i);
            }
        }

        int maxAllowedCorrect = Mathf.Min(3, validOptions.Count - 1);
        if (correctCount < 1 || correctCount > maxAllowedCorrect)
        {
            errorMessage = $"selecciona entre 1 y {maxAllowedCorrect} respuestas correctas.";
            return false;
        }

        flags = validFlags.ToArray();
        question = new Question
        {
            statement = statement,
            options = validOptions.ToArray(),
            correctOptionIndex = selectedCorrectIndices.Count > 0 ? selectedCorrectIndices[0] : 0,
            correctOptionIndices = selectedCorrectIndices.ToArray(),
            duration = GetConfiguredDuration()
        };

        return true;
    }

    private float GetConfiguredDuration()
    {
        if (inputDurationSeconds != null && float.TryParse(inputDurationSeconds.text.Trim(), out float parsed))
        {
            return Mathf.Max(5f, parsed);
        }
        return 15f;
    }

    private void ClearQuestionFields()
    {
        if (inputStatement != null)
            inputStatement.text = string.Empty;

        for (int i = 0; i < inputOptions.Length; i++)
        {
            if (inputOptions[i] != null)
                inputOptions[i].text = string.Empty;

            if (correctToggles != null && i < correctToggles.Length && correctToggles[i] != null)
                correctToggles[i].isOn = false;
        }
    }

    private void UpdateCountersUI()
    {
        if (textQuestionsCount != null)
        {
            textQuestionsCount.text = $"Questions quantity : {editableQuestions.Count}";
        }

        if (textQuestionIndex != null)
        {
            textQuestionIndex.text = (editableQuestions.Count > 0 && currentQuestionIndex >= 0)
                ? $"Question {currentQuestionIndex + 1} / {editableQuestions.Count}"
                : "Question 0 / 0";
        }
    }
}