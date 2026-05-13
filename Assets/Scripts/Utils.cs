using UnityEngine;

public static class Utils
{
    public static bool CompareLayer(LayerMask myLayerMask, int targetLayer)
    {
        return ((1 << myLayerMask) & targetLayer) != 0;
    }
}