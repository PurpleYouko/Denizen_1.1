using UnityEngine;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour 
{

	public int width;
	private string seed;
	private bool useRandomSeed = true;

	[Range(0,100)]
	public int randomCaveSeedPercent = 50;
	[Range(0, 20)]
	public int SmoothingIterations = 5;

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


	[Range(0, 8)]
	public int BirthLimit = 4;		//if the number of walls around the selected blank point is high enough then fill it in
	[Range(0, 8)]
	public int DeathLimit = 5;		//if the number of walls around the selected non-blank point is not too high then kill it

	const int GROUND_SURFACE_BUFFER = 5;
	const int CAVE = 0;
	const int WALL = 1;				// might need to be expanded ot to allow for different wall types later

	// ToDo
	// Add a class MapCol so that each x in Map will be a MapCol object
	// Code has been partially added to some parts of the code base and commented out where already applied


	int[,] map;
	int[,] newMap;
	//MapCol[] map;		//Possible future code style.
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
		//map = new MapCol[width];
		map = new int[width,height];
		Topography = new int[width];
		heightSum = 0;
		ClearMap ();
		GenerateHills ();
		SeedCaves ();

		for (int i = 0; i < SmoothingIterations; i ++) 
		{
			SmoothMap();
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
			//map[x].Topography = (int)GroundTop;
			Topography[x] = (int)GroundTop;
			//heightSum += map[x].Topography;
			heightSum += Topography[x];
			for(int y = 0;y < height;y++)
			{
				if(y <= GroundTop)
				{
					//map[x].Column[y] = WALL;
					map[x,y] = WALL;
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
				map[x,y] = CAVE;
				//map[x].Column[y] = CAVE;
			}
		}
	}

	void SwapMaps()
	{
		for (int x = 0; x < width; x ++) 
		{
			for (int y = 0; y < height; y ++) 
			{
				map[x,y] = newMap[x,y];
			}
		}
	}

	void SeedCaves()	//generate a number of 'holes' in the ground where caves could form later
	{
		if (useRandomSeed) 
		{
			seed = Time.time.ToString();
		}
		System.Random pseudoRandom = new System.Random(seed.GetHashCode());
		for (int x = 1; x < width-1; x ++)  //Don't seed the edges
		{
			//for (int y = 0; y < map[x].Topography - GroundBuffer; y ++) //stops 1 block short of ground surface
			for (int y = 3; y < Topography[x] - GROUND_SURFACE_BUFFER; y ++) //stops 5 block short of ground surface
			{
				//if(map[x].Column[y] != CAVE)
				if(map[x,y] != CAVE)
				{
					//should we seed a cave here?
					if(pseudoRandom.Next(0,100) < randomCaveSeedPercent)
					{
						//map[x].Column[y] = CAVE;
						map[x,y] = CAVE;
					}
				}
			}
		}
	}


	void SmoothMap() 
	{
		newMap = new int[width,height];
		for (int x = 0; x < width; x ++) 
		{
			for (int y = 0; y < height; y ++) 
			{
				int neighbourWallTiles = GetSurroundingWallCount(x,y);
				if(x > 0 && x  < width -1 && y > 0 && y < Topography[x] - GROUND_SURFACE_BUFFER)  //do not process any cells that are out of bounds
				{
					if(map[x,y] != CAVE) //Location IS a wall. Uses != CAVE rather than == WALL to allow for multiple values for walls in the future
					{
						if (neighbourWallTiles < DeathLimit)
						{
							//map[x].Column[y] = CAVE;
							newMap[x,y] = CAVE;
						}
						else
						{
							newMap[x,y] = WALL;
						}
					}
					else  //Location IS NOT a wall
					{
						if (neighbourWallTiles > BirthLimit)  //Does it have enough neighbors to become a wall?
						{
							//map[x].Column[y] = WALL;
							newMap[x,y] = WALL;
						}
						else
						{
							newMap[x,y] = CAVE;
						}
					}
				}
				else //if out of bounds then just copy in values from map instead
				{
					newMap[x,y] = map[x,y];
				}
			}
		}
		//map = newMap;		//does this actually work? and if so is it faster than calling SwapMaps?
		SwapMaps ();
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
						//NOTE: This method only works if ALL walls are set to 1. If they are any other number it will return meaningless data
						//wallCount += map[neighbourX].Column[neighbourY];
						wallCount += map[neighbourX, neighbourY];
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
					Gizmos.color = (map[x, y] == 1)?Color.black:Color.white;
					Vector3 pos = new Vector3(-width / 2 + x + .5f, -height / 2 + y + .5f, 0 );
					Gizmos.DrawCube(pos,Vector3.one);
				}
			}
		}
	}

}
