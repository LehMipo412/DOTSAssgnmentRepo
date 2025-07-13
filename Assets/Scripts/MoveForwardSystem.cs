using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ParticleSpawnerSystem : SystemBase
{
    private Entity prefab;
    private bool hasSpawned = false;

    protected override void OnCreate()
    {
        RequireForUpdate<MoveForward>();
    }

    protected override void OnStartRunning()
    {
        // Get a prefab entity with the Particle component.
        Entities.WithAll<MoveForward>().ForEach((Entity entity, in MoveForward p) =>
        {
            prefab = entity;
        }).WithoutBurst().Run();
    }

    protected override void OnUpdate()
    {
        if (hasSpawned || prefab == Entity.Null) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        float3 spawnPosition = new float3(0, 0, 0); // spawn at the origin (or adjust)
        int count = 100;

        for (int i = 0; i < count; i++)
        {
            Entity e = commandBuffer.Instantiate(prefab);

            // Correct the initial velocity to go upwards (positive Y-axis)
            float3 velocity = new float3(0f, UnityEngine.Random.Range(8f, 12f), 0f); // Ensure Y is positive
            
            commandBuffer.SetComponent(e, new MoveForward
            {
                Velocity = velocity,     // Positive Y velocity for ascent
                Position = spawnPosition,
                Lifetime = 0f,
                MaxLifetime = 2f,        // Total lifetime (including ascent and explosion)
                Size = 1f,
                MaxSpeed = 10f,
                IsAscent = true,         // Set to ascent mode initially
                AscentTime = 0f          // Start ascent timer
            });

            // Set the initial position explicitly
            commandBuffer.SetComponent(e, new LocalTransform
            {
                Position = spawnPosition,  // Ensure the spawn position is correct
                Rotation = quaternion.identity,
                Scale = 1f
            });
        }

        commandBuffer.Playback(entityManager);
        commandBuffer.Dispose();
        hasSpawned = true;
    }
}
