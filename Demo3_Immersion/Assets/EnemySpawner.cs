using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] Transform playerTarget; //where it'll shoot towards
    [SerializeField] GameObject enemyPrefab; //what it'll spawn
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(spawnEnemiesProcess());
    }

    IEnumerator spawnEnemiesProcess()
    {
        yield return new WaitForSeconds(3);
        while (true)
        {
            yield return new WaitForSeconds(1);

            GameObject newEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
            Rigidbody enemyRb = newEnemy.GetComponent<Rigidbody>();
            Vector3 towardsPlayer = (playerTarget.position - enemyRb.position).normalized; //get the unit vector towards the player
            enemyRb.linearVelocity = towardsPlayer * .5f;
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
