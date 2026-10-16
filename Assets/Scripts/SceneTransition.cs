using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public enum DoorType { Left_Entry, Right_Exit }

    [Header("Door Settings")]
    [SerializeField] private DoorType doorType;

    [Header("Spawn Settings (Left_Entry doors only)")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Vector2 exitDirection;
    [SerializeField] private float exitTime;

    private bool transitioning = false;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        if (doorType == DoorType.Left_Entry && GameManager.Instance.transitionedFromScene == "Right_Exit_Triggered")
        {
            if (PlayerController.Instance != null && startPoint != null)
            {
                Rigidbody2D rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                PlayerController.Instance.transform.position = startPoint.position;
                PlayerController.Instance.ResetDashState();
                StartCoroutine(PlayerController.Instance.WalkIntoNewScene(exitDirection, exitTime));
            }
            GameManager.Instance.transitionedFromScene = "";
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (transitioning) return;

        if (doorType == DoorType.Right_Exit && _other.CompareTag("Player"))
        {
            if (PlayerController.Instance != null && PlayerController.Instance.pState.cutscene)
            {
                return;
            }

            transitioning = true;

            Rigidbody2D playerRb = _other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }

            PlayerController.Instance.ResetDashState();
            GameManager.Instance.transitionedFromScene = "Right_Exit_Triggered";
            PlayerController.Instance.pState.cutscene = true;

            // Track progress
            GameManager.Instance.roomsCleared++;
            string nextScene;

            if (GameManager.Instance.roomsCleared == 11)
            {
                nextScene = "Pre_Boss" + Random.Range(1, 4).ToString();
            }
            else if (GameManager.Instance.roomsCleared == 12)
            {
                nextScene = "Boss_Cave";
            }
            else
            {
                // Random room logic
                nextScene = "Cave_1_" + Random.Range(1, 4).ToString();
            }

            SceneManager.LoadScene(nextScene);
        }
    }
}