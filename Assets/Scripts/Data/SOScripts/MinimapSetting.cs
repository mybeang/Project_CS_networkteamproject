using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MinimapSettingsSO", menuName = "Scriptable Objects/MinimapSettingsSO")]
public class MinimapSetting : ScriptableObject
{
    [SerializeField] private PlayerTeamEnum _teamNum;
    [SerializeField] private Material _minimapMaterial;
    [SerializeField] private RenderTexture _maximapTexture;
    
    public PlayerTeamEnum TeamNum => _teamNum;
    public Material MinimapMaterial => _minimapMaterial;
    public RenderTexture MaximapTexture => _maximapTexture;
}