using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeMainMenu : MonoBehaviour
{
    /// <summary>
    /// este codigo únicamente será utilizado para el Prefab de Menú , y cambiar de escena
    /// de esta forma no tendremos que ir rehaciendolo
    /// </summary>
    public void ChangeToMainMenu()
    {
        SceneManager.LoadScene("01_MainMenu");
    }
}
