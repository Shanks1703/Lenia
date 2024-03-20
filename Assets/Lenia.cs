using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Lenia : MonoBehaviour
{
    [SerializeField] private float timestep = 0.05f;
    [SerializeField] private int kernelSize = 12;
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private ComputeShader cs;

    public Texture2D gradient;
    
    private int initKernelID;
    private int drawKernelID;
    private int mainKernelID;
    private int drawKernelKernelID;

    public RenderTexture result;
    public RenderTexture cells;
    public RenderTexture cellsBuffer;
    public RenderTexture kernel;
    public RenderTexture upscale;
    
    void Start()
    {
        initKernelID = cs.FindKernel("Init");
        drawKernelID = cs.FindKernel("Draw");
        mainKernelID = cs.FindKernel("Main");
        drawKernelKernelID = cs.FindKernel("DrawKernel");
        
        upscale = RenderTexture.GetTemporary(3840, 2160);
        
        kernel = RenderTexture.GetTemporary(kernelSize, kernelSize);
        kernel.enableRandomWrite = true;
        
        cells = RenderTexture.GetTemporary(Screen.width, Screen.height);
        cells.enableRandomWrite = true;
        cells.wrapMode = TextureWrapMode.Clamp;
        
        cellsBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height);
        cellsBuffer.wrapMode = TextureWrapMode.Clamp;
        
        result = RenderTexture.GetTemporary(Screen.width, Screen.height);
        result.enableRandomWrite = true;
        result.wrapMode = TextureWrapMode.Clamp;
        
        cs.SetFloat("Timestep", timestep);
        cs.SetInt("Width", Screen.width);
        cs.SetInt("Height", Screen.height);
        
        /*cs.SetTexture(initKernelID, "Cells", cells);
        cs.Dispatch(initKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);*/
        
        GenerateGradientLookupTexture();
        StartCoroutine(Step());
    }

    private void GenerateGradientLookupTexture()
    {
        gradient = new Texture2D(128, 1, TextureFormat.RGB24, false);
        gradient.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < 128; i++)
        {
            Color c = colorGradient.Evaluate((float)i / 127);
            gradient.SetPixel(i, 0, c);
        }
        
        gradient.Apply();
    }

    private void Update()
    {
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
        cs.SetVector("PointerPosition", Input.mousePosition);
        cs.SetTexture(drawKernelKernelID, "Result", kernel);
        
        cs.Dispatch(drawKernelKernelID, Mathf.CeilToInt(kernelSize / 8f), Mathf.CeilToInt(kernelSize / 8f), 1);
        
        if (!Input.GetKey(KeyCode.Mouse0)) return;
        
        Graphics.CopyTexture(cells, cellsBuffer);
        cs.SetTexture(drawKernelID, "Result", result);
        cs.SetTexture(drawKernelID, "Cells", cells);
        cs.SetTexture(drawKernelID, "CellsBuffer", cellsBuffer);
        cs.SetTexture(drawKernelID, "Gradient", gradient);
        cs.Dispatch(drawKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);
    }

    private IEnumerator Step()
    {
        while (true)
        {
            Graphics.CopyTexture(cells, cellsBuffer);
            cs.SetTexture(mainKernelID, "Result", result);
            cs.SetTexture(mainKernelID, "Cells", cells);
            cs.SetTexture(mainKernelID, "CellsBuffer", cellsBuffer);
            cs.SetTexture(mainKernelID, "Gradient", gradient);
            cs.Dispatch(mainKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);
            Graphics.Blit(result, upscale);
            yield return new WaitForSeconds(timestep);
        }
    }

    Vector2Int GetThreadGroupSize()
    {
        cs.GetKernelThreadGroupSizes(mainKernelID, out uint x, out uint y, out _);
        return new Vector2Int(Mathf.CeilToInt((float)result.width / x), Mathf.CeilToInt((float)result.height / y));
    }
    
    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(result);
        RenderTexture.ReleaseTemporary(cells);
        RenderTexture.ReleaseTemporary(cellsBuffer);
        RenderTexture.ReleaseTemporary(kernel);
        RenderTexture.ReleaseTemporary(upscale);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), upscale);
        
        /*GUI.DrawTexture(new Rect(0, 0, 192, 108), cells);
        GUI.DrawTexture(new Rect(192, 0, 192, 108), cellsBuffer);
        GUI.DrawTexture(new Rect(384, 0, 128, 128), kernel);*/
    }
}
