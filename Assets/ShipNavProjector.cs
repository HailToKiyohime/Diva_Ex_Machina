using UnityEngine;

public static class ShipNavProjector
{
    public static Vector3 RealToGhostPoint(Transform realShip, Transform ghostShip, Vector3 realWorldPos)
    {
        Vector3 local = realShip.InverseTransformPoint(realWorldPos);
        return ghostShip.TransformPoint(local);
    }

    public static Vector3 GhostToRealPoint(Transform realShip, Transform ghostShip, Vector3 ghostWorldPos)
    {
        Vector3 local = ghostShip.InverseTransformPoint(ghostWorldPos);
        return realShip.TransformPoint(local);
    }

    public static Vector3 RealToGhostDir(Transform realShip, Transform ghostShip, Vector3 realWorldDir)
    {
        Vector3 localDir = realShip.InverseTransformDirection(realWorldDir);
        return ghostShip.TransformDirection(localDir);
    }

    public static Vector3 GhostToRealDir(Transform realShip, Transform ghostShip, Vector3 ghostWorldDir)
    {
        Vector3 localDir = ghostShip.InverseTransformDirection(ghostWorldDir);
        return realShip.TransformDirection(localDir);
    }
}
