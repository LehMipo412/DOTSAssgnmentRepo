using Unity.Entities;
using Unity.Mathematics;

public struct MoveForward : IComponentData
{
    public float3 Velocity;
    public float3 Position;
    public float Lifetime;
    public float MaxLifetime;
    public float Size;
    public float MaxSpeed;
    public bool IsAscent;      // Whether the particle is in ascent mode
    public float AscentTime;   // Time spent moving upwards

}
