using UnityEngine;
using TestMethod = Prime.PrimalityTest.TestMethod;

namespace Prime
{
	public class PrimalityTestMono : MonoBehaviour
	{
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

		public static bool TestPrime(uint n, TestMethod testMethod) => PrimalityTest.TestPrime(n, testMethod);
	}
}