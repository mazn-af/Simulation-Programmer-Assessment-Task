using UnityEngine;

public class VehicleAccess : MonoBehaviour
{
    [Header("Refs")]
    public VehicleController vehicle;
    public Transform seat;               
    public Transform exitPoint;          
    public Camera playerCamera;          
    public Camera carCamera;             

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
        if (!seat) seat = transform;
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

        player.transform.SetParent(seat, worldPositionStays: false);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        if (playerCamera) playerCamera.enabled = false;
        if (carCamera) carCamera.enabled = true;

        vehicle.SetControlEnabled(true);

        isDriving = true;
    }

    void ExitVehicle()
    {
        player.transform.SetParent(null);
        player.transform.position = exitPoint ? exitPoint.position : (seat.position + seat.right * 1.2f);
        player.transform.rotation = exitPoint ? exitPoint.rotation : Quaternion.LookRotation(seat.right, Vector3.up);

        if (playerCC) playerCC.enabled = true;
        player.enabled = true;

        if (carCamera) carCamera.enabled = false;
        if (playerCamera) playerCamera.enabled = true;

        vehicle.SetControlEnabled(false);

        isDriving = false;
    }
}
