using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "Map";
    [SerializeField] private string uiSceneName = "UI";

    private void Start()
    {
        LoadIfNeeded(mapSceneName);
        LoadIfNeeded(uiSceneName);
    }

    private void LoadIfNeeded(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}