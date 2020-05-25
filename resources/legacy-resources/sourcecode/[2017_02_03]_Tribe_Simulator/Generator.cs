using UnityEngine;

/**
 * Author: Willem Meijer
 * Student Code: 343586
 * Class: CMV2A
 * Assignment: Final Assignment
 * Course: EGD2 - RST II
 * DoA: 03.02.17
 */

public class Generator : MonoBehaviour {
    private int[,] fieldMap = new int[0,0];
    private int mapSize;
    private string parentName;
    [SerializeField] private bool field;
    [SerializeField] private float yOffSet;
    [SerializeField] private float objectFrequency;
    public GameObject[] items;
    [SerializeField] private float[] objectValues;
    [SerializeField] private bool[] fieldRestrictions;

    void Start()
    {
        parentName = name;
        if (!field) fieldMap = GameObject.Find("Main Camera").GetComponent<MapSize>().TileMap;
        mapSize = GameObject.Find("Main Camera").GetComponent<MapSize>().mapSize;
        InstantiateMap(GenerateMap());
    }

    /// <summary>
    /// Generates a tilemap with info what should be placed where.
    /// </summary>
    private int[,] GenerateMap()
    {
        Vector2 tileMapSize = new Vector2(mapSize / objectFrequency, mapSize / objectFrequency);
        int[,] tileMap = new int[(int)(tileMapSize.x), (int)tileMapSize.y];
        float maxDistance = 0;
        for (int w = 0; w < (int)tileMapSize.x; w++)
        {
            for (int h = 0; h < (int)tileMapSize.y; h++)
            {
                tileMap[w, h] = -1;
                float distance = Vector2.Distance(new Vector2(mapSize / 2, mapSize / 2), new Vector2(w, h));
                if (distance > maxDistance) maxDistance = distance;
            }
        }

        for (int w = 0; w < tileMapSize.x; w++)
        {
            for(int h = 0; h < tileMapSize.y; h++)
            {
                if (field
                    || (fieldRestrictions[fieldMap[(int)Mathf.Floor(w * objectFrequency), (int)Mathf.Floor(h * objectFrequency)]]
                        && (!(Mathf.Ceil(w * objectFrequency) == mapSize || Mathf.Ceil(h * objectFrequency) == mapSize)
                            && fieldRestrictions[fieldMap[(int)Mathf.Ceil(w * objectFrequency), (int)Mathf.Ceil(h * objectFrequency)]]))
                   )
                {
                    float itemValue = (fieldMap.Length == 0) ? (Vector2.Distance(new Vector2(mapSize / 2, mapSize / 2), new Vector2(w, h)) / maxDistance) * 100 : Random.Range(0, 100);
                    bool valueFound = false;
                    for (int o = 0; o < objectValues.Length && !valueFound; o++)
                    {
                        if (o != objectValues.Length && itemValue >= objectValues[o])
                        {
                            tileMap[w, h] = o;
                            valueFound = true;
                        }
                        else
                        {
                            tileMap[w, h] = -1;
                        }
                    }
                }
            }
        }
        if (field) GameObject.Find("Main Camera").GetComponent<MapSize>().TileMap = tileMap;
        return tileMap;
    }

    /// <summary>
    /// Instantiates a tilemap
    /// </summary>
    /// <param name="tileMap"></param>
    private void InstantiateMap(int[,] tileMap)
    {
        Vector2 tileMapSize = new Vector2(mapSize / objectFrequency, mapSize / objectFrequency);
        // making sure all tiles are filled up (width).
        for (int w = 0; w < (int)tileMapSize.x; w++)
        {
            // making sure all tiles are filled up (height)
            for (int h = 0; h < (int)tileMapSize.y; h++)
            {
                bool objectFound = false;
                for (int o = 0; o < objectValues.Length && !objectFound; o++)
                {
                    if (tileMap[w, h] == o)
                    {
                        GameObject newTile = (Instantiate(items[o], new Vector3(w * objectFrequency, yOffSet, h * objectFrequency), Quaternion.identity) as GameObject);
                        newTile.transform.parent = GameObject.Find(parentName).transform;
                        newTile.name = items[o].name + " " + w + " " + h;
                        objectFound = true;
                    }
                }
            }
        }
        enabled = false;
    }
}