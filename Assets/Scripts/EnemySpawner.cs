using System.Collections.Generic;
using UnityEngine;

// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviour
{
    private readonly List<Enemy> enemies = new List<Enemy>();

    public float damageMax = 40f;
    public float damageMin = 20f;
    public Enemy enemyPrefab;

    public float healthMax = 200f;
    public float healthMin = 100f;

    public Transform[] spawnPoints;

    public float speedMax = 12f;
    public float speedMin = 3f;

    public Color strongEnemyColor = Color.red;
    private int wave;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameover) return;
        
        if (enemies.Count <= 0) SpawnWave();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateWaveText(wave, enemies.Count);
    }
    
    private void SpawnWave()
    {
        wave++;

        var spawnCount = Mathf.RoundToInt(wave * 5f);

        for(var i=0; i<spawnCount; i++) {

            var enemyIntensity = Random.Range(0f, 1f);

            CreateEnemy(enemyIntensity);
        }
    }
    
    private void CreateEnemy(float intensity)
    {
        var health = Mathf.Lerp(healthMin, healthMax, intensity);
        var damage = Mathf.Lerp(damageMin, damageMax, intensity);
        var speed = Mathf.Lerp(speedMin, speedMax, intensity);

        var skinColor = Color.Lerp(Color.white, strongEnemyColor, intensity);

        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        var enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        enemy.Setup(health, damage, speed, speed * 0.3f, skinColor);

        enemies.Add(enemy);

        enemy.OnDeath += () => enemies.Remove(enemy);
        enemy.OnDeath += () => Destroy(enemy.gameObject, 10f);
        enemy.OnDeath += () => GameManager.Instance.AddScore(100);
    }
}