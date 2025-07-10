using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MoveForwardAuthoring : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float maxLifetime = 2f;
    public float initialSize = 1f;

   
    
}

public class ParticleBaker : Baker<MoveForwardAuthoring>
{
    public override void Bake(MoveForwardAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MoveForward
        {
            MaxSpeed = authoring.maxSpeed,
            Lifetime = 0f,
            MaxLifetime = authoring.maxLifetime,
            Size = authoring.initialSize
        });
    }
}





