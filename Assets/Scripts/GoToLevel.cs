using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLevel : MonoBehaviour
{
    [SerializeField] string sceneName;

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
