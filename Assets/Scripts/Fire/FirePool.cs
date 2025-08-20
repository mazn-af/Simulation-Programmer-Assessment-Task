using UnityEngine;
using System.Collections.Generic;

public class FirePool : MonoBehaviour
{
    [SerializeField] GameObject firePrefab;
    [SerializeField] int initialCount = 20;

    readonly Queue<FireNode> pool = new();

    void Awake()
    {
        for (int i = 0; i < initialCount; i++)
        {
            var node = Create();
            Return(node);
        }
    }

    FireNode Create()
    {
        var go = Instantiate(firePrefab, transform);
        go.SetActive(false);
        return go.GetComponent<FireNode>();
    }

    public FireNode Get(Vector3 pos, Quaternion rot, float health = -1f)
    {
        var node = pool.Count > 0 ? pool.Dequeue() : Create();
        var t = node.transform;
        t.SetPositionAndRotation(pos, rot);
        node.gameObject.SetActive(true);
        node.Ignite(health);
        return node;
    }

    public void Return(FireNode node)
    {
        node.gameObject.SetActive(false);
        pool.Enqueue(node);
    }
}
