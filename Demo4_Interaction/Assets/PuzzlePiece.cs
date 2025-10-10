using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(XRGrabbable))]
//this class handles  the behavior of puzzle pieces, if any
public class PuzzlePiece : MonoBehaviour
{
    XRGrabbable grabbable;
    public List<Vector3Int> solution;
    public SolutionChecker solutionChecker;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabbable = GetComponent<XRGrabbable>();
        solutionChecker = GameObject.FindAnyObjectByType<SolutionChecker>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
       
        if(grabbable.grabbedBy != null)
        {
            //turn the target squares blue (horribly inefficient, because it does it every frame) 
            for (int i = 0; i < solution.Count; i++)
            {
                int x = solution[i].x;
                int y = solution[i].y;
                int z = solution[i].z;
                var g = x+""+y+""+z;
                solutionChecker.gridMap[g].GetComponent<Renderer>().material.color = Color.blue;
            }
        }
        
    }
}
