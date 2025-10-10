using System.Collections.Generic;
using UnityEngine;

public class CubePuzzleGenerator : MonoBehaviour
{
	public float cubeSideLength = .02f;
	public int cubeSize = 3;                   // 3x3x3 puzzle
	public int targetPieces = 6;               // Aim for ~6 puzzle pieces
	public GameObject cubePrefab;              // Assign in Inspector (plain cube mesh)
	public Material baseMaterial;              // Base material (cloned per piece)
	public Transform[] spawnPoints;            // Assign 6 spawn locations in Inspector
	public Material outlineMat;		   // How pieces will be outlined
	private bool[,,] grid;                     // Occupied cubes
	private List<List<Vector3Int>> pieces;     // Final puzzle pieces
	public AudioClip hitClip;
	void Start()
	{
		//GeneratePuzzle();
	}

	public void GeneratePuzzle()
	{
		grid = new bool[cubeSize, cubeSize, cubeSize];
		pieces = new List<List<Vector3Int>>();

		// --- Step 1: Choose random seed positions ---
		List<Vector3Int> seeds = new List<Vector3Int>();
		for (int i = 0; i < targetPieces; i++)
		{
			Vector3Int pos;
			do
			{
				pos = new Vector3Int(
					Random.Range(0, cubeSize),
					Random.Range(0, cubeSize),
					Random.Range(0, cubeSize));
			}
			while (grid[pos.x, pos.y, pos.z]);

			seeds.Add(pos);
			grid[pos.x, pos.y, pos.z] = true;
			pieces.Add(new List<Vector3Int> { pos });
		}

		// --- Step 2: Region growing until full ---
		bool expanded = true;
		while (expanded)
		{
			expanded = false;
			for (int i = 0; i < pieces.Count; i++)
			{
				List<Vector3Int> candidates = new List<Vector3Int>();
				foreach (var pos in pieces[i])
				{
					foreach (var n in GetNeighbors(pos))
					{
						if (!grid[n.x, n.y, n.z])
							candidates.Add(n);
					}
				}

				if (candidates.Count > 0)
				{
					if (Random.value < 0.7f) // bias toward growing
					{
						Vector3Int chosen = candidates[Random.Range(0, candidates.Count)];
						grid[chosen.x, chosen.y, chosen.z] = true;
						pieces[i].Add(chosen);
						expanded = true;
					}
				}
			}
		}

		// --- Step 3: Attach leftovers ---
		for (int x = 0; x < cubeSize; x++)
		{
			for (int y = 0; y < cubeSize; y++)
			{
				for (int z = 0; z < cubeSize; z++)
				{
					if (!grid[x, y, z])
					{
						Vector3Int pos = new Vector3Int(x, y, z);
						int closest = 0;
						float bestDist = float.MaxValue;
						for (int i = 0; i < pieces.Count; i++)
						{
							foreach (var p in pieces[i])
							{
								float d = Vector3Int.Distance(p, pos);
								if (d < bestDist)
								{
									bestDist = d;
									closest = i;
								}
							}
						}
						pieces[closest].Add(pos);
						grid[x, y, z] = true;
					}
				}
			}
		}

		// --- Step 4: Instantiate puzzle pieces ---
		InstantiatePieces();
	}

	List<Vector3Int> GetNeighbors(Vector3Int pos)
	{
		List<Vector3Int> n = new List<Vector3Int>();
		Vector3Int[] dirs = {
			new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
			new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
			new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
		};

		foreach (var d in dirs)
		{
			Vector3Int np = pos + d;
			if (np.x >= 0 && np.x < cubeSize &&
				np.y >= 0 && np.y < cubeSize &&
				np.z >= 0 && np.z < cubeSize)
			{
				n.Add(np);
			}
		}
		return n;
	}

	void InstantiatePieces()
	{
		for (int i = 0; i < pieces.Count; i++)
		{
			var piece = pieces[i];

			// Parent object with Rigidbody and set up stuff for grabbing
			GameObject parent = new GameObject("Piece_" + i);
			var outline = parent.AddComponent<InvertedHullOutline>();
			outline.outlineMaterial = outlineMat;
			var grabbable = parent.AddComponent<XRGrabbable>();
			grabbable.outline = outline;
			grabbable.collisionSound = hitClip;
			var puzzlePiece = parent.AddComponent<PuzzlePiece>();
			puzzlePiece.solution = pieces[i];
			
			
			parent.transform.localScale = cubeSideLength * Vector3.one;
			Rigidbody rb = parent.GetComponent<Rigidbody>();
			rb.mass = piece.Count;

			// Assign random unique color
			Material pieceMat = new Material(baseMaterial);
			pieceMat.color = Random.ColorHSV(i / (float)pieces.Count, i/(float)pieces.Count, 0.7f, 1f, 0.7f, 1f);

			// Build cubes in local space (center around origin)
			Vector3 center = Vector3.zero;
			foreach (var pos in piece)
				center += (Vector3)pos;
			center /= piece.Count;

			foreach (var pos in piece)
			{
				Vector3 localPos = (Vector3)pos - center;
				GameObject cube = Instantiate(cubePrefab, parent.transform);
				cube.transform.localPosition = localPos;

				if (!cube.TryGetComponent<BoxCollider>(out _))
					cube.AddComponent<BoxCollider>();

				Renderer r = cube.GetComponent<Renderer>();
				if (r != null) r.material = pieceMat;

				if (cube.TryGetComponent<Rigidbody>(out Rigidbody childRb))
					Destroy(childRb);
			}

			// --- Place at spawn location with random rotation ---
			if (spawnPoints != null && spawnPoints.Length > 0)
			{
				Transform spawn = spawnPoints[i % spawnPoints.Length];
				parent.transform.position = spawn.position;
				parent.transform.rotation = Random.rotation;
			}
		}
	}
}
