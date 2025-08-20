using UnityEngine;

public class Flammable : MonoBehaviour
{
    [SerializeField] float igniteDelay = 3f;   
    [SerializeField] float checkRadius = 2f;   
    [SerializeField] LayerMask fireMask;       
    [SerializeField] FirePool firePool;        

    bool isIgnited = false;
    float igniteTimer = 0f;

    void Update()
    {
        if (isIgnited) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius, fireMask);
        if (hits.Length > 0)
        {
            igniteTimer += Time.deltaTime;
            if (igniteTimer >= igniteDelay)
                Ignite();
        }
        else
        {
            igniteTimer = 0f; 
        }
    }

    void Ignite()
    {
        if (isIgnited) return;

        isIgnited = true;
        if (firePool)
        {
            var node = firePool.Get(transform.position, Quaternion.identity);
            node.transform.SetParent(transform);
        }
    }
}
