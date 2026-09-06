using UnityEngine;

public class PBDBody
{
    public struct PBDParticle
    {
        public Vector3 pos;
        public Vector3 predictedPos;
        public Vector3 vel;
        public float invMass;

        public PBDParticle(Vector3 p, float invM)
        {
            pos = p;
            predictedPos = Vector3.zero;
            vel = Vector3.zero;
            invMass = invM;
        }
    }

    public ComputeBuffer particleBuffer;

    // 実装したい拘束
    // 距離（たて、よこ、ななめ）
    // 曲げ
    // 自己衝突

    // その他
    // メッシュの分割を細かくする
    // 摩擦

    // HDRP Fabric Shader Graph

    public ComputeBuffer distConstBuffer;
    public ComputeBuffer bendConstBuffer;

    public int numParticles;
    public int numWidth;
    public int numHeight;
    public float particleSpacing;

    public PBDBody(int numWidth, int numHeight, float dist)
    {
        this.numWidth = numWidth;
        this.numHeight = numHeight;
        this.particleSpacing = dist;
        numParticles = numWidth * numHeight;

        // パーティクルの初期配置
        PBDParticle[] particleArray = new PBDParticle[numParticles];
        for (int i = 0; i < numHeight; i++)
        {
            for (int j = 0; j < numWidth; j++)
            {
                // 格子状にパーティクルを配置する
                // 固定するパーティクルのinvMassは0にしておく
                if ((i == 0 && j == 0)/* || (i == 0 && j == numWidth - 1)*/) particleArray[i * numWidth + j] = new PBDParticle(new Vector3(j * dist, 1.0f, i * dist), 0.0f);
                else particleArray[i * numWidth + j] = new PBDParticle(new Vector3(j * dist, 1.0f, i * dist), 1.0f);
            }
        }

        // バッファの作成
        particleBuffer = ComputeHelper.CreateStructuredBuffer(particleArray);
    }

    public void ReleaseBuffers()
    {
        ComputeHelper.Release(particleBuffer);
    }
}
