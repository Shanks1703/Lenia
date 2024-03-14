using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Compute : MonoBehaviour
{
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private RawImage img;
    [SerializeField] private ComputeShader cs;

    public Texture2D gradient;
    
    private int initKernelID;
    private int drawKernelID;
    private int mainKernelID;

    private RenderTexture result;
    private RenderTexture cells;

    private bool started = false;
    
    void Start()
    {
        initKernelID = cs.FindKernel("Init");
        drawKernelID = cs.FindKernel("Draw");
        mainKernelID = cs.FindKernel("Main");
        
        cells = RenderTexture.GetTemporary(1920, 1080);
        cells.enableRandomWrite = true;
        cells.filterMode = FilterMode.Bilinear;
        cells.wrapMode = TextureWrapMode.Repeat;
        
        result = RenderTexture.GetTemporary(1920, 1080);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Bilinear;
        result.wrapMode = TextureWrapMode.Repeat;
        
        cs.SetTexture(initKernelID, "Result", result);
        cs.Dispatch(initKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);
        
        img.texture = result;
        
        GenerateGradientLookupTexture();
        StartCoroutine(Step());
    }

    private void GenerateGradientLookupTexture()
    {
        gradient = new Texture2D(128, 1, TextureFormat.RGB24, false);

        for (int i = 0; i < 128; i++)
        {
            Color c = colorGradient.Evaluate((float)i / 127);
            gradient.SetPixel(i, 1, c);
        }
        
        gradient.Apply();
    }

    private void Update()
    {
        if (!started && Input.GetKey(KeyCode.Mouse0))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(img.rectTransform, Input.mousePosition, null, out Vector2 position))
            {
                cs.SetTexture(drawKernelID, "Result", result);
                cs.SetTexture(drawKernelID, "Cells", cells);
                cs.SetTexture(drawKernelID, "Gradient", gradient);
                cs.SetVector("PointerPosition", position);
                cs.Dispatch(drawKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Space) && !started)
        {
            started = true;
            StartCoroutine(Step());
        }
    }

    IEnumerator Step()
    {
        while (true)
        {
            cs.SetTexture(mainKernelID, "Result", result);
            cs.SetTexture(mainKernelID, "Cells", cells);
            cs.SetTexture(mainKernelID, "Gradient", gradient);
            cs.Dispatch(mainKernelID, GetThreadGroupSize().x, GetThreadGroupSize().y, 1);
            yield return new WaitForSeconds(0.05f);
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
    }
}
