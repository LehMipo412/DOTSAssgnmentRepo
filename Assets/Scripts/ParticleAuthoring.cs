using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ParticleAuthoring : MonoBehaviour
{
    public float3 initialVelocityMin = new(0f, -20f, 0f), initialVelocityMax = new(0f, 20f, 0f);
	public float3 maxVelocity = 50f;
	public float3 gravityScale = new(0f, 1f, 0f);
	public float3 scaleVelocityOverLifetime = new(1f, 0f, 1f);
	public float scaleSizeOverLifeTime = 1f;
	public float minlifetime = 2f, maxLifetime = 3f;
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
			if (authoring.minlifetime == authoring.maxLifetime)
			{
				AddComponent<InitialLifeTime>(entity, authoring.minlifetime);
				AddComponent<RemainingLifeTime>(entity, authoring.minlifetime);
			}
			else
				AddComponent<RandomInitialLifeTime>(entity, new(authoring.minlifetime, authoring.maxLifetime));
		}
	}
}
