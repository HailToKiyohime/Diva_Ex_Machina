//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_AdvancedGirdBuilderGridHelper : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  static readonly variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static readonly Vector3 s_RotationModifier = new Vector3(0f, 90f, 0f);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Static functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static Vector3 GetParentPositionAlignment(ABS_BuildingElement p_Element)
        {
            ABS_AdvancedGridBuilderSettings settings = p_Element.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings;
            switch (p_Element.AdvancedGridType)
            {
                case ABS_AdvancedGridType.EdgeVertical:
                    return new Vector3(settings.HalfGridSize.x, settings.HalfGridSize.y * -1, settings.HalfGridSize.z);
                case ABS_AdvancedGridType.Corner:
                    return new Vector3(settings.HalfGridSize.x, 0, settings.HalfGridSize.z);

                case ABS_AdvancedGridType.Wall:
                    return new Vector3(0f, settings.HalfGridSize.y * -1, settings.HalfGridSize.z);
                case ABS_AdvancedGridType.EdgeHorizontal:
                    return new Vector3(0f, 0, settings.HalfGridSize.z);
                case ABS_AdvancedGridType.Center:
                    return new Vector3(0f, settings.HalfGridSize.y * -1, 0);
                case ABS_AdvancedGridType.Floor:
                default:
                    return Vector3.zero;
            }
        }

        public static bool RotationByPositionIsNeeded(ABS_BuildingElement p_Element, ABS_AdvancedGridBuilding m_Building)
        {
            Vector3 gridSize = (p_Element.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings).GridSize;
            return RotationByPositionIsNeeded(p_Element.AdvancedGridType,
                                            p_Element.transform.localPosition,
                                            m_Building.BuildingPositionModifier,
                                            gridSize);
        }

        public static bool RotationByPositionIsNeeded(in ABS_AdvancedGridType p_Type, in Vector3 p_LocalPosition, in Vector3 p_BuildingModifier, in Vector3 p_GridSize)
        {
            return ((p_Type == ABS_AdvancedGridType.Wall || p_Type == ABS_AdvancedGridType.EdgeHorizontal)
                    && IsSnappingOnTheGrid(p_LocalPosition.z - p_BuildingModifier.z, p_GridSize.z)
                    && !IsSnappingOnTheGrid(p_LocalPosition.x - p_BuildingModifier.x, p_GridSize.x));
        }

        public static bool IsSnappingOnTheGrid(in float p_Position, in float p_GridSize)
        {
            float rest = Math.Abs(p_Position % p_GridSize);
            return rest < 0.0001 || Math.Abs(rest - p_GridSize) < 0.001f;
        }

        public static Vector3 GetGridPosition(Vector3 p_RaycastPosition, in ABS_BuildingElement p_BuildingElement)
        {
            ABS_AdvancedGridBuilderSettings settings = p_BuildingElement.PositionAlgorithmSettings as ABS_AdvancedGridBuilderSettings;
            if (settings == null)
            {
                REST_Logging.Error("ABS_AdvancedGridBuilder", $"Can not get the setting of ABS_BuildingElement : {p_BuildingElement.name}");
                return p_RaycastPosition;
            }

            return GetGridPosition(p_RaycastPosition, p_BuildingElement, settings);
        }

        public static Vector3 GetGridPosition(in Vector3 p_PositionToCheck, in ABS_BuildingElement p_BuildingElement, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            switch (p_BuildingElement.AdvancedGridType)
            {
                case ABS_AdvancedGridType.Floor:
                    {
                        return GetGridPositionFloor(p_PositionToCheck, p_Settings);
                    }
                case ABS_AdvancedGridType.Corner:
                    {
                        return GetGridPositionCorner(p_PositionToCheck, p_Settings);
                    }
                case ABS_AdvancedGridType.Center:
                    {
                        return GetGridPositionCenter(p_PositionToCheck, p_Settings);
                    }
                case ABS_AdvancedGridType.EdgeVertical:
                    {
                        return GetGridPositionEdgeVertical(p_PositionToCheck, p_Settings);
                    }
                case ABS_AdvancedGridType.EdgeHorizontal:
                case ABS_AdvancedGridType.Wall:
                    {
                        return GetGridPositionWallOrHEdge(p_PositionToCheck, p_BuildingElement, p_Settings);
                    }
            }

            return Vector3.zero;
        }


        //If the raycast hit exactly the edge of the grid the algorithm can not decide which snappoint is the nearest
        //becasue both snappoint are on the same distance.
        //In that case a Z fight like effect and the element is jumping between the 2 snappoint.
        //For fixing that if the raycast is exactly on the edge of the grid we move the hitpoint with 0.001f to the normal of the raycast hitpoint
        public static Vector3 FixGridEdgeCase(in Vector3 p_OriginalRaycastLicalPosition, in Vector3 p_GridSize, Vector3 p_HitNormal)
        {
            bool p_IsHalfWayX = Math.Abs(Math.Abs(p_OriginalRaycastLicalPosition.x % p_GridSize.x) - (p_GridSize.x / 2)) < 0.001f;
            bool p_IsHalfWayY = Math.Abs(Math.Abs(p_OriginalRaycastLicalPosition.y % p_GridSize.y) - (p_GridSize.y / 2)) < 0.001f;
            bool p_IsHalfWayZ = Math.Abs(Math.Abs(p_OriginalRaycastLicalPosition.z % p_GridSize.z) - (p_GridSize.z / 2)) < 0.001f;

            return new Vector3(
                p_IsHalfWayX ? ((p_HitNormal.x <= 0 ? -1 : 1) * 0.01f) + p_OriginalRaycastLicalPosition.x : p_OriginalRaycastLicalPosition.x,
                p_IsHalfWayY ? ((p_HitNormal.y <= 0 ? -1 : 1) * 0.01f) + p_OriginalRaycastLicalPosition.y : p_OriginalRaycastLicalPosition.y,
                p_IsHalfWayZ ? ((p_HitNormal.z <= 0 ? -1 : 1) * 0.01f) + p_OriginalRaycastLicalPosition.z : p_OriginalRaycastLicalPosition.z
                );
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Static functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static Vector3 GetGridPositionFloor(in Vector3 p_PositionToCheck, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            return new Vector3(GetNearestGridPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetNearestGridPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetNearestGridPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
        }

        private static Vector3 GetGridPositionCorner(in Vector3 p_PositionToCheck, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            return new Vector3(GetShiftedNearestPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetNearestGridPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetShiftedNearestPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
        }

        private static Vector3 GetGridPositionCenter(in Vector3 p_PositionToCheck, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            return new Vector3(GetNearestGridPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetShiftedNearestPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetNearestGridPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
        }

        private static Vector3 GetGridPositionEdgeVertical(in Vector3 p_PositionToCheck, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            return new Vector3(GetShiftedNearestPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetShiftedNearestPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetShiftedNearestPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
        }

        private static Vector3 GetGridPositionWallOrHEdge(in Vector3 p_PositionToCheck, in ABS_BuildingElement p_BuildingElement, in ABS_AdvancedGridBuilderSettings p_Settings)
        {
            if (p_BuildingElement.AdvancedGridAxisType != ABS_AdvancedGridAxisType.Both)
            {
                if (p_BuildingElement.AdvancedGridType == ABS_AdvancedGridType.Wall)
                {
                    if (p_BuildingElement.AdvancedGridAxisType == ABS_AdvancedGridAxisType.X)
                    {
                        return new Vector3(GetShiftedNearestPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetShiftedNearestPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetNearestGridPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
                    }
                    else if (p_BuildingElement.AdvancedGridAxisType == ABS_AdvancedGridAxisType.Z)
                    {
                        return new Vector3(GetNearestGridPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetShiftedNearestPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetShiftedNearestPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
                    }
                }
                else
                {
                    if (p_BuildingElement.AdvancedGridAxisType == ABS_AdvancedGridAxisType.X)
                    {
                        return new Vector3(GetShiftedNearestPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetNearestGridPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetNearestGridPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
                    }
                    else if (p_BuildingElement.AdvancedGridAxisType == ABS_AdvancedGridAxisType.Z)
                    {
                        return new Vector3(GetNearestGridPosition(p_PositionToCheck.x, p_Settings.GridSize.x), GetNearestGridPosition(p_PositionToCheck.y, p_Settings.GridSize.y), GetShiftedNearestPosition(p_PositionToCheck.z, p_Settings.GridSize.z));
                    }
                }
            }

            float nearestX = GetNearestHalfGridPosition(p_PositionToCheck.x, p_Settings.GridSize.x);
            float nearestY = p_BuildingElement.AdvancedGridType == ABS_AdvancedGridType.Wall
                ? GetShiftedNearestPosition(p_PositionToCheck.y, p_Settings.GridSize.y)
                : GetNearestGridPosition(p_PositionToCheck.y, p_Settings.GridSize.y);
            float nearestZ = GetNearestHalfGridPosition(p_PositionToCheck.z, p_Settings.GridSize.z);

            bool isSNappingOnTheGridX = IsSnappingOnTheGrid(nearestX, p_Settings.GridSize.x);
            bool isSNappingOnTheGridZ = IsSnappingOnTheGrid(nearestZ, p_Settings.GridSize.z);

            Vector3 result = new Vector3(nearestX, nearestY, nearestZ);

            if (isSNappingOnTheGridX)
            {
                if(isSNappingOnTheGridZ)
                {
                    result = CorrectPosition(p_PositionToCheck, result, p_Settings.GridSize);
                }
                else
                {
                    return result;
                }
            }
            else
            {
                if (isSNappingOnTheGridZ)
                {
                    return result;
                }
                else
                {
                    result = CorrectPosition(p_PositionToCheck, result, p_Settings.GridSize);
                }
            }

            return result;
        }

        private static Vector3 CorrectPosition (Vector3 p_PositionToCheck, Vector3 p_NearestPosition, Vector3 p_GridSize)
        {
            Vector3 nearestZPosition = new Vector3(
                p_NearestPosition.x,
                p_NearestPosition.y,
                p_NearestPosition.z - ((p_PositionToCheck.z < p_NearestPosition.z ? 0.5f : -0.5f) * p_GridSize.z));

            Vector3 nearestXPosition = new Vector3(
                p_NearestPosition.x - ((p_PositionToCheck.x < p_NearestPosition.x ? 0.5f : -0.5f) * p_GridSize.x),
                p_NearestPosition.y,
                p_NearestPosition.z);

            float distanceZPosition = Vector3.Distance(p_PositionToCheck, nearestZPosition);
            float distanceXPosition = Vector3.Distance(p_PositionToCheck, nearestXPosition);

            return distanceZPosition < distanceXPosition ? nearestZPosition : nearestXPosition;
        }

        private static float GetNearestGridPosition(in float p_Position, in float p_GridSize)
        {
            return p_GridSize * (float)Math.Round(p_Position / p_GridSize, 0);
        }

        private static float GetNearestHalfGridPosition(in float p_Position, in float p_GridSize)
        {
            return GetNearestGridPosition(p_Position, (0.5f * p_GridSize));
        }

        private static float GetShiftedNearestPosition(in float p_Position, in float p_GridSize)
        {
            //Remove the half of the gridsize
            //Calualcte the grid Position
            //Add back the grid position
            //It will work like that positioning with shifting with half grid size
            return GetNearestGridPosition((p_Position - (0.5f * p_GridSize)), p_GridSize) + (0.5f * p_GridSize);
        }
    }
}