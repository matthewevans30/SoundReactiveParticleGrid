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

    bool isLerping;
    bool isReturning;


    //Beat reaction customization
    public float _lerpPosTimer;
    public float _lerpPosSpeed;                     
    public Vector2 _lerpPosSpeedMinMax;
    float lerpSpeed;
    Vector2 lerpSpeedMinMax = new Vector2(0, 2);

    //Create grid
    private void OnEnable() {
        particleSystem = GetComponent<ParticleSystem>();
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
                position.x = (i * spacing.x) - middleOffset.x;
                position.y = 0;
                position.z = (j * spacing.z) - middleOffset.z;

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
        if (Input.GetKeyDown(KeyCode.M)) {
            print(particles.Length);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            print("NoiseAdded");
            GetTargetHeights();
        }
        WaveOnAudio2();
        GetTargetHeights();

    }

    //Sets height of each particle to perlinNoise value * scale - creates wave effect on grid
    void AddNoise() {
        for (int i = 0; i < resolution.z; i++) {
            for (int j = 0; j < resolution.x; j++) {
                float yOffset = Mathf.PerlinNoise(i * perlinScale, (j+perlinStartX) * perlinScale);
                //particles[(int)(i*resolution.x + j)].position.x
                Vector3 heightOffset = new Vector3(particles[(int)(i * resolution.x + j)].position.x, yOffset*distortionScale, particles[(int)(i * resolution.x + j)].position.z);
                particles[(int)(i*resolution.x + j)].position = heightOffset;
            }
        }
        particleSystem.SetParticles(particles);
        perlinStartX += perlinSpeed;
        perlinStartY += perlinSpeed;
    }

    void GetTargetHeights() {
        for (int i = 0; i < resolution.z; i++) {
            for (int j = 0; j < resolution.x; j++) {
                float height = Mathf.PerlinNoise((i+perlinStartX)*perlinScale, (j+perlinStartY)*perlinScale);
                height = Remap(height, 0, 1, -1, 1);
                distortionHeight[(int)(i * resolution.x + j)] = height * distortionScale;
                print(height);
            }
        }
        perlinStartX += perlinSpeed;
        perlinStartY += perlinSpeed;
    }


    void WaveOnAudio() {
        
        float hitStrength = Analyzer.bandBuffer[Analyzer.FocusBand];        //between 0 and 1

        if (hitStrength > threshold && !isLerping && !isReturning) {
            isLerping = true;
            _hitTimer = 0f;
        }

        if (isLerping) {
            _hitTimer += Time.deltaTime * _hitSpeed;
            _hitTimer = Mathf.Clamp01(_hitTimer);

            //we have a hit, lerp to perlin height based off strength of hit
            for (int i = 0; i < resolution.z; i++) {
                for (int j = 0; j < resolution.x; j++) {
                    float newHeight = Mathf.Lerp(0, distortionHeight[(int)(i * resolution.x + j)], _hitTimer);
                    particles[(int)(i * resolution.x + j)].position = new Vector3(particles[(int)(i * resolution.x + j)].position.x, newHeight, particles[(int)(i * resolution.x + j)].position.z);
                }
            }
            _coolDownTimer = 0f;
        }

        if(_hitTimer >= 1f) {
            isLerping = false;
            isReturning = true;
        }

        if (isReturning) {
            //lerp back to original height
            _coolDownTimer += Time.deltaTime * _coolDownSpeed;
            _coolDownTimer = Mathf.Clamp01(_coolDownTimer);

            for (int i = 0; i < resolution.z; i++) {
                for (int j = 0; j < resolution.x; j++) {

                    //Vector3 currentPos = particles[(int)(i * resolution.x + j)].position;
                    float newHeight = Mathf.Lerp(distortionHeight[(int)(i * resolution.x + j)], 0, _coolDownTimer);
                    particles[(int)(i * resolution.x + j)].position = new Vector3(particles[(int)(i * resolution.x + j)].position.x, newHeight, particles[(int)(i * resolution.x + j)].position.z);
                }
            }
        }

        if(_coolDownTimer >= 1f) {
            isReturning = false;
            _coolDownTimer = 0f;
        }

        particleSystem.SetParticles(particles);
    }

    void WaveOnAudio2() {
        float hitStrength = Analyzer.bandBuffer[Analyzer.FocusBand];        //between 0 and 1
        
        for (int i = 0; i < resolution.z; i++) {
            for (int j = 0; j < resolution.x; j++) {
                float newHeight = Mathf.Lerp(0, distortionHeight[(int)(i * resolution.x + j)], hitStrength);
                particles[(int)(i * resolution.x + j)].position = new Vector3(particles[(int)(i * resolution.x + j)].position.x, newHeight, particles[(int)(i * resolution.x + j)].position.z);
            }
        }
        particleSystem.SetParticles(particles);
    }

    public float Remap(float value, float fromMin, float fromMax, float toMin, float toMax) {
        float fromAbs = value - fromMin;
        float fromMaxAbs = fromMax - fromMin;

        float normal = fromAbs / fromMaxAbs;

        float toMaxAbs = toMax - toMin;
        float toAbs = toMaxAbs * normal;

        float to = toAbs + toMin;

        return to;
    }
}
