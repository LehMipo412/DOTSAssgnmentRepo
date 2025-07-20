using Unity.Entities;
using Unity.Mathematics;

public struct SpawnOnLifeTimeExpire : ICleanupComponentData
{
    public Entity toSpawn;
	public int count;
}

public struct InitialVelocity : IComponentData
{
	public float3 min, max;

	public InitialVelocity(float3 min, float3 max)
	{
		this.min = min;
		this.max = max;
	}
}

public struct Velocity : IComponentData
{
	public float3 velocity;

	public static implicit operator float3(in Velocity velocity) => velocity.velocity;
	public static implicit operator Velocity(in float3 velocity) => new() { velocity = velocity };
}

public struct MaxVelocity : IComponentData
{
	public float3 maxVelocity;

	public static implicit operator float3(in MaxVelocity maxVelocity) => maxVelocity.maxVelocity;
	public static implicit operator MaxVelocity(in float3 maxVelocity) => new() { maxVelocity = maxVelocity };
}

public struct LifeTime : IComponentData
{
	public float lifetime;

	public static implicit operator float(LifeTime lifetime) => lifetime.lifetime;
	public static implicit operator LifeTime(float lifetime) => new() { lifetime = lifetime };
}

public struct GravityScale : IComponentData
{
	public float3 gravityScale;

	public static implicit operator float3(in GravityScale gravityScale) => gravityScale.gravityScale;
	public static implicit operator GravityScale(in float3 gravityScale) => new() { gravityScale = gravityScale };
}
