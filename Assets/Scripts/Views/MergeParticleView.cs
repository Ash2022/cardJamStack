using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeParticleView : MonoBehaviour
{
    [SerializeField] ParticleSystem _mainParticleSystem;
    [SerializeField] ParticleSystem _particleSplash;
    [SerializeField] ParticleSystem _particleShadow;
    [SerializeField] ParticleSystem _particleDrops;


    public void InitParticles(float scale, Color baseColor, Gradient gradColor, Gradient grad2Color)
    {
        transform.localScale = new Vector3(scale, scale, scale);

        ParticleSystem.ColorOverLifetimeModule mainColorOverLifeTime = _mainParticleSystem.colorOverLifetime;
        mainColorOverLifeTime.color = grad2Color;

        ParticleSystem.ColorOverLifetimeModule shadowColorOverLifeTime = _particleShadow.colorOverLifetime;
        shadowColorOverLifeTime.color = gradColor;

        ParticleSystem.MainModule spalshStartColor = _particleSplash.main;
        spalshStartColor.startColor = baseColor;

        ParticleSystem.MainModule dropshStartColor = _particleDrops.main;
        dropshStartColor.startColor = baseColor;

        gameObject.SetActive(true);

        Invoke("DestroyOnComplete", 1.5f);

    }

    private void DestroyOnComplete()
    {
        Destroy(gameObject);
    }
}
