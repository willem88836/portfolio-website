using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This script is responsible for most of each character's behaviour. 
 * This script contains the foundation for each character.
 * movement, health, Stats, trait, interaction, etc. 
 * Without this script, all traits would have to include all of this themselves.
 */

public class CharacterBehaviour : MonoBehaviour {

    private Color myColor;
    
    [SerializeField]
    float scareValue;
    public bool carryReturn;
    
    public Item 
        tool1,
        tool2,
        tool3;
    
    public enum Item { Ore, Wood, Stone, Food, WeaponT1, WeaponT2, WeaponT3, ToolT1, ToolT2, ToolT3, Nothing }
    public enum State { Working, Fighting, Resting, Retreating }
    
    public float interactSpeed;

    protected StatData myData;
    protected DefaultStatData defStats;

    protected bool moving = false;
    protected GameObject target;
    private List<GameObject> seenObjects = new List<GameObject>();
    
    private bool OutSideMap
    {
        get
        {
            int[,] map = GameObject.Find("Main Camera").GetComponent<MapSize>().TileMap;

            if (
                (transform.position.x < 0 || transform.position.x > MapSize._mapSize || transform.position.z < 0 || transform.position.z > MapSize._mapSize)
                ||
                map[((transform.position.x > MapSize._mapSize / 2) ?
                (int)Mathf.Floor(transform.position.x) : (int)Mathf.Ceil(transform.position.x)),
                ((transform.position.z > MapSize._mapSize / 2) ?
                (int)Mathf.Floor(transform.position.z) : (int)Mathf.Ceil(transform.position.z))]
                == (int)MapGenerator.GroundType.Water)
                return true;
            else 
                return false;
        }
    }
    
    /// <summary>
    /// Creates the stat values of a newly created character/trait.
    /// </summary>
    /// <param name="storedData"></param>
    public void NewTrait(StatData storedData)
    {
        defStats = transform.parent.GetComponent<DefaultStatData>();
        myData = storedData;
        if (storedData.EmptyData())
        {
            myData.NewData(transform.parent.gameObject);
            gameObject.GetComponent<SphereCollider>().radius = defStats.visionRange;
        }
        StartCoroutine(CheckStatus());
        myData.currentStateNo = (int)State.Working;
        if (myData.noTarget == null)
        {
            myData.noTarget = Instantiate(defStats.emptyObject);
            myData.noTarget.transform.parent = transform.parent;
            myData.noTarget.name = name + " Target";
        }
        SetHeldObject();
        myColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
    public StatData GetData()
    {
        return myData;
    }
    public void UpdateSeenObjects()
    {
        myData.seenObjects = seenObjects;
    }
    public void SetTrait(string traitName)
    {
        myData.trait = traitName;
        if (myData.trait != "NewChar") myData.traitObject.GetComponent<Renderer>().material = (Resources.Load<Material>("Trait/" + myData.trait + "M") as Material);
    }
    
    /// <summary>
    /// returns false if the item will exceed your maximum carrying capacity.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool CheckWeight(int item, int amount)
    {
        if (myData.carryWeight + (myData.inventory[(int)item] * defStats.objectWeight[(int)item]) > defStats.MaxCarryWeight)
        {
            return false;
        } else
        {
            return true;
        }
    }
    /// <summary>
    /// Removes all items of one type.
    /// returns amount of items that have been removed.
    /// </summary>
    /// <param name="item"></param>
    public int RemoveItem(Item item)
    {
        int itemAmount = myData.inventory[(int)item];
        myData.inventory[(int)item] = 0;
        UpdateCarryWeight();
        return itemAmount;
    }
    public int RemoveItem(int itemNo)
    {
        int itemAmount = myData.inventory[itemNo];
        myData.inventory[itemNo] = 0;
        UpdateCarryWeight();
        return itemAmount;
    }
    /// <summary>
    /// Removes 'amount' 'item' from inventory.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    public void ChangeItem(Item item, int amount)
    {
        myData.inventory[(int)item] += amount;
        if (myData.inventory[(int)item] < 0) myData.inventory[(int)item] = 0;
        UpdateCarryWeight();
    }
    public void ChangeItem(int item, int quantity)
    {
        myData.inventory[item] += quantity;
        if (myData.inventory[item] < 0) myData.inventory[item] = 0;
        UpdateCarryWeight();
    }
    /// <summary>
    /// Changes currently held object to desired object.
    /// Returns false when unavailable.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public void SetHeldObject()
    {
        if (myData.inventory[(int)tool1] != 0)
            myData.heldObjectNo = (int)tool1;
        else if (myData.inventory[(int)tool2] != 0)
            myData.heldObjectNo = (int)tool2;
        else if (myData.inventory[(int)tool3] != 0)
            myData.heldObjectNo = (int)tool3;
        else
            myData.heldObjectNo = (int)Item.Nothing;
    }
    /// <summary>
    /// Updates the weight of the currently held items.
    /// </summary>
    private void UpdateCarryWeight()
    {
        myData.carryWeight = 0;
        for (int i = 0; i < myData.inventory.Length; i++)
        {
            myData.carryWeight += myData.inventory[i] * defStats.objectWeight[i];
        }
    }
    public void UseEnergy()
    {
        myData.energy--;
    }

    /// <summary>
    /// Character deals damage.
    /// </summary>
    public void Attack()
    {
        target.GetComponent<CharacterBehaviour>().Damage(defStats.objectDamage[myData.heldObjectNo], gameObject);
        UseEnergy();
    }
    /// <summary>
    /// Receives damage.
    /// </summary>
    /// <param name="damage"></param>
    public void Damage(int damage, GameObject Enemy)
    {
        if (target == null) target = Enemy;
        if (Enemy.tag != "Tribesman") myData.currentStateNo = (int)State.Fighting;
        myData.health -= damage;
        UseEnergy();
    }

    /// <summary>
    /// Checks the character's left health and energy.
    /// </summary>
    /// <returns></returns>
    public IEnumerator CheckStatus()
    {
        while (true)
        {
            if (Random.Range(0, 100) > defStats.energyChance && myData.energy > 0) myData.energy--;
            if (Random.Range(0, 100) > defStats.libidoChance) myData.libido++;
            if (myData.currentStateNo != (int)State.Retreating && (myData.energy <= 3 || myData.health <= 3 || myData.libido >= defStats.MaxLibido || (myData.carryWeight >= defStats.MaxCarryWeight && !carryReturn)))
            {
                myData.currentStateNo = (int)State.Retreating;
                StopAllCoroutines();
                SetTarget(defStats.returnPoint);
                StartCoroutine(CheckStatus());
                SingleCharacter("Returning");
            }
            if (myData.energy <= 0)
                myData.health--;
            
            if (myData.health <= 0 || OutSideMap)
            {
                //Debug.Log(name + " Died");
                SingleCharacter("DIED");
                GetTownHall.population--;
                Destroy(myData.noTarget);
                Destroy(gameObject);
            }
            yield return new WaitForSeconds(1);
        }
    }
    
    /// <summary>
    /// Sets a new Target.
    /// </summary>
    /// <param name="newTarget"></param>
    public void SetTarget(GameObject newTarget)
    {
        StopCoroutine(Movement());
        target = newTarget;
        StartCoroutine(Movement());
    }
    public void SetTarget(string targetName)
    {
        StopCoroutine(Movement());
        foreach (GameObject item in GameObject.FindGameObjectsWithTag(targetName))
        {
            if (item.GetComponent<DataStorage>().tribe == myData.tribe)
            {
                SetTarget(item);
                return;
            }
        }
    }
    public void SetTarget(Vector3 newTarget)
    {
        StopCoroutine(Movement());
        myData.noTarget.transform.position = newTarget;
        SetTarget(myData.noTarget);
    }
    public void RandomTarget()
    {
        StopCoroutine(Movement());
        Vector3 newTarget = Vector3.zero;
        int[,] map = GameObject.Find("Main Camera").GetComponent<MapSize>().TileMap;

        List<GameObject> closeTiles = new List<GameObject>();
        foreach (GameObject item in seenObjects)
        {
            if (item != null && 
                Vector3.Distance(transform.position, item.transform.position) < 5 && 
                item.tag == "Field" && map[(int)item.transform.position.x, (int)item.transform.position.z] != (int)MapGenerator.GroundType.Water &&
                HeatMap.GetEnemyMap(myData.tribe)[(int)newTarget.x, (int)newTarget.z] <= scareValue)
            {
                closeTiles.Add(item);
            }
            if (HeatMap.GetEnemyMap(myData.tribe)[(int)newTarget.x, (int)newTarget.z] <= scareValue) myData.seenEnemies++;
        }
        if (closeTiles.Count != 0)
        {
            newTarget = closeTiles[Random.Range(0, closeTiles.Count)].transform.position + new Vector3(0, 0.8f, 0) + new Vector3((transform.position.x > MapSize._mapSize / 2) ? -1 : 1, 0, (transform.position.z > MapSize._mapSize / 2) ? -1 : 1);
            SetTarget(newTarget);
        }
        else
            myData.energy = 3;
    }

    /// <summary>
    /// Generates a path and moves towards that.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Movement()
    {
        Stack<Vector3> path = new Stack<Vector3>();
        Vector3 direction = Vector3.zero;
        if (target == null)
            moving = false;
        else
        {
            moving = true;
            path.Push((target.transform.position - transform.position).normalized);
            direction = path.Pop();
        } 
        while (moving)
        {
            if (target == null)
            {
                moving = false;
                StopCoroutine(Movement());
            }
            else if (Vector3.Distance(transform.position, target.transform.position) > defStats.interactionRange)
            {
                Debug.DrawLine(transform.position, (Vector3.up * 3) + transform.position, myColor, Time.deltaTime * 2);
                Debug.DrawLine(transform.position, target.transform.position, myColor, Time.deltaTime * 2);
                transform.rotation = Quaternion.Euler(0, -((Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) - 90), 0);
                transform.Translate(Vector3.forward * defStats.speed * Time.deltaTime);
                
                if (myData.currentStateNo == (int)State.Fighting)
                {
                    path.Push((target.transform.position - transform.position).normalized);
                    direction = path.Pop();
                }
            }
            else if (path.Count == 0)
            {
                moving = false;
            }
            else
            {
                direction = path.Pop();
            }
            yield return new WaitForEndOfFrame();
        }
        moving = false;
        StopCoroutine(Movement());
        if (target != null && target.tag == defStats.returnPoint && target.GetComponent<DataStorage>().isActiveAndEnabled && myData.currentStateNo == (int)State.Retreating)
        {
            target.GetComponent<DataStorage>().Interact(gameObject);
        }
    }
    public bool InRange()
    {
            return (Vector3.Distance(target.transform.position, transform.position) <= defStats.interactionRange);
    }

    public void OnTriggerEnter(Collider col)
    {
        seenObjects.Add(col.gameObject);
    }
    public void OnTriggerExit(Collider col)
    {
        seenObjects.Remove(col.gameObject);
    }
    /// <summary>
    /// Returns the Object with the searchtag that is closest to you.
    /// </summary>
    /// <param name="searchTag"></param>
    /// <returns></returns>
    public GameObject FindObject(string searchTag)
    {
        int objectNo = 0;
        if (myData.seenObjects.Count > 0)
        {
            for (int i = 0; i < myData.seenObjects.Count; i++)
            {
                if (myData.seenObjects[i] != null)
                {
                    if (myData.seenObjects[i].tag == searchTag)
                    {
                        objectNo = i;
                    }
                }
            }
            return myData.seenObjects[objectNo];
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// Returns a List with all Objects with 'searchTag'.
    /// </summary>
    /// <param name="searchTag"></param>
    /// <returns></returns>
    public List<GameObject> FindObjects(string searchTag)
    {
        //Debug.Log(searchTag);
        List<GameObject> foundObjects = new List<GameObject>();
        for (int i = 0; i < seenObjects.Count; i++)
        {
            if (seenObjects[i] != null &&  seenObjects[i].tag == searchTag)
                foundObjects.Add(seenObjects[i]);
        }
        return foundObjects;
    }
    public GameObject FindObjectGlobal(string searchTag)
    {
        float minDistance = 999;
        int objectNo = 0;
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(searchTag);
        if (foundObjects.Length > 0)
        {
            for (int i = 0; i < foundObjects.Length; i++)
            {
                if (foundObjects[i] != null)
                {

                    float distance = Vector3.Distance(transform.position, foundObjects[i].transform.position);

                    if (myData.seenObjects[i].transform.parent.name == myData.tribe && distance < minDistance)
                    {
                        minDistance = distance;
                        objectNo = i;
                    }
                }
            }
            return myData.seenObjects[objectNo];
        }
        else
        {
            return null;
        }
    }
    public DataStorage GetTownHall
    {
        get
        {
            foreach (GameObject item in GameObject.FindGameObjectsWithTag(defStats.returnPoint))
            {
                if (item.GetComponent<DataStorage>().tribe == myData.tribe)
                    return item.GetComponent<DataStorage>();
            }
            return null;
        }
    }
    public void GlobalRemoveSeenObject(GameObject item)
    {
        GameObject[] TribeMen = GameObject.FindGameObjectsWithTag("TribeMan");
        foreach (GameObject person in TribeMen)
        {
            person.GetComponent<CharacterBehaviour>().RemoveSeenObject(item);
        }
    }
    public void RemoveSeenObject(GameObject item)
    {
        seenObjects.Remove(item);
    }

    /// <summary>
    /// Basically Debug.Log(), but only logs in case of one specific character.
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    public bool SingleCharacter(string log)
    {
        if(name == "Tribesman A 2 1")
        {
            Debug.Log(log);
            return true;
        }
        return false;
    }

    public void Disable()
    {
        StopAllCoroutines();
    }
}