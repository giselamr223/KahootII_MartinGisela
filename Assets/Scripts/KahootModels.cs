using System;
using System.Collections.Generic;

[System.Serializable]
public class Question
{
    public string statement;                  // Enunciat de la pregunta
    public string[] options;                  // Posibles respostes
    public int correctOptionIndex;            // Índex de la resposta correcta (0-3)
    public int[] correctOptionIndices = new int[0]; // Índexs de múltiples respostes correctes
    public float duration;                    // Duració de la pregunta en segons
}

[System.Serializable]
public class KahootData
{
    public string title;                      // Títol del kahoot
    public List<Question> questions = new List<Question>();
}