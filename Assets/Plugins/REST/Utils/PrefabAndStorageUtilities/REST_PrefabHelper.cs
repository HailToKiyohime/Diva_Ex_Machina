//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************


namespace REST.Utils
{
    public class REST_PrefabHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        public static string GetPrefabSourceGUID(in GameObject p_GameObject)
        {
            GameObject source = GetPrefabSource(p_GameObject);
            if (source != null)
            {
                return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            }

            return null;
        }

        public static GameObject GetPrefabSource(in GameObject p_GameObject)
        {
            if (p_GameObject == null) return null;

            if (PrefabUtility.IsPartOfPrefabInstance(p_GameObject))
            {
                return (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(p_GameObject);
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(p_GameObject))
            {
                return p_GameObject;
            }
            else
            {
                return null;
            }
        }
#endif
    }
}