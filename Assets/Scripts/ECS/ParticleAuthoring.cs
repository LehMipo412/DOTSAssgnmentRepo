using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace ECS
{
	public class ParticleAuthoring : MonoBehaviour
	{
		public float3 initialVelocityMin = new(0f, -20f, 0f), initialVelocityMax = new(0f, 20f, 0f);
		public float3 maxVelocity = 50f;
		public float3 gravityScale = new(0f, 1f, 0f);
		public float3 scaleVelocityOverLifetime = new(1f, 0f, 1f);
		public float scaleSizeOverLifeTime = 1f;
		[FormerlySerializedAs("minlifetime")] public float minLifetime = 2f;
		public float maxLifetime = 3f;
		public GameObject toSpawnOnDeath;
		public int spawnCount = 50;
		public bool prefab;

		public class ParticleBaker : Baker<ParticleAuthoring>
		{
			public override void Bake(ParticleAuthoring authoring)
			{
				var entity = GetEntity(TransformUsageFlags.Dynamic);
				if (authoring.prefab)
					AddComponent<Prefab>(entity);
				if (math.all(authoring.initialVelocityMin == authoring.initialVelocityMax))
				{
					AddComponent<InitialVelocity>(entity, authoring.initialVelocityMin);
					AddComponent<Velocity>(entity, authoring.initialVelocityMin);
				}
				else
					AddComponent<RandomInitialVelocity>(entity, new(authoring.initialVelocityMin, authoring.initialVelocityMax));
				if (math.any(authoring.maxVelocity != float3.zero))
					AddComponent<MaxVelocity>(entity, authoring.maxVelocity);
				if (math.any(authoring.gravityScale != float3.zero))
					AddComponent<GravityScale>(entity, authoring.gravityScale);
				if (math.any(authoring.scaleVelocityOverLifetime != float3.zero))
					AddComponent<ScaleVelocityOverLifeTime>(entity, authoring.scaleVelocityOverLifetime);
				if (authoring.scaleSizeOverLifeTime != 0f)
					AddComponent<ScaleSizeOverLifeTime>(entity, authoring.scaleSizeOverLifeTime);
				if (authoring.toSpawnOnDeath && authoring.spawnCount > 0)
					AddComponent<SpawnOnLifeTimeExpire>(entity, new() { toSpawn = GetEntity(authoring.toSpawnOnDeath, TransformUsageFlags.Dynamic), count = authoring.spawnCount });
				AddComponent<InitialSize>(entity, authoring.transform.localScale.x);
				if (authoring.minLifetime == authoring.maxLifetime)
				{
					AddComponent<InitialLifeTime>(entity, authoring.minLifetime);
					AddComponent<RemainingLifeTime>(entity, authoring.minLifetime);
				}
				else
					AddComponent<RandomInitialLifeTime>(entity, new(authoring.minLifetime, authoring.maxLifetime));
			}
		}
	}
}