using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class KahootScrollSelectionRouter : MonoBehaviour
{
    [Header("scrollview y ui")]
    [SerializeField] private Transform scrollContentParent;
    [SerializeField] private KahootEditListItem listItemPrefab;
    [SerializeField] private TextMeshProUGUI textFeedback;

    [Header("rutas y escenas")]
    [SerializeField] private string kahootsFolderName = "kahoots";
    [SerializeField] private string createSceneName = "06_KahootCreator";
    [SerializeField] private string editSceneName = "09_KahootEdit";

    private void Start()
    {
        RefreshKahootList();
    }

    // recarga la lista de kahoots disponibles desde la carpeta persistentDataPath/kahoots
    public void RefreshKahootList()
    {
        if (scrollContentParent == null || listItemPrefab == null)
            return;

        // limpiamos los elementos anteriores antes de generar los nuevos
        for (int i = scrollContentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(scrollContentParent.GetChild(i).gameObject);
        }

        string[] files = FileManager.GetAllJsonFiles(kahootsFolderName);
        if (files == null || files.Length == 0)
        {
            SetFeedback("no hay kahoots guardados en la carpeta.");
            return;
        }

        foreach (string filePath in files)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            KahootEditListItem item = Instantiate(listItemPrefab, scrollContentParent);
            item.Setup(nameWithoutExt, OnEditKahootSelected);
        }

        SetFeedback($"kahoots disponibles: {files.Length}");
    }

    // carga el json del kahoot elegido y salta a la escena de edicion
    private void OnEditKahootSelected(string selectedFileName)
    {
        try
        {
            KahootData loadedData = FileManager.LoadJson<KahootData>(kahootsFolderName, selectedFileName);
            bool hasContent = loadedData != null
                && (!string.IsNullOrWhiteSpace(loadedData.title) || (loadedData.questions != null && loadedData.questions.Count > 0));

            if (!hasContent)
            {
                string msg = $"no se pudo cargar '{selectedFileName}' o el archivo esta vacio/corrupto.";
                SetFeedback(msg);
                Debug.LogError($"[KahootScrollSelectionRouter] {msg} ({DateTime.Now})");
                return;
            }

            KahootEditTransfer.SetForEdit(loadedData, selectedFileName);
            SceneManager.LoadScene(editSceneName);
        }
        catch (Exception e)
        {
            string msg = $"error al abrir '{selectedFileName}' para editar: {e.Message}";
            SetFeedback(msg);
            Debug.LogError($"[KahootScrollSelectionRouter] {msg} ({DateTime.Now})");
        }
    }

    // limpia cualquier transferencia previa y abre la escena de creacion
    public void OpenCreateScene()
    {
        KahootEditTransfer.Clear();
        SceneManager.LoadScene(createSceneName);
    }

    private void SetFeedback(string message)
    {
        if (textFeedback != null)
            textFeedback.text = message;

        Debug.Log("[KahootScrollSelectionRouter] " + message);
    }
}