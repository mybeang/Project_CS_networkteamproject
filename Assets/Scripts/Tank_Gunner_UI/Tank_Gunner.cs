using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tank_Gunner : MonoBehaviour
{
    [SerializeField] RenderTexture _minimapRenderTexture;
    [SerializeField] Camera _MinimapCamera;
    [SerializeField] Slider _reloadSlider;
    [SerializeField] GameObject _readyToFireText;

    private void Start()
    {
        if( _MinimapCamera != null)
            _MinimapCamera.targetTexture = _minimapRenderTexture;
        else
        {
            Debug.LogError("미니맵 카메라 참조 누락 감지됌");
        }
    }

    private void ReadyToFire()
    {
        _readyToFireText.SetActive(true);
    }

    /// <summary>
    /// 포수가 사격을 했을 때 호출되어야하는 메소드
    /// 자체적으로 재장전 시간을 0으로 바꾸는 방어코드 추가되어 있음
    /// </summary>
    public void Fire()
    {
        _readyToFireText.SetActive(false);
        _reloadSlider.value = 0;
    }

    /// <summary>
    /// 재장전 시 현재 재장전 시간을 기입하여 호출 해야되며, 0~1초를 float 형태로 기입할 것.
    /// </summary>
    /// <param name="value">0~1 초</param>
    public void UpdateToReloadUI(float value)
    {
        if (value >= 1f)
            ReadyToFire();
        _reloadSlider.value = value;
    }
}
