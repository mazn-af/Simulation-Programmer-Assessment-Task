using UnityEngine;

public class VehicleAccess : MonoBehaviour
{
    [Header("Refs")]
    public VehicleController vehicle;
    public VehicleSeats seats;
    public Transform driverSeat;
    public Transform exitPoint;
    public Camera playerCamera;
    public Camera carCamera;
    public WaypointIndicator wayPoint;

    [Header("Player")]
    public PlayerResponder player;
    public CharacterController playerCC;

    [Header("Input")]
    public KeyCode actionKey = KeyCode.F;

    bool playerInsideTrigger = false;
    bool isDriving = false;

    void Reset()
    {
        if (!vehicle) vehicle = GetComponentInParent<VehicleController>();
        if (!driverSeat) driverSeat = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerResponder>()) playerInsideTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerResponder>()) playerInsideTrigger = false;
    }

    void Update()
    {
        if (!player || !vehicle) return;

        if (!isDriving)
        {
            if (playerInsideTrigger && Input.GetKeyDown(actionKey))
                EnterVehicle();
        }
        else
        {
            if (Input.GetKeyDown(actionKey))
                ExitVehicle();
        }
    }

    void EnterVehicle()
    {
        if (playerCC) playerCC.enabled = false;
        player.enabled = false;

        player.transform.SetParent(driverSeat, false);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        if (playerCamera) { playerCamera.enabled = false; }
        if (carCamera) { carCamera.enabled = true; }
        wayPoint.SetCamera(carCamera);

        vehicle.SetControlEnabled(true);
        if (seats) EventBus.OnLeaderEnteredVehicle?.Invoke(seats, player.transform);

        isDriving = true;
    }

    void ExitVehicle()
    {
        player.transform.SetParent(null, true);

        if (exitPoint)
        {
            if (playerCC) { playerCC.enabled = false; }
            player.transform.position = exitPoint.position;
            player.transform.rotation = exitPoint.rotation;
            if (playerCC) { playerCC.enabled = true; }
        }

        player.enabled = true;

        if (carCamera) carCamera.enabled = false;
        if (playerCamera) playerCamera.enabled = true;
        wayPoint.SetCamera(playerCamera);

        vehicle.SetControlEnabled(false);
        if (seats) EventBus.OnLeaderExitedVehicle?.Invoke(seats, player.transform);

        isDriving = false;
    }
}
