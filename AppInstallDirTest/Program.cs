using Henke37.Valve.Steam;
using System;

namespace AppInstallDirTest {
	class Program {
		static void Main(string[] args) {
			int appId = int.Parse(args[0]);
			string path=SteamHelper.GetInstallPathForApp(appId);
			Console.WriteLine(path);
		}
	}
}
