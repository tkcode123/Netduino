using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using TKCode123;

namespace NetduinoWeb
{
    public class Program
    {
        public static void Main()
        {
            bool isEmulator = (Microsoft.SPOT.Hardware.SystemInfo.SystemID.SKU == 3);
            TimeSpan last = new TimeSpan(-2, 0, 0, 0);
            while (true)
            {
                BlinkStart();
                if (isEmulator || TKCode123.Net.NetUtils.EnsureNetworkIsAvailable((x) => x.WaitSeconds(2)))
                {
                    if (isEmulator == false)
                    {
                        var now = Microsoft.SPOT.Hardware.Utility.GetMachineTime();
                        if (now - last > new TimeSpan(1, 0, 0, 0))
                        {
                            last = now;
                            DateTime networkTime;
                            if (TKCode123.Net.Ntp.NtpClient.TryGetNetworkTime(out networkTime))
                            {
                                Microsoft.SPOT.Hardware.Utility.SetLocalTime(networkTime);
                                TKCode123.Debugger.Write("Network time obtained: ", networkTime);
                            }
                            else
                                BlinkError();
                        }
                    }
                    using (var web = new Server(8888, null))
                    {
                        web.Handle();
                    }
                }
            }
        }
        
        private static void BlinkStart()
        {
            using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
            {
                for (int i = 0; i < 10; i++)
                {
                    onboardLED.Write(!onboardLED.Read());
                    Thread.Sleep(80);
                }
            }
        }

        internal static void BlinkError()
        {
            using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
            {
                for (int i = 0; i < 10; i++)
                {
                    onboardLED.Write(true);
                    Thread.Sleep(100);
                    onboardLED.Write(false);
                    Thread.Sleep(400);
                }
            }
        }
    }
}
