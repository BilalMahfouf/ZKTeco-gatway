namespace ZKTecoGatway.ZKTeco;

public interface IZKemSession : IDisposable
{
    public bool ConnectNet(string ipAddress, int port);

    public int GetLastError();

    public bool GetSerialNumber(int machineNumber, out string serialNumber);

    public bool ReadGeneralLogData(int machineNumber);

    public bool GetGeneralLogData(
         int machineNumber,
         out string enrollNumber,
         out int verifyMode,
         out int inOutMode,
         out int year,
         out int month,
         out int day,
         out int hour,
         out int minute,
         out int second,
         ref int workCode);
}

public interface IZKemSessionFactory
{
    public IZKemSession Create();
}