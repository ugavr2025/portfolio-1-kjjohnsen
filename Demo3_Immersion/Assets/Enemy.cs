using UnityEngine;

public class Enemy : MonoBehaviour
{
    float lifeTimer = 0;
    public float maxLifeTime = 20;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifeTimer += Time.deltaTime;
        if(lifeTimer > maxLifeTime)
        {
            Destroy(gameObject);
        }
    }
}
