using UnityEngine;
using UnityEngine.AI;

public class CarObstacleCarver : MonoBehaviour
{
    [SerializeField] NavMeshObstacle obstacle;
    [SerializeField] float speedThreshold = 0.5f;

    [SerializeField] Rigidbody rb;

    void Awake()
    {
        if (!obstacle) obstacle = GetComponent<NavMeshObstacle>();
    }

    void Update()
    {

        float speed = rb.linearVelocity.magnitude;
        if (!obstacle) return;
        bool shouldCarve = speed < speedThreshold;
        if (obstacle.carving != shouldCarve) obstacle.carving = shouldCarve;
    }
}
