using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class FileManager
{
    // Guarda cualquier objeto serializable en JSON dentro de una carpeta específica
    public static void SaveJson<T>(string folderName, string fileName, T data)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        string filePath = Path.Combine(folderPath, safeFileName + ".json");

        string jsonString = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, jsonString);
    }

    // Carga un objeto JSON con seguridad y manejo de excepciones 
    public static T LoadJson<T>(string folderName, string fileName) where T : new()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        string safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        string filePath = Path.Combine(folderPath, safeFileName + ".json");

        if (!File.Exists(filePath))
        {
            return new T();
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            return JsonUtility.FromJson<T>(jsonContent);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al leer el archivo JSON en {filePath}: {e.Message}");
            return new T();
        }
    }

    // Obtiene todas las rutas de archivos JSON de una carpeta
    public static string[] GetAllJsonFiles(string folderName)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return new string[0];
        }
        return Directory.GetFiles(folderPath, "*.json");
    }
}