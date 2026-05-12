public interface ILocalSceneLoader
{
    public void LoadScene(string sceneName);
    public void LoadScene(int sceneId);
    public string GetCurrentSceneName();
}
