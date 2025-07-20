using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Syncs the velocity to the transform.
/// </summary>
[BurstCompile]
public partial struct VelocityToTransform : ISystem
{
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<Velocity>().WithAllRW<LocalTransform>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		state.Dependency = new MoveJob().ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct MoveJob : IJobEntity
	{
		public readonly void Execute(in Velocity velocity, ref LocalTransform trans) => trans.Position += velocity;
	}
}

/// <summary>
/// Clamps the velocity.
/// </summary>
[BurstCompile]
public partial struct ClampVelocity : ISystem
{
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAllRW<Velocity>().WithAll<MaxVelocity>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		state.Dependency = new GravityJob().ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct GravityJob : IJobEntity
	{
		public readonly void Execute(ref Velocity velocity, in MaxVelocity maxVelocity) => velocity = math.clamp(velocity, -maxVelocity.maxVelocity, maxVelocity);
	}
}

/// <summary>
/// Applies gravity.
/// </summary>
[BurstCompile]
public partial struct GravitySystem : ISystem
{
	private static readonly float3 GRAVITY = new (0f, -9.81f, 0f);
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAllRW<Velocity>().WithAll<GravityScale>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		state.Dependency = new GravityJob() { gravityTimesDelataTime = GRAVITY * SystemAPI.Time.DeltaTime }.ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct GravityJob : IJobEntity
	{
		public float3 gravityTimesDelataTime;

		public readonly void Execute(ref Velocity velocity, in GravityScale scale) => velocity -= gravityTimesDelataTime * scale;
	}
}

/// <summary>
/// Counts the lifetime of the entity, and destroys it if lifetime reaches 0.
/// </summary>
[BurstCompile]
public partial struct LifeTimeSystem : ISystem
{
	private EntityQuery _entityQuery;

	public void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithPresentRW<LifeTime>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var endSimulationECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		var toDestroy = new NativeQueue<Entity>(Allocator.TempJob);
		state.Dependency = new LifeTimeJob() { deltaTime = SystemAPI.Time.DeltaTime, toDestroy = toDestroy.AsParallelWriter() }.ScheduleParallel(_entityQuery, state.Dependency);
		state.Dependency = new DestroyWithECBQueueJob() { toDestroy = toDestroy, ecb = endSimulationECB }.Schedule(state.Dependency);
		toDestroy.Dispose(state.Dependency);
	}

	[BurstCompile]
	private partial struct LifeTimeJob : IJobEntity
	{
		public NativeQueue<Entity>.ParallelWriter toDestroy;
		public float deltaTime;

		public readonly void Execute(ref LifeTime lifetime, Entity entity)
		{
			lifetime -= deltaTime;
			if (lifetime <= 0f)
				toDestroy.Enqueue(entity);
		}
	}

	[BurstCompile]
	public partial struct DestroyWithECBQueueJob : IJob
	{
		[ReadOnly] public NativeQueue<Entity> toDestroy;
		public EntityCommandBuffer ecb;

		public void Execute()
		{
			if (toDestroy.Count > 0)
			{
				var temp = toDestroy.ToArray(Allocator.Temp);
				ecb.DestroyEntity(temp);
				temp.Dispose();
			}
		}
	}
}

[BurstCompile]
public partial struct SetInitialVelocitySystem : ISystem
{
	private EntityQuery _entityQuery;

	public void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<InitialVelocity>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var endSimulationECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		var toDestroy = new NativeQueue<Entity>(Allocator.TempJob);
		state.Dependency = new SetInitialVelocityJob() { ecb = endSimulationECB, random = new((uint)SystemAPI.Time.ElapsedTime) }.Schedule(_entityQuery, state.Dependency);
		toDestroy.Dispose(state.Dependency);
	}

	[BurstCompile]
	private partial struct SetInitialVelocityJob : IJobEntity
	{
		public EntityCommandBuffer ecb;
		public Random random;

		public readonly void Execute(in InitialVelocity initialVelocity, Entity entity)
		{
			ecb.AddComponent<Velocity>(entity, random.NextFloat3(initialVelocity.min, initialVelocity.max));
			ecb.RemoveComponent<InitialVelocity>(entity);
		}
	}
}

[BurstCompile]
public partial struct SpawnEntityOnDeath : ISystem
{

}