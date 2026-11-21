using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ResultData
{
    public string username;
    public int score;
}

[XmlRoot("Leaderboard")]
public class LeaderboardList
{
    [XmlArray("Results")]
    [XmlArrayItem("Result")]
    public List<ResultData> results = new List<ResultData>();
}

public class KahootGameManager : MonoBehaviour
{
    public static KahootGameManager Instance { get; private set; }

    public event Action<Question, int, int> OnQuestionLoaded;
    public event Action<int> OnScoreUpdated;
    public event Action<float> OnTimerUpdated;
    public event Action<int, int> OnAnswerEvaluated;
    public event Action OnGameFinished;

    [Header("configuracion de carpeta y escenas")]
    [SerializeField] private string kahootsFolderName = "kahoots";
    [SerializeField] private string leaderboardsFolderName = "Leaderboards";
    [SerializeField] private string leaderboardSceneName = "04_Leaderboard";

    private KahootData currentKahoot;
    private int currentQuestionIndex = 0;
    private int score = 0;

    private float timeLeft;
    private bool isTimerActive = false;
    private bool isAnswering = false;
    private string currentUsername;
    private string kahootTitle;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        InitializeSessionData();
        StartGameSession();
    }

    void Update()
    {
        if (!isTimerActive) return;

        timeLeft -= Time.deltaTime;
        OnTimerUpdated?.Invoke(Mathf.Max(0, Mathf.Ceil(timeLeft)));

        if (timeLeft <= 0)
        {
            isTimerActive = false;
            OnTimeOut();
        }
    }

    // recupera el usuario y el kahoot seleccionado desde playerprefs
    void InitializeSessionData()
    {
        currentUsername = PlayerPrefs.GetString("CurrentUsername", "Anonymous");
        if (string.IsNullOrWhiteSpace(currentUsername) || currentUsername.ToLower().Contains("write your"))
        {
            currentUsername = "Anonymous";
        }

        kahootTitle = PlayerPrefs.GetString("CurrentKahootTitle", "").Trim();
        Debug.Log($"[GameManager] titulo recibido desde PlayerPrefs: '{kahootTitle}'");

        if (string.IsNullOrEmpty(kahootTitle))
        {
            string folderPath = Path.Combine(Application.persistentDataPath, kahootsFolderName);
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.json");
                if (files.Length > 0)
                {
                    kahootTitle = Path.GetFileNameWithoutExtension(files[0]).Replace("_", " ");
                    Debug.Log($"[GameManager] sin titulo previo, usando el primer kahoot disponible: '{kahootTitle}'");
                }
            }
        }
    }

    // inicia la sesion y muestra la primera pregunta si el kahoot es valido
    void StartGameSession()
    {
        LoadKahootData();

        if (IsKahootValid())
        {
            currentQuestionIndex = 0;
            score = 0;
            OnScoreUpdated?.Invoke(score);
            ShowCurrentQuestion();
        }
        else
        {
            Debug.LogError("No se han podido cargar las preguntas o el Kahoot no es valido.");
        }
    }

    bool IsKahootValid()
    {
        return currentKahoot != null && currentKahoot.questions != null && currentKahoot.questions.Count > 0;
    }

    // carga el json correspondiente al kahoot seleccionado
    void LoadKahootData()
    {
        string filePath = BuildKahootFilePath();

        if (!File.Exists(filePath))
        {
            string folderPath = Path.Combine(Application.persistentDataPath, kahootsFolderName);
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.json");
                foreach (string file in files)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file).Replace("_", " ").ToLower();
                    string searchTitle = kahootTitle.Replace("_", " ").ToLower();

                    if (fileNameWithoutExt.Contains(searchTitle) || searchTitle.Contains(fileNameWithoutExt))
                    {
                        filePath = file;
                        Debug.Log($"[GameManager] archivo encontrado por coincidencia flexible: {filePath}");
                        break;
                    }
                }
            }
        }

        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"No existe el JSON en: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);
            currentKahoot = JsonUtility.FromJson<KahootData>(jsonContent);

            if (currentKahoot == null || currentKahoot.questions == null || currentKahoot.questions.Count == 0)
            {
                Debug.LogWarning("[GameManager] JsonUtility dejo la lista de preguntas vacia. Aplicando banco de respaldo.");

                currentKahoot = new KahootData
                {
                    title = kahootTitle,
                    questions = new List<Question>
                    {
                        new Question { statement = "¿Cuanto es 2 + 2?", options = new string[] { "3", "4", "5", "6" }, correctOptionIndex = 1, duration = 15f },
                        new Question { statement = "¿Cual es la capital de Francia?", options = new string[] { "Londres", "Berlin", "Paris", "Madrid" }, correctOptionIndex = 2, duration = 15f },
                        new Question { statement = "¿De que color es el cielo despejado?", options = new string[] { "Azul", "Verde", "Rojo", "Amarillo" }, correctOptionIndex = 0, duration = 15f }
                    }
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al leer el JSON del juego: " + e.Message);
        }
    }

    string BuildKahootFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, kahootsFolderName);
        string safeFileName = string.Join("_", kahootTitle.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(folderPath, safeFileName + ".json");
    }

    // muestra la pregunta actual y activa su temporizador
    void ShowCurrentQuestion()
    {
        if (currentKahoot == null || currentQuestionIndex >= currentKahoot.questions.Count) return;

        isAnswering = false;
        Question currentQ = currentKahoot.questions[currentQuestionIndex];

        OnQuestionLoaded?.Invoke(currentQ, currentQuestionIndex, currentKahoot.questions.Count);

        timeLeft = currentQ.duration > 0 ? currentQ.duration : 15f;
        isTimerActive = true;
    }

    // refresca la ui si se suscribe tarde
    public void ForceRefreshUI()
    {
        if (IsKahootValid())
        {
            ShowCurrentQuestion();
            OnScoreUpdated?.Invoke(score);
        }
    }

    // comprueba si el indice elegido es correcto (soporta multiples respuestas correctas)
    bool IsAnswerCorrect(Question question, int selectedIndex)
    {
        if (question == null) return false;

        if (question.correctOptionIndices != null && question.correctOptionIndices.Length > 0)
        {
            return Array.IndexOf(question.correctOptionIndices, selectedIndex) >= 0;
        }

        return selectedIndex == question.correctOptionIndex;
    }

    // evalua la respuesta del jugador, suma puntos segun tiempo restante y lanza el feedback
    public void AnswerQuestion(int selectedIndex)
    {
        if (isAnswering) return;

        isTimerActive = false;
        isAnswering = true;

        Question currentQ = currentKahoot.questions[currentQuestionIndex];
        int correctIndex = currentQ.correctOptionIndex;

        if (IsAnswerCorrect(currentQ, selectedIndex))
        {
            int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, timeLeft) * 10f);
            score += 100 + timeBonus;
            OnScoreUpdated?.Invoke(score);
        }

        OnAnswerEvaluated?.Invoke(selectedIndex, correctIndex);

        StartCoroutine(WaitAndNextQuestion());
    }

    // gestiona el fin de tiempo sin respuesta
    void OnTimeOut()
    {
        if (isAnswering) return;
        isAnswering = true;

        Question currentQ = currentKahoot.questions[currentQuestionIndex];
        OnAnswerEvaluated?.Invoke(-1, currentQ.correctOptionIndex);

        StartCoroutine(WaitAndNextQuestion());
    }

    // espera brevemente para mostrar el feedback antes de pasar a la siguiente pregunta
    IEnumerator WaitAndNextQuestion()
    {
        yield return new WaitForSeconds(1.5f);

        currentQuestionIndex++;
        if (currentQuestionIndex < currentKahoot.questions.Count)
        {
            ShowCurrentQuestion();
        }
        else
        {
            FinishGame();
        }
    }

    // guarda el resultado en xml y pasa a la escena de leaderboard
    void FinishGame()
    {
        OnGameFinished?.Invoke();
        SaveLeaderboardResult();
        SceneManager.LoadScene(leaderboardSceneName);
    }

    // guarda o actualiza el archivo xml de puntuaciones del kahoot actual
    void SaveLeaderboardResult()
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, leaderboardsFolderName);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string safeFileName = string.Join("_", kahootTitle.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(folderPath, safeFileName + "_results.xml");

            LeaderboardList leaderboardList = new LeaderboardList();
            if (File.Exists(filePath))
            {
                XmlSerializer reader = new XmlSerializer(typeof(LeaderboardList));
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    leaderboardList = (LeaderboardList)reader.Deserialize(stream);
                }
            }

            leaderboardList.results.Add(new ResultData { username = currentUsername, score = score });

            XmlSerializer writer = new XmlSerializer(typeof(LeaderboardList));
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                writer.Serialize(stream, leaderboardList);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar el XML del Leaderboard: " + e.Message);
        }
    }
}