using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class MessagePopUpUIController : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleUI;
    [SerializeField] private TextMeshProUGUI _textUI;
    [SerializeField] private TextMeshProUGUI _closedBtTxtUI;
    
    [Header("Others")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _panelUI;
    [SerializeField] private List<PopUpPreset> _presets;

    private void OnDisable() => _closeButton.onClick.RemoveAllListeners();
    
    public void Open(MessageType msgType, string text, string closedText = "닫 기", UnityAction callback = null)
    {
        if (callback != null) _closeButton.onClick.AddListener(callback);
        _closeButton.onClick.AddListener(() => _panelUI.SetActive(false));
        int index = (int)msgType;
        _titleUI.text = _presets[index].titleText;
        _closeButton.GetComponent<Image>().sprite = _presets[index].btSprite;
        _backgroundImage.sprite = _presets[index].bgSprite;
        
        _textUI.text = text;  // ToDo. Message 는 추후 어떤 방향으로 넣는게 좋은지 고민 필요.
        _closedBtTxtUI.text = closedText;
        _panelUI.SetActive(true);
    }
}
