using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Lenia : MonoBehaviour
{
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 360;
    [SerializeField] private float mu = 0.21f;
    [SerializeField] private float sigma = 0.027f;
    [SerializeField] private float timestep = 0.05f;
    [SerializeField] private int kernelSize = 12;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private ComputeShader leniaCompute;

    private Texture2D gradient;
    
    private int drawKernelID;
    private int mainKernelID;
    private int drawKernelKernelID;
    private int applyColorKernekID;

    private RenderTexture cells;
    private RenderTexture previousCells;
    private RenderTexture kernel;
    private RenderTexture result;

    private Gradient previousColorGradient;
    
    private static readonly int Timestep = Shader.PropertyToID("Timestep");
    private static readonly int Width = Shader.PropertyToID("Width");
    private static readonly int Height = Shader.PropertyToID("Height");
    private static readonly int KernelSize = Shader.PropertyToID("KernelSize");
    private static readonly int PointerPosition = Shader.PropertyToID("PointerPosition");
    private static readonly int Kernel = Shader.PropertyToID("Kernel");
    private static readonly int Cells = Shader.PropertyToID("Cells");
    private static readonly int PreviousCells = Shader.PropertyToID("PreviousCells");
    private static readonly int Mu = Shader.PropertyToID("Mu");
    private static readonly int Sigma = Shader.PropertyToID("Sigma");
    private static readonly int Result = Shader.PropertyToID("Result");
    private static readonly int Gradient1 = Shader.PropertyToID("Gradient");

    void Start()
    {
        drawKernelID = leniaCompute.FindKernel("Draw");
        mainKernelID = leniaCompute.FindKernel("Main");
        drawKernelKernelID = leniaCompute.FindKernel("DrawKernel");
        applyColorKernekID = leniaCompute.FindKernel("ApplyColor");
        
        result = RenderTexture.GetTemporary(width, height);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Bilinear;
        
        kernel = RenderTexture.GetTemporary(kernelSize, kernelSize);
        kernel.enableRandomWrite = true;
        
        cells = RenderTexture.GetTemporary(width, height);
        cells.enableRandomWrite = true;
        cells.wrapMode = TextureWrapMode.Clamp;
        
        previousCells = RenderTexture.GetTemporary(width, height);
        previousCells.wrapMode = TextureWrapMode.Clamp;
        
        leniaCompute.SetFloat(Timestep, timestep);
        leniaCompute.SetInt(Width, width);
        leniaCompute.SetInt(Height, height);
        
        GenerateGradientLookupTexture();
        StartCoroutine(Step());
    }

    private void GenerateGradientLookupTexture()
    {
        gradient = new Texture2D(512, 1, TextureFormat.RGB24, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        for (int i = 0; i < 512; i++)
        {
            Color c = colorGradient.Evaluate((float)i / (512 - 1));
            gradient.SetPixel(i, 0, c);
        }
        
        gradient.Apply();
    }

    private void Update()
    {
        if (previousColorGradient == null || previousColorGradient.Equals(colorGradient)) GenerateGradientLookupTexture(); 
        ReallocateResources();
        Draw();
    }

    private void ReallocateResources()
    {
        if (kernel.width != kernelSize || kernel.height != kernelSize)
        {
            RenderTexture.ReleaseTemporary(kernel);
            kernel = RenderTexture.GetTemporary(kernelSize, kernelSize);
            kernel.enableRandomWrite = true;
        }
    }
    
    private void Draw()
    {
        leniaCompute.SetInt(KernelSize, kernelSize);
        leniaCompute.SetVector(PointerPosition, new Vector2(Input.mousePosition.x / Screen.width * width, Input.mousePosition.y / Screen.height * height));
        
        leniaCompute.SetTexture(drawKernelKernelID, Kernel, kernel);
        leniaCompute.Dispatch(drawKernelKernelID, Mathf.CeilToInt(kernelSize / 8f), Mathf.CeilToInt(kernelSize / 8f), 1);
        
        if (!Input.GetKey(KeyCode.Mouse0)) return;
        
        Graphics.CopyTexture(cells, previousCells);
        leniaCompute.SetTexture(drawKernelID, Cells, cells);
        leniaCompute.SetTexture(drawKernelID, PreviousCells, previousCells);
        Vector2Int groups = GetThreadGroupSize(width, height);
        leniaCompute.Dispatch(drawKernelID, groups.x, groups.y, 1);
    }

    private IEnumerator Step()
    {
        while (true)
        {
            leniaCompute.SetFloat(Mu, mu);
            leniaCompute.SetFloat(Sigma, sigma);
            
            Vector2Int groups = GetThreadGroupSize(width, height);
            
            Graphics.CopyTexture(cells, previousCells);
            leniaCompute.SetTexture(mainKernelID, Cells, cells);
            leniaCompute.SetTexture(mainKernelID, PreviousCells, previousCells);
            leniaCompute.Dispatch(mainKernelID, groups.x, groups.y, 1);

            leniaCompute.SetTexture(applyColorKernekID, Result, result);
            leniaCompute.SetTexture(applyColorKernekID, Cells, cells);
            leniaCompute.SetTexture(applyColorKernekID, Gradient1, gradient);
            leniaCompute.Dispatch(applyColorKernekID, groups.x, groups.y, 1);
            
            yield return new WaitForSeconds(timestep);
        }
    }

    Vector2Int GetThreadGroupSize(int w, int h)
    {
        leniaCompute.GetKernelThreadGroupSizes(mainKernelID, out uint x, out uint y, out _);
        return new Vector2Int(Mathf.CeilToInt((float)w / x), Mathf.CeilToInt((float)h / y));
    }
    
    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(cells);
        RenderTexture.ReleaseTemporary(previousCells);
        RenderTexture.ReleaseTemporary(kernel);
        RenderTexture.ReleaseTemporary(result);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), result);
        
        //GUI.DrawTexture(new Rect(384, 0, 128, 128), kernel);
    }
}
