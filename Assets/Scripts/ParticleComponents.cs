using Unity.Entities;
using Unity.Mathematics;

public struct SpawnOnLifeTimeExpire : IComponentData
{
    public Entity toSpawn;
	public int count;
}

public struct SpawnOnLifeTimeExpireCleanup : ICleanupComponentData
{
	public Entity toSpawn;
	public float3 position;
	public int count;
}

public struct RandomInitialVelocity : IComponentData
{
	public float3 min, max;

	public RandomInitialVelocity(float3 min, float3 max)
	{
		this.min = min;
		this.max = max;
	}
}

public struct InitialVelocity : IComponentData
{
	public float3 velocity;

	public static implicit operator float3(in InitialVelocity velocity) => velocity.velocity;
	public static implicit operator InitialVelocity(in float3 velocity) => new() { velocity = velocity };
}

public struct InitialPosition : IComponentData
{
	public float3 position;

	public static implicit operator float3(in InitialPosition position) => position.position;
	public static implicit operator InitialPosition(in float3 position) => new() { position = position };
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

public struct RemainingLifeTime : IComponentData
{
	public float lifetime;

	public static implicit operator float(RemainingLifeTime lifetime) => lifetime.lifetime;
	public static implicit operator RemainingLifeTime(float lifetime) => new() { lifetime = lifetime };
}

public struct RandomInitialLifeTime : IComponentData
{
	public float min, max;

	public RandomInitialLifeTime(float min,  float max)
	{
		this.min = min;
		this.max = max;
	}
}

public struct InitialLifeTime : IComponentData
{
	public float lifetime, inverseLifeTime;

	public InitialLifeTime(float lifetime)
	{
		this.lifetime = lifetime;
		inverseLifeTime = 1f / lifetime;
	}

	public static implicit operator float(InitialLifeTime lifetime) => lifetime.lifetime;
	public static implicit operator InitialLifeTime(float lifetime) => new(lifetime);
}

public struct InitialSize : IComponentData
{
	public float size;

	public static implicit operator float(InitialSize size) => size.size;
	public static implicit operator InitialSize(float size) => new() { size = size };
}

public struct GravityScale : IComponentData
{
	public float3 gravityScale;

	public static implicit operator float3(in GravityScale gravityScale) => gravityScale.gravityScale;
	public static implicit operator GravityScale(in float3 gravityScale) => new() { gravityScale = gravityScale };
}

public struct ScaleVelocityOverLifeTime : IComponentData
{
	public float3 strength;

	public static implicit operator float3(in ScaleVelocityOverLifeTime strength) => strength.strength;
	public static implicit operator ScaleVelocityOverLifeTime(in float3 strength) => new() { strength = strength };
}

public struct ScaleSizeOverLifeTime : IComponentData
{
	public float strength;

	public static implicit operator float(ScaleSizeOverLifeTime strength) => strength.strength;
	public static implicit operator ScaleSizeOverLifeTime(float strength) => new() { strength = strength };
}
