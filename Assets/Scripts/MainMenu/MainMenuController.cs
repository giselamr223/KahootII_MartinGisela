using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // abre la pantalla de informacion / about
    public void GoToAboutMe()
    {
        SceneManager.LoadScene("07_About");
    }

    // abre el selector de kahoots para jugar
    public void GoToKahootSelector()
    {
        SceneManager.LoadScene("02_KahootSelector");
    }

    // abre la escena de creacion y edicion de kahoots
    public void GoToKahootCreator()
    {
        SceneManager.LoadScene("08_Create_Edit");
    }

    // abre la pantalla de leaderboards generales
    public void GoToLeaderboards()
    {
        SceneManager.LoadScene("04_Leaderboard");
    }

    // abre el visor de informes de errores en txt
    public void GoToExceptionViewer()
    {
        SceneManager.LoadScene("05_ExceptionViewer");
    }

    // cierra la aplicacion
    public void QuitApp()
    {
        Debug.Log("Saliendo de la aplicacion...");
        Application.Quit();
    }
}