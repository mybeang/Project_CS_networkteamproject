public class InputSystemManager : Manager<InputSystemManager>, IInputSystem
{
    private InputSystem_Actions _inputSystemActions = new ();
    
    protected override void Register() => ServiceLocator.Register<InputSystemManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<InputSystemManager>();
    
    public InputSystem_Actions GetInputSystem() => _inputSystemActions;
}