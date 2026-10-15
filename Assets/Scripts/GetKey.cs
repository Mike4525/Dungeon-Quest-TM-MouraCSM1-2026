using UnityEngine;

public class GetKey : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.Instance.pState.hasKey = true;

            Destroy(gameObject);
        }
    }
}
