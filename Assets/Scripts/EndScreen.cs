using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EndScreen : MonoBehaviour
{
    public static EndScreen Instance { get; private set; }

    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text statsText;

    [Header("Timing")]
    [SerializeField] private float fadeTime = 1.5f;
    [SerializeField] private float buttonDelay = 0.8f;

    [Header("Look")]
    [SerializeField] private Color loseTint = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color winTint = new Color(0.35f, 0.25f, 0.05f, 0.9f);

    private CanvasGroup group;
    private bool bound;

    private void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        // The player is spawned in the same frame as the HUD, so bind here, not in Awake
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.Died += ShowLoss;
            bound = true;
        }
    }

    private void OnDestroy()
    {
        if (bound && PlayerController.Instance != null)
            PlayerController.Instance.Died -= ShowLoss;
        if (Instance == this) Instance = null;
    }

    // A new scene (e.g. after Retry) always starts with the screen hidden
    private void OnSceneLoaded(Scene s, LoadSceneMode m) => HideImmediate();

    public void ShowLoss()
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine("YOU DIED", "The cave claims yet another adventurer", loseTint, false));
    }

    public void ShowWin()
    {
        PlayerController.Instance?.Freeze();
        StopAllCoroutines();
        StartCoroutine(ShowRoutine("TREASURE FOUND", "You escaped the cave", winTint, true));
    }

    public void OnRetryClicked() => GameFlow.StartNewGame("Cave_1_1");
    public void OnMenuClicked()
    {
        Debug.Log("[EndScreen] Menu button clicked.");
        GameFlow.ReturnToMenu();
    }

    private IEnumerator ShowRoutine(string title, string subtitle, Color tint, bool showStats)
    {
        EnsureEventSystem();

        titleText.text = title;
        subtitleText.text = subtitle;
        background.color = tint;
        statsText.text = showStats ? "Time  " + FormatTime(Time.realtimeSinceStartup - GameFlow.RunStartTime) : "Rooms cleared : roomsCleared";

        group.blocksRaycasts = true; // swallow clicks while visible
        yield return Fade(group, 1f, fadeTime);

        yield return new WaitForSecondsRealtime(buttonDelay); 

        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;
        yield return Fade(buttonsGroup, 1f, 0.4f);

        Time.timeScale = 0f; // freeze the world behind the screen
    }

    private IEnumerator Fade(CanvasGroup g, float to, float duration)
    {
        float from = g.alpha, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }

    private void HideImmediate()
    {
        StopAllCoroutines();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    // Debug (Buttons need an EventSystem, and the HUD outlives the scene's own one)
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}