using UnityEngine;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour {

	public int width;
	public string seed;
	public bool useRandomSeed;

	[Range(0,100)]
	public int randomCaveSeedPercent;
	[Range(0, 10)]
	public int SmoothingIterations;

	public float heightScale = 1.0F;
	public float xScale = 1.0F;
	public float yNoise = 0f;
	public float groundDepth = 50.0f;
	public float hillDepth = 20.0f;
	public float skyDepth = 20.0f;
	public int height;

	public AnimationCurve contourCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
	private float GroundTop;
	public long heightSum;
	[Range(5, 10)]
	public int MinCountToFill = 7;
	[Range(0, 4)]
	public int MaxCountToEmpty = 2;

	int[,] map;
	int[] Topography;

	void Start() 
	{
		GenerateMap();
	}

	void Update() 
	{
		if (Input.GetMouseButtonDown(0)) 
		{
			GenerateMap();
		}
	}

	void GenerateMap() 
	{
		height = (int)(groundDepth + hillDepth + skyDepth);
		map = new int[width,height];
		Topography = new int[width];
		heightSum = 0;
		ClearMap ();
		//RandomFillMap();
		GenerateHills ();
		GenerateCaves ();

		for (int i = 0; i < SmoothingIterations; i ++) 
		{
			//SmoothMap();
			RandomSmooth();
		}
	}

	void GenerateHills()
	{

		if (useRandomSeed) 
		{
			seed = Time.time.ToString();
		}
		for(int x = 0;x < width;x++)
		{
			float xf = (float)x / (float)(width-1);
			//height will evaluate to a value ~ between 0 and 1
			GroundTop = contourCurve.Evaluate(xf) * heightScale * Mathf.PerlinNoise(xf * xScale, yNoise);
			//multiply by hillDepth to get a realistic range of hills
			GroundTop *= hillDepth;
			//Add a solid chunk of ground underneath the hills
			GroundTop += groundDepth;
			Topography[x] = (int)GroundTop;
			heightSum += Topography[x];
			for(int y = 0;y < height;y++)
			{
				if(y <= GroundTop)
				{
					map[x,y] = 1;
				}
				else
				{
					y = (int)height; 
				}
			}
		}
	}

	void ClearMap()
	{
		for (int x = 0; x < width; x ++) 
		{
			for (int y = 0; y < height; y ++) 
			{
				map[x,y] = 0;
			}
		}
	}

	void GenerateCaves()
	{
		if (useRandomSeed) 
		{
			seed = Time.time.ToString();
		}
		System.Random pseudoRandom = new System.Random(seed.GetHashCode());
		for (int x = 0; x < width; x ++) 
		{
			for (int y = 0; y < Topography[x]-1; y ++) //stops 1 block short of ground surface
			{
				if(map[x,y] != 0)
				{
					//should we seed a cave here?
					if(pseudoRandom.Next(0,100) < randomCaveSeedPercent)
					{
						map[x,y] = 0;
					}
				}
			}
		}
	}


	void SmoothMap() {
		for (int x = 0; x < width; x ++) {
			for (int y = 0; y < height; y ++) {
				int neighbourWallTiles = GetSurroundingWallCount(x,y);

				if (neighbourWallTiles > 4)
					map[x,y] = 1;
				else if (neighbourWallTiles < 4)
					map[x,y] = 0;

			}
		}
	}

	void RandomSmooth()
	{
		if (useRandomSeed) 
		{
			seed = Time.time.ToString();
		}
		System.Random pseudoRandom = new System.Random(seed.GetHashCode());
		for (long i=0; i<heightSum; i++) 
		{
			int x = pseudoRandom.Next(1, width - 1);
			int y = pseudoRandom.Next(1, Topography[x] - 2);
			int neighbourWallTiles = GetSurroundingWallCount(x,y);

			if (map[x,y] != 0) //this location is a wall
			{
				if(neighbourWallTiles < 7) // there is at least 2 spaces next to me
				{
					map[x,y] = 0;
				}
			}
			else //this block is a space
			{
				if (neighbourWallTiles == 8) // fill in isolated hole
				{
					map[x,y] = 1;
				}
			}
		}

	}

	int GetSurroundingWallCount(int gridX, int gridY) 
	{
		int wallCount = 0; //maximum possible  = 8
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX ++) 
		{
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY ++) 
			{
				if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height) //excludes edge tiles to prevent crashes
				{
					if (neighbourX != gridX || neighbourY != gridY) //do not count our own centre tile at X,Y
					{
						wallCount += map[neighbourX,neighbourY];
					}
				}
				else 
				{
					wallCount ++;
				}
			}
		}

		return wallCount;
	}


	void OnDrawGizmos() 
	{
		if (map != null) 
		{
			for (int x = 0; x < width; x ++) 
			{
				for (int y = 0; y < height; y ++) 
				{
					Gizmos.color = (map[x,y] == 1)?Color.black:Color.white;
					Vector3 pos = new Vector3(-width/2 + x + .5f,0, -height/2 + y+.5f);
					Gizmos.DrawCube(pos,Vector3.one);
				}
			}
		}
	}

}
