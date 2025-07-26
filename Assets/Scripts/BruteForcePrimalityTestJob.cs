using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

[BurstCompile]
public struct BruteForcePrimalityTestJob : IJobParallelForBatch
{
    public uint n;
	public uint offset;
    public NativeArray<uint> results;

	public BruteForcePrimalityTestJob(uint n, uint start, NativeArray<uint> results)
	{
        this.n = n;
		offset = start;
		this.results = results;
	}

	public void Execute(int startIndex, int count)
	{
		for (uint p = (uint)(startIndex + offset); p < count; p++)
		{
			if (n % p == 0)
			{
				results[JobsUtility.ThreadIndex] = p;
				break;
			}
		}
	}
}
