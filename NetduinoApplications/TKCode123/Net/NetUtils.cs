using System;
using System.Net;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;

namespace TKCode123.Net
{
    public class NetUtils
    {
        public static bool EnsureNetworkIsAvailable(ConditionWaitFunc func)
        {
            var nis = NetworkInterface.GetAllNetworkInterfaces();
            if ((nis != null) && (nis.Length == 1))
            {
                NetworkInterface ni = nis[0];
                byte[] adr = ni.PhysicalAddress;
                if ((adr[0] == 0x00) &&
                    (adr[1] == 0x00) &&
                    (adr[2] == 0x00) &&
                    (adr[3] == 0x00) &&
                    (adr[4] == 0x00) &&
                    (adr[5] == 0x00))
                {
                    Debugger.Write("Please set the MAC address to the address printed on the board. You can use the tool MFDeploy for this purpose.");
                    return false;
                }
                if (ni.IsDhcpEnabled == false)
                {
                    Debugger.Write("DHCP is disabled (WARNING: make sure that you have set a unique static IP address. You can use the MFDeploy tool for this purpose.)");
                }
            }
            else
            {
                Debugger.Write("Zero or more than one network interface found.");
                return false;
            }
            var myAdr = IPAddress.GetDefaultLocalAddress();
            var start = Utility.GetMachineTime();
            while (myAdr == IPAddress.Any)
            {
                if (func(Utility.GetMachineTime()-start) == false)  // wait until local address is set
                {
                    Debugger.Write("Waiting for default local address failed.");
                    return false;
                }
                myAdr = IPAddress.GetDefaultLocalAddress();
            }
            return true;
        }
    }
}
