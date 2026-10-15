using UnityEngine;

public class EnemyRoomTracker : MonoBehaviour
{
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform keySpawnPoint;

    private int alive;
    private bool keySpawned;

    private void Start()
    {
        var enemies = GetComponentsInChildren<Enemy>(true);
        alive = enemies.Length;

        if (alive == 0)
        {
            Debug.LogWarning("EnemyRoomTracker: no enemies found, so no key will ever spawn.", this);
            return;
        }

        foreach (var e in enemies) e.Died += OnEnemyDied;
    }

    private void OnEnemyDied(Enemy e)
    {
        e.Died -= OnEnemyDied;
        alive--;

        if (alive <= 0 && !keySpawned)
        {
            keySpawned = true;
            Instantiate(keyPrefab, keySpawnPoint.position, Quaternion.identity);
        }
    }
}