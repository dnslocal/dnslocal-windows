using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DNSlocal.Util;
using DNSlocal.Properties;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Forms;
using SimpleJson;
using System.Text.RegularExpressions;
using System.Configuration.Install;
using System.Runtime.InteropServices;

namespace DNSlocal.Controller
{
    public class Controller
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]

        private static extern UInt32 DnsFlushResolverCache();

        public static string programpath;

        public const string SoftVersion = "1.0.2";
        public string LatestSoftVersion;

        private string ConfigVersion;
        public string LatestConfigVersion;

        private const string SoftVersionUpdateURL = "https://api.github.com/repos/dnslocal/dnslocal-windows/releases";
        private const string ConfigVersionUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/version";
        private const string PrivoxyRuleUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/user-rule.txt";
        private const string DnsRuleUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/rules.cfg";
        private const string DnsConfigUpdateURL = "https://coding.net/u/banben/p/dnslocal-windows/git/raw/master/config/options.cfg";

        public string Log;

        public Controller()
        {
            programpath = FileManager.ProgramFilesx86();
            LatestSoftVersion = SoftVersion;
            try
            {
                DnsFlushResolverCache();
                if (!Directory.Exists(Path.Combine(programpath, "DNSlocal/")))
                {
                    Directory.CreateDirectory(Path.Combine(programpath, "DNSlocal/"));
                }
                if (!Directory.Exists(Path.Combine(programpath, "DNSlocal/templates")))
                {
                    Directory.CreateDirectory(Path.Combine(programpath, "DNSlocal/templates"));
                }
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/hsts_privoxy.exe")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/hsts_privoxy.exe"), Resources.privoxy_exe);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/mgwz.dll")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/mgwz.dll"), Resources.mgwz_dll);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/ARSoft.Tools.Net.dll")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/ARSoft.Tools.Net.dll"), Resources.ARSoft_Tools_Net_dll);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/DNSAgent.exe")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/DNSAgent.exe"), Resources.DNSAgent_exe);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/DNSManager.exe")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/DNSManager.exe"), Resources.DNSManager_exe);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/Newtonsoft.Json.dll")))
                    FileManager.UncompressFile(Path.Combine(programpath, "DNSlocal/Newtonsoft.Json.dll"), Resources.Newtonsoft_Json_dll);
                FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/templates/connect-failed"), Resources.connect_failed);
                FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/templates/connection-timeout"), Resources.connection_timeout);
                FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/templates/forwarding-failed"), Resources.forwarding_failed);
                FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/templates/no-server-data"), Resources.no_server_data);
                FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/templates/no-such-domain"), Resources.no_such_domain);
                if (!File.Exists(Path.Combine(programpath, "DNSlocal/version")))
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/version"), Encoding.UTF8.GetBytes("0.0.0"));
                Log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Initialization Success, please update configuration first..." + Environment.NewLine;
            }
            catch (Exception e)
            {
                Log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Initialization Failed..." + Environment.NewLine + e.ToString() + Environment.NewLine;
            }
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

        public void GetConfigVersion()
        {
            try
            {
                ConfigVersion = File.ReadLines(Path.Combine(programpath, "DNSlocal/version")).First();
            }
            catch { }
        }

        public async Task Update()
        {
            string response;
            WebClient http = new WebClient();
            http.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36");
            try
            {
                GetConfigVersion();
                response = await http.DownloadStringTaskAsync(SoftVersionUpdateURL);
                SoftVersionUpdate(response);
                response = await http.DownloadStringTaskAsync(ConfigVersionUpdateURL);
                LatestConfigVersion = response;
                Log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Update Start, ";
                if (CompareVersion(response, ConfigVersion) > 0)
                {
                    Log += "new configuration " + response + " found.";
                }
                else 
                {
                    Log += "current configuration " + ConfigVersion + " is latest.";
                }
                // need update config
                // if (CompareVersion(response, ConfigVersion) > 0)
                {
                    response = await http.DownloadStringTaskAsync(PrivoxyRuleUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/user-rule.txt"), Encoding.UTF8.GetBytes(response));
                    response = await http.DownloadStringTaskAsync(DnsRuleUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/rules.cfg"), Encoding.UTF8.GetBytes(response));
                    response = await http.DownloadStringTaskAsync(DnsConfigUpdateURL);
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/options.cfg"), Encoding.UTF8.GetBytes(response));
                    FileManager.ByteArrayToFile(Path.Combine(programpath, "DNSlocal/version"), Encoding.UTF8.GetBytes(LatestConfigVersion));
                }
                Log += Environment.NewLine + DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Update Success..." + Environment.NewLine;
            }
            catch (Exception e)
            {
                Log = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + "Update Failed..." + Environment.NewLine + e.ToString() + Environment.NewLine;
            }
        }

        private void SoftVersionUpdate(string response)
        {
            try
            {
                JsonArray result = (JsonArray)SimpleJson.SimpleJson.DeserializeObject(response);
                List<string> versions = new List<string>();

                foreach (JsonObject release in result)
                {
                    if ((bool)release["prerelease"])
                    {
                        continue;
                    }
                    foreach (JsonObject asset in (JsonArray)release["assets"])
                    {
                        string url = (string)asset["browser_download_url"];
                        if (IsNewVersion(url))
                        {
                            versions.Add(url);
                        }
                    }
                }
                if (versions.Count == 0)
                {
                    return;
                }
                // sort versions
                SortVersions(versions);
                LatestSoftVersion = ParseVersionFromURL(versions[versions.Count - 1]);
                // do sth.
            }
            catch { }
        }

        private void SortVersions(List<string> versions)
        {
            versions.Sort(new VersionComparer());
        }

        private static string ParseVersionFromURL(string url)
        {
            Match match = Regex.Match(url, @".*DNSlocal.*?-([\d\.]+)\.\w+", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups.Count == 2)
                {
                    return match.Groups[1].Value;
                }
            }
            return null;
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

        public class VersionComparer : IComparer<string>
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed. 
            public int Compare(string x, string y)
            {
                return CompareVersion(ParseVersionFromURL(x), ParseVersionFromURL(y));
            }
        }

        private bool IsNewVersion(string url)
        {
            if (url.IndexOf("prerelease") >= 0)
            {
                return false;
            }
            string version = ParseVersionFromURL(url);
            if (version == null)
            {
                return false;
            }
            string currentVersion = SoftVersion;

            return CompareVersion(version, currentVersion) > 0;
        }

        public void Start()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", Path.Combine(programpath, "DNSlocal/DNSAgent.exe") });
            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", Path.Combine(programpath, "DNSlocal/DNSManager.exe") });
            DNSManager.setIPv4DNS("127.0.0.1");
            DNSManager.setIPv6DNS("::1");
            DnsFlushResolverCache();
        }

        public void Stop()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", "/u", Path.Combine(programpath, "DNSlocal/DNSAgent.exe") });
            ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", "/u", Path.Combine(programpath, "DNSlocal/DNSManager.exe") });
            DNSManager.autoDNS();
        }
    }
}
