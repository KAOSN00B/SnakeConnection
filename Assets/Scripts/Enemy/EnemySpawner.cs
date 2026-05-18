using System;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    void Start()
    {
        SpawnEnemy();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SpawnEnemy()
    {
        
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            Transform spawnPoint = _spawnPoints[i];
            Instantiate(_enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

}
