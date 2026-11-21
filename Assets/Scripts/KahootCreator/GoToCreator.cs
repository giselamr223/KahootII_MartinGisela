using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToCreator : MonoBehaviour
{
 public void ChangeToCreator()
    {
        SceneManager.LoadScene("06_KahootCreator");
    }
}
