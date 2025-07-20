using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Syncs the velocity to the transform.
/// </summary>
[BurstCompile, UpdateBefore(typeof(TransformSystemGroup))]
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
		state.Dependency = new MoveJob() { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct MoveJob : IJobEntity
	{
		public float deltaTime;

		public readonly void Execute(in Velocity velocity, ref LocalTransform trans) => trans.Position += velocity.velocity * deltaTime;
	}
}

/// <summary>
/// Sets the last position of the entity.
/// </summary>
[BurstCompile, UpdateAfter(typeof(TransformSystemGroup))]
public partial struct SetDeathPosition : ISystem
{
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAllRW<SpawnOnLifeTimeExpireCleanup>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		state.Dependency = new SetPositionJob().ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct SetPositionJob : IJobEntity
	{
		public readonly void Execute(in LocalTransform trans, ref SpawnOnLifeTimeExpireCleanup lifetime) => lifetime.position = trans.Position;
	}
}

/// <summary>
/// Clamps the velocity.
/// </summary>
[BurstCompile, UpdateBefore(typeof(VelocityToTransform))]
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
[BurstCompile, UpdateBefore(typeof(ClampVelocity))]
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
		state.Dependency = new GravityJob() { gravityTimesDeltaTime = GRAVITY * SystemAPI.Time.DeltaTime }.ScheduleParallel(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct GravityJob : IJobEntity
	{
		public float3 gravityTimesDeltaTime;

		public readonly void Execute(ref Velocity velocity, in GravityScale scale) => velocity += gravityTimesDeltaTime * scale;
	}
}

/// <summary>
/// Counts the lifetime of the entity, and destroys it if lifetime reaches 0.
/// </summary>
[BurstCompile, UpdateInGroup(typeof(SimulationSystemGroup))]
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

/// <summary>
/// Sets the initial position of the entity when it first spawns.
/// </summary>
[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup)), UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial struct SetInitialPositionSystem : ISystem
{
	private EntityQuery _entityQuery;

	public void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<InitialPosition>().WithAllRW<LocalTransform>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		state.Dependency = new SetInitialPositionJob() { ecb = endSimulationECB }.Schedule(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct SetInitialPositionJob : IJobEntity
	{
		public EntityCommandBuffer ecb;

		public readonly void Execute(in InitialPosition initialPosition, ref LocalTransform transform, Entity entity)
		{
			transform.Position = initialPosition;
			ecb.RemoveComponent<InitialPosition>(entity);
		}
	}
}

/// <summary>
/// Sets the initial velocity of the entity when it first spawns.
/// </summary>
[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup)), UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
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
		var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		state.Dependency = new SetInitialVelocityJob() { ecb = endSimulationECB }.Schedule(_entityQuery, state.Dependency);
	}

	[BurstCompile]
	private partial struct SetInitialVelocityJob : IJobEntity
	{
		public EntityCommandBuffer ecb;

		public readonly void Execute(in InitialVelocity initialVelocity, Entity entity)
		{
			ecb.AddComponent<Velocity>(entity, new Random((uint)System.HashCode.Combine(entity.Index, entity.Version)).NextFloat3(initialVelocity.min, initialVelocity.max));
			ecb.RemoveComponent<InitialVelocity>(entity);
		}
	}
}

/// <summary>
/// Adds the cleanup component responsible for spawning entities after this entity is destroyed.
/// </summary>
[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup)), UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial struct AddCleanUpComponent : ISystem
{
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<SpawnOnLifeTimeExpire>().WithNone<SpawnOnLifeTimeExpireCleanup>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		foreach ((var lifetimeExpire, var entity) in SystemAPI.Query<SpawnOnLifeTimeExpire>().WithNone<SpawnOnLifeTimeExpireCleanup>().WithEntityAccess())
		{
			endSimulationECB.AddComponent<SpawnOnLifeTimeExpireCleanup>(entity, new() { toSpawn = lifetimeExpire.toSpawn, count = lifetimeExpire.count });
			endSimulationECB.RemoveComponent<SpawnOnLifeTimeExpire>(entity);
		}
	}
}

/// <summary>
/// Causes entities marked with <see cref="SpawnOnLifeTimeExpire"/> to spawn their entities on the last position.
/// </summary>
[BurstCompile, UpdateAfter(typeof(LifeTimeSystem)), UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
public partial struct SpawnEntityOnDeath : ISystem
{
	private EntityQuery _entityQuery;

	private void OnCreate(ref SystemState state)
	{
		_entityQuery = SystemAPI.QueryBuilder().WithAll<SpawnOnLifeTimeExpireCleanup>().WithNone<LifeTime>().Build();
		state.RequireForUpdate(_entityQuery);
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		var endSimulationECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
		foreach ((var lifetimeExpire, var entity) in SystemAPI.Query<SpawnOnLifeTimeExpireCleanup>().WithNone<LifeTime>().WithEntityAccess())
		{
			var entities = new NativeArray<Entity>(lifetimeExpire.count, Allocator.Temp);
			endSimulationECB.Instantiate(lifetimeExpire.toSpawn, entities);
			foreach (var newEntity in entities)
				endSimulationECB.AddComponent<InitialPosition>(newEntity, lifetimeExpire.position);
			entities.Dispose();
			endSimulationECB.RemoveComponent<SpawnOnLifeTimeExpireCleanup>(entity);
		}
	}
}