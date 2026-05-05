using System;
using System.Collections;
using UnityEngine;

public class TornadoAttack : MonoBehaviour
{
    [SerializeField] private int _attackPointPerSeconds;
    [SerializeField][Range(3f, 10f)] private float _effectArea;
    [SerializeField] private LayerMask _playerLayer;
    private Coroutine _coroutine;
    private readonly WaitForSeconds _wait = new (1f);

    private void OnEnable() => _coroutine ??= StartCoroutine(Attack());
    private void OnDisable()
    {
        if (_coroutine == null) return;
        StopCoroutine(_coroutine);
        _coroutine = null;
    }

    private IEnumerator Attack()
    {
        while (true)
        {
            var colliders = Physics.OverlapSphere(transform.position, _effectArea, _playerLayer);
            foreach (var col in colliders)
                col.GetComponent<IDamageableObject>()?.TakeDamaged(_attackPointPerSeconds);
            yield return _wait;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _effectArea);
    }
}
