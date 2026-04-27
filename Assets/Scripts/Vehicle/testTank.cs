using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class testTank : NetworkBehaviour
{

    // driver 클라이언트 ID
    private NetworkVariable<ulong> _driverID = new();
    public NetworkVariable<ulong> DriverID
    {
        get { return _driverID; }
    }
    // gunner 클라이언트 ID
    private NetworkVariable<ulong> _gunnerID = new();
    public NetworkVariable<ulong> GunnerID
    {
        get { return _gunnerID; }
    }

    // 현재 HP
    private int _hp;
    // 공격 쿨다운
    private float _coolDown;
    
    //탱크 초기화 함수
    public void Init(ulong driverID, ulong gunnerID)
    {
        if (!IsServer) return;

        _driverID.Value = driverID;
        _gunnerID.Value = gunnerID;

        //로컬 변수 초기화
        //_hp = ;
        //_coolDown;
    }

    //Body 자체를 회전하고 움직이는 함수
    public void MoveBody(Vector2 input)
    {
        // 회전
       // transform.Rotate(0, input.x * rotateSpeed * Time.deltaTime, 0);

        // 이동
        // Vector3 move = transform.forward * input.y;
        // Move(move * speed * Time.deltaTime);
    }

    //Turret을 회전하는 함수
    public void MoveTurret()
    {

    }

    public void Shoot()
    {

    }
}
