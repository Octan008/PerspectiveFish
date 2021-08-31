using UnityEngine;

public class Render : MonoBehaviour
{
    [SerializeField] GameObject plane;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] float deltaSize = 0.1f;
    [SerializeField] float waveCoef = 1.0f;
    private RenderTexture waveTexture, drawTexture;
    public RenderTexture cam0, cam1;
    public RenderTexture orth;
    public GameObject _cam0, _cam1;
    public GameObject floor;
    private Vector4 camCoord;

    private int kernelInitialize, kernelAddWave, kernelUpdate, kernelDraw;
    private ThreadSize threadSizeInitialize, threadSizeUpdate, threadSizeDraw;

    struct ThreadSize
    {
        public int x;
        public int y;
        public int z;

        public ThreadSize(uint x, uint y, uint z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    private void Start()
    {
        camCoord.x = floor.transform.localScale.x * 10 /2;
        camCoord.y = camCoord.x*2;
        camCoord.z = floor.transform.localScale.z * 10 /2;
        camCoord.w = camCoord.z*2;
        // カーネルIdの取得
        kernelInitialize = computeShader.FindKernel("Initialize");
        kernelAddWave = computeShader.FindKernel("AddWave");
        kernelUpdate = computeShader.FindKernel("Update");
        kernelDraw = computeShader.FindKernel("Draw");

        // 波の高さを格納するテクスチャの作成
        waveTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.RG32);
        waveTexture.wrapMode = TextureWrapMode.Clamp;
        waveTexture.enableRandomWrite = true;
        waveTexture.Create();
        // レンダリング用のテクスチャの作成
        drawTexture = new RenderTexture(1000, 1000, 0, RenderTextureFormat.ARGB32);
        drawTexture.enableRandomWrite = true;
        drawTexture.Create();

        // スレッド数の取得
        uint threadSizeX, threadSizeY, threadSizeZ;
        computeShader.GetKernelThreadGroupSizes(kernelInitialize, out threadSizeX, out threadSizeY, out threadSizeZ);
        threadSizeInitialize = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);
        computeShader.GetKernelThreadGroupSizes(kernelUpdate, out threadSizeX, out threadSizeY, out threadSizeZ);
        threadSizeUpdate = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);
        computeShader.GetKernelThreadGroupSizes(kernelDraw, out threadSizeX, out threadSizeY, out threadSizeZ);
        threadSizeDraw = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);

        // 波の高さの初期化
        computeShader.SetTexture(kernelInitialize, "waveTexture", waveTexture);
        computeShader.Dispatch(kernelInitialize, Mathf.CeilToInt(waveTexture.width / threadSizeInitialize.x), Mathf.CeilToInt(waveTexture.height / threadSizeInitialize.y), 1);
    }

    private void FixedUpdate()
    {
        // 波の追加
        this.computeShader.SetFloat("time", Time.time);
        this.computeShader.SetTexture(kernelAddWave, "waveTexture", waveTexture);
        this.computeShader.Dispatch(kernelAddWave, Mathf.CeilToInt(waveTexture.width / threadSizeUpdate.x), Mathf.CeilToInt(waveTexture.height / threadSizeUpdate.y), 1);

        // 波の高さの更新
        this.computeShader.SetFloat("deltaSize", deltaSize);
        this.computeShader.SetFloat("deltaTime", Time.deltaTime * 2.0f);
        this.computeShader.SetFloat("waveCoef", waveCoef);
        this.computeShader.SetTexture(kernelUpdate, "waveTexture", waveTexture);
        this.computeShader.Dispatch(kernelUpdate, Mathf.CeilToInt(waveTexture.width / threadSizeUpdate.x), Mathf.CeilToInt(waveTexture.height / threadSizeUpdate.y), 1);

        // 波の高さをもとにレンダリング用のテクスチャを作成
        this.computeShader.SetTexture(kernelDraw, "waveTexture", waveTexture);
        this.computeShader.SetTexture(kernelDraw, "drawTexture", drawTexture);
        this.computeShader.SetTexture(kernelDraw, "cam0Texture", cam0);
        this.computeShader.SetTexture(kernelDraw, "cam1Texture", cam1);
        this.computeShader.SetTexture(kernelDraw, "OrthTexture", orth);
        this.computeShader.SetVector( "_camPos0", new Vector3(
            (_cam0.transform.position.x + camCoord.x)/camCoord.y,
             (_cam0.transform.position.y),
              (_cam0.transform.position.z + camCoord.z)/camCoord.w
            ));
        this.computeShader.SetVector( "_camPos1", new Vector3(
            (_cam1.transform.position.x + camCoord.x)/camCoord.y,
             (_cam1.transform.position.y),
              (_cam1.transform.position.z + camCoord.z)/camCoord.w
            ));
        this.computeShader.SetVector("_realScale", new Vector2(camCoord.y, camCoord.w));
        this.computeShader.SetFloat("_gogleRad", 1.5f);
        this.computeShader.Dispatch(kernelDraw, Mathf.CeilToInt(drawTexture.width / threadSizeDraw.x), Mathf.CeilToInt(drawTexture.height / threadSizeDraw.y), 1);
        plane.GetComponent<Renderer>().material.mainTexture = drawTexture;
    }
}