using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ParticleUpdateSystem : ISystem
{
    private const float gravity = -9.81f;
    private Vector3 endPos;

    public void AssignEndPos(Vector3 target)
    {
        endPos = target;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Create an EntityCommandBuffer to store structural changes (like entity destruction)
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        // Iterate through all the particles
        foreach (var (particle, transform, entity) in SystemAPI.Query<RefRW<MoveForward>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (particle.ValueRW.IsAscent)
            {
                // Update the ascent phase (move upwards)
                //particle.ValueRW.Position += (particle.ValueRW.Velocity * deltaTime); //not needed anymore
                particle.ValueRW.AscentTime += deltaTime;

                // Transition to explosion phase after 2 seconds
                if (particle.ValueRW.AscentTime >= 2f)
                {
                    // correct position (needs fixing later)
                    if (particle.ValueRW.IsAscent)
                    {
                        //particle.ValueRW.Position = PositionReader.ExplosionEndPos;
                        //Hard-coded result
                        particle.ValueRW.Position = new float3(0f, 20f, 0f);
                    }

                    particle.ValueRW.IsAscent = false;

                    // Switch to explosion phase (random direction)
                    particle.ValueRW.Velocity = UnityEngine.Random.onUnitSphere * particle.ValueRW.MaxSpeed;
                }
            }
            else
            {
                // Explosion phase: Apply gravity and reduce velocity over time
                particle.ValueRW.Lifetime += deltaTime;

                float lifetimeFraction = particle.ValueRW.Lifetime / particle.ValueRW.MaxLifetime;

                // Reduce speed over time
                particle.ValueRW.Velocity *= (1f - deltaTime / particle.ValueRW.MaxLifetime);

                // Add gravity
                particle.ValueRW.Velocity.y += gravity * deltaTime * (1f - lifetimeFraction);

                // Update position
                particle.ValueRW.Position += particle.ValueRW.Velocity * deltaTime;

                // Update size (shrink over time)
                particle.ValueRW.Size = math.lerp(1f, 0f, lifetimeFraction);

                transform.ValueRW.Position = particle.ValueRW.Position;
                transform.ValueRW.Scale = particle.ValueRW.Size;

                // Destroy particle when lifetime is up (queued in the command buffer)
                if (particle.ValueRW.Lifetime >= particle.ValueRW.MaxLifetime)
                {
                    // Queue the entity destruction to avoid structural changes during iteration
                    commandBuffer.DestroyEntity(entity);
                }
            }
        }

        // Playback the command buffer after the iteration is complete
        commandBuffer.Playback(state.EntityManager);
        commandBuffer.Dispose();
    }
}