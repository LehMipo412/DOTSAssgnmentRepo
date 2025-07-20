using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ParticleAuthoring : MonoBehaviour
{
    public float3 initialVelocityMin = new(0f, -20f, 0f);
	public float3 initialVelocityMax = new(0f, 20f, 0f);
	public float3 maxVelocity = 50f;
	public float3 gravityScale = new(0f, 1f, 0f);
	public float lifetime = 2f;
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
			AddComponent<InitialVelocity>(entity, new(authoring.initialVelocityMin, authoring.initialVelocityMax));
			AddComponent<Velocity>(entity);
			if (math.any(authoring.maxVelocity != float3.zero))
				AddComponent<MaxVelocity>(entity, authoring.maxVelocity);
			if (math.any(authoring.gravityScale != float3.zero))
				AddComponent<GravityScale>(entity, authoring.gravityScale);
			AddComponent<LifeTime>(entity, authoring.lifetime);
			if (authoring.toSpawnOnDeath && authoring.spawnCount > 0)
				AddComponent<SpawnOnLifeTimeExpire>(entity, new() { toSpawn = GetEntity(authoring.toSpawnOnDeath, TransformUsageFlags.Dynamic), count = authoring.spawnCount });
		}
	}
}
