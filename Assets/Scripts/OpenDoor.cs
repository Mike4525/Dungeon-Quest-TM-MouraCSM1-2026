using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && PlayerController.Instance.pState.hasKey)
        {
            Destroy(gameObject);

            PlayerController.Instance.pState.hasKey = false;
        }
    }
}
