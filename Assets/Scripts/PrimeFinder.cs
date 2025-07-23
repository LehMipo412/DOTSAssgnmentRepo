using UnityEngine;

public class PrimeFinder : MonoBehaviour
{

    public int primeSusNumber;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
   [ContextMenu ("CheckIfPrime")]
   public void CheckIfPrime()
    {
        bool isPrime = true;
        for (int i = 2; i <= Mathf.Sqrt(primeSusNumber); i++)
        {
            if (primeSusNumber%i ==0)
            {
                isPrime = false;
            }
        }
        if (!isPrime)
        {
            Debug.Log("this number is not prime ");
        }
        else
        {
            Debug.Log("this number is prime ");
        }
    }
}
