//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_IDManager
    {
        public static string CreateGuid()
        {
            return System.Guid.NewGuid().ToString();
        }
    }
}