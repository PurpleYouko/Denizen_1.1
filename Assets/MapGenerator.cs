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


	int[,] map;
	int[,] newMap;
	//MapCol[] map;
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
		//RandomFillMap();
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
					//map[x].Column[y] = 1;
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
				//map[x].Column[y] = 0;
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
			//for (int y = 0; y < map[x].Topography-1; y ++) //stops 1 block short of ground surface
			for (int y = 3; y < Topography[x]-5; y ++) //stops 5 block short of ground surface
			{
				//if(map[x].Column[y] != 0)
				if(map[x,y] != 0)
				{
					//should we seed a cave here?
					if(pseudoRandom.Next(0,100) < randomCaveSeedPercent)
					{
						//map[x].Column[y] = 0;
						map[x,y] = 0;
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
				if(x > 0 && x  < width -1 && y > 0 && y < Topography[x] - 5)  //do not process any cells that are out of bounds
				{
					if(map[x,y] != 0) //Location IS a wall. Uses != 0 rather than == 1 to allow for multiple values for walls in the future
					{
						if (neighbourWallTiles < DeathLimit)
						{
							//map[x].Column[y] = 0;
							newMap[x,y] = 0;
						}
						else
						{
							newMap[x,y] = 1;
						}
					}
					else  //Location IS NOT a wall
					{
						if (neighbourWallTiles > BirthLimit)  //Does it have enough neighbors to become a wall?
						{
							//map[x].Column[y] = 1;
							newMap[x,y] = 1;
						}
						else
						{
							newMap[x,y] = 0;
						}
					}
				}
				else //if out of bounds then just copy in values from map instead
				{
					newMap[x,y] = map[x,y];
				}
			}
		}
		//map = newMap;
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


	string GetCellValue(int gridX, int gridY)
	{
		string CellValue = "0";
		if (gridX > 1 && gridX < width - 2) 
		{
			if (gridY > 1 && gridY < Topography[gridX] - 5) //this logic might seem weird but we cannot check Topography unless we know X is within bounds first
			{
				{
					if (map [gridX, gridY] != 0) //it's a wall of some kind
					{ 
						CellValue = "1";
					}
				} 
			}
			else
			{
				CellValue = "1";  // if we are close to the sides, bottom or the surface then return a wall
			}
			//doesn't matter what X is if we are above the ground surface
		}
		else 
		{
			CellValue = "1"; // if we are close to the sides, bottom or the surface then return a wall
		}
		return CellValue;
	}


	void OnDrawGizmos() 
	{
		if (map != null) 
		{
			for (int x = 0; x < width; x ++) 
			{
				for (int y = 0; y < height; y ++) 
				{
					//Gizmos.color = (map[x].Column[y] == 1)?Color.black:Color.white;
					Gizmos.color = (map[x, y] == 1)?Color.black:Color.white;
					Vector3 pos = new Vector3(-width/2 + x + .5f, -height/2 + y+.5f,0 );
					Gizmos.DrawCube(pos,Vector3.one);
					//Gizmos.DrawIcon (pos, "Ground1.jpg", true);
				}
			}
		}
	}

}
