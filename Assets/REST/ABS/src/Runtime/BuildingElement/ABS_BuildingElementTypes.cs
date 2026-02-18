//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_BuildingElementAreaType
    {
        Unkown,
        Wall,
        HalfWall,
        Floor,
        HorizontalEdge,
        VerticalEdge,
        Stair
    }

    public enum ABS_BuildingElementSnapPointType
    {
        Unkown,
        Wall,
        HalfWall,
        Floor,
        TriangleFloor,
        HorizontalEdge,
        VerticalEdge,
        Corner,
        Stair
    }
}