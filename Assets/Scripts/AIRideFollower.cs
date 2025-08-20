using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIRideFollower : MonoBehaviour
{
    [SerializeField] Transform leader;
    [SerializeField] float mountWarpDistance = 2.0f;

    NavMeshAgent agent;
    VehicleSeats ridingVehicle;
    Transform mySeat;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!leader && GameObject.FindWithTag("Player"))
            leader = GameObject.FindWithTag("Player").transform;
    }

    void OnEnable()
    {
        EventBus.OnLeaderEnteredVehicle += OnLeaderEnterVehicle;
        EventBus.OnLeaderExitedVehicle += OnLeaderExitVehicle;
    }

    void OnDisable()
    {
        EventBus.OnLeaderEnteredVehicle -= OnLeaderEnterVehicle;
        EventBus.OnLeaderExitedVehicle -= OnLeaderExitVehicle;
    }

    void OnLeaderEnterVehicle(VehicleSeats seats, Transform who)
    {
        if (leader != who || seats == null) return;
        if (ridingVehicle) return;

        if (seats.TryAssignSeat(transform, out var seatAnchor))
        {
            ridingVehicle = seats;
            mySeat = seatAnchor;

            if (agent && agent.isOnNavMesh)
            {
                Vector3 doorPos = seatAnchor.position;
                if (NavMesh.SamplePosition(doorPos, out var hit, 2.5f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
            }

            if (agent) { agent.isStopped = true; agent.enabled = false; }
            transform.SetParent(seatAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    void OnLeaderExitVehicle(VehicleSeats seats, Transform who)
    {
        if (ridingVehicle != seats || leader != who) return;

        Transform exit = seats.ExitPoint;
        transform.SetParent(null, true);

        if (agent)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(exit.position, out var hit, 2.5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            agent.isStopped = false;
        }

        seats.Vacate(transform);
        ridingVehicle = null;
        mySeat = null;
    }
}
