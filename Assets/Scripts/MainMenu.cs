using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string tutorialScene = "Tuto_1";
    [SerializeField] private string firstRoomScene = "Cave_1_1";

    private void Start()
    {
        Time.timeScale = 1f; 

        GameFlow.ResetGuards();
        if (PlayerController.Instance != null) Object.Destroy(PlayerController.Instance.gameObject);
        if (GameManager.Instance != null) Object.Destroy(GameManager.Instance.gameObject);
        if (PersistentHUD.Instance != null) Object.Destroy(PersistentHUD.Instance.gameObject);
    }

    public void OnPlayClicked() => GameFlow.StartNewGame(firstRoomScene);
    public void OnTutorialClicked() => GameFlow.StartNewGame(tutorialScene);
}