using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExitTrigger : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level2";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLevelEndObject())
            return;

        if (other.CompareTag("Player"))
            SceneManager.LoadScene(nextSceneName);
    }

    private bool IsLevelEndObject()
    {
        return CompareTag("LevelEnd") || name.Contains("LevelEnd");
    }
}
