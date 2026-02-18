//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_PersistencyManager
    {
        public static string ToJson<Type>(in Type p_ObjectToSave, in bool p_PrettyPrint)
            where Type : class
        {
            return JsonUtility.ToJson(p_ObjectToSave, p_PrettyPrint);
        }

        public static Type FromJson<Type>(in string p_Data)
            where Type : class
        {
            return JsonUtility.FromJson<Type>(p_Data);
        }
    }
}

