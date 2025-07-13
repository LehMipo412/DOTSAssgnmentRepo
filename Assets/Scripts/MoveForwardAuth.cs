using System;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MoveForwardAuthoring : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float maxLifetime = 2f;
    public float initialSize = 1f;
    
    public float3 velocity = new float3(0f, 20f, 0f);




}

public class ParticleBaker : Baker<MoveForwardAuthoring>
{
    public static event Action<Vector3> OnReachedPeak;
    public override void Bake(MoveForwardAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MoveForward
        {
            MaxSpeed = authoring.maxSpeed,
            Lifetime = 0f,
            MaxLifetime = authoring.maxLifetime,
            Size = authoring.initialSize,

            Velocity = authoring.velocity,
        });
        float3 vel = authoring.velocity;
        float LT = authoring.maxLifetime;
        PositionReader.ExplosionEndPos = new float3(vel.x * LT, vel.y * LT, vel.z * LT);
        OnReachedPeak?.Invoke(PositionReader.ExplosionEndPos);
    }
}





