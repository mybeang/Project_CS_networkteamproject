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

    public void SetPreset(MessageType msgType)
    {
        int index = (int)msgType;
        _titleUI.text = _presets[index].titleText;
        _closeButton.GetComponent<Image>().sprite = _presets[index].btSprite;
        _backgroundImage.sprite = _presets[index].bgSprite;
    }
    public void SetText(string text) => _textUI.text = text;
    public void Open() => _panelUI.SetActive(true);
    public void Close() => _panelUI.SetActive(false);
    public void SetCloseButtonCallback(UnityAction callback) => _closeButton.onClick.AddListener(callback);
    public void UnsetCloseButtonCallback(UnityAction callback) => _closeButton.onClick.RemoveListener(callback);
}
