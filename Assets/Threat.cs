using UnityEngine;

public class Threat : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //파티클 시스템에서 생서오딘 입자를 받아와서 메테리얼 수정
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        int particleCount = ps.GetParticles(particles);
        //
        // for (int i = 0; i < particleCount; i++)
        // {
        //     // 입자의 색상을 수정하거나 메테리얼 관련 작업 수행
        //     // particles[i]
        // }

        ps.SetParticles(particles, particleCount);
    }
}
