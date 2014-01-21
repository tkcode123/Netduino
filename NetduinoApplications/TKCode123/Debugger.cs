using System;
using System.Text;
using Microsoft.SPOT;

namespace TKCode123
{
    public class Debugger
    {
        public static void Write(string fmt, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                var sb = new StringBuilder(64);
                sb.Append(fmt);
                for (int i = 0, n = args.Length; i < n; i++)
                {
                    if (args[i] == null)
                        sb.Append("<NULL>");
                    else if (args[i] is DateTime)
                        sb.Append(((DateTime)args[i]).ToRFCString());
                    else
                        sb.Append(args[i]);
                }
                fmt = sb.ToString();
            }
            Debug.Print(fmt);
        }
    }
}
