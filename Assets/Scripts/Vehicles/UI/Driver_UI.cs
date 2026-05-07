using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Driver_UI : MonoBehaviour
{
    #region Show_in_Inspecter
    [Header("시야 관련")]
    [SerializeField] [Tooltip("시야를 제한하고 싶은 경우 체크")] protected bool _isMask;
    [SerializeField][Tooltip("전체를 가릴 마스크 이미지")] protected Sprite _driverOpticalImg;
    [SerializeField] [Tooltip("전체를 가릴 마스크 이미지")] protected GameObject _driverOptical;
    [SerializeField][Tooltip("가려진 마스크 중 보여질 부분의 이미지")] protected Sprite _driverOpticalMaskImg;
    [SerializeField] [Tooltip("가려진 마스크 중 보여질 부분")] protected GameObject _driverOpticalMask;
    [SerializeField] [Tooltip("보여질 범위 지정")] protected Vector2 _viewSize;
    [SerializeField][Tooltip("보여지는 부분의 위치 조정")] protected Vector2 _viewOffset;

    [Header("단순 쓰기용")]
    [SerializeField][Tooltip("타이머 TMP 등록")] private TextMeshProUGUI _timer;
    [SerializeField][Tooltip("체력 바")] protected Slider _hpSlider;
    [SerializeField][Tooltip("킬로그 표시용 TMP")] protected TextMeshProUGUI _killLog;

    [Header("작업자 실수 방지용 참조")]
    [SerializeField] protected CanvasScaler _canvas;
    #endregion

    #region basicVariable

    

    #endregion

    private void Awake()
    {
        _driverOpticalMask.SetActive(_isMask);

        if (_canvas == null)
        {
            Debug.LogError($"필수 참조 목록이 비어있습니다. {name}에 CanvasScaler를 반드시 참조해주십시오.");
        }

        _driverOptical.GetComponent<Image>().sprite = _driverOpticalImg;
        _driverOpticalMask.GetComponent<Image>().sprite = _driverOpticalMaskImg;

        (_driverOpticalMask.transform as RectTransform).sizeDelta = _viewSize;
        _driverOpticalMask.transform.localPosition = _viewOffset;
        _driverOptical.transform.localPosition = _viewOffset * new Vector2(1,-1);

        _killLog.text = "";
        _hpSlider.value = 1;

        _canvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvas.referenceResolution = new Vector2(1920,1080);
        _canvas.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvas.matchWidthOrHeight = 1;
        _canvas.referencePixelsPerUnit = 100;


        Debug.Log($"캔버스 설정 완료됌.");
    }

    protected abstract void UpdateKillLog(PlayerTeamEnum self, PlayerTeamEnum enemy);

    // private void // TODO : 나중에 GameManager에서 시간 받아오기

    public abstract void ChangeVehicleHealth(float value);
}
