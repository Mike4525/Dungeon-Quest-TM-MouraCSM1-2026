using UnityEngine;

public class Treasure : MonoBehaviour
{
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.gameObject.CompareTag("Player")) return;
        collected = true;

        EndScreen.Instance?.ShowWin();
        // later: particles, a sound, hide the chest sprite here
    }
}