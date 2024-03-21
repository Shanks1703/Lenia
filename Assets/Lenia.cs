using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Lenia : MonoBehaviour
{
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 360;
    [SerializeField] private float timestep = 0.05f;
    [SerializeField] private int kernelSize = 12;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private ComputeShader cs;

    public Texture2D gradient;
    
    private int initKernelID;
    private int drawKernelID;
    private int mainKernelID;
    private int drawKernelKernelID;
    private int bilinearUpscaleID;

    public RenderTexture cells;
    public RenderTexture previousCells;
    public RenderTexture kernel;
    public RenderTexture upscale;

    private Gradient previousColorGradient;
    
    void Start()
    {
        initKernelID = cs.FindKernel("Init");
        drawKernelID = cs.FindKernel("Draw");
        mainKernelID = cs.FindKernel("Main");
        drawKernelKernelID = cs.FindKernel("DrawKernel");
        bilinearUpscaleID = cs.FindKernel("BilinearUpscale");
        
        upscale = RenderTexture.GetTemporary(Screen.width, Screen.height);
        upscale.enableRandomWrite = true;
        upscale.filterMode = FilterMode.Bilinear;
        
        kernel = RenderTexture.GetTemporary(kernelSize, kernelSize);
        kernel.enableRandomWrite = true;
        
        cells = RenderTexture.GetTemporary(width, height);
        cells.enableRandomWrite = true;
        cells.wrapMode = TextureWrapMode.Clamp;
        
        previousCells = RenderTexture.GetTemporary(width, height);
        previousCells.wrapMode = TextureWrapMode.Clamp;
        
        cs.SetFloat("Timestep", timestep);
        cs.SetInt("Width", width);
        cs.SetInt("Height", height);
        cs.SetInt("UpscaleFactor", Screen.width / width);
        
        /*cs.SetTexture(initKernelID, "Cells", cells);
        cs.Dispatch(initKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);*/
        
        GenerateGradientLookupTexture();
        StartCoroutine(Step());
    }

    private void GenerateGradientLookupTexture()
    {
        gradient = new Texture2D(512, 1, TextureFormat.RGB24, false);
        gradient.wrapMode = TextureWrapMode.Clamp;

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
        cs.SetInt("KernelSize", kernelSize);
        cs.SetVector("PointerPosition", new Vector2(Input.mousePosition.x / Screen.width * width, Input.mousePosition.y / Screen.height * height));
        
        cs.SetTexture(drawKernelKernelID, "Result", kernel);
        cs.Dispatch(drawKernelKernelID, Mathf.CeilToInt(kernelSize / 8f), Mathf.CeilToInt(kernelSize / 8f), 1);
        
        if (!Input.GetKey(KeyCode.Mouse0)) return;
        
        Graphics.CopyTexture(cells, previousCells);
        cs.SetTexture(drawKernelID, "Cells", cells);
        cs.SetTexture(drawKernelID, "PreviousCells", previousCells);
        Vector2Int groups = GetThreadGroupSize(width, height);
        cs.Dispatch(drawKernelID, groups.x, groups.y, 1);
    }

    private IEnumerator Step()
    {
        while (true)
        {
            Graphics.CopyTexture(cells, previousCells);
            cs.SetTexture(mainKernelID, "Cells", cells);
            cs.SetTexture(mainKernelID, "PreviousCells", previousCells);
            Vector2Int groups = GetThreadGroupSize(width, height);
            cs.Dispatch(mainKernelID, groups.x, groups.y, 1);

            cs.SetTexture(bilinearUpscaleID, "Upscale", upscale);
            cs.SetTexture(bilinearUpscaleID, "Cells", cells);
            cs.SetTexture(bilinearUpscaleID, "Gradient", gradient);
            groups = GetThreadGroupSize(Screen.width, Screen.height);
            cs.Dispatch(bilinearUpscaleID, groups.x, groups.y, 1);
            
            yield return new WaitForSeconds(timestep);
        }
    }

    Vector2Int GetThreadGroupSize(int w, int h)
    {
        cs.GetKernelThreadGroupSizes(mainKernelID, out uint x, out uint y, out _);
        return new Vector2Int(Mathf.CeilToInt((float)w / x), Mathf.CeilToInt((float)h / y));
    }
    
    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(cells);
        RenderTexture.ReleaseTemporary(previousCells);
        RenderTexture.ReleaseTemporary(kernel);
        RenderTexture.ReleaseTemporary(upscale);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), upscale);
        
        /*GUI.DrawTexture(new Rect(0, 0, 192, 108), cells);
        GUI.DrawTexture(new Rect(192, 0, 192, 108), previousCells);
        GUI.DrawTexture(new Rect(384, 0, 128, 128), kernel);*/
    }
}
