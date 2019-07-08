using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using KVLib;

namespace Henke37.Valve.Steam {
    public class SteamHelper {

        static readonly List<string> libraryFolders;

        static SteamHelper() {
            libraryFolders = FindLibraries();
        }

        static List<string> FindLibraries() {
            var steamKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            string steamInstallPath = (string)steamKey.GetValue("InstallPath");

            var libraries = new List<string>();

            libraries.Add(steamInstallPath);

            var kv = parseKVFile($@"{steamInstallPath}\steamapps\libraryfolders.vdf");

            
            foreach(var folderKv in kv.Children) {
                if(!int.TryParse(folderKv.Key,out int libraryIndex)) continue;
				string folder = fixSlashes(folderKv.GetString());
				libraries.Add(folder);
            }

            return libraries;
        }

        private static string fixSlashes(string v) {
            return Regex.Replace(v, @"\\\\", @"\");
        }

        public static string GetInstallPathForApp(int appId) {
            foreach(var libraryFolder in libraryFolders) {
                try {
                    var manifest = parseKVFile($@"{libraryFolder}\steamapps\appmanifest_{appId}.acf");
                    string installDir = fixSlashes(manifest["installdir"].GetString());
                    return $@"{libraryFolder}\steamapps\common\{installDir}";
                } catch(FileNotFoundException) {
                    continue;
                }
            }
            throw new KeyNotFoundException();
        }

        private static KeyValue parseKVFile(string filename) {
			var kvs=KVLib.KVParser.ParseAllKVRootNodes(File.ReadAllText(filename));

			return kvs[0];
        }
    }
}