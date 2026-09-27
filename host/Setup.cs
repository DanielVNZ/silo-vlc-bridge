using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Microsoft.Win32;

class Setup : Form {
  static string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VLCBridge");
  static string[] browsers={"Google\\Chrome","Microsoft\\Edge"};
  Label status;
  Button folder;
  static readonly Color Background=Color.FromArgb(17,23,21);
  static readonly Color Surface=Color.FromArgb(28,37,32);
  static readonly Color Accent=Color.FromArgb(197,237,145);
  static readonly Color Ink=Color.FromArgb(239,244,234);
  static readonly Color Muted=Color.FromArgb(170,184,165);
  static Label TextLabel(Control parent,string text,int x,int y,int width,int height,float size,Color color,bool bold=false) {
    var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(width,height),Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent};parent.Controls.Add(label);return label;
  }
  static Button StyledButton(Control parent,string text,int x,int y,int width,int height,bool primary=false) {
    var button=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,height),FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Surface,ForeColor=primary?Background:Ink,Font=new Font("Segoe UI",10,FontStyle.Bold),Cursor=Cursors.Hand,UseVisualStyleBackColor=false};
    button.UseMnemonic=false;button.FlatAppearance.BorderSize=primary?0:1;button.FlatAppearance.BorderColor=Color.FromArgb(66,83,62);button.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(215,249,177):Color.FromArgb(43,57,45);parent.Controls.Add(button);return button;
  }
  static LinkLabel TextLink(Control parent,string text,int x,int y,int width,Action action) {
    var link=new LinkLabel{Text=text,Location=new Point(x,y),Size=new Size(width,26),Font=new Font("Segoe UI",11),LinkColor=Accent,ActiveLinkColor=Ink,VisitedLinkColor=Accent,BackColor=Color.Transparent};
    link.LinkClicked+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"Open link",MessageBoxButtons.OK,MessageBoxIcon.Information);}};parent.Controls.Add(link);return link;
  }
  void CopyExtensionsAddress(bool edge) {
    string page=edge?"edge://extensions":"chrome://extensions";
    Clipboard.SetText(page);
    status.ForeColor=Accent;
    status.Text="Copied "+page+". Paste into your browser's address bar and press Enter.";
  }
  public Setup() {
    Text="VLC Bridge | Setup";ClientSize=new Size(930,Math.Min(715,Screen.PrimaryScreen.WorkingArea.Height-80));StartPosition=FormStartPosition.CenterScreen;
    AutoScroll=true;AutoScrollMinSize=new Size(930,715);
    AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
    Font=new Font("Segoe UI",10);BackColor=Background;ForeColor=Ink;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
    var sidebar=new Panel{Location=new Point(0,0),Size=new Size(252,715),BackColor=Color.FromArgb(24,34,27)};Controls.Add(sidebar);
    var mark=new Panel{Location=new Point(28,36),Size=new Size(42,42),BackColor=Accent};sidebar.Controls.Add(mark);
    TextLabel(mark,"▶",11,8,30,30,16,Background,true);
    TextLabel(sidebar,"VLC Bridge",28,99,210,38,23,Ink,true);
    TextLabel(sidebar,"YOUR LIBRARY.\nYOUR PLAYER.",30,158,200,56,10,Accent,true);
    TextLabel(sidebar,"Original quality.\nYour favourite player.\nYour place, remembered.",30,238,200,98,12,Muted);
    TextLabel(sidebar,"SILO   →   VLC",30,383,200,30,14,Accent,true);
    TextLabel(sidebar,"No server conversion.\nProgress saved as you watch.",30,432,195,68,10,Muted);
    TextLabel(sidebar,"WINDOWS SETUP\nVersion 0.2.8",30,620,195,55,10,Muted);
    TextLabel(this,"Make room for a better player.",285,31,615,46,24,Ink,true);
    TextLabel(this,"Your bridge is bundled. Follow the guide below to get playing.",287,86,604,30,11,Muted);
    var install=StyledButton(this,"Install & open CopyMe  →",285,133,611,48,true);
    TextLabel(this,"Installs for your Windows account. No administrator access needed.",288,192,604,24,9,Muted);
    var steps=new Panel{Location=new Point(285,229),Size=new Size(611,340),BackColor=Surface};Controls.Add(steps);
    TextLabel(steps,"1",24,21,25,28,12,Accent,true);
    TextLink(steps,"Install VLC",60,21,89,()=>Process.Start(new ProcessStartInfo("https://www.videolan.org/vlc/"){UseShellExecute=true}));
    TextLabel(steps,"if you haven't already.",151,21,420,28,11,Ink);
    TextLabel(steps,"2",24,59,25,28,12,Accent,true);
    TextLabel(steps,"Copy the CopyMe folder path from Explorer's address bar.",60,59,528,28,11,Ink);
    TextLabel(steps,"Example: C:\\Users\\USERNAME\\AppData\\Local\\VLCBridge\\CopyMe",60,86,528,34,9,Muted);
    TextLabel(steps,"3",24,127,25,28,12,Accent,true);TextLabel(steps,"Copy",60,127,43,28,11,Ink);
    TextLink(steps,"edge://extensions",104,127,145,()=>CopyExtensionsAddress(true));
    TextLabel(steps,"or",254,127,28,28,11,Ink);
    TextLink(steps,"chrome://extensions",283,127,175,()=>CopyExtensionsAddress(false));
    TextLabel(steps,"Paste into your browser's address bar and press Enter.",60,152,528,22,9,Muted);
    TextLabel(steps,"4",24,180,25,28,12,Accent,true);TextLabel(steps,"Enable Developer mode.",60,180,528,28,11,Ink);
    TextLabel(steps,"5",24,203,25,28,12,Accent,true);TextLabel(steps,"Click Load unpacked.",60,203,528,28,11,Ink);
    TextLabel(steps,"6",24,241,25,28,12,Accent,true);TextLabel(steps,"Paste the folder path into the window that opens,\nthen click Select Folder.",60,241,528,47,11,Ink);
    TextLabel(steps,"7",24,296,25,28,12,Accent,true);TextLabel(steps,"Enter your Silo address in settings — and you're ready!",60,296,528,28,11,Ink);
    status=TextLabel(this,"Ready when you are. Explorer will open inside the CopyMe folder.",288,585,604,45,10,Muted);
    folder=StyledButton(this,"Open CopyMe folder",285,645,205,39);folder.Enabled=Directory.Exists(Path.Combine(root,"CopyMe"));
    folder.Click+=(s,e)=>{try{ShowExtensionFolder();}catch(Exception ex){status.ForeColor=Color.Salmon;status.Text=ex.Message;}};
    var help=StyledButton(this,"Browser page help",502,645,205,39);
    help.Click+=(s,e)=>{string page="edge://extensions/  or  chrome://extensions/";MessageBox.Show("If your browser opened its home page, type this in its address bar:\n\n"+page+"\n\nExplorer is open inside CopyMe. Copy its address, choose Load unpacked in your browser, then paste that address into the folder picker and click Select Folder.\n\nUpdating? Reload the extension. If it uses the old extension folder, remove that entry and load CopyMe once.","Finish browser setup",MessageBoxButtons.OK,MessageBoxIcon.Information);};
    var uninstall=StyledButton(this,"Uninstall",719,645,177,39);uninstall.ForeColor=Color.FromArgb(238,172,160);
    uninstall.Click+=(s,e)=>{Uninstall();};
    install.Click+=(s,e)=>{
      install.Enabled=false;install.Text="Installing…";status.ForeColor=Muted;status.Text="Preparing the launcher and browser extension…";Refresh();
      try {
        Install();folder.Enabled=true;status.ForeColor=Accent;status.Text="Installed. Follow the guide above to finish.";
        try {
          ShowExtensionFolder();

          status.Text="Installed! Copy the CopyMe address from Explorer and follow the guide. Browser on its home page? Use Browser page help.";
        }catch(Exception ex){status.Text="Installed. "+ex.Message;}
      }catch(Exception ex){status.ForeColor=Color.Salmon;status.Text="Installation failed. "+ex.Message;}
      finally{install.Enabled=true;install.Text="Install & open CopyMe  →";}
    };
    AcceptButton=install;
  }
  static void ShowExtensionFolder() {
    Process.Start("explorer.exe","\""+Path.Combine(root,"CopyMe")+"\"");
  }
  static string FindBrowser(bool edge) {
    string relative=edge?"Microsoft\\Edge\\Application\\msedge.exe":"Google\\Chrome\\Application\\chrome.exe";
    foreach(var directory in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}) {
      string file=Path.Combine(directory,relative);if(File.Exists(file))return file;
    }
    return null;
  }
  static string Extract(string destination) {
    Directory.CreateDirectory(destination);
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
    using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)) {
      foreach(var entry in zip.Entries) {
        var target=Path.GetFullPath(Path.Combine(destination,entry.FullName));
        if(!target.StartsWith(Path.GetFullPath(destination)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new Exception("Invalid setup archive.");
        if(entry.Name==""){Directory.CreateDirectory(target);continue;}
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        using(var source=entry.Open()) using(var output=File.Create(target)) source.CopyTo(output);
      }
    }
    var json=new JavaScriptSerializer();
    var manifest=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(destination,"CopyMe","manifest.json")));
    byte[] hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(Convert.FromBase64String((string)manifest["key"]));
    var id=new StringBuilder();for(int i=0;i<16;i++){id.Append((char)('a'+(hash[i]>>4)));id.Append((char)('a'+(hash[i]&15)));}
    var native=new {name="cc.nightbyte.vlc_bridge",description="VLC Bridge Windows launcher",path=Path.Combine(destination,"host","vlc-bridge.exe"),type="stdio",allowed_origins=new[]{"chrome-extension://"+id+"/"}};
    var file=Path.Combine(destination,"host","native-host.json");File.WriteAllText(file,json.Serialize(native),new UTF8Encoding(false));return file;
  }
  static void Install() {
    string manifest=Extract(root);
    foreach(var browser in browsers) using(var key=Registry.CurrentUser.CreateSubKey("Software\\"+browser+"\\NativeMessagingHosts\\cc.nightbyte.vlc_bridge"))key.SetValue("",manifest);
    string setup=Path.Combine(root,"VLC-Bridge-Setup.exe");
    if(!string.Equals(Application.ExecutablePath,setup,StringComparison.OrdinalIgnoreCase))File.Copy(Application.ExecutablePath,setup,true);
    using(var key=Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\VLCBridge")) {
      key.SetValue("DisplayName","VLC Bridge for Silo");key.SetValue("DisplayVersion","0.2.8");
      key.SetValue("UninstallString","\""+setup+"\" /uninstall");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);
    }
  }
  static int Uninstall() {
    if(MessageBox.Show("Uninstall VLC Bridge and delete its entire installation folder?\n\nClose VLC playback first. Setup will close to finish removal. Remove the extension entry in Chrome or Edge afterward.","Uninstall VLC Bridge",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return 2;
    try {
      // Run outside the installation directory so the running setup can be removed.
      string temporary=Path.Combine(Path.GetTempPath(),"VLCBridge-Uninstall-"+Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(temporary);
      string helper=Path.Combine(temporary,"cleanup.exe");
      File.Copy(Application.ExecutablePath,helper);
      Process.Start(new ProcessStartInfo(helper,"/cleanup "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=temporary});
      Application.Exit();return 0;
    }catch(Exception e){MessageBox.Show("Uninstall could not start.\n\n"+e.Message,"Uninstall failed");return 1;}
  }
  static void RemoveInstallFolder(string installRoot) {
    string target=Path.GetFullPath(installRoot).TrimEnd(Path.DirectorySeparatorChar);
    if(Path.GetFileName(target)!="VLCBridge" || string.Equals(target,Path.GetPathRoot(target),StringComparison.OrdinalIgnoreCase))throw new IOException("Unexpected installation folder path.");
    if(!Directory.Exists(target))return;
    CheckNoLinks(target);
    Directory.Delete(target,true);
    if(Directory.Exists(target))throw new IOException("The VLCBridge folder could not be removed.");
  }
  static int Cleanup(int parentId) {
    try {
      try {using(var parent=Process.GetProcessById(parentId)) {if(!parent.WaitForExit(15000))throw new IOException("Setup is still open. Close it and try uninstalling again.");}}catch(ArgumentException){}
      Exception last=null;
      for(int attempt=0;attempt<8;attempt++) {
        try{RemoveInstallFolder(root);last=null;break;}
        catch(IOException ex){last=ex;System.Threading.Thread.Sleep(1000);}
        catch(UnauthorizedAccessException ex){last=ex;System.Threading.Thread.Sleep(1000);}
      }
      if(last!=null)throw last;
      foreach(var browser in browsers) {
        string path="Software\\"+browser+"\\NativeMessagingHosts\\cc.nightbyte.vlc_bridge";
        using(var key=Registry.CurrentUser.OpenSubKey(path)){if(key==null || (string)key.GetValue("")!=Path.Combine(root,"host","native-host.json"))continue;}
        Registry.CurrentUser.DeleteSubKey(path,false);
      }
      Registry.CurrentUser.DeleteSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\VLCBridge",false);
      MessageBox.Show("VLC Bridge uninstalled. The entire installation folder has been deleted.\n\nRemove the VLC Bridge extension entry in Chrome or Edge to finish.","VLC Bridge",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;
    }catch(Exception e){MessageBox.Show("Uninstall did not finish. Some files may still be present.\n\n"+e.Message+"\n\nClose VLC, your browser, and any other setup windows, then run Uninstall again.","Uninstall failed",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
  }
  static void CheckNoLinks(string directory) {
    if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)throw new IOException("The extension folder contains a link; automatic removal was stopped.");
    foreach(string entry in Directory.GetFileSystemEntries(directory)) {
      var attributes=File.GetAttributes(entry);
      if((attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("The extension folder contains a link; automatic removal was stopped.");
      if((attributes&FileAttributes.Directory)!=0)CheckNoLinks(entry);
    }
  }
  [STAThread] static int Main(string[] args) {
    // QA mode extracts the exact embedded payload without touching the registry.
    if(args.Length==2 && args[0]=="/verify") {try{Extract(Path.GetFullPath(args[1]));return 0;}catch{return 1;}}
    Application.EnableVisualStyles();
    if(args.Length==2 && args[0]=="/cleanup") {int parentId;if(!int.TryParse(args[1],out parentId))return 1;return Cleanup(parentId);}
    // Render the setup UI for layout QA without installing or opening a browser.
    if(args.Length==2 && args[0]=="/preview") {
      using(var form=new Setup()) {
        form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.ShowInTaskbar=false;
        form.Show();Application.DoEvents();
        using(var bitmap=new Bitmap(form.Width,form.Height)) {
          form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));
          bitmap.Save(Path.GetFullPath(args[1]),System.Drawing.Imaging.ImageFormat.Png);
        }
      }
      return 0;
    }
    if(args.Length==1 && args[0]=="/uninstall") {
      return Uninstall();
    }
    Application.Run(new Setup());return 0;
  }
}









