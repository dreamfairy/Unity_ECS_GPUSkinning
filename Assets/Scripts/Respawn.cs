using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class Respawn : MonoBehaviour
{
    public GameObject Prefab;
    public Vector2Int RespawnCount;
    public float Interval = 1;

    public void Start()
    {
        for(int i = 0; i < RespawnCount.x; i++)
        {
            for(int j= 0; j < RespawnCount.y; j++)
            {
                GameObject go = GameObject.Instantiate(Prefab);
                go.transform.position = new Vector3(i * Interval, 0, j * Interval);
                go.SetActive(true);
            }
        }

        Prefab.SetActive(false);
    }
}