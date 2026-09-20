using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ShotLight : MonoBehaviour
{
    [SerializeField] private GameObject light;
    [SerializeField] private ParticleSystem particle;
    private bool canLight = true;

    private void Awake()
    {
        TryGetComponent(out particle);
    }

    private void Update()
    {
        if (particle.isPlaying && canLight)
        {
            StartCoroutine(OnLight());
        }
        else
        {
            StopCoroutine(OnLight());
            light.SetActive(false);
        }
    }
    private IEnumerator OnLight()
    {
        canLight = false;
        light.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        light.SetActive(false);
        canLight = true;
    }
}
