using Unity.Collections;
using Unity.Jobs;

public struct PrimeEmployment : IJobParallelFor
{
    
    [ReadOnly] public NativeArray<int> Number; 

    
    public NativeArray<bool> IsPrime;

    [ReadOnly] public int MaxIndex; 

    // No constructor is needed for jobs when setting members directly from the main thread.
    // The constructor you had was creating a new NativeArray, which was part of the problem.

    public void Execute(int index)
    {
        // If the number has already been found to be not prime by another thread,
        // we can early out. This is an optimization.
        // However, direct reading and writing to IsPrime[0] across threads without an Atomic Safety Handle
        // or a mutex for a single boolean can be problematic for correctness in highly concurrent scenarios
        // (though for a simple boolean flag, it often works "enough" for this case).
        // For strict correctness and larger scale, Interlocked operations or a NativeQueue/NativeHashMap
        // could be considered to report findings. For primality, simply finding *any* divisor makes it not prime.
        if (!IsPrime[0])
        {
            return;
        }

        // The 'index' in IJobParallelFor goes from 0 to length-1.
        // We want to check odd divisors starting from 3.
        // So, index 0 corresponds to divisor 3, index 1 to divisor 5, etc.
        // divisor = 2 * index + 3
        int TempIndex = 2 * index + 3;

        // Only check divisors up to MaxDivisor
        if (TempIndex > MaxIndex)
        {
            return;
        }

        // Get the number to check from the NativeArray
        int num = Number[0];

        // If the number is divisible by the current divisor, it's not prime.
        if (num % TempIndex == 0)
        {
            IsPrime[0] = false; // Mark as not prime
        }
    }
}