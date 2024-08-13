using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour
{
    public int team;
    public GameObject baseUnit;

	// Use this for initialization
    void Awake()
    {
        Spawn();
    }
    
	void Spawn ()
    {
        GameObject temp = (GameObject)Instantiate(baseUnit, transform.position, Quaternion.identity);
        temp.GetComponent<UnitSetup>().team = team;
        temp.GetComponent<UnitSetup>().Init();
        temp.GetComponent<UnitBehaviour>().team = team;
	}
	
	// Update is called once per frame
	void OnDrawGizmos ()
    {
        Gizmos.color = team == 0? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.0f);
	}
}
