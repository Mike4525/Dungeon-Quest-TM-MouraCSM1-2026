using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameFlow
{
    public const string MenuScene = "MainMenu";
    public static float RunStartTime;
    private static bool returning;
    public static void ResetGuards() => returning = false;

    public static void StartNewGame(string firstScene)
    {
        Time.timeScale = 1f;
        PersistentBootstrap.Ensure();

        GameManager.Instance.transitionedFromScene = "";
        var player = PlayerController.Instance;
        if (player != null)
        {
            player.Health = player.maxHealth;
            player.Mana = 0f;
            player.pState.hasKey = false;
            player.pState.lookingRight = true;
        }
        RunStartTime = Time.realtimeSinceStartup;

        SceneManager.LoadScene(firstScene);
    }

    public static void ReturnToMenu()
    {
        if (returning) return;
        returning = true;

        Time.timeScale = 1f;
        Debug.Log("[GameFlow] Returning to Main Menu. Loading scene...");

        SceneManager.LoadScene(MenuScene);
    }
}