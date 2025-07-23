using Unity.Jobs;
using UnityEngine;

public class PrimeEmployment : IJobParallelFor
{

    public int employedSusPrime;
    public bool isPrime = true;
    public void Execute(int index)
    {
        if (employedSusPrime%index ==0)
        {
            isPrime = false;
        }
    }
}
