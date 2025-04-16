using UnityEditor;

public class ReserializeAssetsTool
{
    [MenuItem("Tools/Force Reserialize All Assets")]
    public static void ForceReserializeAllAssets()
    {
        // 모든 에셋 강제 재직렬화
        AssetDatabase.ForceReserializeAssets();
    }

    [MenuItem("Tools/Force Reserialize Selected Assets")]
    public static void ForceReserializeSelectedAssets()
    {
        // 선택한 에셋만 재직렬화
        var selectedGUIDs = Selection.assetGUIDs;
        AssetDatabase.ForceReserializeAssets(selectedGUIDs);
    }
}
