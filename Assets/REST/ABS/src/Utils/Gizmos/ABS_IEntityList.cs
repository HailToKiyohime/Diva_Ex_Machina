//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IEntity
    {
        public ABS_IEntity Clone();
        public string Name { get; }
    }

    public interface ABS_IEntityList
    {
        public int EntityCount { get; }
        public ABS_IEntity GetEntity(int p_Index);
        public string GetName(int p_Index);
        public void Remove(int p_Index);
        public void Duplicate(int p_Index);
        public bool Top(int p_Index);
        public bool Up(int p_Index);
        public bool Down(int p_Index);
        public bool Bottom(int p_Index);
        public ABS_IEntity CreateEntity();
    }

    public interface ABS_IEntityListHolder
    {
        public ABS_IEntityList EntityList { get; }
    }
}
