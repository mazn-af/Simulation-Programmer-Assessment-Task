using UnityEngine;
using System.Collections.Generic;

public class VehicleSeats : MonoBehaviour
{
    [System.Serializable]
    public class Seat
    {
        public Transform anchor;
        [HideInInspector] public Transform occupant;
        public bool IsFree => anchor && occupant == null;
    }

    [SerializeField] List<Seat> seats = new List<Seat>();
    [SerializeField] Transform exitPoint;

    public bool TryAssignSeat(Transform who, out Transform seatAnchor)
    {
        seatAnchor = null;
        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i].IsFree)
            {
                seats[i].occupant = who;
                seatAnchor = seats[i].anchor;
                return true;
            }
        }
        return false;
    }

    public void Vacate(Transform who)
    {
        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i].occupant == who)
            {
                seats[i].occupant = null;
                break;
            }
        }
    }

    public Transform ExitPoint => exitPoint ? exitPoint : transform;
}
