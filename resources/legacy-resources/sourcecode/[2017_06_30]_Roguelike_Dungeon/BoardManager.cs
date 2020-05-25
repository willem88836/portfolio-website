using UnityEngine;
using System;
using System.Collections.Generic; 		//Allows us to use Lists.
using Random = UnityEngine.Random; 		//Tells Random to use the Unity Engine random number generator.

/**
 * Editor: Willem Meijer
 * Studentcode: 343586
 * University: Hanze University of Applied Sciences
 */

/** Ways to improve!
 * Path Generation is not completely representative for the reference material.
 *      "Staircase paths" should be replaced with more straight pathways instead.
 * Merge spawning of monsters & players etc. into one script!
 * Monsters are sometimes spawned inside walls.
 * Inside MapCreation(), I am very positive the direction checking can be improved.
 * Sometimes door blocks incidentally spawn next to each other, this results in very large doors.
 * Refined Key & door instantiation (Using a dedicated system instead of random & everywhere).
 * 
 * Bug Fixing (Doors rotation)!
 */

namespace Completed
{
	public class BoardManager : MonoBehaviour
    {
        public static BoardManager instance;
        
        //Types of mapdata elements.
        const int wall = 0;
        const int path = 1;
        const int room = 2;

        const string loadingText = "Paving your way!";
        const string failureText = "No more maps can be generated, you have won!";

        [SerializeField]
        private bool randomSeed;
        [SerializeField]
        private int presetSeed;

        [Space]

        private int seed;                                               // A value that is used to determine random values.
        public int[,] MapData
        {
            get { return mapData; }
        }
        private int[,] mapData;                                          // The type of tiles will be placed on what location.
        private List<Vector2>[] potentialDoorIndex;                         // The tiles on which a door can be placed.
        private List<Vector2>[] roomsTiles;
        private int totalFloor = 0;
        private float difficultyFactor = 1.0f;
        private bool generationBreak = false;
        private int generationFailureIndex = 0;
        
        private Transform boardHolder;									//A variable to store a reference to the transform of our Board object.
        private Transform mapHolder;                                    //A variable to store a reference to the transform of our map object.
        private Transform debugHolder;                                  //A variable to store a reference to the transform of our debug object.
        private Transform monsterHolder;                                //A variable to store a reference to the transform of our monster object.
        private Transform playerHolder;                                 //A variable to store a reference to the transform of our player object.

        [SerializeField] private int mapWidth = 80;                     //Number of columns in our game board.
        [SerializeField] private int mapHeight = 60;                    //Number of rows in our game board.
        [Tooltip("It is recommended NOT to change this variable.")]
        [SerializeField]
        private int mapMultiplication = 2;                               //The amount with which the final amount of tiles that will be spawned is multiplied.
        [Tooltip("Please do not increase this number too much, this will cause Unity to crash.")]
        [SerializeField]
        private Vector2 _roomCount;                                      //The minimum and maximum percentage of rooms that can be spawned (relative to map size).
        private Count roomCount;                                         //The minimum and maximum amount of rooms that can be spawned.
        [SerializeField] private Count roomSize = new Count(4, 10);      //The minimum and maximum amount the size of a room can have.
        [SerializeField] private AnimationCurve doorCurve;               //Contains data of how many doors a room can have (relative to room size).
        [SerializeField] private int maxLoops;                           //A value that prevents map generation from crashing when unable to generate a map with the current settings.
        [SerializeField] private int maxGenerationFailures;              //A value that prevents the map generation from failing infinite times.
        [SerializeField] private int minDistance;                        //A value that contains the minimum distance the exit should have from the player's starting position.
        [SerializeField] private float doorSpawnChance;
        [SerializeField] private int lootSpawnMultiplication;
        [SerializeField] private int monsterSpawnMultiplication;
        [SerializeField] private int doorSpawnMultiplication;
        [SerializeField] private float difficultyIncreaseFactor;

        [Space]
        [Header("Objects")]
        [SerializeField]
		private GameObject exit;								        //Prefab to spawn for exit.
        [SerializeField]
        private GameObject[] floorTiles;								//Array of floor prefabs.
        [SerializeField]
        private GameObject[] wallTiles;									//Array of wall prefabs.
        [SerializeField]
        private GameObject[] lootTiles;									//Array of food prefabs.
        [SerializeField]
        private GameObject[] enemyTiles;								//Array of enemy prefabs.
        [SerializeField]
        private GameObject[] doorTiles;                                 //Array of door prefabs.
        [SerializeField]
        private GameObject[] outerWallTiles;							//Array of outer tile prefabs.

        public GameObject UIprefab;

        [Space]
        [Header("Game Data")]   
        public Vector2 spawnPoint;                                      //The point on which the players will be spawned.
        public Vector2 endPoint;                                        //The point on which the exit will be spawned.
        
        // Using Serializable allows us to embed a class with sub properties in the inspector.
        [System.Serializable]
        public class Count
        {
            public int minimum;             //Minimum value for our Count class.
            public int maximum;             //Maximum value for our Count class.


            //Assignment constructor.
            public Count(int min, int max)
            {
                minimum = min;
                maximum = max;
            }

            public void Adapt(float change)
            {
                minimum = (int)(minimum * change);
                maximum = (int)(maximum * change);
            }

        }
        
        /// <summary>
        /// Generates an X amount of seperate rooms.
        /// </summary>
        private void GenerateRooms()
        {
            int rooms = Random.Range(roomCount.minimum, roomCount.maximum); //Calculates the amount of rooms that will be spawned.
            potentialDoorIndex = new List<Vector2>[rooms]; //Contains which tiles will be considered to place doors on.
            roomsTiles = new List<Vector2>[rooms];
            for (int i = 0; i < rooms; i++)
            {
                int width = 0;
                int height = 0;
                Vector2 roomLocation = Vector3.zero;
                List<Vector2> doorPotential = new List<Vector2>(); //Stores all tiles that can be considered to place doors on.
                List<Vector2> changedTiles = new List<Vector2>(); //The tiles that have changed for this specific room.

                bool taken = true; //Determines whether the chosen location for this room is viable.
                for (int l = 0;  taken; l++)
                {
                    Random.InitState(seed * l * i * Random.Range(int.MinValue, int.MaxValue)); //To prevent repetition in randomisation each loop the seed of the Random class will be updated.
                    taken = false;
                    doorPotential.Clear(); //In case the lists are still filled they are cleared out.
                    changedTiles.Clear();
                    width = Random.Range(roomSize.minimum, roomSize.maximum); //The real roomsize is determined.
                    height = Random.Range(roomSize.minimum, roomSize.maximum);
                    roomLocation = new Vector2((int)Random.Range(0, mapWidth), Random.Range(0, mapHeight)); //The room location is determined.

                    //In case the current parameters cannot be used to generate a map an error is logged.
                    if (l > maxLoops)
                    {
                        //All data is logged (current room, the generation parameters, and what to do to solve it).
                        Debug.LogError("Can't place room! - Room: Loc(" + roomLocation + ") Size(" + width + ", " + height + ") at boundaries: Size(" + roomSize.minimum + ", " + roomSize.maximum + ") Count(" + roomCount.minimum + ", " + roomCount.maximum +")" + "\n" + "Lower room parameters in inspector!");
                        break;
                    }
                    //The representation of borders of the tile set that will be assessed on room and door viability.
                    //The borders have been enlarged to prevent rooms from spawning directly next to each other.
                    int borderLeft = (int)roomLocation.x - 1;
                    int borderRight = (int)roomLocation.x + width + 2;
                    int borderTop = (int)roomLocation.y + height + 2;
                    int borderBottom = (int)roomLocation.y - 1;
                    //Goes through each selected tile to check room and door viability.
                    for (int w = borderLeft; w < borderRight; w++)
                    {
                        if (borderRight < mapWidth && borderLeft > 0)
                        {
                            for (int h = borderBottom; h < borderTop; h++)
                            {
                                if (borderTop < mapHeight && borderBottom > 0)
                                {
                                    int tile = mapData[w, h]; //Represents the tile corrosponding with the current tile location.
                                
                                    Vector2 current = new Vector2(w, h); //Represents the two-dimensional version of the tile location.
                                    // if the current tile is on 0 or is already used the room will be considered non-viable, and therefore not applied (The index is checked for 0 to prevent world looping).
                                    if (tile == room || tile == path || h == 0 || w == 0)
                                    {
                                        taken = true;
                                        w = int.MaxValue - 10; //To make sure the loop will no longer continue.
                                        h = int.MaxValue - 10;
                                    }
                                    //Only changes when current tile is not part of the outer borders.
                                    else if (!(w == borderLeft|| w == borderRight - 1 || h == borderBottom || h == borderTop - 1))
                                    {
                                        changedTiles.Add(new Vector2(w,h));
                                    }
                                    //Only if the current tile belongs to the outer ring of the room in one axis and the inner ring in the other it is considered a potential spot for a door.
                                    else if (((w == borderLeft || w == borderRight - 1) && (h > borderBottom + 1 && h < borderTop - 2)) || ((h == borderBottom || h == borderTop - 1) && (w > borderLeft + 1 && w < borderRight - 2)))
                                    {
                                        doorPotential.Add(new Vector2(w,h));
                                    }
                                }
                                else { taken = true; }
                            }
                        }
                        else { taken = true; }
                    }
                }
                //After the current room is considered viable all changes will be applied to map data.
                potentialDoorIndex[i] = doorPotential;
                
                roomsTiles[i] = changedTiles;
                foreach (Vector2 tile in changedTiles)
                {
                    mapData[(int)tile.x, (int)tile.y] = room;
                    totalFloor++;
                }
            }
        }
        /// <summary>
        /// Generates a path between the created rooms.
        /// </summary>
        private void GeneratePath()
        {
            Tile[,] tiles = new Tile[mapWidth, mapHeight]; //re-stores all mapdata in a two-dimensional array.
            for (int w = 0; w < mapWidth; w++)
            {
                for (int h = 0; h < mapHeight; h++)
                {
                    tiles[w, h] = new Tile(new Vector2(w, h), mapData[w,h]);
                    if (tiles[w, h].type == path || tiles[w, h].type == room) totalFloor++;
                }
            }

            // The doors are stored twice to make sure all doors have been assessed at least once.
            List<Vector2> doorLocations = new List<Vector2>(); //Stores all locations on which a door will be placed.

            // Removes Surplus of potential door spots for each room.
            for (int i = 0; i < potentialDoorIndex.Length; i++)
            {
                int doors = (int)(doorSpawnMultiplication * doorCurve.Evaluate(potentialDoorIndex[i].Count / (/*Maximum room size*/roomSize.maximum * 4))); //Determines the amount of doors based on the room's size and the pre-determined curve.

                //Adds new doors to the list. 
                for (int o = 0; o < doors && potentialDoorIndex[i].Count > 0; o++)
                {
                    int index = Random.Range(0, potentialDoorIndex[i].Count);
                    Vector2 door = potentialDoorIndex[i][index];
                    doorLocations.Add(door);
                    mapData[(int)door.x, (int)door.y] = path;
                    totalFloor++;
                }
            }
            potentialDoorIndex = new List<Vector2>[1] { doorLocations }; //Repurposed the initial array for the global storage of the door indices.
            
            //Goes through each tile inside 'doorPositions' until all have been processed (this is to avoid doors from being not connected).
            //Using a simplified A* method a pathway is generated.
            for (int i = 0; i < doorLocations.Count; i++)
            {
                Vector2 tile = doorLocations[i];
                Tile start = tiles[(int)tile.x, (int)tile.y]; //The starting tile.
                Tile current; //The tile that currently is assessed.

                //A target and start tile are chosen.
                Tile target;
                do
                {
                    tile = potentialDoorIndex[0][Random.Range(0, potentialDoorIndex[0].Count)];
                    target = tiles[(int)tile.x, (int)tile.y]; //The target tile.
                } while (start.connection == target || target.connection == start || start == target);

                start.connection = target;

                Stack<Tile> openList = new Stack<Tile>(); //The list with tiles that will/can be assessed.
                Stack<Tile> closedList = new Stack<Tile>(); //The list with tiles that have already been assessed.

                openList.Push(start);
                current = start;

                Tile previousTile = new Tile(Vector2.zero, wall); //The the tile that has been previously assessed.

                Tile bestTile; //The tile the path will continue with.
                int loops = 0;
                bool finished = false; //Determines whether the path has been completed.
                do
                {
                    loops++;
                    bestTile = new Tile(Vector2.zero, wall);
                    bestTile.tWeight = int.MaxValue;
                    //All neighbouring tiles are assessed to continue the path with.
                    foreach (Tile neighbour in GetNeighbours(current, tiles))
                    {
                        neighbour.tWeight = Vector2.Distance(target.position, neighbour.position); 
                        //If the neighbouring tile is a path way the pathfinding process is stopped (to prevent pathways from being every where AND perfomance improvements).
                        if (neighbour.type == path && neighbour != previousTile)
                        {
                            bestTile = neighbour;
                            continue;
                        }
                        //Only if a tile is closer by than the current best tile, it is not the 0th tile, or is the target it wil be noted down as the best tile.
                        if (neighbour.tWeight < bestTile.tWeight && (neighbour.tWeight != 0 || neighbour == target) && neighbour != previousTile && neighbour.position != Vector2.zero && (neighbour.type != room))
                        {
                            bestTile = neighbour;
                        }
                    }
                    bestTile.type = path; //The type is set.
                    previousTile = current;
                    current = bestTile;

                    //Determines whether the path is finished.
                    finished = ((current.tWeight > 1 && mapData[(int)bestTile.position.x, (int)bestTile.position.y] != path) && current.position != Vector2.zero); 
                    mapData[(int)bestTile.position.x, (int)bestTile.position.y] = path; //The mapdata is updated.
                    if (!finished) totalFloor++;
                } while (finished && loops < maxLoops);
            }
        }
        /// <summary>
        /// Spawns a start and end point.
        /// </summary>
        private void GeneratePorts()
        {
            // Selects a starting point.
            Vector2 startPoint;
            bool accessible = true;
            do
            {
                startPoint = new Vector2(Random.Range(0, mapData.GetLength(0)), Random.Range(0, mapData.GetLength(1) / 4));
                accessible = true;
                for (int w = -2; w < 3; w++)
                {
                    for (int h = -2; h < 3; h++)
                    {
                        Vector2 position = startPoint + new Vector2(w, h);
                        if ((startPoint.x + w >= 1 && startPoint.y + h >= 1 && startPoint.x + w < mapData.GetLength(0) - 1 && startPoint.y + h < mapData.GetLength(1) - 1) && mapData[(int)position.x, (int)position.y] != room)
                            accessible = false;
                    }
                }
            } while (!accessible);
            spawnPoint = startPoint;

            Tile[,] tiles = new Tile[mapWidth, mapHeight]; //re-stores all mapdata in a two-dimensional array.
            for (int w = 0; w < mapWidth; w++)
            {
                for (int h = 0; h < mapHeight; h++)
                {
                    tiles[w, h] = new Tile(new Vector2(w, h), mapData[w, h]);
                }
            }

            // Inhere key, door, loot, exit and monster placement is determined. If needed, the map will be repaired as well.
            List<Vector2> potentialEndPoints = new List<Vector2>();
            Stack<Tile> openTiles = new Stack<Tile>(); // Contains all tiles that still need to be processed.
            List<Tile> closedTiles = new List<Tile>();
            int tilesProcessed = 0;
            bool complete = false; // determines whether all tiles have been assessed (if a tile cannot be reached this will remain false).
            int completionLoops = 0;
            while (!complete) // Continues until the entire map has been assessed.
            {
                completionLoops++;
                tilesProcessed = 0;
                closedTiles.Clear();
                openTiles.Push(new Tile(startPoint, mapData[(int)startPoint.x, (int)startPoint.y]));
                while (openTiles.Count > 0) //Continues until their are no more tiles to discover at this point.
                {
                    // processes the map using a*;
                    // In here: key, door, loot and monster placement will be determined based on the distance they have from the player, and whether the player can reach that point.
                    Tile current = openTiles.Pop();
                    closedTiles.Add(current);

                    List<Tile> neighbours = GetNeighbours(current, tiles);
                    for (int d = 0; d < neighbours.Count; d++)
                    {
                        // current is a path. if neighbour = path -> add; if neighbours = room, if current = doorindex -> add;
                        // current is a room. if neighbours = room -> add; if neighbours = path, if neighbour = doorindex -> add;
                        if (((current.type == path && (neighbours[d].type == path || potentialDoorIndex[0].Contains(current.position))) ||
                            (current.type == room && (neighbours[d].type == room || potentialDoorIndex[0].Contains(neighbours[d].position)))) && 
                            !closedTiles.Contains(neighbours[d]) && neighbours[d].type != wall)
                        {
                            openTiles.Push(neighbours[d]);
                        }
                    }
                    if (Vector2.Distance(startPoint, current.position) >= minDistance) potentialEndPoints.Add(current.position);
                    tilesProcessed++;
                }
                LoadingScreen.instance.UpdateLoadingScreen(loadingText, (tilesProcessed / (float)totalFloor));
                complete = (tilesProcessed >= totalFloor); // Only completes when just as many tiles have been assessed as there are floors + rooms.
                // if not all tiles have been processed, this means the map is split up, therefore a connection between the parts must be made.
                if (!complete)
                {
                    // Generates path between two pathways.
                    Tile start = new Tile(Vector2.zero, wall);
                    do
                    {
                        start = tiles[Random.Range(0, mapData.GetLength(0)), Random.Range(0, mapData.GetLength(1))];
                    } while (!closedTiles.Contains(start) || start.type != path);
                    
                    // Randomly selects a tile to start from.
                    // by spreading its range a tile will be selected to target.
                    Tile end = new Tile(start.position, wall);
                    do
                    {
                        end = tiles[Random.Range(0, mapData.GetLength(0)), Random.Range(0, mapData.GetLength(1))];
                    } while (closedTiles.Contains(end) || end.type != path);
                    
                    // Draws a path between the newly selected start and end position.
                    Tile current = start; //The tile that currently is assessed.
                    Tile previousTile = new Tile(Vector2.zero, wall); //The the tile that has been previously assessed.
                    Tile bestTile; //The tile the path will continue with.
                    bool finished = false; //Determines whether the path has been completed.
                    int pathLoops = 0;
                    do
                    {
                        bestTile = new Tile(Vector2.zero, wall);
                        bestTile.tWeight = int.MaxValue;
                        //All neighbouring tiles are assessed to continue the path with.
                        foreach (Tile neighbour in GetNeighbours(current, tiles))
                        {
                            neighbour.tWeight = Vector2.Distance(end.position, neighbour.position);
                            //Only if a tile is closer by than the current best tile, it is not the 0th tile, or is the target it wil be noted down as the best tile.
                            neighbour.tWeight = Vector2.Distance(neighbour.position, end.position);
                            if (neighbour.tWeight < bestTile.tWeight && ( neighbour.tWeight != 0 || neighbour == end) && neighbour != previousTile && neighbour.position != Vector2.zero && (neighbour.type != room))
                            {
                                bestTile = neighbour;
                            }
                        }
                        bestTile.type = path; //The type is set.
                        previousTile = current;
                        current = bestTile;

                        //Determines whether the path is finished.
                        finished = (current.tWeight > 1 && current.position != Vector2.zero);
                        mapData[(int)bestTile.position.x, (int)bestTile.position.y] = path; //The mapdata is updated.
                        tiles[(int)bestTile.position.x, (int)bestTile.position.y].type = path;
                        
                        if (!finished) totalFloor++;
                        pathLoops++;
                        if (pathLoops > maxLoops) { generationBreak = true; return; }
                    } while (finished);
                }
                if (completionLoops >= maxLoops) { generationBreak = true; return; }
            }
            
            Vector2[] directions = new Vector2[4] { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
            for (int i = 0; i < GameManagerNew.players.Length; i++)
            {
                if (GameManagerNew.players[i] != null)
                {
                    GameObject newCharacter = GameManagerNew.players[i];
                    newCharacter.GetComponent<PlayerTemp>().playerNr = i + 1;
                    newCharacter.transform.position = spawnPoint * mapMultiplication + directions[i];
                    GameManagerNew.gamePlayers[i] = (Instantiate(newCharacter));
                    GameManagerNew.gamePlayers[i].transform.SetParent(playerHolder);
                }
            }
            int endPointLoops = 0;
            do
            {
                endPoint = potentialEndPoints[Random.Range(0, potentialEndPoints.Count)];
                endPointLoops++;
                if (endPointLoops >= maxLoops) { generationBreak = true; return; }
            } while (GetAccessibleTile(null, new Tile(endPoint, mapData[(int)endPoint.x, (int)endPoint.y]), 3, new int[2] { path, wall }).position == endPoint);
            Instantiate(exit, endPoint * mapMultiplication, Quaternion.identity);
        }
        /// <summary>
        /// Spawns the provided item inside all rooms.
        /// </summary>
        /// <param name="objects"></param>
        private void SpawnObject(GameObject[] objects, int multiplication, Transform parent, int spawnAvoidRadius, int exitAvoidRadius)
        {
            for (int i = 0; i < roomsTiles.Length; i++)
            {
                int length = roomsTiles[i].Count;
                List<Vector2> positions = new List<Vector2>();
                Vector2 previousTile = roomsTiles[i][0];
                Vector4 borders = new Vector4(roomsTiles[i][0].x, roomsTiles[i][roomsTiles[i].Count - 1].x , roomsTiles[i][0].y, roomsTiles[i][roomsTiles[i].Count - 1].y); // xmin, xmax, ymin, ymax;
                for (int t = 0; t < length; t++)
                {
                    if (roomsTiles[i][t].x != borders.x && roomsTiles[i][t].y != borders.z && roomsTiles[i][t].x != borders.y && roomsTiles[i][t].y != borders.w)
                    {
                        positions.Add(roomsTiles[i][t]);
                    }
                }
                int spawners = (int)(multiplication * doorCurve.Evaluate(positions.Count / (/*Maximum room size*/roomSize.maximum * 4))); //Determines the amount of doors based on the room's size and the pre-determined curve.
                List<Vector2> usedPositions = new List<Vector2>();
                //Adds new doors to the list. 
                for (int o = 0; o < spawners && positions.Count > 0; o++)
                {
                    Vector2 position;
                    int loops = 0;
                    do
                    {
                        position = positions[Random.Range(0, positions.Count)];
                        loops++;
                    } while (loops < positions.Count && (Vector2.Distance(position, spawnPoint) <= spawnAvoidRadius || Vector2.Distance(position,endPoint) <= exitAvoidRadius));

                    if (loops >= positions.Count) break;
                    GameObject newObject = objects[Random.Range(0, objects.Length)];
                    newObject.transform.position = position * mapMultiplication;
                    (Instantiate(newObject)).transform.SetParent(parent);
                    roomsTiles[i].Remove(position);
                    positions.Remove(position);
                }
            }
        }
        
        private Tile GetAccessibleTile(Tile[,] tiles, Tile current, int range, int[] breakers)
        {
            Tile accessibleTile;
            bool accessible = true;
            do
            {
                accessible = true;
                Vector2 position = (current == null) ? new Vector2(Random.Range(0, mapWidth), Random.Range(0, mapHeight)) : current.position;
                accessibleTile = (tiles == null) ? 
                    (new Tile(position, mapData[(int)position.x, (int)position.y])) : 
                    (tiles[(int)position.x, (int)position.y]);

                for (int w = (int)position.x - range; w < (int)position.x + range; w++)
                {
                    for (int h = (int)position.y - range; h < (int)position.y + range; h++)
                    {
                        foreach (int item in breakers)
                        {
                            if ((w >= 0 || h >= 0 || w < mapData.GetLength(0) - 1 || h < mapData.GetLength(1) - 1) || 
                                ((/*there is no tilemap*/ tiles == null && mapData[w,h] == item) || 
                                (/*there is a tilemap*/ tiles != null && tiles[w,h].type == item)))
                            {
                                accessible = false;
                                break;
                            }
                        }
                        if (!accessible) break;
                    }
                    if (!accessible) break;
                }
            } while (!accessible && current == null);
            return ((current != null) ? ((accessible) ? accessibleTile : new Tile(Vector2.zero, wall)) : accessibleTile);
        }

        /// <summary>
        /// Instantiates the map.
        /// </summary>
        private void CreateMap()
        {
            for (int mw = 0; mw < mapData.GetLength(0); mw++)
            {
                for (int mh = 0; mh < mapData.GetLength(1); mh++)
                {
                    int doorLoc = (Random.Range(0f, 1f) < doorSpawnChance) ? Random.Range(0, 2) : 2; //A variable which determines on what tile a potential door will be instantiated.
                    for (int w = 0; w < mapMultiplication; w++)
                    {
                        for (int h = 0; h < mapMultiplication; h++)
                        {
                            //All checks on which can be determined what type of block should be instantiated.
                            Vector2[] directionsStraight = new Vector2[4] { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
                            Vector2[] directionDiagonal = new Vector2[4] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) };
                            //GameObject newTile;
                        
                            //Spawns a wall if this tile is neigbouring to a non wall block (this is done to prevent the screen from completely filling up with wallblocks.).
                            if (mapData[mw, mh] == wall)
                            {
                                Vector2 position = Vector2.zero;
                                for (int d = 0; d < directionsStraight.Length && position == Vector2.zero; d++)
                                {
                                    // NOTE: NEEDS TO BE IMPROVED TO EVERY MAP MULTIPLICATION VALUE!
                                    if ((/*BottomLeft*/ (w == 0 && h == 0) && (d == 0 || d == 3)) || (/*BottomRight*/ (w == 1 && h == 0) && (d == 2 || d == 3)) || (/*TopLeft*/ (w == 0 && h == 1) && (d == 0 || d == 1)) || (/*TopRight*/(w == 1 && h == 1) && (d == 1 || d == 2)))
                                        position = CheckNeighbourWall(new Vector2(mw, mh), new int[1] { path }, directionsStraight[d]);
                                }
                                for (int d = 0; d < directionDiagonal.Length && position == Vector2.zero; d++)
                                {
                                    if ((/*BottomLeft*/ (w == 0 && h == 0) && (d == 2)) || (/*BottomRight*/ (w == 1 && h == 0) && (d == 1)) || (/*TopLeft*/ (w == 0 && h == 1) && (d == 3)) || (/*TopRight*/(w == 1 && h == 1) && (d == 0)))
                                        position = CheckNeighbourWall(new Vector2(mw, mh), new int[1] { path }, directionDiagonal[d]);
                                }
                            
                                //If one of the neighbouring tiles is non-wall, a wall block will be instantiated.
                                if (position != Vector2.zero)
                                {
                                    SpawnTile(wallTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                }
                            }
                            //Spawns a floor tile.
                            else if (mapData[mw, mh] == path)
                            {
                                // Simply spawn a path.
                                SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                            }
                            //Spawns walls when neighbouring a path tile (with an exception to doors), and a floor tile when not (or when neighbouring a door).
                            else if (mapData[mw, mh] == room)
                            {
                                Vector2 position = Vector2.zero;
                                // For each tile all directions are checked.
                                for (int d = 0; d < directionsStraight.Length && position == Vector2.zero; d++)
                                {
                                    //Only in specific scenarios the neighbouring tiles are checked (to lower the amount of calculations that have to be done).
                                    if ((/*BottomLeft*/ (w == 0 && h == 0) && (d == 0 || d == 3)) || (/*BottomRight*/ (w == 1 && h == 0) && (d == 2 || d == 3)) || (/*TopLeft*/ (w == 0 && h == 1) && (d == 0 || d == 1)) || (/*TopRight*/(w == 1 && h == 1) && (d == 1 || d == 2)))
                                        position = CheckNeighbourWall(new Vector2(mw, mh), new int[2] { wall, path }, directionsStraight[d]);
                                }
                                //If there is a conflicting neighbouring tile special action will be taken.
                                if (position != Vector2.zero)
                                {
                                    Vector2 neighbouringDoor = Vector2.zero; 
                                    bool isDoor = false;
                                    for (int d = 0; d < directionsStraight.Length; d++)
                                    {
                                        if (potentialDoorIndex[0].Contains(new Vector2(mw,mh) + directionsStraight[d]))
                                        {
                                            isDoor = true;
                                            neighbouringDoor = directionsStraight[d];
                                            break;
                                        }
                                    }
                                    //If there is no neighbouring door a wall tile will be spawned.
                                    if (!isDoor)
                                    {
                                        SpawnTile(wallTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                    }
                                    //If there is a neighbouring door a floor tile will be spawned.
                                    else
                                    {
                                        if (doorLoc == 2)
                                        {
                                            SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                        }
                                        // Determines where a door will be spawned.
                                        else if ((neighbouringDoor == directionsStraight[0] /*Left*/ && w == 0) || 
                                            (neighbouringDoor == directionsStraight[2] /*Right*/ && w == 1)) 
                                        {
                                            if (h == doorLoc)
                                            {
                                                SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                                (SpawnTile(doorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder)).transform.rotation = Quaternion.Euler(0, 0, 90);
                                            }
                                            else
                                            {
                                                SpawnTile(wallTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                            }
                                        }
                                        else if ((neighbouringDoor == directionsStraight[1] /*Up*/ && h == 1) ||
                                            (neighbouringDoor == directionsStraight[3] /*Down*/ && h == 0))
                                        {
                                            if (w == doorLoc)
                                            {
                                                SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                                (SpawnTile(doorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder)).transform.rotation = Quaternion.Euler(0, 0, 0);
                                            }
                                            else
                                            {
                                                SpawnTile(wallTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                            }
                                        }
                                        else
                                        {
                                            SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                        }
                                    }
                                }
                                //If there is no conflicting neighbouring tile a floor tile will be spawned.
                                else
                                {
                                    SpawnTile(floorTiles, new Vector2(mw, mh) * mapMultiplication + new Vector2(w, h), mapHolder);
                                }
                            }
                        }
                    }
                }
            }
        }

        private GameObject SpawnTile(GameObject[] tiles, Vector2 position, Transform parent)
        {
            GameObject newTile = tiles[Random.Range(0, tiles.Length)];
            newTile.transform.position = position;
            (Instantiate(newTile)).transform.SetParent(parent);
            return newTile;
        }
        
        /// <summary>
        /// Returns a list of the neighbouring tiles (only returns tiles that do exist).
        /// </summary>
        List<Tile> GetNeighbours(Tile current, Tile[,] list)
        {
            Vector2 left = new Vector2(current.position.x - 1, (int)current.position.y);
            Vector2 right = new Vector2(current.position.x + 1, (int)current.position.y);
            Vector2 up = new Vector2(current.position.x, (int)current.position.y + 1);
            Vector2 down = new Vector2(current.position.x, (int)current.position.y - 1);

            List<Tile> tiles = new List<Tile>();

            if (left.x >= 0) tiles.Add(list[(int)left.x, (int)left.y]);
            if (right.x < mapWidth) tiles.Add(list[(int)right.x, (int)right.y]);
            if (down.y >= 0) tiles.Add(list[(int)down.x, (int)down.y]);
            if (up.y < mapHeight) tiles.Add(list[(int)up.x, (int)up.y]);

            return tiles;

        }
		/// <summary>
        /// Checks if there is a conflicting wall into one of the provided directions.
        /// </summary>
        private Vector2 CheckNeighbourWall(Vector2 index, int[] walls, Vector2 direction)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                Vector2 position2D = index + direction;
                if (position2D.x >= 0 && position2D.x < mapWidth && position2D.y >= 0 && position2D.y < mapHeight)
                {
                    Vector2 position = index + direction;
                    if (mapData[(int)position.x, (int)position.y] == walls[i])
                    {
                            return direction;
                    }
                }
            }
            return Vector2.zero;    
        }

        public void Reset()
        {
            difficultyFactor += difficultyIncreaseFactor;

            Destroy(mapHolder.gameObject);
            Destroy(debugHolder.gameObject);
            Destroy(monsterHolder.gameObject);
            Destroy(playerHolder.gameObject);
            Destroy(boardHolder.gameObject);
            Destroy(GameObject.Find("Exit(Clone)"));
            Debug.Log("");

        }

        private void UpdateDifficulty(int level)
        {
            difficultyFactor += level * difficultyIncreaseFactor;
            //float _difficultyFactor = Mathf.Pow(difficultyFactor, 2);
            //mapWidth = (int)(mapWidth + level * difficultyFactor) ;
            //mapHeight = (int)(mapHeight + level * difficultyFactor);
            //minDistance = (int)(minDistance + level * difficultyFactor);
            doorSpawnChance *= difficultyFactor;
            lootSpawnMultiplication = (int)(lootSpawnMultiplication / (difficultyFactor / level));
            monsterSpawnMultiplication = (int)(monsterSpawnMultiplication * (difficultyFactor / level));
        }
        
        private void Restart(int level)
        {
            generationFailureIndex++;
            if (generationFailureIndex >= maxGenerationFailures)
            {
                LoadingScreen.instance.UpdateLoadingScreen(failureText, 1);
                return;
            }
            else
            {
                LoadingScreen.instance.UpdateLoadingScreen(loadingText, 0);
                Reset();
                SetupScene(level);
            }
        }
        //SetupScene initializes our level and calls the previous functions to lay out the game board
        public void SetupScene(int level)
        {
            LoadingScreen.instance.UpdateLoadingScreen(loadingText, 0);

            UpdateDifficulty(level);
            instance = this;

            mapHolder = new GameObject("Map").transform;
            debugHolder = new GameObject("Debug").transform;
            monsterHolder = new GameObject("Monsters").transform;
            playerHolder = new GameObject("Players").transform;
            boardHolder = new GameObject("Objects").transform;
            
            //Sets the first seed.
            seed = (randomSeed || difficultyFactor != 1.0f) ? Random.Range(int.MinValue, int.MaxValue) : presetSeed;
            Random.InitState(seed);
            Debug.Log("Level: " + level + " - Difficulty: " + difficultyFactor +" - Seed: " + seed);
            
            //Sets the real map size.
            mapData = new int[mapWidth, mapHeight];
            int mapSize = mapWidth * mapHeight;
            roomCount = new Count((int)(mapSize * _roomCount.x), (int)(mapSize * _roomCount.y));
            
            // All the various procedures to generate a map.
            GenerateRooms();
            GeneratePath();
            GeneratePorts();
            if (generationBreak) { Restart(level); return; }
            SpawnObject(enemyTiles, monsterSpawnMultiplication, monsterHolder, 10, 0);
            SpawnObject(lootTiles, lootSpawnMultiplication, boardHolder, 1, 1);
            CreateMap();

            potentialDoorIndex = new List<Vector2>[0];
            roomsTiles = new List<Vector2>[0];
            Debug.Log("Generation finished: " + Time.realtimeSinceStartup);
        }

    }
}

public class Tile
{
    public Vector2 position;
    public float tWeight;
    public Tile parent;
    public int type;
    public Tile connection;

    public Tile(Vector2 _position, int _type)
    {
        position = _position;
        type = _type;
    }
}