using UnityEngine;
using UnityEngine.InputSystem;

// cierra el panel visor de excepciones y vuelve al menu de informes al pulsar escape
public class CloseViewerOnKey : MonoBehaviour
{
    [Header("referencias de paneles")]
    [SerializeField] private GameObject exceptionViewerPanel; // panel del visor detallado a cerrar
    [SerializeField] private GameObject mainExceptionsMenu;   // panel principal de listado de excepciones

    void Update()
    {
        // solo actuamos si el visor esta activo y se pulsa escape
        if (exceptionViewerPanel != null && exceptionViewerPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseViewer();
            }
        }
    }

    // metodo publico para cerrar el visor tambien desde un boton de ui si se desea
    public void CloseViewer()
    {
        if (exceptionViewerPanel != null)
            exceptionViewerPanel.SetActive(false);

        if (mainExceptionsMenu != null)
            mainExceptionsMenu.SetActive(true);
    }
}