using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using DNSManager.Properties;
using DNSManager.Util;
using System.IO;
using System.Timers;

namespace DNSManager
{
    public partial class Service : ServiceBase
    {
        private Timer timer;

        private string ConfigVersion;
        public string LatestConfigVersion;

        public static string programpath;
        private const string ConfigVersionUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/version";
        private const string PrivoxyRuleUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/user-rule.txt";
        private const string DnsRuleUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/rules.cfg";
        private const string DnsConfigUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/options.cfg";

        public Service()
        {
            InitializeComponent();
            programpath = FileManager.ProgramFilesx86();
        }

        private void StartTimer()
        {
            timer = new Timer(1800000);
            timer.Elapsed += async (sender, e) => await HandleTimer();
            timer.Start();
        }

        private async Task HandleTimer()
        {
            await Update();
        }

        public void GetConfigVersion()
        {
            try
            {
                ConfigVersion = File.ReadLines(Path.Combine(programpath, "DNSlocal/version")).First();
            }
            catch { }
        }

        public static int CompareVersion(string l, string r)
        {
            var ls = l.Split('.');
            var rs = r.Split('.');
            for (int i = 0; i < Math.Max(ls.Length, rs.Length); i++)
            {
                int lp = (i < ls.Length) ? int.Parse(ls[i]) : 0;
                int rp = (i < rs.Length) ? int.Parse(rs[i]) : 0;
                if (lp != rp)
                {
                    return lp - rp;
                }
            }
            return 0;
        }

        public async Task Update()
        {
            string response;
            WebClient http = new WebClient();
            http.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36");
            try
            {
                GetConfigVersion();
                response = await http.DownloadStringTaskAsync(ConfigVersionUpdateURL);
                LatestConfigVersion = response;

                // need update config
                if (CompareVersion(response, ConfigVersion) > 0)
                {
                    response = await http.DownloadStringTaskAsync(PrivoxyRuleUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/user-rule.txt"), Encoding.UTF8.GetBytes(response));
                    response = await http.DownloadStringTaskAsync(DnsRuleUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/rules.cfg"), Encoding.UTF8.GetBytes(response));
                    response = await http.DownloadStringTaskAsync(DnsConfigUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/options.cfg"), Encoding.UTF8.GetBytes(response));
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/version"), Encoding.UTF8.GetBytes(LatestConfigVersion));
                }
            }
            catch { }
        }

        private int GetFreePort()
        {
            int defaultPort = 13787;
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

                List<int> usedPorts = new List<int>();
                foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    usedPorts.Add(endPoint.Port);
                }
                for (int port = defaultPort; port <= 65535; port++)
                {
                    if (!usedPorts.Contains(port))
                    {
                        return port;
                    }
                }
            }
            catch
            {
                // in case access denied
                return defaultPort;
            }
            throw new Exception("No free port found.");
        }

        protected override void OnStart(string[] args)
        {
            Process[] existingPrivoxy = Process.GetProcessesByName("hsts_privoxy");
            foreach (Process p in existingPrivoxy)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch { }
            }
            string privoxyConfig = Resources.privoxy_conf;
            privoxyConfig = privoxyConfig.Replace("__PRIVOXY_BIND_PORT__", this.GetFreePort().ToString());
            privoxyConfig = privoxyConfig.Replace("__RULE_PATH__", Path.Combine(programpath, "DNSlocal/user-rule.txt"));
            privoxyConfig = privoxyConfig.Replace("__APP_PATH__", Path.Combine(programpath, "DNSlocal"));
            FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/privoxy_conf.txt"), System.Text.Encoding.UTF8.GetBytes(privoxyConfig));

            Process process;
            process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = Path.Combine(programpath, "DNSlocal/hsts_privoxy.exe");
            process.StartInfo.Arguments = " \"" + Path.Combine(programpath, "DNSlocal/privoxy_conf.txt") + "\"";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            //_process.StartInfo.RedirectStandardOutput = true;
            //_process.StartInfo.RedirectStandardError = true;
            process.Start();
            StartTimer();
        }

        protected override void OnStop()
        {
            Process[] existingPrivoxy = Process.GetProcessesByName("hsts_privoxy");
            foreach (Process p in existingPrivoxy)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch { }
            }
            timer.Enabled = false;
        }
    }
}
