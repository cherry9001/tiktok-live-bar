using UnityEngine;
using System.Collections;

public class SmokeMachineController : MonoBehaviour
{
    private ParticleSystem[] smokeMachines;

    void Start()
    {
        smokeMachines = GetComponentsInChildren<ParticleSystem>();
        StartCoroutine(SmokeRoutine());
    }

    private IEnumerator SmokeRoutine()
    {
        while (true)
        {
            // Play all smoke machines
            foreach (var ps in smokeMachines)
            {
                if (ps != null) ps.Play();
            }
            
            // Wait for 15 seconds of emission
            yield return new WaitForSeconds(15f);
            
            // Stop emitting (particles will fade out naturally based on their lifetime)
            foreach (var ps in smokeMachines)
            {
                if (ps != null) ps.Stop();
            }
            
            // Wait for 45 seconds before the next burst
            yield return new WaitForSeconds(45f);
        }
    }
}
