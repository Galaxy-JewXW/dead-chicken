using UnityEngine;
using System.Linq;

public class PowerlineMaterialManager : MonoBehaviour
{
    [Header("电力线材质设置")]
    public Material powerlineMaterial;
    public Color powerlineColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 深灰色
    public float metallic = 0.8f;
    public float smoothness = 0.9f;
    public bool enableEmission = false;
    public Color emissionColor = Color.yellow;
    public float emissionIntensity = 0.5f;
    
    [Header("电塔材质设置")]
    public Material towerMaterial;
    public Color towerColor = Color.gray;
    public float towerMetallic = 0.6f;
    public float towerSmoothness = 0.4f;
    
    void Start()
    {
        CreatePowerlineMaterial();
        CreateTowerMaterial();
    }
    
    /// <summary>
    /// 创建电力线材质
    /// </summary>
    void CreatePowerlineMaterial()
    {
        if (powerlineMaterial == null)
        {
            powerlineMaterial = new Material(Shader.Find("Standard"));
        }
        
        powerlineMaterial.color = powerlineColor;
        powerlineMaterial.SetFloat("_Metallic", metallic);
        powerlineMaterial.SetFloat("_Glossiness", smoothness);
        
        if (enableEmission)
        {
            powerlineMaterial.EnableKeyword("_EMISSION");
            powerlineMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        }
        
        Debug.Log("电力线材质创建完成");
    }
    
    /// <summary>
    /// 创建电塔材质
    /// </summary>
    void CreateTowerMaterial()
    {
        if (towerMaterial == null)
        {
            towerMaterial = new Material(Shader.Find("Standard"));
        }
        
        towerMaterial.color = towerColor;
        towerMaterial.SetFloat("_Metallic", towerMetallic);
        towerMaterial.SetFloat("_Glossiness", towerSmoothness);
        
        Debug.Log("电塔材质创建完成");
    }
    
    /// <summary>
    /// 应用材质到所有电力线
    /// </summary>
    public void ApplyMaterialToPowerlines()
    {
        LineRenderer[] lineRenderers = FindObjectsOfType<LineRenderer>();
        foreach (var lr in lineRenderers)
        {
            if (lr.gameObject.name.Contains("Powerline"))
            {
                lr.material = powerlineMaterial;
            }
        }
        Debug.Log($"已应用材质到 {lineRenderers.Length} 条电力线");
    }
    
    /// <summary>
    /// 应用材质到所有电塔
    /// </summary>
    public void ApplyMaterialToTowers()
    {
        GameObject[] towers = null;
        try
        {
            towers = GameObject.FindGameObjectsWithTag("Tower");
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tower标签未定义，将通过名称查找电塔");
            // 通过名称查找电塔
            towers = FindObjectsOfType<GameObject>().Where(go => 
                go.name.Contains("Tower") || go.name.Contains("GoodTower")).ToArray();
        }
        
        foreach (var tower in towers)
        {
            Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = towerMaterial;
            }
        }
        Debug.Log($"已应用材质到 {towers.Length} 座电塔");
    }
} 