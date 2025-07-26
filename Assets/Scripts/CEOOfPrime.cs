using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CEOOfPrime : MonoBehaviour
{
    public int executivePrimeSusNum;

    [ContextMenu("AssignJobToSus")]
    public void AssignJobToSus()
    {
       
        NativeArray<int> numberToCheck = new NativeArray<int>(1, Allocator.TempJob);
        NativeArray<bool> isPrimeResult = new NativeArray<bool>(1, Allocator.TempJob);

        
        numberToCheck[0] = executivePrimeSusNum;
        isPrimeResult[0] = true; 

       
        int root = (int)Mathf.Ceil(Mathf.Sqrt(executivePrimeSusNum));

      
        if (executivePrimeSusNum < 2)
        {
            isPrimeResult[0] = false;
            Debug.Log($"The number {executivePrimeSusNum} is not prime.");
           
            numberToCheck.Dispose();
            isPrimeResult.Dispose();
            return;
        }
       
        if (executivePrimeSusNum == 2)
        {
            isPrimeResult[0] = true;
            Debug.Log($"The number {executivePrimeSusNum} is prime.");
            
            numberToCheck.Dispose();
            isPrimeResult.Dispose();
            return;
        }
       
        if (executivePrimeSusNum % 2 == 0)
        {
            isPrimeResult[0] = false;
            Debug.Log($"The number {executivePrimeSusNum} is not prime.");
           
            numberToCheck.Dispose();
            isPrimeResult.Dispose();
            return;
        }


        // Create the job instance
        var job = new PrimeEmployment
        {
            Number = numberToCheck,
            IsPrime = isPrimeResult,
            MaxIndex = root 
        };

        // Schedule the job. The length parameter here defines how many times Execute(index) will be called.
        // We need to iterate from 2 up to 'root'.
        // Since we already handled even numbers, we can iterate through odd numbers starting from 3.
        // The `length` parameter for Schedule is the number of iterations.
        // We'll calculate it such that 'index' in the job represents the odd divisor.
        // For example, if root is 10, we want to check 3, 5, 7, 9.
        // The loop in Execute will adjust 'index' to be the actual divisor.
        // A simple way is to iterate from 1 up to (root - 1) / 2 to cover odd divisors.
        // Example: 3 -> index 0 (2 * 0 + 3 = 3)
        //          5 -> index 1 (2 * 1 + 3 = 5)
        //          7 -> index 2 (2 * 2 + 3 = 7)
        // The total number of odd divisors to check up to 'root' (excluding 1)
        // will be roughly (root - 3) / 2 + 1 if root >= 3.
        // If root < 3, no iterations are needed beyond edge cases already handled.
        int iterations = (root >= 3) ? (root - 3) / 2 + 1 : 0;
        if (iterations < 0) iterations = 0; // Ensure non-negative iterations

        // Schedule the job. The batchCount (64) is a hint for how many items to process per job chunk.
        JobHandle handle = job.ScheduleByRef(iterations, 64);

        // Wait for the job to complete
        handle.Complete();

        // Read the result from the NativeArray
        if (!isPrimeResult[0])
        {
            Debug.Log($"The number {executivePrimeSusNum} is not prime.");
        }
        else
        {
            Debug.Log($"The number {executivePrimeSusNum} is prime.");
        }

        // Dispose of the NativeArrays to prevent memory leaks
        numberToCheck.Dispose();
        isPrimeResult.Dispose();
    }
}