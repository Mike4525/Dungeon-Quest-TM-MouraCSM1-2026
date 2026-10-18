using System.Collections.Generic;
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

        if (firstScene == "Tuto_1")
        {
            GameManager.Instance.roomsCleared = -1;
        }
        else
        {
            GameManager.Instance.roomsCleared = 0;
        }

        GameManager.Instance.sceneIndex = new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10" };

        SceneManager.LoadScene(firstScene);
    }

    public static void ReturnToMenu()
    {
        if (returning) return;
        returning = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }
}