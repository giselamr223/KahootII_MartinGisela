using System;
using System.IO;
using System.Text;
using UnityEngine;

public class ExceptionLogger : MonoBehaviour
{
    // singleton para que solo exista un logger activo en toda la aplicacion
    private static ExceptionLogger instance;

    // evita que un error al guardar el informe vuelva a disparar el log
    private bool isWritingReport;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // mantiene el logger vivo al cambiar de escena
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // destruye duplicados si volvemos a la escena del menu
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // nos suscribimos a los logs de unity para capturar errores y excepciones
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        // desuscribimos al desactivar para evitar fugas o llamadas duplicadas
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (isWritingReport) return;

        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            SaveCleanErrorReport(logString, stackTrace, type);
        }
    }

    void SaveCleanErrorReport(string logString, string stackTrace, LogType type)
    {
        isWritingReport = true;
        try
        {
            // carpeta de informes dentro de persistentDataPath
            string folderPath = Path.Combine(Application.persistentDataPath, "ErrorReports");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // incluimos milisegundos para no pisar informes si ocurren dos errores seguidos
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string fileName = $"Error_{timestamp}.txt";
            string fullPath = Path.Combine(folderPath, fileName);

            string cleanStackTrace = CleanUpStackTrace(stackTrace);

            StringBuilder reportContent = new StringBuilder();
            reportContent.AppendLine("=== INFORME DE EXCEPCION ===");
            reportContent.AppendLine($"Archivo: {fileName}");
            reportContent.AppendLine($"Fecha y hora: {DateTime.Now}");
            reportContent.AppendLine($"Tipo: {type}");
            reportContent.AppendLine($"Mensaje: {logString}");
            reportContent.AppendLine("----------------------------------------");
            reportContent.AppendLine("Origen (Stack Trace limpio):");
            reportContent.AppendLine(cleanStackTrace);

            File.WriteAllText(fullPath, reportContent.ToString());
        }
        catch (Exception ex)
        {
            // si falla la escritura del txt, no dejamos que rompa el juego
            Debug.LogWarning("No se pudo guardar el informe de error: " + ex.Message);
        }
        finally
        {
            isWritingReport = false;
        }
    }

    string CleanUpStackTrace(string rawStackTrace)
    {
        if (string.IsNullOrEmpty(rawStackTrace)) return "No hay stack trace disponible.";

        StringBuilder cleanBuilder = new StringBuilder();
        string[] lines = rawStackTrace.Split('\n');

        foreach (string line in lines)
        {
            // filtramos lineas de unity y del sistema para quedarnos con nuestro codigo
            if (!string.IsNullOrWhiteSpace(line) &&
                !line.Contains("UnityEngine.") &&
                !line.Contains("UnityEditor.") &&
                !line.Contains("mscorlib") &&
                !line.Contains("System."))
            {
                cleanBuilder.AppendLine(line.Trim());
            }
        }

        return cleanBuilder.Length > 0 ? cleanBuilder.ToString() : rawStackTrace;
    }
}