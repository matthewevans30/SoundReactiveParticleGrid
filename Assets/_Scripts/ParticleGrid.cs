using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGrid : MonoBehaviour
{
    //Grid setup
    ParticleSystem particleSystem;
    ParticleSystem.Particle[] particles;
    public Vector3 gridSize = new Vector3(10f, 10f, 10f);
    public Vector3 resolution = new Vector3(10f, 1f, 10f);
    int numParticles;

    //Perlin customizations
    public float perlinScale = 1f;
    public float distortionScale = 2f;
    public float perlinStartX = 0f;
    public float perlinStartY = 0f;
    public float perlinSpeed = 1f;

    public AudioAnalysis Analyzer;
    public float threshold = 0.6f;
    float _hitTimer;
    float _coolDownTimer = 1f;
    public float _hitSpeed = 1f;
    public float _coolDownSpeed = 1f;
    Vector3[] originalPos;
    float[] distortionHeight;

    float hitStrength;

    public Vector2 minMaxSpeed;
    public Vector2 minMaxEmission;
    Material particleMat;
    Color matColor;

    //Create grid
    private void OnEnable() {
        particleSystem = GetComponent<ParticleSystem>();
        particleMat = GetComponent<Renderer>().material;
        matColor = particleMat.GetColor("_EmissionColor");
        print(matColor);
        numParticles = (int)(resolution.x * resolution.y * resolution.z);
        originalPos = new Vector3[numParticles];
        distortionHeight = new float[numParticles];

        Vector3 spacing;
        Vector3 middleOffset = gridSize / 2.0f;

        spacing.x = gridSize.x / resolution.x;
        spacing.y = gridSize.y / resolution.y;
        spacing.z = gridSize.z / resolution.z;

        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();

        for(int i = 0; i < resolution.z; i++) {
            for(int j = 0; j < resolution.x; j++) {       
                Vector3 position;
                position.x = (j * spacing.x) - middleOffset.x;
                position.y = 0;
                position.z = (i * spacing.z) - middleOffset.z;

                originalPos[(int)(i * resolution.x + j)] = position;
                ep.position = position;
                particleSystem.Emit(ep, 1); 
            }
        }
        particles = new ParticleSystem.Particle[numParticles];
        particleSystem.GetParticles(particles);

        GetTargetHeights();
    }

    //Update is called once per frame
    void Update()
    {
        hitStrength = Analyzer.bandBuffer[Analyzer.FocusBand];        //between 0 and 1

        ChangeHue();
        GetTargetHeights();
        WaveOnAudio();
        particleSystem.SetParticles(particles);
    }

    void ChangeHue() {
        if(hitStrength > threshold) {
            //float h, s, v;
            float newHue = Random.Range(0f,1f);

            matColor = Color.HSVToRGB(newHue, 1f, 0.75f, true);
            //print(matColor);
        }
    }

    //update distortionHeight so we n
    void GetTargetHeights() {
        for (int i = 0; i < resolution.z; i++) {
            for (int j = 0; j < resolution.x; j++) {
                float height = Mathf.PerlinNoise((i+perlinStartX)*perlinScale, (j+perlinStartY)*perlinScale);
                height = Remap(height, 0, 1, -1, 1);
                distortionHeight[(int)(i * resolution.x + j)] = height * distortionScale;
                particles[(int)(i * resolution.x + j)].position = new Vector3(particles[(int)(i * resolution.x + j)].position.x, distortionHeight[(int)(i * resolution.x + j)], particles[(int)(i * resolution.x + j)].position.z);
            }
        }
        perlinStartX += perlinSpeed;
        perlinStartY += perlinSpeed;
    }

    void WaveOnAudio() {
        
        for (int i = 0; i < resolution.z; i++) {
            for (int j = 0; j < resolution.x; j++) {
                float newHeight = Mathf.Lerp(0, distortionHeight[(int)(i * resolution.x + j)], hitStrength);
                float newEmission = Remap(hitStrength, 0, 1, minMaxEmission.x, minMaxEmission.y);
                //print(newEmission);
                Color newColor = (matColor * newEmission);
                print(newColor);
                particleMat.SetColor("_Color", newColor);
                particleMat.SetColor("_EmissionColor", newColor);
                particles[(int)(i * resolution.x + j)].position = new Vector3(particles[(int)(i * resolution.x + j)].position.x, newHeight, particles[(int)(i * resolution.x + j)].position.z);
            }
        }
    }

    public float Remap(float value, float fromA, float toA, float fromB, float toB) {
        float normal = Mathf.InverseLerp(fromA, toA, value);
        float newValue = Mathf.Lerp(fromB, toB, normal);
        return newValue;
    }
}