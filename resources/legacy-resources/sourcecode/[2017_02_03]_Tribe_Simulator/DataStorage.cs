using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/**
 * This Class is responsible for tracking the current inventory, and tribe requirements. 
 * Every type of item is assigned a value of priority. Together with the amount of currently stored items the trait of each character is determined. 
 * 
 */

    /// Contains all information used by the tribe.
    /// Determines what trait the character should get depending on this information.
public class DataStorage : MonoBehaviour {
    
    [SerializeField]
    int maxTribesmen;
    [SerializeField]
    float breakChance;
    
    private enum Item { Ore, Wood, Stone, Food, WeaponT1, WeaponT2, WeaponT3, ToolT1, ToolT2, ToolT3, Nothing }
    [SerializeField]
    float[] toolsIndices;
    [SerializeField]
    private float[] itemPriority = new float[11]
    {
        0.2f, 5f,2f,0.8f,0.1f,0.1f,0.1f,0.4f,0.4f,0.4f,0.1f
    };
    [SerializeField]
    float[] currentPriority;
    
    [SerializeField]
    public float[] inventory = new float[11] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
    private float[] currentTraits = new float[6] { 1, 1, 1, 1, 9, 1 };
    private enum Trait { Builder, Forester, Gatherer, Miner, NewCharacter, Warrior}

    public float enemiesSpotted = 1;
    public float population = 0;

    public string tribe;

    private DataStorage otherTribe;
    
	// Use this for initialization
	void Start () {
        tribe = transform.parent.name;
        foreach (GameObject item in GameObject.FindGameObjectsWithTag("TownHall"))
        {
            if (item.transform.parent != transform.parent)
                otherTribe = item.GetComponent<DataStorage>();
        }
	}
	
    public void Interact(GameObject interactingObject)
    {
        EmptyData(interactingObject);
        
        SetNewTrait(interactingObject);
    }

    /// <summary>
    /// Retrieves all data from the interacting character.
    /// Empties the character's information
    /// Updates the tribe's information.
    /// </summary>
    /// <param name="interactingObject"></param>
    private void EmptyData(GameObject interactingObject)
    {
        #region 
        // keeps track of all current traits occupied
        if (interactingObject.GetComponent<Builder>().isActiveAndEnabled)
            currentTraits[(int)Trait.Builder]--;
        else if (interactingObject.GetComponent<Forester>().isActiveAndEnabled)
            currentTraits[(int)Trait.Forester]--;
        else if (interactingObject.GetComponent<Gatherer>().isActiveAndEnabled)
            currentTraits[(int)Trait.Gatherer]--;
        else if (interactingObject.GetComponent<Miner>().isActiveAndEnabled)
            currentTraits[(int)Trait.Miner]--;
        else if (interactingObject.GetComponent<NewCharacter>().isActiveAndEnabled)
            currentTraits[(int)Trait.NewCharacter]--;
        else if (interactingObject.GetComponent<Warrior>().isActiveAndEnabled)
            currentTraits[(int)Trait.Warrior]--;
        #endregion  
        CharacterBehaviour _thisCharacter = interactingObject.GetComponent<CharacterBehaviour>();
        
        // Empties character's inventory and updates tribe's inventory.
        int[] _inventory = _thisCharacter.GetData().inventory;

        for (int i = 0; i < _inventory.Length; i++)
        {
            inventory[i] += ((_inventory[i] > 0 && Random.Range(0, 100) <= breakChance) ? -1 : 0) +  _thisCharacter.RemoveItem(i);
        }
        enemiesSpotted += _thisCharacter.GetData().seenEnemies;
        _thisCharacter.GetData().seenEnemies = 0;

        DefaultStatData defstats = transform.parent.gameObject.GetComponent<DefaultStatData>();

        // Restores the health and energy of the character with food.
        if (inventory[(int)Item.Food] < defstats.maxHealth - _thisCharacter.GetData().health)
        {
            _thisCharacter.GetData().health += (int)inventory[(int)Item.Food];
            inventory[(int)Item.Food] = 0;
        }
        else
        {
            inventory[(int)Item.Food] -= defstats.maxHealth - _thisCharacter.GetData().health;
            _thisCharacter.GetData().health = defstats.maxHealth;
        }

        if (inventory[(int)Item.Food] < defstats.maxEnergy - _thisCharacter.GetData().energy)
        {
            _thisCharacter.GetData().energy += (int)inventory[(int)Item.Food];
            inventory[(int)Item.Food] = 0;
        }
        else
        {
            inventory[(int)Item.Food] -= defstats.maxEnergy - _thisCharacter.GetData().energy;
            _thisCharacter.GetData().energy = defstats.maxHealth;
        }

        // Spawns a new character when its libido is too high
        if (_thisCharacter.GetData().libido >= defstats.MaxLibido && transform.parent.gameObject.GetComponentsInChildren<Transform>().Length < maxTribesmen)
        {
            if (Random.Range(0,100) > defstats.spawnChance)
                (Instantiate((transform.parent.gameObject.GetComponent<Generator>().items[0]), new Vector3(transform.position.x, 0.8f , transform.position.z), Quaternion.identity) as GameObject).transform.parent = transform.parent;
            _thisCharacter.GetData().libido = 0;
        }
        else if (_thisCharacter.GetData().libido >= defstats.MaxLibido)
        {
            _thisCharacter.GetData().libido = 0;
        }
    }

    private void SetNewTrait(GameObject interactingObject)
    {
        /**
         * 1 check enemies
         *   -> Higher priority for weapons.
         * 2 check food
         *   -> Higher priority for food.
         * 3 check tools
         *   -> high priority for wood -> Then stone -> then ore
         * convert to needs for individual items -> stone, wood, ore, food
         */
         
        CharacterBehaviour _thisCharacter = interactingObject.GetComponent<CharacterBehaviour>();
        _thisCharacter.UpdateSeenObjects();
        // Disables all behaviours.
        _thisCharacter.GetComponent<Builder>().enabled = false;
        _thisCharacter.GetComponent<Forester>().enabled = false;
        _thisCharacter.GetComponent<Gatherer>().enabled = false;
        _thisCharacter.GetComponent<Miner>().enabled = false;
        _thisCharacter.GetComponent<NewCharacter>().enabled = false;
        _thisCharacter.GetComponent<Warrior>().enabled = false;

        // changed '(int)Item.Ore' etc. to chars to make code more readible.
        int
            O = (int)Item.Ore,
            W = (int)Item.Wood,
            S = (int)Item.Stone,
            F = (int)Item.Food,
            W1 = (int)Item.WeaponT1,
            W2 = (int)Item.WeaponT2,
            W3 = (int)Item.WeaponT3,
            T1 = (int)Item.ToolT1,
            T2 = (int)Item.ToolT2,
            T3 = (int)Item.ToolT3,
            N = (int)Item.Nothing;

        // calculates priority
        currentPriority = new float[itemPriority.Length];
        float[] TraitFactor = new float[currentTraits.Length];
        for (int i = 0; i < TraitFactor.Length; i++)
            TraitFactor[i] = (currentTraits[i] / population) * 10;
        
        currentPriority[N] = (((otherTribe.enemiesSpotted / enemiesSpotted) / population) / TraitFactor[(int)Trait.Warrior]) / itemPriority[N];
        currentPriority[F] = ((inventory[F] / population) / TraitFactor[(int)Trait.Gatherer]) / itemPriority[F];

        currentPriority[T1] = ((inventory[T1] / population + inventory[T2] / population + inventory[T3] / population) / 3) / itemPriority[T1];
        currentPriority[T2] = ((inventory[T2] / population + inventory[T3] / population) / 2) / itemPriority[T2];
        currentPriority[T3] = (inventory[T3] / population) / itemPriority[T3];

        currentPriority[W1] = ((inventory[W1] / currentPriority[N] + inventory[W2] / currentPriority[N] + inventory[W3] / currentPriority[N]) / 3) / itemPriority[W1];
        currentPriority[W2] = ((inventory[W2] / currentPriority[N] + inventory[W3] / currentPriority[N]) / 2) / itemPriority[W2];
        currentPriority[W3] = (inventory[W3] / currentPriority[N]) / itemPriority[W3];

        currentPriority[O] = ((((inventory[O] / population) + currentPriority[T3] + currentPriority[T3]) / 3) / TraitFactor[(int)Trait.Miner]) / itemPriority[O];
        currentPriority[W] = ((((inventory[W] / population) + currentPriority[T1] + currentPriority[T2] + currentPriority[T3] + currentPriority[W1] + currentPriority[W2] + currentPriority[W3]) / 7) / TraitFactor[(int)Trait.Forester]) / itemPriority[W];
        currentPriority[S] = ((((inventory[S] / population) + currentPriority[T2] + currentPriority[W2]) / 3) / TraitFactor[(int)Trait.Miner]) / itemPriority[S];

        int lowestIndex = 0;

        // Determines lowest index
        for (int i = 0; i < currentPriority.Length; i++)
        {
            if (currentPriority[i] < currentPriority[lowestIndex])
                lowestIndex = i;

        }
        
        // sets the new trait
        if (lowestIndex == O || lowestIndex == S)
        {
            Miner _thisminer = interactingObject.GetComponent<Miner>();
            _thisminer.enabled = true;
            GiveItem(interactingObject.GetComponent<CharacterBehaviour>(), _thisminer.tool1, _thisminer.tool2, _thisminer.tool3);
            interactingObject.GetComponent<Miner>().NewTrait(interactingObject.GetComponent<CharacterBehaviour>().GetData());
            currentTraits[(int)Trait.Miner]++;
        }
        else if (lowestIndex == W)
        {
            Forester _thisForester = interactingObject.GetComponent<Forester>();
            _thisForester.enabled = true;   
            GiveItem(interactingObject.GetComponent<CharacterBehaviour>(), _thisForester.tool1, _thisForester.tool2, _thisForester.tool3);
            interactingObject.GetComponent<Forester>().NewTrait(interactingObject.GetComponent<CharacterBehaviour>().GetData());
            currentTraits[(int)Trait.Forester]++;
        }
        else if (lowestIndex == F)
        {
            Gatherer _thisGatherer = interactingObject.GetComponent<Gatherer>();
            _thisGatherer.enabled = true;   
            GiveItem(interactingObject.GetComponent<CharacterBehaviour>(), _thisGatherer.tool1, _thisGatherer.tool2, _thisGatherer.tool3);
            interactingObject.GetComponent<Gatherer>().NewTrait(interactingObject.GetComponent<CharacterBehaviour>().GetData());
            currentTraits[(int)Trait.Gatherer]++;
        }
        else if (lowestIndex == N)
        {
            Warrior _thisWarrior = interactingObject.GetComponent<Warrior>();
            _thisWarrior.enabled = true;    
            GiveItem(interactingObject.GetComponent<CharacterBehaviour>(), _thisWarrior.tool1, _thisWarrior.tool2, _thisWarrior.tool3);
            interactingObject.GetComponent<Warrior>().NewTrait(interactingObject.GetComponent<CharacterBehaviour>().GetData());
            currentTraits[(int)Trait.Warrior]++;
        }
        else if (lowestIndex == W1 || lowestIndex == W2 || lowestIndex == W3 || lowestIndex == T1 || lowestIndex == T2 || lowestIndex == T3)
        {
            Builder _thisBuilder = interactingObject.GetComponent<Builder>();
            _thisBuilder.enabled = true;
            GiveItem(interactingObject.GetComponent<CharacterBehaviour>(), _thisBuilder.tool1, _thisBuilder.tool2, _thisBuilder.tool3);
            interactingObject.GetComponent<Builder>().NewTrait(interactingObject.GetComponent<CharacterBehaviour>().GetData());
            currentTraits[(int)Trait.Builder]++;
        }
    }
    /// <summary>
    /// Gives one out of three items to character provided.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="item1"></param>
    /// <param name="item2"></param>
    /// <param name="item3"></param>
    private void GiveItem(CharacterBehaviour character, CharacterBehaviour.Item item1, CharacterBehaviour.Item item2, CharacterBehaviour.Item item3)
    {
        if (inventory[(int)item1] > 0)
        {
            character.ChangeItem(item1, 1);
            inventory[(int)item1]--;
        }
        else if (inventory[(int)item2] > 0)
        {
            character.ChangeItem(item2, 1);
            inventory[(int)item2]--;
        }
        else if (inventory[(int)item3] > 0)
        {
            character.ChangeItem(item3, 1);
            inventory[(int)item3]--;
        }
    }
    /// <summary>
    /// Gives provided character the item asked for.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="item"></param>
    /// <param name="quantity"></param>
    public void GiveItem(CharacterBehaviour character, CharacterBehaviour.Item item, int quantity)
    {
        if (inventory[(int)item] < quantity)
        {
            character.ChangeItem(item, (int)inventory[(int)item]);
            inventory[(int)item] = 0;
        }
        else
        {
            character.ChangeItem(item, quantity);
            inventory[(int)item] -= quantity;
        }
    }
    /// <summary>
    /// Returns index of most demanded tool.
    /// </summary>
    /// <returns></returns>
    public int NeededTool()
    {
        int index = 4;

        for (int i = 4; i < inventory.Length; i++)
        {
            if (currentPriority[i] < currentPriority[index])
            {
                index = i;
            }
        }
        return index;
    }
}