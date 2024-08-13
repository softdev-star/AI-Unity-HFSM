using UnityEngine;
using System.Collections;

public class CoverZone : MonoBehaviour
{
    public Vector3 dir;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, dir);
    }
}
