using UnityEngine.SceneManagement;

public class LocalSceneManager : Manager<LocalSceneManager>, ILocalSceneLoader
{
    protected override void Register() => ServiceLocator.Register<ILocalSceneLoader>(this);
    protected override void Unregister() => ServiceLocator.Unregister<ILocalSceneLoader>();

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
    public void LoadScene(int sceneId) => SceneManager.LoadScene(sceneId);
    public string GetCurrentSceneName()  => SceneManager.GetActiveScene().name;
}