using Henke37.Valve.Steam;
using System;
using System.IO;

namespace AppInstallDirTest {
	class Program {
		static int Main(string[] args) {
			int appId = int.Parse(args[0]);
			string path=SteamHelper.GetInstallPathForApp(appId);
			if(!Directory.Exists(path)) {
				return 1;
			}
			Console.WriteLine(path);
			return 0;
		}
	}
}
