using System.Collections;
using UnityEngine;

public class OpenChestWin : MonoBehaviour
{
    private Animator anim;
    private bool opened = false;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && PlayerController.Instance.pState.hasKey == true && !opened)
        {
            opened = true;
            anim.SetTrigger("Open");
            StartCoroutine(Win());
        }
    }

    private IEnumerator Win()
    {
        yield return new WaitForSecondsRealtime(1.5f);

        EndScreen.Instance.ShowWin();
    }
}
