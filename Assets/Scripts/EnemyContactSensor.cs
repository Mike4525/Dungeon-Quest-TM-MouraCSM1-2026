using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Debug
public class EnemyContactSensor : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        GetComponent<Collider2D>().isTrigger = true; // Debug
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (enemy == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        enemy.NotifyPlayerContact();
    }
}