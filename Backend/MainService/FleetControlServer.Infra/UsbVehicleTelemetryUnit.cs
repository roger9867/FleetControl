using System.Diagnostics;
using System.Text;

namespace FleetControlServer.Infra;

using System.IO.Ports;

// Interface für dependeny injection, mocks und fakes, isoliertes Testen
public interface IUsbVehicleTelemetryUnit
{
    Task<string?> SendCommandAsync(string portName, string commandMessage);
    List<string> GetAvailablePortNames();
}


public class UsbVehicleTelemetryUnit : IUsbVehicleTelemetryUnit
{
    private const int Baudrate = 115200;
    
    public List<string> GetAvailablePortNames()
    {
        List<string> usbPortNames = new();
        foreach (var portName in SerialPort.GetPortNames())
        {
            usbPortNames.Add(portName);
        }
        return usbPortNames;
    }

    
    public async Task<string?> SendCommandAsync(string portName, string commandMessage)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var port = new SerialPort(portName, Baudrate)
                {
                    ReadTimeout = 4000,
                    WriteTimeout = 4000,
                    NewLine = "\n" // auf MCU terminator abstimmen
                };

                port.Open();
                Console.WriteLine("Message: {0}", commandMessage);

                // Nachricht senden
                port.Write($"{commandMessage}\n"); // explizit terminieren

                // Byteweise lesen
                var responseBuffer = new List<byte>();
                var sw = Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < port.ReadTimeout)
                {
                    if (port.BytesToRead > 0)
                    {
                        int b = port.ReadByte();
                        responseBuffer.Add((byte)b);
                        if (b == (byte)'\n') break; // Terminator erreicht
                    }
                    else
                    {
                        Thread.Sleep(1); // CPU schonen
                    }
                }

                string response = Encoding.ASCII.GetString(responseBuffer.ToArray()).TrimEnd('\r','\n');
                return string.IsNullOrEmpty(response) ? null : response;
            }
            catch (Exception ex)
            {
                // Setzt \n automatisch
                Console.WriteLine($"Port {portName} Error: {ex.Message}");
                return null;
            }
        });
    }
}
