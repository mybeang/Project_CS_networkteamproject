using UnityEngine;

public class InputSystemManager : Manager<InputSystemManager>, IInputSystem
{
    private InputSystem_Actions _inputSystemActions;
    
    protected override void Register() => ServiceLocator.Register<IInputSystem>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IInputSystem>(this);

    protected override void Init()
    {
        _inputSystemActions = new InputSystem_Actions();
        Debug.Log($"Input System Manager initialized : {_inputSystemActions.GetType()}"); 
        _inputSystemActions.Enable();
    }

    private void OnDisable()
    {
        _inputSystemActions.Disable();
    }
    
    public InputSystem_Actions GetInputSystem() => _inputSystemActions;
}