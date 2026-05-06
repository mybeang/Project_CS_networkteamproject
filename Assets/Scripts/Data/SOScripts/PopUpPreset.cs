using UnityEngine;


[CreateAssetMenu(fileName = "PopUpPreset", menuName = "UI/PopUpPreset")]
public class PopUpPreset : ScriptableObject
{
    public MessageType msgType;
    public string titleText;
    public Sprite bgSprite;
    public Sprite btSprite;
}