using System;
using Microsoft.SPOT;
using TKCode123.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Text;
using System.Collections;
using System.Net;
using System.Reflection;
using System.IO;

namespace NetduinoWeb
{
    class Server : TinyWeb
    {
        protected readonly OutputPort _onboardLED;
        protected readonly InterruptPort _button;
        protected bool _withSDCard;

        public Server(int port, X509Certificate certificate)
            : base(port, certificate)
        {
            _onboardLED = new OutputPort(Pins.ONBOARD_LED, false);
            _button = new InterruptPort(Pins.ONBOARD_SW1, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);
            AddVariable(new InputPortVariable("Button", _button));
            AddVariable(new MachineTime());
            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
        }

        public override void Dispose()
        {
            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            if (_onboardLED != null)
                _onboardLED.Dispose();
            if (_button != null)
                _button.Dispose();
            base.Dispose();
        }

        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Shutdown("Network Address Changed");
        }

        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Shutdown("Network Availablity Changed");
        }

        protected override void PreHandle()
        {
            if (_onboardLED != null) _onboardLED.Write(true);
        }

        protected override void PostHandle()
        {
            if (_onboardLED != null) _onboardLED.Write(false);
        }

        protected virtual TinyContext CreateContext(HttpListenerContext c)
        {
            return new NetduinoContext(c, this);
        }

        class NetduinoContext : TinyContext
        {
            internal NetduinoContext(HttpListenerContext r, Server s) : base(r, s) { }

            internal string Content { get; set; }

            public override string ReplaceSymbol(string source)
            {
                if ("CONTENT".Equals(source))
                    return Content ?? "";
                return base.ReplaceSymbol(source);
            }
        }

        class VariableBase : TinyWeb.IVariable
        {
            readonly string _name;

            protected VariableBase(string name)
            {
                _name = name;
            }

            public virtual bool IsReadOnly
            {
                get { return true; }
            }

            public string Name
            {
                get { return _name; }
            }

            public virtual string ValueString
            {
                get { return ""; }
                set { throw new NotImplementedException(); }
            }
        }

        class MachineTime : VariableBase
        {
            internal MachineTime() : base("MachineTime") { }
            public override string ValueString
            {
                get
                {
                    return Microsoft.SPOT.Hardware.Utility.GetMachineTime().ToString();
                }
                set
                {
                    base.ValueString = value;
                }
            }
        }

        class InputPortVariable : VariableBase
        {
            private readonly InputPort _port;
            internal InputPortVariable(string name, InputPort port) : base(name) { _port = port; }
            public override string ValueString
            {
                get
                {
                    return _port.Read() ? "1" : "0";
                }                
            }
        }
       
        protected override bool Handle(TinyContext context)
        {
            string url = context.ClientContext.Request.RawUrl.ToUpper();
            if (context.ClientContext.Request.HttpMethod == "GET")
            {
                if (url == "/TEST")
                {
                    return Respond(context, System.Net.HttpStatusCode.OK, true, "text/html", true, "<html><head></head><body>Button={@Button@} {@MachineTime@}</body></html>");
                }
                if (url == "/RESET")
                {
                    using (var resp = context.ClientContext.Response)
                    {
                        resp.StatusCode = (int)HttpStatusCode.ResetContent;
                    }
                    return false;
                }
                if (url == "/")
                    url = "/DEFAULT.HTM";
                bool withSyms = true;
                string contentType = "text/html";
                var st = Statics.Find(url, ref withSyms, ref contentType);
                if (st != null)
                {
                    return Respond(context, HttpStatusCode.OK, withSyms, contentType, url == "/DEFAULT.HTM", st);
                }                
                if (_withSDCard && ((st = FindSDCard(url, ref withSyms, ref contentType)) != null))
                {
                    return Respond(context, HttpStatusCode.OK, withSyms, contentType, url == "/DEFAULT.HTM", st);
                }

                ((NetduinoContext)context).Content = "Page '{@RAWURL@}' not found.<br>You should update {@REFERER@}.";
                return Respond(context, HttpStatusCode.NotFound, true, "text/html", true, Statics.HTML._DEFAULT_HTM);
            }
            return base.Handle(context);
        }

        protected static object FindSDCard(string name, ref bool withSyms, ref string contentType)
        {
            StringBuilder sb = new StringBuilder("\\SD\\");
            sb.Append(name);
            sb.Replace('/', '\\');
            string replaced = sb.ToString();
            try
            {
                if (new FileInfo(replaced).Exists)
                {
                    int dot = replaced.LastIndexOf('.');
                    if (dot >= 0 && replaced.Length > dot + 1)
                    {
                        var ext = replaced.Substring(dot);
                        if (ext == ".HTM")
                        {
                            contentType = "text/html; charset=UTF-8;";
                            withSyms = true;
                        }
                        else if (ext == ".CSS")
                        {
                            contentType = "text/css; charset=UTF-8;";
                            withSyms = false;
                        }
                        else if (ext == ".JS")
                        {
                            contentType = "text/javascript; charset=UTF-8;";
                            withSyms = false;
                        }
                        else if (ext == ".ICO")
                        {
                            contentType = "image/x-icon";
                        }
                        else if (ext == ".PNG")
                        {
                            contentType = "image/png";
                        }
                    }
                    return File.ReadAllBytes(replaced);
                }
            }
            catch { }
            return null;
        }

        public override string Title
        {
            get { return "NETDUINO WEB"; }
        }

        class Statics
        {
            public static object Find(string name, ref bool withSyms, ref string contentType)
            {
                if (name == "/NETDUINOWEB.APPCACHE")
                {
                    contentType = "text/cache-manifest";
                    return APPCACHE;
                }

                StringBuilder sb = new StringBuilder(name);
                sb.Replace('.', '_');
                sb.Replace('/', '_');
                string repl = sb.ToString();
                FieldInfo fi; 
                if ((fi = typeof(HTML).GetField(repl)) != null)
                {
                    withSyms = true;
                    contentType = "text/html; charset=UTF-8;";
                    return (string)fi.GetValue(null);
                }
                if ((fi = typeof(CSS).GetField(repl)) != null)
                {
                    withSyms = false;
                    contentType = "text/css; charset=UTF-8;";
                    return (string)fi.GetValue(null);
                }
                if ((fi = typeof(JAVASCRIPT).GetField(repl)) != null)
                {
                    withSyms = false;
                    contentType = "application/javascript; charset=UTF-8;";
                    return (string)fi.GetValue(null);
                }
                if ((fi = typeof(BINARY).GetField(repl)) != null)
                {
                    withSyms = false;
                    if (repl[repl.Length-1] == 'O')
                        contentType = "image/x-icon";
                    else if (repl[repl.Length-1] == 'G')
                        contentType = "image/png";
                    return System.Convert.FromBase64String((string)fi.GetValue(null));
                }
                return null;
            }
           
            internal static readonly string APPCACHE =
@"CACHE MANIFEST
# {@SERVERSTART@}

CACHE:
#DEFAULT.CSS
FAVICON.ICO

NETWORK:
#DEFAULT.JS
DEFAULT.CS
*

FALLBACK:

";
            internal class CSS
            {
                public static readonly string _DEFAULT_CSS =
@"<style type='text/css'>
body     { margin:0px; background-color:#dcdcdc; font-family:sans-serif; }
a        { color:#003366; }
a:hover  { background-color:#eeeeff; }
#content { float:left; }
#menu    { background-color:#cce0ff; width:80px; float:left; }
#footer  { background-color:#b2d1ff; color:Black; clear:both; text-align:center; }
#header  { background-color:#b2d1ff; color:DarkBlue; padding:0px; margin:0px; }
#container { background-color:#dcdcdc; overflow:auto; width:100%; height:100%; }
</style>
";
            }

            internal class HTML
            {
                public static readonly string _DEFAULT_HTM = 
@"<!DOCTYPE html>
<html manifest='NETDUINOWEB.APPCACHE'> 
<head>
	<meta charset='UTF-8' />
	<meta name='generator' content='{@GENERATOR@}' />
	<link rel='icon' href='FAVICON.ICO' type='image/x-icon'>
	<title>{@TITLE@}</title> 
    <meta name='viewport' content='height=device-height, width=device-width'>
	<link rel='stylesheet' href='DEFAULT.CSS' type='text/css'>
</head>
<body>

<div id='container' >

<div id='header'>
<b>Welcome to {@SERVERLINK@}.</b>
</div>

<div id='menu'>
<b>Menu</b><br>
<a href='RESET'>RESET</a><br>
<a href='lkjl'>CSS</a><br>
<a href='ABOUT'>About</a>
</div>

<div id='content'>
{@CONTENT@}
</div>

<div id='footer'>
<small>{@CURRENTDATETIME@} Calls:{@CALLS@} Duration:{@DURATION@}</small>
</div>

</div>
</body>
</html>
";
            }

            internal class JAVASCRIPT
            {
                public static readonly string _DEFAULT_JS = 
@"<script></script>";
            }

            internal class BINARY
            {
                public static readonly string _FAVICON_ICO =
@"AAABAAEAEBAQAAEABAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAAAAAAgAAAAAAAAAAAAAAAEAAAAAAAAAD3zosAAAAAAPD29wCl5/AAcICCAIqdoQDe3t4AW1peAFdUXgDr7dMA0tbWAEZESgDt7e0AAAAAAAAAAAAAAAAAFmZmZmZmZmEWMzMzMAAzYRYzMzMwADNhFjMzMzAAM2EWM5mjNAAzYRYzlaMwADNhFjOVozAAM2Emw6qjMAA8YkJsMzMzM8YktCbDMzM8YksbQmwzM8YksRG0JsM8YksREXhCYzYksRERt7QmYksRERF7G0IksREREbcRtEsRERGAAQAAgAEAAIABAACAAQAAgAEAAIABAACAAQAAAAAAAAAAAAAAAAAAgAEAAMADAADABwAAwA8AAMgfAADMPwAA";
            }
        }
    }
}
