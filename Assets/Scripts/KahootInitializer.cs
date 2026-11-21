using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class KahootInitializer : MonoBehaviour
{
    void Awake()
    {
        CreateDefaultKahoots();
    }

    void CreateDefaultKahoots()
    {
        // carpeta donde se guardan los json por defecto en persistentDataPath
        string folderPath = Path.Combine(Application.persistentDataPath, "kahoots");

        // creamos la carpeta si aun no existe
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // kahoot de matematicas con 10 preguntas
        CreateKahootFile(folderPath, "Math_Challenge.json", "Math Challenge", new List<Question>
        {
            new Question { statement = "What is 5 + 5 * 2?", options = new string[] { "20", "15", "10", "25" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the square root of 81?", options = new string[] { "7", "8", "9", "6" }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "What is 12 divided by 4?", options = new string[] { "2", "3", "4", "6" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is 9 squared?", options = new string[] { "81", "72", "90", "99" }, correctOptionIndex = 0, duration = 15f },
            new Question { statement = "What is 15% of 200?", options = new string[] { "20", "25", "30", "35" }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "Solve for x: 2x + 4 = 10", options = new string[] { "2", "3", "4", "5" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the value of Pi (approx)?", options = new string[] { "3.14", "2.71", "1.41", "1.73" }, correctOptionIndex = 0, duration = 15f },
            new Question { statement = "How many degrees are in a triangle?", options = new string[] { "90", "180", "360", "270" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is 7 multiplied by 8?", options = new string[] { "54", "56", "64", "48" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is 100 minus 37?", options = new string[] { "63", "73", "53", "67" }, correctOptionIndex = 0, duration = 15f }
        });

        // kahoot de persona 5 con 10 preguntas
        CreateKahootFile(folderPath, "Persona_5_Quiz.json", "Persona 5 Quiz", new List<Question>
        {
            new Question { statement = "What is the name of Joker's initial Persona?", options = new string[] { "Captain Kidd", "Zorro", "Arsene", "Carmen" }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "What color is the Phantom Thieves' calling card?", options = new string[] { "Blue", "Red", "Black", "Gold" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the name of the talking cat in the group?", options = new string[] { "Morgana", "Teddie", "Cait Sith", "Sojiro" }, correctOptionIndex = 0, duration = 15f },
            new Question { statement = "By day, what is Joker's undercover high school?", options = new string[] { "Yenwood High", "Shujin Academy", "Gekkoukan High", "Yasogami High" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the codename of Ryuji Sakamoto?", options = new string[] { "Skull", "Panther", "Queen", "Fox" }, correctOptionIndex = 0, duration = 15f },
            new Question { statement = "Who is the genius hacker of the Phantom Thieves?", options = new string[] { "Makoto", "Haru", "Futaba", "Ann" }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "What is the name of the cafe where Joker lives?", options = new string[] { "Velvet Cafe", "Leblanc", "Central Cafe", "Blue Bird" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the codename of Makoto Niijima?", options = new string[] { "Noir", "Queen", "Oracle", "Panther" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "Who oversees the Velvet Room in Persona 5?", options = new string[] { "Igor", "Philemon", "Margaret", "Elizabeth" }, correctOptionIndex = 0, duration = 15f },
            new Question { statement = "What is Ann Takamaki's codename?", options = new string[] { "Skull", "Queen", "Panther", "Fox" }, correctOptionIndex = 2, duration = 15f }
        });

        // kahoot de ingles con 10 preguntas
        CreateKahootFile(folderPath, "English_Practice.json", "English Practice", new List<Question>
        {
            new Question { statement = "What is the past tense of 'go'?", options = new string[] { "Goed", "Went", "Gone", "Going" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "Translate to English: 'Biblioteca'", options = new string[] { "Bookstore", "Library", "Classroom", "Office" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "Which word is a synonym of 'Happy'?", options = new string[] { "Sad", "Joyful", "Angry", "Tired" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the plural form of 'Child'?", options = new string[] { "Childs", "Children", "Childes", "Childrens" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "Choose the correct spelling:", options = new string[] { "Recieve", "Receive", "Receiv", "Riciive" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the opposite of 'Ancient'?", options = new string[] { "Old", "Modern", "Far", "Heavy" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "Which one is a conjunction?", options = new string[] { "Quickly", "Because", "Table", "Run" }, correctOptionIndex = 1, duration = 15f },
            new Question { statement = "What is the past participle of 'Eat'?", options = new string[] { "Ate", "Eated", "Eaten", "Eating" }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "Choose the correct sentence:", options = new string[] { "She don't like apples.", "She doesn't likes apples.", "She doesn't like apples.", "She not like apples." }, correctOptionIndex = 2, duration = 15f },
            new Question { statement = "What does the idiom 'Piece of cake' mean?", options = new string[] { "Something very easy", "A delicious dessert", "A birthday party", "Something very difficult" }, correctOptionIndex = 0, duration = 15f }
        });
    }

    // genera el archivo json controlando posibles errores de escritura
    void CreateKahootFile(string folderPath, string fileName, string title, List<Question> questions)
    {
        string filePath = Path.Combine(folderPath, fileName);

        // si ya existe el archivo no lo sobreescribimos
        if (File.Exists(filePath))
        {
            return;
        }

        KahootData kahoot = new KahootData
        {
            title = title,
            questions = questions
        };

        try
        {
            string jsonContent = JsonUtility.ToJson(kahoot, true);
            File.WriteAllText(filePath, jsonContent);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear {fileName}: {e.Message}");
        }
    }
}