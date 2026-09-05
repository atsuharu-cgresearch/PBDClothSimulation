using UnityEngine;

public class PBDSolver
{
    [System.Serializable]
    public class SolverParameters
    {
        public int numIterations = 3;
        public int numSubsteps = 2;
        public Vector3 gravity = Physics.gravity;
        public float damping = 0.99f;
        public float stiffness = 0.9f;
    }

    private ComputeShader compute;

    private int kernelExternalForce;
    private int kernelCollision;
    private int kernelPredict;
    private int kernelUpdate;

    private int kernelStretchConstH;
    private int kernelStretchConstV;

    static readonly int ThreadGroupSize = 64;

    public PBDSolver()
    {
        compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/PBDSolver"));

        kernelExternalForce = compute.FindKernel("ExternalForce");
        kernelCollision = compute.FindKernel("Collision");
        kernelPredict = compute.FindKernel("Predict");
        kernelUpdate = compute.FindKernel("Update");

        kernelStretchConstH = compute.FindKernel("StretchConstHorizontal");
        kernelStretchConstV = compute.FindKernel("StretchConstVertical");
    }

    public void Execute(PBDBody body, SolverParameters parameters, float dt)
    {
        float subDt = dt / parameters.numSubsteps;

        SetCSData(body, parameters, subDt);

        for (int i = 0; i < parameters.numSubsteps; i++)
        {
            Substep(parameters.numIterations, body);
        }
    }

    private void SetCSData(PBDBody body, SolverParameters parameters, float subDt)
    {
        compute.SetBuffer(kernelExternalForce, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelCollision, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelPredict, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelUpdate, "_Particles", body.particleBuffer);

        compute.SetBuffer(kernelStretchConstH, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelStretchConstV, "_Particles", body.particleBuffer);

        compute.SetInt("_NumParticles", body.numParticles);
        compute.SetInt("_NumWidth", body.numWidth);
        compute.SetInt("_NumHeight", body.numHeight);
        compute.SetFloat("_ParticleSpacing", body.particleSpacing);

        compute.SetVector("_Gravity", parameters.gravity);
        compute.SetFloat("_Damping", parameters.damping);
        compute.SetFloat("_Dt", subDt);

        compute.SetFloat("_KStretchH", parameters.stiffness);
    }

    private void Substep(int numIterations, PBDBody body)
    {
        // ŠO—Í‚Ì“K—p
        compute.Dispatch(kernelExternalForce, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // Õ“Ë”»’è
        compute.Dispatch(kernelCollision, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // —\‘ªˆÊ’u‚ÌŒvŽZ
        compute.Dispatch(kernelPredict, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // S‘©ðŒ‚É‚æ‚éˆÊ’u‚ÌC³
        for (int i = 0; i < numIterations; i++)
        {
            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstH, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstH, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstV, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstV, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
        }

        // ˆÊ’u‚ÌC³Œ‹‰Ê‚ð“K—p‚µA‘¬“x‚ðŒvŽZ
        compute.Dispatch(kernelUpdate, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
    }
}
