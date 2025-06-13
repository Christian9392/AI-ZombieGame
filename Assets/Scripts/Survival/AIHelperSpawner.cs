using UnityEngine;
using System.Collections;

public class AIHelperSpawner : MonoBehaviour
{
    public GameObject aiHelperPrefab;    
    public float helperLifetime = 5f;    
    public Vector3 offset = new Vector3(1f, 0, 0); 

    private bool helperActive = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !helperActive)
        {
            StartCoroutine(SpawnHelper());
        }
    }

    IEnumerator SpawnHelper()
    {
        helperActive = true;

        Vector3 spawnPos = transform.position + offset;
        GameObject helper = Instantiate(aiHelperPrefab, spawnPos, Quaternion.identity);

        yield return new WaitForSeconds(helperLifetime);

        Destroy(helper);
        helperActive = false;
    }
}
