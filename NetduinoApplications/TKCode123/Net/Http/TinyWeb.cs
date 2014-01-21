using System;
using System.Collections;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace TKCode123.Net.Http
{
    public class TinyWeb : IDisposable
    {
        public interface IVariable
        {
            bool IsReadOnly { get; }
            string Name { get; }
            string ValueString { get; set; }
        }

        protected readonly HttpListener _listener;
        private readonly string _startTime;
        private readonly string _me;
        private readonly Hashtable _variablesByName;
        private int _calls;
        private bool _ok;

        public TinyWeb(int port, X509Certificate certificate)
        {
            _ok = true;
            string prefix = certificate != null ? "https" : "http";
            _me = prefix + "://" + IPAddress.GetDefaultLocalAddress() + ":" + port;
            // Create a listener.
            _listener = new HttpListener(prefix, port);
            if (certificate != null)
            {
                _listener.HttpsCert = certificate;
            }           
            _startTime = DateTime.Now.ToString("g");
            _variablesByName = new Hashtable();
        }

        protected virtual void Shutdown(string reason)
        {
            Debugger.Write(reason);
            _ok = false;
        }
        
        public virtual void Dispose()
        {            
            if (_listener != null)
            {
                _listener.Close();
            }           
        }

        public virtual void Handle()
        {
            try
            {
                _listener.Start();
                Debugger.Write("Listening ", _me);
                while (_ok && _listener.IsListening)
                {
                    // Note: The GetContext method blocks while waiting for a request. 
                    var req = _listener.GetContext();
                    if (req != null && _ok)
                    {
                        _calls++;

                        PreHandle();
                        
                        try
                        {
                            DebugOut(req.Request, false);

                            var ctx = CreateContext(req);
                            _ok = Handle(ctx);

                            DebugOut(req.Response, false);
                        }
                        catch (Exception e)
                        {
                            Debugger.Write(e.ToString());
                            try
                            {
                                req.Close();
                            }
                            catch
                            {
                            }
                        }

                        PostHandle();
                    }
                }
                Debugger.Write("Stopped.");
            }
            catch (Exception e)
            {
                Debugger.Write(e.ToString());
            }
        }

        protected virtual TinyContext CreateContext(HttpListenerContext c)
        {
            return new TinyContext(c, this);
        }

        protected virtual void PreHandle() { }
        protected virtual void PostHandle() { }

        protected virtual bool Handle(TinyContext context)
        {
            using (HttpListenerResponse response = context.ClientContext.Response)
            {
                HttpListenerRequest request = context.ClientContext.Request;

                if (string.Equals(request.HttpMethod, "GET"))
                {
                    return HandlePageNotFound(context);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                }
                return true;
            }
        }

        protected virtual bool HandlePageNotFound(TinyContext context)
        {
            return Respond(context, HttpStatusCode.NotFound, true, "text/html", false, PageNotFound);
        }

        protected virtual bool Respond(TinyContext context, HttpStatusCode code, bool replaceSyms, string contentType, bool dontCache, object content)
        {
            try
            {
                using (HttpListenerResponse response = context.ClientContext.Response)
                {
                    response.StatusCode = (int)code;
                    if (dontCache) response.Headers.Add("cache-control", "dont-cache");
                    if (contentType != null) response.ContentType = contentType;
                    //if (context.ClientContext.Request.KeepAlive) response.KeepAlive = true;
                    if (content.GetType() == typeof(string))
                    {
                        response.ContentEncoding = System.Text.Encoding.UTF8;
                        new ReplacingUTF8Encoder(context, replaceSyms, response.OutputStream).Encode(content.ToString());
                    }
                    else if (content is byte[])
                    {
                        byte [] arr = content as byte[];
                        response.ContentLength64 = arr.Length;
                        response.OutputStream.Write(arr, 0, arr.Length);
                    }
                    response.OutputStream.Close();
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }

        public virtual IVariable AddVariable(IVariable v)
        {
            IVariable old = (IVariable)_variablesByName[v.Name];
            _variablesByName[v.Name] = v;
            return old;
        }

        public virtual IVariable GetVariable(string n)
        {
            return (IVariable)_variablesByName[n];
        }

        public virtual bool RemoveVariable(IVariable v)
        {
            IVariable old = (IVariable)_variablesByName[v.Name];
            _variablesByName.Remove(v.Name);
            return old != null;
        }

        protected virtual bool TryParseDouble(string s, out double val)
        {
            if (s != null && s.Length > 0)
            {
                return double.TryParse(s, out val);
            }
            val = 0.0;
            return false;
        }

        protected virtual bool TryParseBoolean(string s, out bool val)
        {
            if (s != null && s.Length > 0)
            {
                if (s.Equals(bool.TrueString) || s.Equals("1") || s.Equals("ON"))
                {
                    val = true;
                    return true;
                }
                if (s.Equals(bool.FalseString) || s.Equals("0") || s.Equals("OFF"))
                {
                    val = false;
                    return true;
                }
            }
            val = false;
            return false;
        }

        public virtual string ReplaceSymbol(string source, TinyContext context)
        {
            IVariable v = GetVariable(source);
            if (v != null)
                return v.ValueString;
            switch (source)
            {
                case "CURRENTDATETIME": return this.CurrentDateTime;
                case "CALLS": return this.Calls;
                case "SERVERLINK": return this.ServerLink;
                case "REMOTEENDPOINT": return context.ClientContext.Request.RemoteEndPoint.ToString();
                case "HTTPMETHOD": return context.ClientContext.Request.HttpMethod;
                case "RAWURL": return context.ClientContext.Request.RawUrl;
                case "LOCALENDPOINT": return context.ClientContext.Request.LocalEndPoint.ToString();
                case "TITLE": return this.Title;
                case "GENERATOR": return this.ToString();
                case "SERVERSTART": return this._startTime;
                default: return context.ClientContext.Request.Headers[source];
            }
        }

        public string ServerStartTime
        {
            get { return _startTime; }
        }

        public virtual string PageNotFound
        {
            get { return "<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8' /><title>{Title}</title></head><body>PAGE '{RawUrl}' NOT FOUND<br/>This page was referenced from {Referer}.<br/>Here it is {CurrentDateTime}.<br/>Connected from {RemoteEndPoint}.<br/><a href='{ServerLink}'>Root Page Link</a> for this server.<br/>This is {ServerVersion}.</body></html>"; }
        }

        public virtual string Title
        {
            get { return this.GetType().Name; }
        }
       
        public virtual string CurrentDateTime
        {
            get { return DateTime.Now.ToRFCString(); }
        }

        public virtual string Calls
        {
            get { return _calls.ToString(); }
        }

        public virtual string ServerLink
        {
            get { return _me; }
        }

        public override string ToString()
        {
            return "TinyWeb/1.0";
        }

        protected virtual void DebugOut(HttpListenerRequest req, bool hdr)
        {
            Debugger.Write(req.HttpMethod, " ", req.RawUrl);
            if (hdr)
            {
                foreach (var k in req.Headers.AllKeys)
                    Debugger.Write(k, ":", req.Headers.GetValues(k)[0]);
            }
            if (req.ContentType != null) Debugger.Write("TYPE=", req.ContentType);
            if (req.ContentLength64 > 0) Debugger.Write("LENGTH=", req.ContentLength64);
        }

        protected virtual void DebugOut(HttpListenerResponse resp, bool hdr)
        {
            Debugger.Write(" ==> ", resp.StatusCode);
            if (hdr)
            {
                foreach (var k in resp.Headers.AllKeys)
                    Debugger.Write(k, ":", resp.Headers.GetValues(k)[0]);
            }
        }

        public class TinyContext
        {
            private readonly HttpListenerContext _listenerContext;
            private readonly TinyWeb _server;
            private readonly long _start;

            public TinyContext(HttpListenerContext ctx, TinyWeb server)
            {
                _listenerContext = ctx; _server = server; _start = DateTime.Now.Ticks;
            }

            public TinyWeb Server { get { return _server; } }
            public HttpListenerContext ClientContext { get { return _listenerContext; } }
            public TimeSpan Duration { get { return new TimeSpan(DateTime.Now.Ticks - _start); } }
            
            public virtual string ReplaceSymbol(string source)
            {
                if ("DURATION".Equals(source))
                    return Duration.ToString();
                return Server.ReplaceSymbol(source, this);
            }
        }

        class ReplacingUTF8Encoder
        {
            private readonly TinyContext _ctx;
            private readonly byte[] _tmp;
            private readonly Stream _stream;
            private readonly bool _replace;

            internal ReplacingUTF8Encoder(TinyContext context, bool repl, Stream stream)
            {
                _tmp = new byte[1000];
                _ctx = context;
                _stream = stream;
                _replace = repl;
            }

            internal void Encode(string source)
            {
                int len = source.Length;
                if (_replace)
                {
                    int pos = 0;

                    while (pos < len)
                    {
                        int a = source.IndexOf("{@", pos);
                        int b = source.IndexOf("@}", pos);
                        if (a >= 0 && b > (a+2))
                        {
                            if (pos < a)
                                SimpleUTF8(source, pos, a - pos);
                            string symbol = source.Substring(a + 2, b - a - 2);
                            string repl = _ctx.ReplaceSymbol(symbol);
                            if (repl != null)
                            {
                                Encode(repl);
                            }
                            else
                                SimpleUTF8(source, a, b - a + 2);
                            pos = b + 2;
                        }
                        else
                            pos += SimpleUTF8(source, pos, len - pos);
                    }
                }
                else
                {
                    SimpleUTF8(source, 0, len);
                }
            }            

            private int SimpleUTF8Rec(string source, int pos, int len)
            {
                int todo = System.Math.Min(_tmp.Length, len);
                for (int i = 0; i < todo; i++)
                    _tmp[i] = (byte)source[pos + i];
                _stream.Write(_tmp, 0, todo);
                return todo;
            }

            private int SimpleUTF8(string source, int pos, int len)
            {
                int done = 0;
                while (done < len)
                {
                    done += SimpleUTF8Rec(source, pos + done, len - done);
                }
                return done;
            }

            private int SimpleUTF8(string source)
            {
                int pos = 0;
                int len = source.Length;
                while (pos < len)
                {
                    pos += SimpleUTF8(source, pos, len - pos);
                }
                return len;
            }
        }
    }
}
