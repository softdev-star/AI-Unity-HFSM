using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitBehaviour : MonoBehaviour
{
    //HFSM
    public enum decisionCategory{Patrol, Combat, Survival}
    public decisionCategory topTier;

    public enum patrolStates{Idle, Wander, Seek}
    public patrolStates patState;

    public enum combatStates{Attack, Charge, Reposition}
    public combatStates combatState;

    public enum survivalStates{SeekCover, SeekHealth, SeekAmmo, Run}
    public survivalStates survState;

    //health and ammo
    public int health;
    private int maxHealth;
    public int ammo;
    private int maxAmmo;

    public bool behindCover = false;
    public bool haveLOS = false;

    //decision making values
    public int baseBravery;
    private float braveryValue;
    private float threatValue;
    private float conditionValue;

    public int team;
    string otherTeam;

    private Animator anim;
    private NavMeshAgent agent;

    public float attackSpeed;
    private float attackTimer = 0;

    private bool isAlive = true;

    public GameObject target;
    public Vector3 shootPivot;

    //Priorities for decision making
    public struct Priorities
    {
        public string key;
        public int value;
        public Priorities(string _key, int _value)
        {
            key = _key;
            value = _value;
        }
    }
    private List<Priorities> priorities;
    public List<GameObject> enemies;

    //---For handling priorities---
    void InitPriorities()
    {
        priorities = new List<Priorities>();
        //Fighting
        priorities.Add(new Priorities("Fighting", 5));
        //Cover
        priorities.Add(new Priorities("Cover", 0));
        //Ammo
        priorities.Add(new Priorities("Ammo", 0));
        //Health
        priorities.Add(new Priorities("Health", 0));
        //Escape
        priorities.Add(new Priorities("Escape", 0));
    }

    void SortPriorities()
    {
        Priorities temp;
        for(int i=0; i<priorities.Count-1; i++)
        {
            int max = i;
            for(int j = i+1; j<priorities.Count; j++)
            {
                if(priorities[j].value > priorities[max].value)
                {
                    max = j;
                }
            }
            if(max != i)
            {
                temp = priorities[i];
                priorities[i] = priorities[max];
                priorities[max] = temp;
            }
        }
    }

    // Use this for initialization
    void Start ()
    {
        otherTeam = team == 0 ? "TeamB" : "TeamA";
        anim = GetComponent<Animator>();
        anim.SetBool("Idling", true);
        anim.SetBool("NonCombat", true);
        agent = GetComponent<NavMeshAgent>();
        InitPriorities();
        enemies = new List<GameObject>();
        maxAmmo = ammo;
        maxHealth = health;
        topTier = decisionCategory.Patrol;
        StartCoroutine("DetermineBehaviour");
        //DetermineBehaviour();
        //SortPriorities();

        //Outputs();
    }

    //Temp, for checking values.
    void Outputs()
    {
        Debug.Log(gameObject.name + " Priorities:");
        foreach (Priorities pri in priorities)
        {
            Debug.Log(pri.key + pri.value);
        }
        Debug.Log("Bravery: " + CalcBravery());
        Debug.Log("Condition: " + CalcCondition());
        Debug.Log("Threat: " + CalcThreat());
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (isAlive)
        {
            shootPivot = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
            if (health <= 0)
            {
                isAlive = false;
                anim.SetInteger("Death", Random.Range(1, 4));
                StopAllCoroutines();
                StartCoroutine("Disappear");
            }
        }
    }
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }
    //---Determine my state and priorities---
    public IEnumerator DetermineBehaviour()
    {
        //yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(1);
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            string resString = "";
            if(topTier == decisionCategory.Combat)
            {
                resString = combatState.ToString();
            }
            else if(topTier == decisionCategory.Patrol)
            {
                resString = patState.ToString();
            }
            else if(topTier == decisionCategory.Survival)
            {
                resString = survState.ToString();
            }
            Debug.Log(gameObject.name + " called from " + topTier.ToString() + " " + resString);
        }
        float threat = CalcThreat();
        float bravery = CalcBravery();
        float condition = CalcCondition();
        //yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(1);
        //determine priorities
        for (int i = 0; i < priorities.Count; i++)
        {
            Priorities pri = priorities[i];
            if (pri.key == "Health")
            {
                pri.value = 5 - (health / maxHealth) * 5;
            }
            if (pri.key == "Cover")
            {
                if (!behindCover && haveLOS)
                {
                    pri.value = 10 - (health / maxHealth) * 10;
                }
                else
                {
                    pri.value = 0;
                }
            }

            if (pri.key == "Ammo")
            {
                pri.value = 4 - (ammo / maxAmmo) * 4;
            }
            if(pri.key == "Fighting")
            {
                pri.value = 3;
                //pri.value = 5 - ((int)threat / (int)bravery) * 5; //this will need refining
            }
            if(pri.key == "Escape")
            {
                //pri.value = 4 - ((int)threat / (int)bravery) * 4; //this will need refining
            }
            priorities[i] = pri;
        }

        SortPriorities();
        //Outputs();
        //Determine which high level state we're in
        if(priorities[0].key == "Health" && priorities[0].value > 3
            || priorities[0].key == "Cover" && priorities[0].value > 3
            || priorities[0].key == "Escape" && priorities[0].value > 3
            || priorities[0].key == "Ammo" && priorities[0].value > 3)
        {
            topTier = decisionCategory.Survival;
        }
        else if (priorities[0].key == "Fighting" && priorities[0].value >= 1)
        {
            topTier = decisionCategory.Combat;
        }
        else
        {
            topTier = decisionCategory.Patrol;
        }
        //yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(1);

        //Determine which low level state we're in
        if (topTier == decisionCategory.Patrol)
        {
            anim.SetBool("NonCombat", true);
            //determine ideal patrol behaviour
            if (threat > 1 && !haveLOS)
            {
                patState = patrolStates.Seek;
                Seek();
            }
            else
            {
                int choice = Random.Range(0,2);
                if (choice == 0)
                {
                    patState = patrolStates.Idle;
                    Idle();
                }
                else
                {
                    patState = patrolStates.Wander;
                    Wander();
                }
            }
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                Debug.Log(gameObject.name + " result is " + topTier.ToString() + " " + patState.ToString());
            }
        }
        else if(topTier == decisionCategory.Combat)
        {
            //determine course of action for combat
            anim.SetBool("NonCombat", false);
            if (haveLOS && ammo > 0)
            {
                if (UnityEditor.Selection.activeGameObject == gameObject)
                {
                    Debug.Log(gameObject.name + " should attack");
                }
                combatState = combatStates.Attack;
                Attack();
            }
            else if(haveLOS && ammo == 0)
            {
                combatState = combatStates.Charge;
                Charge();
            }
            else 
            {
                combatState = combatStates.Reposition;
                Reposition();
            }
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                Debug.Log(gameObject.name + " result is " + topTier.ToString() + " " + combatState.ToString());
            }
        }
        else if(topTier == decisionCategory.Survival)
        {
            anim.SetBool("NonCombat", true);
            if (priorities[0].key == "Health")
            {
                survState = survivalStates.SeekHealth;
                SeekHealth();
            }
            else if (priorities[0].key == "Cover")
            {
                survState = survivalStates.SeekCover;
                SeekCover();
            }
            else if (priorities[0].key == "Escape")
            {
                survState = survivalStates.Run;
                Run();
            }
            else if (priorities[0].key == "Ammo")
            {
                survState = survivalStates.SeekAmmo;
                SeekAmmo();
            }
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                Debug.Log(gameObject.name + " result is " + topTier.ToString() + " " + survState.ToString());
            }
        }
        yield return new WaitForEndOfFrame();
        //yield return new WaitForSeconds(1);
        //StopCoroutine("DetermineBehaviour");
    }

    //---Methods for actions---
    //PATROL
    void Idle()
    {
        StopAllCoroutines();
        agent.ResetPath();
        anim.SetBool("Idling", true);
        //Stand around for a while, 
        //after a time progress to patrol
        //check for threats intermittently, 
        //if you find one and have LOS go to combat, 
        //otherwise, seek
        StartCoroutine("Idling");
        //Debug.Log("Call Idle");
    }
    IEnumerator Idling()
    {
        //Debug.Log("Idling");
        float rand = Random.Range(1, 5);
        yield return new WaitForSeconds(rand);
        StartCoroutine("DetermineBehaviour");
        //DetermineBehaviour();
        yield return null;
    }

    void Wander()
    {
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        //Find a random point nearby and path there, once there, idle
        Vector3 pos = new Vector3(Random.Range(transform.position.x - 50, transform.position.x + 50), transform.position.y, Random.Range(transform.position.z - 50, transform.position.z + 50));
        agent.destination = pos;
        //Debug.Log("Wander Called" + pos.ToString());
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Wander();
        }
        else
        {
            StartCoroutine("Wandering");
        }
    }

    void Seek()
    {
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        //Find a random point nearby and path there, once there, idle
        Vector3 pos = new Vector3(Random.Range(transform.position.x - 10, transform.position.x + 10), transform.position.y, Random.Range(transform.position.z - 10, transform.position.z + 10));
        agent.destination = pos;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Seek();
        }
        else
        {
            StartCoroutine("Wandering");
        }
    }
    IEnumerator Wandering()
    {
        behindCover = false;
        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            StartCoroutine("DetermineBehaviour");
        }
        //DetermineBehaviour();
        yield return null;
    }

    //COMBAT
    void Attack()
    {
        anim.SetBool("Idling", true);
        StopAllCoroutines();
        //Debug.Log("Ready!");
        if(target.transform.tag == otherTeam && ammo > 0)
        {
            StartCoroutine("Attacking");
        }
        else
        {
            //Debug.Log(gameObject.name + " has no ammo or an invalid target");
            StartCoroutine("DetermineBehaviour");
            //DetermineBehaviour();
        }
    }
    IEnumerator Attacking()
    {
        anim.SetBool("Use", false);
        transform.LookAt(target.transform.position);
        //Debug.Log("Aim!");
        yield return new WaitForSeconds(attackSpeed);
        //Debug.Log("Fire!");
        ammo--;
        anim.SetBool("Use", true);
        Debug.DrawLine(shootPivot, target.GetComponent<UnitBehaviour>().shootPivot, Color.red, 3.0f);
        if (!Physics.Linecast(shootPivot, target.GetComponent<UnitBehaviour>().shootPivot))
        {
            transform.LookAt(target.transform.position);
            //Will possibly incorporate other parameters into the equation
            float hitChance = target.GetComponent<UnitBehaviour>().behindCover ? Random.Range(0, 4) : Random.Range(0, 2);
            if (hitChance == 0)
            {
                target.GetComponent<UnitBehaviour>().Hit();
                StartCoroutine("DetermineBehaviour");
                //DetermineBehaviour();
            }
            else
            {
                Attack();
            }
        }
        else
        {
            //Debug.Log("broke LOS");
            haveLOS = false;
            StartCoroutine("DetermineBehaviour");
            //DetermineBehaviour();
        }
        yield return null;
    }

    void Charge()
    {
        //Debug.Log(gameObject.name + " CHARGE!");
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        StartCoroutine("MoveToTarget");
    }

    void Reposition()
    {
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        if (!behindCover)
        {
            //Debug.Log(gameObject.name + " Take Cover");
            SeekCover();
        }
        else
        {
            //Debug.Log(gameObject.name + " Hunt");
            Seek();
        }
    }

    //SURVIVAL

    IEnumerator MoveToTarget()
    {
        behindCover = false;
        Vector3 targPos = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
        agent.SetDestination(targPos);
        if(agent.path.status == NavMeshPathStatus.PathComplete)
        {
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                //Debug.Log(gameObject.name + " Path Complete");
            }
            anim.SetBool("Idling", true);
            if(target.transform.tag == "Cover")
            {
                behindCover = true;
            }
            if(target.transform.tag == "HealthPack")
            {
                health += 50;
                if(health > maxHealth)
                {
                    health = maxHealth;
                }
            }
            if (target.transform.tag == "AmmoPack")
            {
                ammo += 10;
                if (ammo > maxAmmo)
                {
                    ammo = maxAmmo;
                }
            }
            Debug.Log("Arrived at destination");
            StartCoroutine("DetermineBehaviour");
            //DetermineBehaviour();
        }
        yield return null;
    }

    void SeekHealth()
    {
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            //Debug.Log(gameObject.name + " Going for health");
        }
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        //Find the closest health pack
        foreach(GameObject hp in GameObject.FindGameObjectsWithTag("HealthPack"))
        {
            if (target == null || !target.CompareTag("HealthPack"))
            {
                target = hp;
            }
            else if (Vector3.Distance(transform.position, hp.transform.position) < Vector3.Distance(transform.position, target.transform.position))
            {
                target = hp;
            }
        }
        //Move to it.
        StartCoroutine("MoveToTarget");
    }

    void SeekAmmo()
    {
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            //Debug.Log(gameObject.name + " Going for ammo");
        }
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        //Find the closest ammo pack
        foreach (GameObject hp in GameObject.FindGameObjectsWithTag("AmmoPack"))
        {
            if (target == null || !target.CompareTag("AmmoPack"))
            {
                target = hp;
            }
            else if (Vector3.Distance(transform.position, hp.transform.position) < Vector3.Distance(transform.position, target.transform.position))
            {
                target = hp;
            }
        }
        //Move to it.
        StartCoroutine("MoveToTarget");
    }

    void SeekCover()
    {
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            //Debug.Log(gameObject.name + " Going for cover");
        }
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        //Find the closest cover
        foreach (GameObject hp in GameObject.FindGameObjectsWithTag("Cover"))
        {
            if (enemies.Count > 0)
            {
                Vector3 toEnemy = (hp.transform.position - enemies[0].transform.position).normalized;
                if (Vector3.Dot(hp.GetComponent<CoverZone>().dir, toEnemy) > -0.5f)
                {
                    continue;
                }
            }
            if (target == null || !target.CompareTag("Cover"))
            {
                target = hp;
            }
            else if (Vector3.Distance(transform.position, hp.transform.position) < Vector3.Distance(transform.position, target.transform.position))
            {
                target = hp;
            }
        }
        //Move to it.
        StartCoroutine("MoveToTarget");
    }

    void Run()
    {
        StopAllCoroutines();
        anim.SetBool("Idling", false);
        if(team == 0)
        {
            //go to base A
        }
        else
        {
            //go to base B
        }
        StartCoroutine("MoveToTarget");
    }

    //I've been hit!
    void Hit()
    {
        anim.SetBool("Pain", true);
        health -= 25;
        StartCoroutine("DetermineBehaviour");
        //DetermineBehaviour();
    }

    //Calculation methods for determining priorities
    float CalcThreat()
    {
        float ret = 0;
        float dist = 0;
        foreach(GameObject enemy in GameObject.FindGameObjectsWithTag(otherTeam))
        {
            dist = Vector3.Distance(enemy.transform.position, transform.position);
            if (dist < 20)
            {
                if (!enemies.Contains(enemy) && enemy.GetComponent<UnitBehaviour>().health > 0)
                {
                    enemies.Add(enemy);
                }
            }
            else
            {
                if(enemies.Contains(enemy))
                {
                    enemies.Remove(enemy);
                }
            }
            if(enemies.Contains(enemy) && enemy.GetComponent<UnitBehaviour>().health <= 0)
            {
                enemies.Remove(enemy);
            }
        }
        float totalHealth = 0;
        float totalAmmo = 0;
        haveLOS = false;
        if (enemies.Count >= 1)
        {
            //Debug.Log("Have enemies");
            foreach (GameObject enemy in enemies)
            {
                totalHealth += enemy.GetComponent<UnitBehaviour>().health / 100;
                totalAmmo += enemy.GetComponent<UnitBehaviour>().ammo / 100;
                if (!Physics.Linecast(shootPivot, enemy.GetComponent<UnitBehaviour>().shootPivot))
                {
                    //Debug.Log("LOS!");
                    haveLOS = true;
                    if (target == null)
                    {
                        target = enemy;
                    }
                    else if (Vector3.Distance(transform.position, enemy.transform.position) < Vector3.Distance(transform.position, target.transform.position))
                    {
                        target = enemy;
                    }
                }
            }
            ret = ((totalAmmo + totalHealth) / enemies.Count) * enemies.Count;
        }
        else
        {
            ret = 0;
        }
        return ret;
    }

    float CalcBravery()
    {
        float ret = baseBravery; //base bravery
        ret += (float)Random.Range(-1, 2); //add some chaos
        //increase based on condition (higher is better)
        ret += threatValue - conditionValue;
        return ret;
    }

    float CalcCondition()
    {
        float ret = ((health / maxHealth)*2) + (ammo / maxAmmo);
        ret += behindCover ? 5 : 0;
        return ret;
    }
}
