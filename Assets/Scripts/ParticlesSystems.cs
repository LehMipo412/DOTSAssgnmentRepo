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
		_entityQuery = SystemAPI.QueryBuilder().WithAll<Velocity, LocalTransform>().Build();
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
		[BurstCompile]
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
		_entityQuery = SystemAPI.QueryBuilder().WithAll<Velocity, MaxVelocity>().Build();
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
		[BurstCompile]
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
		_entityQuery = SystemAPI.QueryBuilder().WithAll<Velocity, GravityScale>().Build();
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

		[BurstCompile]
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
		_entityQuery = SystemAPI.QueryBuilder().WithAll<LifeTime>().Build();
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

		[BurstCompile]
		public readonly void Execute(ref LifeTime lifetime, Entity entity)
		{
			lifetime.lifetime -= deltaTime;
			if (lifetime.lifetime <= 0f)
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