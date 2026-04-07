using UnityEngine;

[CreateAssetMenu(menuName = "Tidebound/Island Config")]
public class IslandConfigSO : ScriptableObject
{
    [Header("Size")]
    public float MinSize = 150f;
    public float MaxSize = 400f;

    [Header("Amplitude")]
    public float AmplitudeRatio = 0.065f;

    [Header("Frequency")]
    public float FrequencyFactor = 10f;

    [Header("Attenuation")]
    public float InnerRadiusRatio = 0.7f;
    public float OuterRadiusRatio = 1.2f;

    [Header("Submersion")]
    public float SubmergeRatio = 0.005f;

    [Header("Noise Floor")]
    public float NoiseFloorMin = 20f;
    public float NoiseFloorMax = 35f;

    [Header("Mask Perturbation")]
    public float MaskNoiseStrengthMin = 0.2f;
    public float MaskNoiseStrengthMax = 0.4f;
    public float MaskNoiseFreqMin = 0.015f;
    public float MaskNoiseFreqMax = 0.03f;
}