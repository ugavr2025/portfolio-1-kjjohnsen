using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VelNet;
[RequireComponent(typeof(XRGrabbable))]
//this class handles  the behavior of puzzle pieces, if any
public class PuzzlePiece : NetworkComponent,IPackState
{
    XRGrabbable grabbable;
    public SolutionChecker solutionChecker;
	public Material blockMaterial;
	public GameObject cubePrefab;
	//this is my state
	public List<Vector3Int> piece;
	public Color color;
	public float cubeSideLength;
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
            for (int i = 0; i < piece.Count; i++)
            {
                int x = piece[i].x;
                int y = piece[i].y;
                int z = piece[i].z;
                var g = x+""+y+""+z;
                solutionChecker.gridMap[g].GetComponent<Renderer>().material.color = Color.blue;
            }
        }
        
    }

    public void InitializePiece(List<Vector3Int> pieceData, Color c, float cubeSideLength)
    {
		this.piece = pieceData;
		this.color = c;
		this.cubeSideLength = cubeSideLength;
		// Assign random unique color
		Material pieceMat = new Material(blockMaterial);
		pieceMat.color = c;
		

		this.transform.localScale = cubeSideLength * Vector3.one;


		// Build cubes in local space (center around origin)
		Vector3 center = Vector3.zero;
		foreach (var pos in piece)
			center += (Vector3)pos;
		center /= piece.Count;

		foreach (var pos in piece)
		{
			Vector3 localPos = (Vector3)pos - center;
			GameObject cube = Instantiate(cubePrefab, this.transform);
			cube.transform.localPosition = localPos;

			if (!cube.TryGetComponent<BoxCollider>(out _))
				cube.AddComponent<BoxCollider>();

			Renderer r = cube.GetComponent<Renderer>();
			if (r != null) r.material = pieceMat;
		}

		Rigidbody rb = GetComponent<Rigidbody>();
		rb.mass = piece.Count;
	}

	public byte[] PackState()
	{
		MemoryStream ms = new();
		BinaryWriter bw = new(ms);
		bw.Write(piece.Count);
		for(int i=0; i<piece.Count; i++)
		{
			bw.Write(piece[i].x);
			bw.Write(piece[i].y);
			bw.Write(piece[i].z);	
		}
		bw.Write(color);
		bw.Write(cubeSideLength);
		return ms.ToArray();
	}

	public void UnpackState(byte[] state)
	{
		BinaryReader br = new(new MemoryStream(state));
		int N = br.ReadInt32();
		piece = new();
		for (int i = 0; i < N; i++)
		{
			piece.Add(new Vector3Int(br.ReadInt32(), br.ReadInt32(), br.ReadInt32()));
		}
		color = br.ReadColor();
		cubeSideLength = br.ReadSingle();
		InitializePiece(piece, color, cubeSideLength);
	}

	public override void ReceiveBytes(byte[] message)
	{
		throw new System.NotImplementedException();
	}
}
