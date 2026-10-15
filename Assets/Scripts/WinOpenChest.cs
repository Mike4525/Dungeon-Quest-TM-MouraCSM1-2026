using System.Collections;
using UnityEngine;

public class WinOpenChest : MonoBehaviour
{
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerController.Instance.pState.hasKey == true)
        {
            anim.SetTrigger("Open");
            StartCoroutine(Win());
        }
    }

    private IEnumerator Win()
    {
        yield return new WaitForSecondsRealtime(2f);

        EndScreen.Instance.ShowWin();
    }
}
