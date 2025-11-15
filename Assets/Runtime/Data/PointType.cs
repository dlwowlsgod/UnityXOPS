namespace UnityXOPS
{
    public enum PointType : byte
    {
        Human = 1,
        Weapon = 2,
        Path = 3,
        Parameter = 4,
        Object = 5,
        HumanNoPrimaryWeapon = 6,
        RandomWeapon = 7,
        RandomPath = 8,
        EventSuccess = 10,
        EventFailure = 11,
        EventIfHumanKilled = 12,
        EventIfHumanArrived = 13,
        EventInvokePathWait = 14,
        EventIfObjectDestroyed = 15,
        EventIfHumanArrivedWithCase = 16,
        EventTimer = 17,
        EventMessage = 18,
        EventChangeTeamNumberTo0 = 19
    }
}