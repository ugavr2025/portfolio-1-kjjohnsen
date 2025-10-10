using System.Collections.Generic;
using UnityEngine;

public class SolutionChecker : MonoBehaviour
{
    public Dictionary<string,Transform> gridMap = new Dictionary<string,Transform>();
    public Transform[] solutionSpheres;
    public CubePuzzleGenerator puzzleGenerator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < solutionSpheres.Length; i++)
        {
            gridMap[solutionSpheres[i].name] = solutionSpheres[i].transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool allGood = true;
        //make sure all of the solution spheres are inside a puzzle piece cube
        for(int i = 0; i < solutionSpheres.Length; i++)
        {
            //should do a layer, but it's fine right now
            if (Physics.CheckSphere(solutionSpheres[i].position, 0,Physics.AllLayers,QueryTriggerInteraction.Ignore))
            {
                solutionSpheres[i].GetComponent<Renderer>().material.color= Color.green;
            }
            else
            {
				solutionSpheres[i].GetComponent<Renderer>().material.color = Color.red;
			}
        }
        
    }
}
