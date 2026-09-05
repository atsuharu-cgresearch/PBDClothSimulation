using UnityEngine;

public class PBDCloth : MonoBehaviour
{
    // 入力パラメータなど
    public int numWidth = 32;
    public int numHeight = 32;
    public float particleSpacing = 0.05f;

    public PBDSolver.SolverParameters parameters;

    // 物理シミュレーション用
    private PBDBody body;
    private PBDSolver solver;

    // 描画用
    private MeshFilter meshFilter;
    private Mesh mesh;

    private MeshRenderer meshRenderer;
    public Material material;

    private void Start()
    {
        InitPhysics();

        InitRenderers();
    }

    private void InitPhysics()
    {
        // 物理シミュレーションモデルの生成
        body = new PBDBody(numWidth, numHeight, particleSpacing);

        solver = new PBDSolver();
    }

    private void InitRenderers()
    {
        // 描画用オブジェクトの生成
        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh();

        Vector3[] vertices = new Vector3[numWidth * numHeight];
        Vector2[] uvs = new Vector2[numWidth * numHeight];

        for (int j = 0; j < numHeight; j++)
        {
            for (int i = 0; i < numWidth; i++)
            {
                int index = j * numWidth + i;

                // 位置はシェーダーでシミュレーション結果を直接代入するので、適当に与えておく
                vertices[index] = Vector3.zero;

                uvs[index] = new Vector2(
                    (float)i / (numWidth - 1),
                    (float)j / (numHeight - 1)
                    );
            }
        }

        int[] triangles = new int[(numWidth - 1) * (numHeight - 1) * 6];

        int t = 0;

        for (int j = 0; j < numHeight - 1; j++)
        {
            for (int i = 0; i < numWidth - 1; i++)
            {
                int i00 = j * numWidth + i;
                int i10 = j * numWidth + i + 1;
                int i01 = (j + 1) * numWidth + i;
                int i11 = (j + 1) * numWidth + i + 1;

                triangles[t++] = i00;
                triangles[t++] = i01;
                triangles[t++] = i10;

                triangles[t++] = i10;
                triangles[t++] = i01;
                triangles[t++] = i11;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        meshFilter.mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = material;
    }

    private void FixedUpdate()
    {
        // 物理シミュレーションを実行
        solver.Execute(body, parameters, Time.fixedDeltaTime);


        // シミュレーション結果を取得して描画
        material.SetBuffer("_Particles", body.particleBuffer);
    }

    private void OnDestroy()
    {
        body.ReleaseBuffers();
    }
}
