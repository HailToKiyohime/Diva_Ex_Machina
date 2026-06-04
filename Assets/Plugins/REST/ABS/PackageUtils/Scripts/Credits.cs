//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.PackageUtils
{
    public class Credits : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        public Section[] sections;
        public bool loadedLayout;

        [Serializable]
        public class Section
        {
            public Texture2D[] icon;
            public string heading, linkText, url;
        }
    }
}