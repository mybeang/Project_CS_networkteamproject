using Unity.Collections;
using UnityEngine;

[System.Serializable]
public struct SpawnPoint
{
    [field: SerializeField] private Vector3 _position;
    [field : SerializeField][Tooltip("16글자 내로 작성")] private FixedString32Bytes _nickname;

    public Vector3 Position
    {
        get => _position;
        private set => _position = value;
    }
}
