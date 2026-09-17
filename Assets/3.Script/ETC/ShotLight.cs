using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ShotLight : MonoBehaviour
{
    [SerializeField] private GameObject light;
    [SerializeField] private ParticleSystem particle;
    private bool canLight = true;

    private void Update()
    {
        if (particle.isPlaying && canLight)
        {
            StartCoroutine(adsf());
        }
        else
        {
            StopCoroutine(adsf());
            light.SetActive(false);
        }
    }
    private IEnumerator adsf()
    {
        canLight = false;
        light.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        light.SetActive(false);
        canLight = true;
    }
}
