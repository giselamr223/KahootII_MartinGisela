using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("elementos de ui")]
    public Transform scoresContainer;       // contenedor content del scrollview vertical
    public TextMeshProUGUI textTitleLeaderboard;
    public TMP_Dropdown kahootDropdown;     // desplegable para elegir que kahoot consultar

    [Header("navegacion (new input system)")]
    public Selectable mainMenuButton;      // boton volver al menu principal (opcional)
    public Selectable firstSelectedOnStart;

    [Header("estilo de texto")]
    [Tooltip("fuente personalizada de textmeshpro")]
    public TMP_FontAsset customFont;
    [Tooltip("tamano de fuente para las filas del ranking")]
    public float customFontSize = 22f;
    [Tooltip("separacion vertical entre filas de la cola")]
    public float rowSpacing = 12f;

    [Header("carpetas de datos")]
    [SerializeField] private string kahootsFolderName = "kahoots";
    [SerializeField] private string leaderboardsFolderName = "Leaderboards";

    // asocia el nombre visible del kahoot con su ruta xml de resultados
    private Dictionary<string, string> kahootXmlFiles = new Dictionary<string, string>();
    private bool isInitialized = false;

    void Start()
    {
        SetupContainerLayout();
        LoadAvailableLeaderboards();
        ConfigureExplicitNavigation();
        SetInitialFocus();
    }

    // configura el vertical layout y el content size fitter para adaptar el scrollview
    void SetupContainerLayout()
    {
        if (scoresContainer == null) return;

        RectTransform containerRect = scoresContainer as RectTransform;
        if (containerRect == null || !scoresContainer.gameObject.activeInHierarchy) return;

        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);

        HorizontalLayoutGroup hGroup = scoresContainer.GetComponent<HorizontalLayoutGroup>();
        if (hGroup != null)
        {
            hGroup.enabled = false;
            Destroy(hGroup);
        }

        VerticalLayoutGroup vGroup = scoresContainer.GetComponent<VerticalLayoutGroup>();
        if (vGroup == null)
        {
            vGroup = scoresContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        if (vGroup != null)
        {
            vGroup.enabled = true;
            vGroup.childAlignment = TextAnchor.UpperLeft;
            vGroup.childControlWidth = true;
            vGroup.childControlHeight = false;
            vGroup.childForceExpandWidth = true;
            vGroup.childForceExpandHeight = false;
            vGroup.padding = new RectOffset(16, 16, 12, 12);
            vGroup.spacing = rowSpacing;
        }

        ContentSizeFitter fitter = scoresContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = scoresContainer.gameObject.AddComponent<ContentSizeFitter>();
        }

        if (fitter != null)
        {
            fitter.enabled = true;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // carga los kahoots disponibles y sus xml asociados para rellenar el desplegable
    void LoadAvailableLeaderboards()
    {
        if (kahootDropdown != null)
        {
            kahootDropdown.ClearOptions();
            kahootDropdown.onValueChanged.RemoveAllListeners();
        }

        kahootXmlFiles.Clear();

        string xmlFolder = Path.Combine(Application.persistentDataPath, leaderboardsFolderName);
        if (!Directory.Exists(xmlFolder))
        {
            Directory.CreateDirectory(xmlFolder);
        }

        string kahootsFolder = Path.Combine(Application.persistentDataPath, kahootsFolderName);
        if (!Directory.Exists(kahootsFolder))
        {
            Directory.CreateDirectory(kahootsFolder);
        }

        // primero registramos los kahoots existentes para poder ver su leaderboard aunque aun no tenga xml
        string[] jsonFiles = Directory.GetFiles(kahootsFolder, "*.json");
        foreach (string jsonFile in jsonFiles)
        {
            string baseName = Path.GetFileNameWithoutExtension(jsonFile);
            string displayName = baseName.Replace("_", " ");
            string expectedXmlPath = Path.Combine(xmlFolder, baseName + "_results.xml");

            if (!kahootXmlFiles.ContainsKey(displayName))
            {
                kahootXmlFiles[displayName] = expectedXmlPath;
            }
        }

        // anadimos cualquier xml en la carpeta de leaderboards (con o sin sufijo _results)
        string[] xmlFiles = Directory.GetFiles(xmlFolder, "*.xml");
        foreach (string xmlFile in xmlFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(xmlFile);
            string kahootName = fileName.EndsWith("_results", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - "_results".Length).Replace("_", " ")
                : fileName.Replace("_", " ");

            if (string.IsNullOrWhiteSpace(kahootName))
            {
                kahootName = fileName;
            }

            if (!kahootXmlFiles.ContainsKey(kahootName))
            {
                kahootXmlFiles[kahootName] = xmlFile;
            }
            else if (!File.Exists(kahootXmlFiles[kahootName]))
            {
                kahootXmlFiles[kahootName] = xmlFile;
            }
        }

        List<string> dropdownOptions = new List<string>(kahootXmlFiles.Keys);

        if (dropdownOptions.Count > 0)
        {
            if (kahootDropdown != null)
            {
                kahootDropdown.AddOptions(dropdownOptions);
                kahootDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }

            DisplayLeaderboard(dropdownOptions[0]);
            isInitialized = true;
        }
        else if (textTitleLeaderboard != null)
        {
            textTitleLeaderboard.text = "No hay leaderboards disponibles todavia.";
        }
    }

    // se ejecuta al cambiar la seleccion en el desplegable
    void OnDropdownValueChanged(int index)
    {
        if (!isInitialized) return;

        if (kahootDropdown != null && kahootDropdown.options.Count > index)
        {
            string selectedKahoot = kahootDropdown.options[index].text;
            DisplayLeaderboard(selectedKahoot);
        }
    }

    // muestra las puntuaciones ordenadas del kahoot seleccionado usando una cola de filas
    public void DisplayLeaderboard(string kahootName)
    {
        if (textTitleLeaderboard != null)
        {
            textTitleLeaderboard.text = $"Ranking: {kahootName}";
        }

        ClearRows();

        // si no existe el xml, mostramos leaderboard vacia sin lanzar error
        if (!kahootXmlFiles.ContainsKey(kahootName) || !File.Exists(kahootXmlFiles[kahootName]))
        {
            CreateStatusRow("Todavia no hay puntuaciones en este kahoot, se el primero.", Color.white, 20f);
            RebuildContainerLayout();
            return;
        }

        string filePath = kahootXmlFiles[kahootName];
        string xmlFileName = Path.GetFileName(filePath);

        try
        {
            LeaderboardList leaderboardList = null;
            XmlSerializer serializer = new XmlSerializer(typeof(LeaderboardList));

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                leaderboardList = (LeaderboardList)serializer.Deserialize(stream);
            }

            if (leaderboardList != null && leaderboardList.results != null && leaderboardList.results.Count > 0)
            {
                // ordenamos de mayor a menor puntuacion y encolamos para ir pintando en orden
                leaderboardList.results.Sort((a, b) => b.score.CompareTo(a.score));

                Queue<ResultData> rowsQueue = new Queue<ResultData>(leaderboardList.results);
                float rowHeight = Mathf.Max(42f, customFontSize + 14f);
                int queueIndex = 0;

                while (rowsQueue.Count > 0)
                {
                    ResultData result = rowsQueue.Dequeue();

                    string cleanUsername = result.username;
                    string lower = cleanUsername != null ? cleanUsername.Trim().ToLower() : "";
                    if (string.IsNullOrWhiteSpace(cleanUsername) || lower == "write your name..." || lower == "write your name")
                    {
                        cleanUsername = "Anonymous";
                    }

                    int rank = queueIndex + 1;
                    GameObject rowObj = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                    rowObj.transform.SetParent(scoresContainer, false);

                    RectTransform rowRect = rowObj.GetComponent<RectTransform>();
                    ConfigureQueuedRowRect(rowRect, queueIndex, rowHeight);

                    LayoutElement rowLayout = rowObj.GetComponent<LayoutElement>();
                    rowLayout.minHeight = rowHeight;
                    rowLayout.preferredHeight = rowHeight;
                    rowLayout.flexibleHeight = 0f;

                    TextMeshProUGUI rowTmp = rowObj.GetComponent<TextMeshProUGUI>();
                    if (rowTmp != null)
                    {
                        rowTmp.text = $"{rank}.  {cleanUsername}  -  {result.score} pts";
                        rowTmp.fontSize = customFontSize;
                        rowTmp.textWrappingMode = TextWrappingModes.NoWrap;
                        rowTmp.alignment = TextAlignmentOptions.Left;
                        rowTmp.color = Color.white;
                        if (customFont != null) rowTmp.font = customFont;
                    }

                    queueIndex++;
                }

                // aseguramos altura total del contenedor en cola aunque haya layout group
                RectTransform containerRect = scoresContainer as RectTransform;
                if (containerRect != null)
                {
                    float totalHeight = 24f + queueIndex * rowHeight + Mathf.Max(0, queueIndex - 1) * rowSpacing;
                    containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
                }
            }
            else
            {
                CreateStatusRow("El archivo de ranking esta vacio.", Color.white, 20f);
            }
        }
        catch (Exception e)
        {
            // registramos el error incluyendo archivo, fecha y hora para los informes de excepcion
            Debug.LogError($"Error al leer el XML '{xmlFileName}' ({DateTime.Now}): {e.Message}");
            CreateStatusRow("Error al cargar los datos del ranking", Color.red, 20f);
        }

        RebuildContainerLayout();
    }

    void ConfigureQueuedRowRect(RectTransform rect, int queueIndex, float height)
    {
        if (rect == null) return;

        float leftPadding = 16f;
        float rightPadding = 16f;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        // margen izquierdo y derecho limpios sin desplazar fuera del contenedor
        rect.offsetMin = new Vector2(leftPadding, 0f);
        rect.offsetMax = new Vector2(-rightPadding, 0f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        float yPos = -(12f + queueIndex * (height + rowSpacing));
        rect.anchoredPosition = new Vector2(leftPadding, yPos);
    }

    void RebuildContainerLayout()
    {
        if (scoresContainer == null) return;
        RectTransform rect = scoresContainer as RectTransform;
        if (rect != null && rect.gameObject.activeInHierarchy)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    // limpia las filas anteriores del contenedor
    void ClearRows()
    {
        if (scoresContainer == null) return;

        foreach (Transform child in scoresContainer)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // crea una fila informativa (leaderboard vacia o error de lectura)
    void CreateStatusRow(string message, Color color, float fontSize)
    {
        if (scoresContainer == null) return;

        float rowHeight = Mathf.Max(44f, fontSize + 14f);

        GameObject statusObj = new GameObject("StatusRow", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        statusObj.transform.SetParent(scoresContainer, false);

        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        ConfigureQueuedRowRect(statusRect, 0, rowHeight);

        LayoutElement statusLayout = statusObj.GetComponent<LayoutElement>();
        statusLayout.minHeight = rowHeight;
        statusLayout.preferredHeight = rowHeight;
        statusLayout.flexibleHeight = 0f;

        TextMeshProUGUI tmp = statusObj.GetComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = fontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        if (customFont != null) tmp.font = customFont;
    }

    void ConfigureExplicitNavigation()
    {
        if (mainMenuButton != null)
        {
            Navigation nav = mainMenuButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnDown = kahootDropdown;
            nav.selectOnRight = kahootDropdown;
            mainMenuButton.navigation = nav;
        }

        if (kahootDropdown != null)
        {
            Navigation nav = kahootDropdown.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = mainMenuButton;
            nav.selectOnLeft = mainMenuButton;
            kahootDropdown.navigation = nav;
        }
    }

    void SetInitialFocus()
    {
        if (EventSystem.current == null) return;

        Selectable target = firstSelectedOnStart != null
            ? firstSelectedOnStart
            : (kahootDropdown != null ? (Selectable)kahootDropdown : mainMenuButton);

        if (target != null && target.gameObject.activeInHierarchy && target.interactable)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}