using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Launcher {
  static JavaScriptSerializer json = new JavaScriptSerializer();
  static void Send(object result) {
    byte[] output=Encoding.UTF8.GetBytes(json.Serialize(result));
    Stream stdout=Console.OpenStandardOutput(); stdout.Write(BitConverter.GetBytes(output.Length),0,4); stdout.Write(output,0,output.Length); stdout.Flush();
  }
  static void Track(string executable, Uri uri, double position) {
    var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
    string password=Guid.NewGuid().ToString("N");
    var info=new ProcessStartInfo(executable,"--no-one-instance --no-loop --no-repeat --extraintf=http --http-host=127.0.0.1 --http-port="+port+" --http-password="+password+" --start-time="+position.ToString(System.Globalization.CultureInfo.InvariantCulture)+" \""+uri.AbsoluteUri+"\"");
    info.UseShellExecute=false;info.RedirectStandardInput=true;info.RedirectStandardOutput=true;info.RedirectStandardError=true;
    using(var process=Process.Start(info)) {
      process.OutputDataReceived+=(s,e)=>{};process.ErrorDataReceived+=(s,e)=>{};
      process.BeginOutputReadLine();process.BeginErrorReadLine();
      Send(new {ok=true,version="0.2.0"});
      bool observed=false;double last=position;string item=null;int failures=0;
      DateTime started=DateTime.UtcNow;
      while(!process.HasExited) {
        try {
          var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/requests/status.json");
          request.Proxy=null;request.Timeout=2000;request.ReadWriteTimeout=2000;
          request.Headers["Authorization"]="Basic "+Convert.ToBase64String(Encoding.ASCII.GetBytes(":"+password));
          Dictionary<string,object> state;
          using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream()))state=json.Deserialize<Dictionary<string,object>>(reader.ReadToEnd());
          failures=0;string playback=Convert.ToString(state["state"]);
          string current=state.ContainsKey("currentplid")?Convert.ToString(state["currentplid"]):null;
          if(observed && (playback=="stopped" || (item!=null && current!=item)))break;
          if(playback=="playing" || playback=="paused") {
            double sample=Convert.ToDouble(state["time"]);
            if(sample>=0 && sample<=31536000) {
              observed=true;item=current;last=sample;
              Send(new {type="sample",position=last,paused=playback=="paused"});
            }
          }
        } catch(WebException) {failures++;if(failures==5)Send(new {type="warning",error="Cannot read VLC playback position. Progress sync is interrupted."});}
        if(!observed && (DateTime.UtcNow-started).TotalSeconds>60) {Send(new {type="warning",error="VLC did not start playback within 60 seconds. Progress was not saved."});break;}
        Thread.Sleep(1000);
      }
      Send(new {type="stopped",position=last,observed=observed});
    }
  }
  static byte[] ReadExact(Stream s, int count) {
    byte[] bytes = new byte[count]; int offset=0;
    while(offset<count) {int n=s.Read(bytes,offset,count-offset); if(n==0) throw new EndOfStreamException(); offset+=n;}
    return bytes;
  }
  static string Vlc() {
    foreach(string dir in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}) {
      string path=Path.Combine(dir,"VideoLAN","VLC","vlc.exe"); if(File.Exists(path)) return path;
    }
    throw new Exception("VLC not found. Install VLC in its standard Windows location.");
  }
  static void Main() {
    object result;
    try {
      Stream stdin=Console.OpenStandardInput();
      int count=BitConverter.ToInt32(ReadExact(stdin,4),0);
      if(count<1 || count>65536) throw new Exception("Invalid request size.");
      var msg=json.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(ReadExact(stdin,count)));
      string action=Convert.ToString(msg["action"]);
      string executable=Vlc();
      if(action=="play") {
        string raw=Convert.ToString(msg["url"]); Uri uri, origin;
        if(!Uri.TryCreate(raw,UriKind.Absolute,out uri) || !Uri.TryCreate(Convert.ToString(msg["origin"]),UriKind.Absolute,out origin) || (uri.Scheme!="https" && uri.Scheme!="http") || uri.GetLeftPart(UriPartial.Authority)!=origin.GetLeftPart(UriPartial.Authority) || uri.UserInfo!="" || raw.IndexOfAny(new[]{'"','\r','\n','\0'})>=0) throw new Exception("Invalid stream address.");
        double position=Convert.ToDouble(msg["position"]);
        if(double.IsNaN(position)||double.IsInfinity(position)||position<0||position>31536000) throw new Exception("Invalid resume position.");
        Track(executable,uri,position);return;
      } else if(action!="check") throw new Exception("Unsupported action.");
      result=new {ok=true,version="0.2.0"};
    } catch(Exception e) { result=new {ok=false,error=e is KeyNotFoundException ? "Incomplete request." : e.Message}; }
    Send(result);
  }
}
