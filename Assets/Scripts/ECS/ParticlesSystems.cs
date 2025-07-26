using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS
{
	#region VELOCITY
	/// <summary>
	/// Sets the initial velocity of the entity when it first spawns.
	/// </summary>
	[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup)), UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
	public partial struct SetInitialVelocitySystem : ISystem
	{
		private EntityQuery _entityQuery;

		public void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAll<RandomInitialVelocity>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
			state.Dependency = new SetInitialVelocityJob() { ecb = endSimulationECB }.Schedule(_entityQuery, state.Dependency);
		}

		[BurstCompile]
		private partial struct SetInitialVelocityJob : IJobEntity
		{
			public EntityCommandBuffer ecb;

			public readonly void Execute(in RandomInitialVelocity randomInitialVelocity, Entity entity)
			{
				var velocity = new Random((uint)System.HashCode.Combine(entity.Index, entity.Version)).NextFloat3(randomInitialVelocity.min, randomInitialVelocity.max);
				ecb.RemoveComponent<RandomInitialVelocity>(entity);
				ecb.AddComponent<InitialVelocity>(entity, velocity);
				ecb.AddComponent<Velocity>(entity, velocity);
			}
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
		public readonly void OnUpdate(ref SystemState state)
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
	/// Slows the velocity as remaining lifetime lowers.
	/// </summary>
	[BurstCompile, UpdateBefore(typeof(ClampVelocity)), UpdateAfter(typeof(LifeTimeSystem))]
	public partial struct ScaleVelocityWithLifeTime : ISystem
	{
		private EntityQuery _entityQuery;

		private void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAllRW<Velocity>().WithAll<ScaleVelocityOverLifeTime, RemainingLifeTime, InitialVelocity, InitialLifeTime>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			state.Dependency = new ScaleVelocityJob().ScheduleParallel(_entityQuery, state.Dependency);
		}

		[BurstCompile]
		private partial struct ScaleVelocityJob : IJobEntity
		{
			public readonly void Execute(ref Velocity velocity, in ScaleVelocityOverLifeTime velocityScale, in RemainingLifeTime lifetime, in InitialVelocity initialVelocity, in InitialLifeTime initialLifeTime)
			{
				var lifetimePercent = lifetime * initialLifeTime.inverseLifeTime;
				var target = initialVelocity.velocity * lifetimePercent;
				velocity = math.lerp(velocity, target, velocityScale.strength * lifetimePercent);
			}
		}
	}

	/// <summary>
	/// Applies gravity.
	/// </summary>
	[BurstCompile, UpdateBefore(typeof(ClampVelocity))]
	public partial struct GravitySystem : ISystem
	{
		private static readonly float3 GRAVITY = new(0f, -9.81f, 0f);
		private EntityQuery _entityQuery;

		private void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAllRW<Velocity>().WithAll<GravityScale>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
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
		public readonly void OnUpdate(ref SystemState state)
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
	#endregion

	#region SIZE
	/// <summary>
	/// Shrinks the scale as remaining lifetime lowers.
	/// </summary>
	[BurstCompile, UpdateBefore(typeof(TransformSystemGroup)), UpdateAfter(typeof(LifeTimeSystem))]
	public partial struct ScaleSizeWithLifeTime : ISystem
	{
		private EntityQuery _entityQuery;

		private void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<ScaleSizeOverLifeTime, RemainingLifeTime, InitialSize, InitialLifeTime>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			state.Dependency = new ScaleVelocityJob().ScheduleParallel(_entityQuery, state.Dependency);
		}

		[BurstCompile]
		private partial struct ScaleVelocityJob : IJobEntity
		{
			public readonly void Execute(ref LocalTransform transform, in ScaleSizeOverLifeTime sizeScale, in RemainingLifeTime lifetime, in InitialSize initialSize, in InitialLifeTime initialLifeTime)
			{
				var lifetimePercent = lifetime * initialLifeTime.inverseLifeTime;
				var target = initialSize.size * lifetimePercent;
				transform.Scale = math.lerp(transform.Scale, target, sizeScale.strength * lifetimePercent);
			}
		}
	}
	#endregion

	#region LIFETIME
	/// <summary>
	/// Counts the remaining lifetime of the entity, and destroys the entity if it reaches 0.
	/// </summary>
	[BurstCompile, UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct LifeTimeSystem : ISystem
	{
		private EntityQuery _entityQuery;

		public void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithPresentRW<RemainingLifeTime>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
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

			public readonly void Execute(ref RemainingLifeTime lifetime, Entity entity)
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

			public readonly void Execute()
			{
				if (toDestroy.Count <= 0)
					return;
				var temp = toDestroy.ToArray(Allocator.Temp);
				ecb.DestroyEntity(temp);
				temp.Dispose();
			}
		}
	}

	/// <summary>
	/// Sets the initial velocity of the entity when it first spawns.
	/// </summary>
	[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup)), UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
	public partial struct SetInitialLifeTime : ISystem
	{
		private EntityQuery _entityQuery;

		public void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAll<RandomInitialLifeTime>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
			state.Dependency = new SetInitialLifeTimeJob() { ecb = endSimulationECB }.Schedule(_entityQuery, state.Dependency);
		}

		[BurstCompile]
		private partial struct SetInitialLifeTimeJob : IJobEntity
		{
			public EntityCommandBuffer ecb;

			public readonly void Execute(in RandomInitialLifeTime randomInitialLifeTime, Entity entity)
			{
				var random = new Random((uint)entity.Index);
				random.NextFloat();
				var lifetime = random.NextFloat(randomInitialLifeTime.min, randomInitialLifeTime.max);
				ecb.RemoveComponent<RandomInitialLifeTime>(entity);
				ecb.AddComponent<InitialLifeTime>(entity, lifetime);
				ecb.AddComponent<RemainingLifeTime>(entity, lifetime);
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

		public void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAll<SpawnOnLifeTimeExpire>().WithNone<SpawnOnLifeTimeExpireCleanup>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			var endSimulationECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
			foreach ((var lifetimeExpire, var entity) in SystemAPI.Query<SpawnOnLifeTimeExpire>().WithNone<SpawnOnLifeTimeExpireCleanup>().WithEntityAccess())
			{
				endSimulationECB.RemoveComponent<SpawnOnLifeTimeExpire>(entity);
				endSimulationECB.AddComponent<SpawnOnLifeTimeExpireCleanup>(entity, new() { toSpawn = lifetimeExpire.toSpawn, count = lifetimeExpire.count });
			}
		}
	}

	/// <summary>
	/// Sets the last position of the entity.
	/// </summary>
	[BurstCompile, UpdateAfter(typeof(VelocityToTransform)), UpdateBefore(typeof(TransformSystemGroup))]
	public partial struct SetDeathPosition : ISystem
	{
		private EntityQuery _entityQuery;

		private void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAllRW<SpawnOnLifeTimeExpireCleanup>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
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
	/// Causes entities marked with <see cref="SpawnOnLifeTimeExpire"/> to spawn their entities on the last position.
	/// </summary>
	[BurstCompile, UpdateAfter(typeof(SetDeathPosition)), UpdateBefore(typeof(TransformSystemGroup))]
	public partial struct SpawnEntityOnDeath : ISystem
	{
		private EntityQuery _entityQuery;

		public void OnCreate(ref SystemState state)
		{
			_entityQuery = SystemAPI.QueryBuilder().WithAll<SpawnOnLifeTimeExpireCleanup>().WithNone<RemainingLifeTime>().Build();
			state.RequireForUpdate(_entityQuery);
		}

		[BurstCompile]
		public readonly void OnUpdate(ref SystemState state)
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);
			foreach ((var lifetimeExpire, var entity) in SystemAPI.Query<SpawnOnLifeTimeExpireCleanup>().WithNone<RemainingLifeTime>().WithEntityAccess())
			{
				var spawnTransform = state.EntityManager.GetComponentData<LocalTransform>(lifetimeExpire.toSpawn);
				spawnTransform.Position = lifetimeExpire.position;
				NativeArray<Entity> entities = new(lifetimeExpire.count, Allocator.Temp);
				ecb.Instantiate(lifetimeExpire.toSpawn, entities);
				foreach (var newEntity in entities)
					ecb.SetComponent(newEntity, spawnTransform);
				entities.Dispose();
				ecb.RemoveComponent<SpawnOnLifeTimeExpireCleanup>(entity);
			}
			ecb.Playback(state.EntityManager);
			ecb.Dispose();
		}
	}
	#endregion
}