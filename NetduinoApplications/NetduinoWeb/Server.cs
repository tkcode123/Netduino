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

namespace NetduinoWeb
{
    class Server : TinyWeb
    {
        protected readonly OutputPort _onboardLED;
        protected readonly InterruptPort _button;

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
                set
                {
                    base.ValueString = value;
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
                if (url == "/")
                    url = "_DEFAULT_HTML";
                bool withSyms = true;
                string contentType = "text/html";
                var st = Statics.Find(url, ref withSyms, ref contentType);
                if (st != null)
                {
                    return Respond(context, HttpStatusCode.OK, withSyms, contentType, false, st);
                }                
                ((NetduinoContext)context).Content = "<h2>The page '{@RAWURL@}' was not found here.</h2> You should update {@REFERER@}.";
                return Respond(context, HttpStatusCode.NotFound, true, "text/html", false, Statics.HTML._DEFAULT_HTML);
            }
            return base.Handle(context);
        }

        public override string Title
        {
            get { return "NETDUINO WEB"; }
        }

        class Statics
        {
            public static string Find(string name, ref bool withSyms, ref string contentType)
            {
                StringBuilder sb = new StringBuilder(name);
                sb.Replace('.', '_');
                sb.Replace('/', '_');
                string repl = sb.ToString();
                if (repl == "_NETDUINO_APPCACHE")
                {
                    contentType = "text/cache-manifest";
                    return APPCACHE;
                }
                FieldInfo fi; 
                if ((fi = typeof(HTML).GetField(repl)) != null)
                {
                    withSyms = true;
                    contentType = "text/html; charset=utf-8;";
                    return (string)fi.GetValue(null);
                }
                if ((fi = typeof(CSS).GetField(repl)) != null)
                {
                    withSyms = false;
                    contentType = "text/css; charset=utf-8;";
                    return (string)fi.GetValue(null);
                }
                if ((fi = typeof(JAVASCRIPT).GetField(repl)) != null)
                {
                    withSyms = false;
                    contentType = "application/x-javascript";
                    return (string)fi.GetValue(null);
                }                
                if ((fi = typeof(BINARY).GetField(repl)) != null)
                {
                    withSyms = false;
                    contentType = "image/png";
                    return (string)fi.GetValue(null);
                }
                if (repl == "_DEFAULT_ICO")
                {
                    withSyms = false;
                    contentType = "image/x-icon";
                    return (string)fi.GetValue(null);
                }
                var o = Microsoft.SPOT.ResourceUtility.GetObject(Resource1.ResourceManager, s.a);
                return o.ToString();
            }
            public enum s
            {
                a
            }
           
            internal static readonly string APPCACHE = 
@"CACHE MANIFEST
# {@SERVERSTART@}

CACHE:
DEFAULT.CSS
DEFAULT.ICO
DEFAULT.JS

NETWORK:

FALLBACK:

";
            internal class CSS
            {
                public static readonly string _DEFAULT_CSS = @"
<style type='text/css'>
#container { background-color:#dcdcdc; font-family:sans-serif; overflow:auto; }
#content { background-color:#dcdcdc;}
#menu    { background-color:#cce0ff;}
#footer  { background-color:#b2d1ff; color:Black; }
#header  { background-color:#b2d1ff; color:DarkBlue; padding:0px; margin:0px; }
body     { margin:0px; }
a        { color:#003366; }
a:hover  { background-color:#eeeeff; }
</style>
";
            }

            internal class HTML
            {
                public static readonly string _DEFAULT_HTML = @"
<!DOCTYPE html>
<html manifest='NETDUINO.APPCACHE'>
<head>
	<meta charset='utf-8' />
	<meta name='generator' content='{@GENERATOR@}' />
	<link rel='icon' href='DEFAULT.ICO' type='image/x-icon'>
	<title>{@TITLE@}</title> 
	<meta name='viewport' content='width=device-width'>
	<link rel='stylesheet' href='DEFAULT.CSS' type='text/css'>
</head>
<body>

<div id='container' >

<div id='header' style=''>
<h1 style='margin:0px;'>Main Title of Web Page</h1>
</div>

<div id='menu' style='height:200px;width:20%;max-width:150px;float:left;'>
<b>Menu</b><br>
<a href='lksd'>HTML</a><br>
<a href='lkjl'>CSS</a><br>
<a href='lklkl'>JavaScript</a>
</div>

<div id='content' style='height:200px;width:80%;float:left;'>
{@CONTENT@}
</div>

<div id='footer' style='clear:both;text-align:center;'>
{@GENERATOR@} Calls:{@CALLS@} Duration:{@DURATION@}
</div>

</div>
</body>
</html>
";
            }

            internal class BINARY
            {
                public static readonly string _DEFAULT_PNG = @"";
            }

            internal class JAVASCRIPT
            {
                public static readonly string _DEFAULT_JS = @"<script></script>";
            }
        }
    }
}
