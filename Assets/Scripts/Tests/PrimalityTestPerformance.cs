using System;
using Unity.PerformanceTesting;
using NUnit.Framework;
using Prime;

public class PrimalityTestPerformance
{
	private const uint SMALL_COMPOSITE = 75,
						MEDIUM_COMPOSITE = 666,
						LARGE_COMPOSITE = LARGE_PRIME - 2,
						XL_COMPOSITE = XL_PRIME - 2,
						SMALL_PRIME = 97,
						MEDIUM_PRIME = 563,
						LARGE_PRIME = 408469,
						XL_PRIME = int.MaxValue; // Today I learned

	#region SERIAL
	[Test, Performance]
	public void SerialSmallComposite() => Measure10X(() => PrimalityTest.TestPrime(SMALL_COMPOSITE, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialMidComposite() => Measure10X(() => PrimalityTest.TestPrime(MEDIUM_COMPOSITE, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialLargeComposite() => Measure10X(() => PrimalityTest.TestPrime(LARGE_COMPOSITE, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialXLComposite() => Measure10X(() => PrimalityTest.TestPrime(XL_COMPOSITE, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialSmallPrime() => Measure10X(() => PrimalityTest.TestPrime(SMALL_PRIME, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialMidPrime() => Measure10X(() => PrimalityTest.TestPrime(MEDIUM_PRIME, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialLargePrime() => Measure10X(() => PrimalityTest.TestPrime(LARGE_PRIME, PrimalityTest.TestMethod.Serial));

	[Test, Performance]
	public void SerialXLPrime() => Measure10X(() => PrimalityTest.TestPrime(XL_PRIME, PrimalityTest.TestMethod.Serial));
	#endregion

	#region JOB
	[Test, Performance]
	public void JobSmallComposite() => Measure10X(() => PrimalityTest.TestPrime(SMALL_COMPOSITE, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobMidComposite() => Measure10X(() => PrimalityTest.TestPrime(MEDIUM_COMPOSITE, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobLargeComposite() => Measure10X(() => PrimalityTest.TestPrime(LARGE_COMPOSITE, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobXLComposite() => Measure10X(() => PrimalityTest.TestPrime(XL_COMPOSITE, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobSmallPrime() => Measure10X(() => PrimalityTest.TestPrime(SMALL_PRIME, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobMidPrime() => Measure10X(() => PrimalityTest.TestPrime(MEDIUM_PRIME, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobLargePrime() => Measure10X(() => PrimalityTest.TestPrime(LARGE_PRIME, PrimalityTest.TestMethod.Job));

	[Test, Performance]
	public void JobXLPrime() => Measure10X(() => PrimalityTest.TestPrime(XL_PRIME, PrimalityTest.TestMethod.Job));
	#endregion

	private void Measure10X(Action toTest) => Measure.Method(toTest).WarmupCount(5).MeasurementCount(10).Run();
}
