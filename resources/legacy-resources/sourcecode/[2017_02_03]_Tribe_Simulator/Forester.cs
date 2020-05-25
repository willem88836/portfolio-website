using UnityEngine;
using System.Collections;

/**
 * This is an example of a simple adaption to the main CharacterBehaviour. 
 * By making use of the inherited class a many of the functionalities do not have to be programmed specifically for this character. 
 * Therefore, many classes can be easily made. 
 * 
 */

public class Forester : CharacterBehaviour
{
    [SerializeField]
    string
        fieldTerm,
        treeTerm;

    [SerializeField]
    int maxFails;
    
    bool interacting = false;

    GameObject foundObject = null;
    
    public void OnEnable()
    {
        StartCoroutine(Delay());
    }
    private IEnumerator Delay()
    {
        yield return new WaitForEndOfFrame();
        SetTrait((this.GetType().Name));
        StartCoroutine(SearchTrees());
        GetData().currentStateNo = (int)State.Working;
    }
    
    private IEnumerator SearchTrees()
    {
        int failedSearch = 0;
        while (true)
        {
            while (!moving && !interacting && GetData().currentStateNo == (int)State.Working)
            {
                foundObject = null;
                foreach (GameObject item in FindObjects((failedSearch > maxFails) ? fieldTerm : treeTerm))
                {
                    if (!item.GetComponent<TreeData>().interacting)
                        foundObject = item;
                }
                if (foundObject != null) 
                {
                    foundObject.GetComponent<TreeData>().interacting = true;
                    SetTarget(foundObject);
                    interacting = true;
                    StartCoroutine(Interact());
                }
                else
                {
                    failedSearch++;
                    RandomTarget();
                }
                yield return new WaitForSeconds(1);
            }
            yield return new WaitForSeconds(1);
        }
    }

    private IEnumerator Interact()
    {
        int damage = defStats.objectDamage[GetData().heldObjectNo];
        while (foundObject != null && foundObject.GetComponent<TreeData>().wood > 0)
        {
            if (InRange())
            {
                foundObject.GetComponent<TreeData>().wood -= damage;
                ChangeItem(Item.Wood, damage);
                UseEnergy();
                moving = false;
            }
            yield return new WaitForSeconds(interactSpeed);
        }
        GlobalRemoveSeenObject(foundObject);
        if (foundObject.GetComponent<TreeData>().wood <= 0 ) Destroy(foundObject);
        interacting = false;
        yield return new WaitForEndOfFrame();
    }
    void OnDisable()
    {
        interacting = false;
        moving = false;
        GetData().currentStateNo = (int)State.Working;
        if (foundObject != null)
        {
            foundObject.GetComponent<TreeData>().interacting = false;
            if (foundObject.GetComponent<TreeData>().wood <= 0) Destroy(foundObject);
        }
        StopAllCoroutines();
        Disable();
    }
}
