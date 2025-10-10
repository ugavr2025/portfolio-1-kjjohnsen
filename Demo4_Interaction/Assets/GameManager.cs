using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] CubePuzzleGenerator generator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartGame()
    {
        generator.GeneratePuzzle();
    }
}
