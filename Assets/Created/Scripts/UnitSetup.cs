using UnityEngine;
using System.Collections;

public class UnitSetup : MonoBehaviour
{
    public int team;
    public GameObject Body;
    public Material[] clothesColours;
    public GameObject[] faces;
    public GameObject[] weapons;
    public GameObject[] hair;
    public string[] ranks;
    public string[] name;
    public int rank;

	// Use this for initialization
	public void Init ()
    {
        //TODO, FIX!
        gameObject.tag = team == 0 ? "TeamA" : "TeamB";
        Body.GetComponent<Renderer>().material = clothesColours[team];
        //Faces
        int faceNum = Random.Range(0, faces.Length);
        for(int i = 0; i < faces.Length; i++)
        {
            if(i != faceNum)
            {
                Destroy(faces[i]);
            }
        }
        //Weapon
        int weaponNum = Random.Range(5, 7);
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i != (weaponNum-5))
            {
                Destroy(weapons[i]);
            }
        }
        //Hair
        int hairNum = Random.Range(0, hair.Length);
        for (int i = 0; i < hair.Length; i++)
        {
            if (i != hairNum)
            {
                Destroy(hair[i]);
            }
        }
        int die1 = Random.Range(0, ranks.Length);
        int die2 = Random.Range(0, ranks.Length);
        rank = (die1 + die2 - ranks.Length) < 0 ? 0 : (die1 + die2 - ranks.Length) > ranks.Length ? ranks.Length : (die1 + die2 - ranks.Length);
        gameObject.name = ranks[rank] + " " + name[Random.Range(0, name.Length)];

        GetComponent<Animator>().SetInteger("WeaponState", weaponNum);
        //GetComponent<UnitBehaviour>().shootPivot = faces[faceNum].transform;
    }
}
