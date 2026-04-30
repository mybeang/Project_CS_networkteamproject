public struct teamInfo
{
    private ulong _driverID;
    private ulong _gunnerID;
    private PlayerableVehicleEnum _vehicle;
    private PlayerTeamEnum _teamNumber;

    public teamInfo(PlayerTeamEnum teamNum, ulong driver, ulong gunner, PlayerableVehicleEnum vehicleNum)
    {
        _teamNumber = teamNum;
        _driverID = driver;
        _gunnerID = gunner;
        _vehicle = vehicleNum;
    }

    /// <summary>
    /// 특정 팀의 전체 정보가 필요한 경우 호출
    /// </summary>
    /// <returns></returns>
    public teamInfo GetTeamInfo() => this;

    public ulong DriverID
    {
        get => _driverID;
        private set => _driverID = value;
    }

    public ulong GunnerID
    {
        get => _gunnerID;
        private set => _gunnerID = value;
    }

    /// <summary>
    /// 팀 번호 호출용
    /// </summary>
    public PlayerTeamEnum TeamNum
    {
        get => _teamNumber;
        private set => _teamNumber = value;
    }

    public PlayerableVehicleEnum VehicleNum
    {
        get => _vehicle;
        private set => _vehicle = value;
    }
}
