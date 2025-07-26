using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;

namespace Prime
{
	public class PrimalityTest : MonoBehaviour
	{
		/// <summary>
		/// Numbers below this threshold are trivial to test.
		/// </summary>
		private const uint TRIVIAL_THRESHOLD = 256;

		/// <summary>
		/// All primes below 256.
		/// </summary>
		private static readonly HashSet<byte> TRIVIAL_PRIMES = new()
	{
		2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
		101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199,
		211, 223, 227, 229, 233, 239, 241, 251
	};

		public uint n = 408469;
		public TestMethod method;

		[ContextMenu("TestPrime")]
		public void TestPrimeDebug()
		{
			if (TestPrime(n, method))
				Debug.Log($"\'{n}\' is prime.");
			else
				Debug.Log($"\'{n}\' is not prime.");
		}

		public static bool TestPrime(uint n, TestMethod testMethod)
		{
			if (IsTrivialToTest(n))
				return TrivialPrimalityTest(n);
			uint start = 2, end = SqRoot(n);
			return testMethod switch
			{
				TestMethod.Serial => IsPrimeSerial(n, start, end),
				TestMethod.SerialBurst => IsPrimeSerialBurst(n, start, end),
				TestMethod.JobBurst => IsPrimeJob(n, start, end),
				_ => false,
			};
		}

		private static bool IsPrimeSerial(uint n, uint start, uint end)
		{
			for (uint i = start; i <= end; i++)
			{
				if (n % i == 0)
					return false;
			}
			return true;
		}

		[BurstCompile]
		private static bool IsPrimeSerialBurst(uint n, uint start, uint end) => IsPrimeSerial(n, start, end);

		[BurstCompile]
		private static bool IsPrimeJob(uint n, uint start, uint end)
		{
			const int BATCH_SIZE = 128;
			NativeArray<uint> results = new(JobsUtility.ThreadIndexCount, Allocator.TempJob);
			var primalityTestHandle = new BruteForcePrimalityTestJob(n, start, results).Schedule(arrayLength: (int)(end - start), indicesPerJobCount: BATCH_SIZE);
			primalityTestHandle.Complete();
			var isPrime = !results.Any(r => r != 0);
			results.Dispose();
			return isPrime;
		}

		/// <returns>Whether <paramref name="n"/> is trivial to test.</returns>
		/// <seealso cref="TrivialPrimalityTest"/>
		private static bool IsTrivialToTest(uint n) => n % 2 == 0 || n < TRIVIAL_THRESHOLD;

		/// <param name="n">The prime suspect to test.</param>
		/// <returns>Whether <paramref name="n"/> is prime.</returns>
		private static bool TrivialPrimalityTest(uint n)
		{
			if (!IsTrivialToTest(n))
				throw new ArgumentOutOfRangeException($"{n} is not trivial to test.");
			if (n != 2 && n % 2 == 0)
				return false;
			return TRIVIAL_PRIMES.Contains((byte)n);
		}

		private static uint SqRoot(uint n) => (uint)Mathf.Ceil(Mathf.Sqrt(n));

		public enum TestMethod : byte
		{
			Serial,
			SerialBurst,
			JobBurst
		}
	}
}