using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This script is responsible for displaying and updating the islands heat map. 
 * This is achieved by tracking all the character's location, and their presense in the world. 
 * The more characters are on one spot, the hotter the area will be.
 */

public class HeatMapUpdate : MonoBehaviour {
    
    [SerializeField]
    Material thisMaterial;
    [SerializeField]
    float minValue;

    bool isActive = false;
    bool isReady = false;
    public int state = 0;

    GameObject[,] currentMap;
    
	// Use this for initialization
	void Start ()
    {
        StartCoroutine(Delay());
	}

    /// <summary>
    /// Is delayed to make sure the map is completely instantiated.
    /// </summary>
    /// <returns></returns>
    IEnumerator Delay()
    {
        yield return new WaitForEndOfFrame();
        currentMap = new GameObject[MapSize._mapSize, MapSize._mapSize];
        foreach(GameObject block in GameObject.FindGameObjectsWithTag("HeatMap"))
        {
            currentMap[(int)block.transform.position.x, (int)block.transform.position.z] = block;
            block.SetActive(false);
        }
        isReady = true;
    }
	
    /// <summary>
    /// Updates the visual heat map.
    /// </summary>
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isActive = !isActive;
            foreach (GameObject item in currentMap)
            {
                item.SetActive(isActive);
            }
        }
		if (isReady)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                state--;
            if (Input.GetKeyDown(KeyCode.E))
                state++;

            if (HeatMap.newMap && isActive)
            {
                float[,] mapInfo = HeatMap.GetMap(state);
                for (int w = 0; w < MapSize._mapSize; w++)
                {
                    for (int h = 0; h < MapSize._mapSize; h++)
                    {
                        Color newColor = thisMaterial.color;
                        newColor.a = (mapInfo == null || !isActive || mapInfo[w,h] < minValue) ? 0 : (mapInfo[w, h] - minValue) * 2;
                        currentMap[w, h].GetComponent<Renderer>().material.color = newColor;
                    }
                }
            }
        }
	}
}
