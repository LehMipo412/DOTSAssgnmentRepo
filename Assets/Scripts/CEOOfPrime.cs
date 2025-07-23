using UnityEngine;

public class CEOOfPrime : MonoBehaviour
{
    public int executivePrimeSusNum;
    
    [ContextMenu ("AssignJobToSus")]
    public void AssignJobToSus()
    {
        var job = new PrimeEmployment { employedSusPrime = executivePrimeSusNum };

        job.Execute((int)Mathf.Sqrt(executivePrimeSusNum));

        if (!(job.isPrime))
        {
            Debug.Log("this number is not prime ");
        }
        else
        {
            Debug.Log("this number is prime ");
        }
    }

}
