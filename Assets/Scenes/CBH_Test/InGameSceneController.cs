using System.Collections;
using UnityEngine;

public class InGameSceneController : MonoBehaviour
{
    private Coroutine _coroutine;
    private void Start()
    {
        _coroutine = StartCoroutine(NxScene());
    }

    private IEnumerator NxScene()
    {
        yield return new WaitForSeconds(5f);
        var loader = ServiceLocator.Get<INetworkSceneLoader>();
        loader.LoadScene("Result");
    }
}
