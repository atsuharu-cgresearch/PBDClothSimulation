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

        public float stiffnessStretch = 0.9f;
        public float stiffnessCompression = 0.3f;
        public float stiffnessShear = 0.8f;
        public float stiffnessShearCompression = 0.4f;

        public float stiffnessBend = 0.1f;
    }

    private ComputeShader compute;

    private int kernelExternalForce;
    private int kernelCollision;
    private int kernelPredict;
    private int kernelUpdate;

    private int kernelStretchConstH;
    private int kernelStretchConstV;
    private int kernelStretchConstDR;
    private int kernelStretchConstDL;

    private int kernelBendH;
    private int kernelBendV;

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
        kernelStretchConstDR = compute.FindKernel("StretchConstDiagonalRight");
        kernelStretchConstDL = compute.FindKernel("StretchConstDiagonalLeft");

        kernelBendH = compute.FindKernel("BendConstHorizontal");
        kernelBendV = compute.FindKernel("BendConstVertical");
    }

    public void Execute(PBDBody body, SolverParameters parameters, float dt, Vector3 anchor)
    {
        float subDt = dt / parameters.numSubsteps;

        SetCSData(body, parameters, subDt, anchor);

        for (int i = 0; i < parameters.numSubsteps; i++)
        {
            Substep(parameters.numIterations, body);
        }
    }

    private void SetCSData(PBDBody body, SolverParameters parameters, float subDt, Vector3 anchor)
    {
        compute.SetBuffer(kernelExternalForce, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelCollision, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelPredict, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelUpdate, "_Particles", body.particleBuffer);

        // ‹——£S‘©
        compute.SetBuffer(kernelStretchConstH, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelStretchConstV, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelStretchConstDR, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelStretchConstDL, "_Particles", body.particleBuffer);

        // ‹È‚°S‘©
        compute.SetBuffer(kernelBendH, "_Particles", body.particleBuffer);
        compute.SetBuffer(kernelBendV, "_Particles", body.particleBuffer);

        compute.SetInt("_NumParticles", body.numParticles);
        compute.SetInt("_NumWidth", body.numWidth);
        compute.SetInt("_NumHeight", body.numHeight);
        compute.SetFloat("_ParticleSpacing", body.particleSpacing);

        compute.SetVector("_Gravity", parameters.gravity);
        compute.SetFloat("_Damping", parameters.damping);
        compute.SetFloat("_Dt", subDt);
        compute.SetVector("_Anchor", anchor); Debug.Log(anchor);

        compute.SetFloat("_KStretch", parameters.stiffnessStretch);
        compute.SetFloat("_KCompression", parameters.stiffnessCompression);
        compute.SetFloat("_KShear", parameters.stiffnessShear);
        compute.SetFloat("_KShearCompression", parameters.stiffnessShearCompression);
        compute.SetFloat("_KBend", parameters.stiffnessBend);
    }

    private void Substep(int numIterations, PBDBody body)
    {
        // ŠO—Í‚Ì“K—p
        compute.Dispatch(kernelExternalForce, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // —\‘ªˆÊ’u‚ÌŒvŽZ
        compute.Dispatch(kernelPredict, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // S‘©ðŒ‚É‚æ‚éˆÊ’u‚ÌC³
        for (int i = 0; i < numIterations; i++)
        {
            // Dispatch‚Ì”‚ÍŒã‚ÅC³‚·‚é
            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstH, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstH, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstV, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstV, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstDR, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstDR, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

            compute.SetInt("_Step", 0);
            compute.Dispatch(kernelStretchConstDL, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
            compute.SetInt("_Step", 1);
            compute.Dispatch(kernelStretchConstDL, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    compute.SetInt("_StepX", x);
                    compute.SetInt("_StepY", y);
                    compute.Dispatch(kernelBendH, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
                    compute.Dispatch(kernelBendV, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
                }
            }
        }

        // Õ“Ë”»’è
        compute.Dispatch(kernelCollision, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);

        // ˆÊ’u‚ÌC³Œ‹‰Ê‚ð“K—p‚µA‘¬“x‚ðŒvŽZ
        compute.Dispatch(kernelUpdate, Mathf.CeilToInt((float)body.numParticles / ThreadGroupSize), 1, 1);
    }
}
