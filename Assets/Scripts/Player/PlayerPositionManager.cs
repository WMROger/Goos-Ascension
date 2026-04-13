using UnityEngine;

public static class PlayerPositionManager
{
    private static Vector3 savedPosition = Vector3.zero;
    private static bool hasSavedPosition = false;

    public static void SavePlayerPosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
    }

    public static Vector3 GetSavedPosition()
    {
        return savedPosition;
    }

    public static bool HasSavedPosition()
    {
        return hasSavedPosition;
    }

    public static void ClearSavedPosition()
    {
        hasSavedPosition = false;
    }
}
