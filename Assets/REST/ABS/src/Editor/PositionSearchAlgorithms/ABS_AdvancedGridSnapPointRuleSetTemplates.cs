//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

using REST.Utils;
using System.Diagnostics;

namespace REST.AdvancedBuildSystem.Editor
{
    public static class ABS_AdvancedGridSnapPointRuleSetTemplates
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum RuleTemplate : ushort
        {
            Floor_BlockedCorner,
            Floor_BlockTwoSide,

            Wall_BlockWallSnapping,
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Floor Template BlockCorner
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_Floor = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_Wall = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_EdgeHorizontal = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_EdgeVertical = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_Corner = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockCorner_Center = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[][] s_Template_Floor_BlockCorner = new ABS_AdvancedGridSnapPointRule.PermissionType[][]
        {
            s_Template_Floor_BlockCorner_Floor,
            s_Template_Floor_BlockCorner_Wall,
            s_Template_Floor_BlockCorner_EdgeHorizontal,
            s_Template_Floor_BlockCorner_EdgeVertical,
            s_Template_Floor_BlockCorner_Corner,
            s_Template_Floor_BlockCorner_Center,
        };

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Floor Template BlockTwoSide
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_Floor = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_Wall = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_EdgeHorizontal = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_EdgeVertical = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_Corner = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Floor_BlockTwoSide_Center = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[][] s_Template_Floor_BlockTwoSide = new ABS_AdvancedGridSnapPointRule.PermissionType[][]
        {
            s_Template_Floor_BlockTwoSide_Floor,
            s_Template_Floor_BlockTwoSide_Wall,
            s_Template_Floor_BlockTwoSide_EdgeHorizontal,
            s_Template_Floor_BlockTwoSide_EdgeVertical,
            s_Template_Floor_BlockTwoSide_Corner,
            s_Template_Floor_BlockTwoSide_Center,
        };

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Wall Template BlockWallSnapping
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_Floor = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_Wall = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
            ABS_AdvancedGridSnapPointRule.PermissionType.Block,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_EdgeHorizontal = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_EdgeVertical = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_Corner = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[] s_Template_Wall_BlockWallSnapping_Center = new ABS_AdvancedGridSnapPointRule.PermissionType[]
        {
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
            ABS_AdvancedGridSnapPointRule.PermissionType.Allow,
        };

        private static ABS_AdvancedGridSnapPointRule.PermissionType[][] s_Template_Wall_BlockWallSnapping = new ABS_AdvancedGridSnapPointRule.PermissionType[][]
        {
            s_Template_Wall_BlockWallSnapping_Floor,
            s_Template_Wall_BlockWallSnapping_Wall,
            s_Template_Wall_BlockWallSnapping_EdgeHorizontal,
            s_Template_Wall_BlockWallSnapping_EdgeVertical,
            s_Template_Wall_BlockWallSnapping_Corner,
            s_Template_Wall_BlockWallSnapping_Center,
        };

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static ABS_AdvancedGridSnapPointRule.PermissionType[][][] s_Template = new ABS_AdvancedGridSnapPointRule.PermissionType[][][]
        {
            s_Template_Floor_BlockCorner,
            s_Template_Floor_BlockTwoSide,

            s_Template_Wall_BlockWallSnapping,
        };

        public static ABS_AdvancedGridSnapPointRule.PermissionType[] GetTemplatePermissions(ABS_AdvancedGridType m_SnappingTargetType, RuleTemplate p_Template)
        {
            return s_Template[(int)p_Template][(int)m_SnappingTargetType];
        }

    }
}