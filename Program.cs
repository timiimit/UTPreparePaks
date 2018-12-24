using Monitor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace UTPreparePaks
{
	class Program
	{
		static int Main(string[] args)
		{
			string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().FullName);
			string mappingsFile = Path.Combine(currentDirectory, "mappings.txt");
			string lastGameModeFile = Path.Combine(currentDirectory, "lastGameMode.bin");

			// create missing files
			if (!File.Exists(mappingsFile))
			{
				using (var writer = File.CreateText(mappingsFile))
				{
					writer.Write(@"################################################################################################################
# whitespace characters (space, tab, ...) and equal sign (=) all count as separators.
# 1st word is the name of folder (usually gamemode prefix) containing paks for this gamemode.
# all other words in the line are names for this gamemode (case insensitive).
# all names have to be unique. if not, they are ignored.
# whitespace in gamemode's name entered from input or as argument in UTPreparePaks.exe is not accounted for
# 
# example: you enter ""CaP t ure the fLA     G, bt"" it will be changed to ""CaPturethefLAG, bt""
# this string will then be compared to all names in this file (case insensitive)
# and then junctions to CTF and BT directory will be created in target directory
################################################################################################################
# lastGameMode.bin contains which gamemodes were last set for every target directory
# file is litle bit encrypted so that users wouldnt edit its content and potentially
# make it so that some unwanted junctions would not be deleted when no longer needed
################################################################################################################

AS		= AS Assault
BR		= BR BombingRun
BT		= BT BunnyTrack
CTF		= CTF CaptureTheFlag CaptureFlag
DM		= DM Deathmatch
Duel	= Duel 1v1 pvp
Elim	= Elim Elimination
FR		= FR FlagRun Blitz
KO		= KO Knockout");
				}
			}

			if (!File.Exists(lastGameModeFile))
			{
				File.Create(lastGameModeFile).Close();
			}




			// get target directory and gameModePrefix			
			string targetDirectory = null;
			List<string> selectedGameModeNames = null;
			if (args.Length > 0)
			{
				targetDirectory = Path.GetFullPath(args[0]);
				if (args.Length > 1)
				{
					selectedGameModeNames = new List<string>(args[1].Replace(" ", "").ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
				}
				else
				{
					Console.Write("Enter one or more gamemode names (comma separated): ");
					selectedGameModeNames = new List<string>(Console.ReadLine().Replace(" ", "").ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
				}

				var lastGameModeTargets = ParseLastGameModesFile(lastGameModeFile);
				var mappings = ParseMappingsFile(mappingsFile);
				var selectedGameModes = new List<string>();

				// get selected gamemodes
				foreach (var selectedGameModeName in selectedGameModeNames)
				{
					if (mappings.TryGetValue(selectedGameModeName, out string selectedGameMode))
					{
						selectedGameModes.Add(selectedGameMode);
					}
					else
					{
						Console.WriteLine($"No mapping defined for '{selectedGameModeName}'. skipped.");
					}
				}

				// get games modes used last time
				if (lastGameModeTargets.TryGetValue(targetDirectory, out List<string> lastGameModes))
				{
					// go through every gamemode from last time
					foreach (var lastGameMode in lastGameModes)
					{
						string lastJunctionDestination = Path.Combine(targetDirectory, lastGameMode);
						if (!selectedGameModes.Contains(lastGameMode) && JunctionPoint.Exists(lastJunctionDestination)) // if supposed to delete and exists
						{
							JunctionPoint.Delete(lastJunctionDestination); // delete
						}
					}
				}

				// go through every selected gamemode
				foreach (var selectedGameMode in selectedGameModes)
				{
					string junctionSource = Path.Combine(currentDirectory, selectedGameMode);
					string junctionDestination = Path.Combine(targetDirectory, selectedGameMode);

					if (!Directory.Exists(junctionSource)) // directory on which junction will point doesnt exist yet
					{
						Directory.CreateDirectory(junctionSource); // make that directory
					}
					JunctionPoint.Create(junctionDestination, junctionSource, true); // finally create junction from targetDirectory to out gamemode directory
				}

				lastGameModeTargets[targetDirectory] = selectedGameModes; // sets which gamemodes were set this time
				WriteLastGameModes(lastGameModeFile, lastGameModeTargets); // save used gamemodes


			}
			else
			{
				Console.WriteLine("UTPreparePaks <TargetDirectory> [GameModeName]");
				return 1;
			}

			return 0;
		}

		static void WriteLastGameModes(string filename, Dictionary<string, List<string>> gameModeTargets)
		{
			using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				using (BinaryWriter w = new BinaryWriter(fs))
				{
					w.Write(gameModeTargets.Count);
					foreach (var pair in gameModeTargets)
					{
						string target = pair.Key;
						List<string> gamemodes = pair.Value;

						if (gamemodes.Count == 0) // no gamemodes were selected - no existing junctions
							continue; // dont waste drive space and just skip

						byte[] buf = Scramble(Encoding.Unicode.GetBytes(pair.Key), key);
						w.Write(buf.Length);
						w.Write(buf);

						w.Write(pair.Value.Count);
						foreach (var gamemode in gamemodes)
						{
							buf = Scramble(Encoding.Unicode.GetBytes(gamemode), key);
							w.Write(buf.Length);
							w.Write(buf);
						}
					}
				}
			}
		}

		static byte key = 0b1010_0010;

		static byte[] Scramble(byte[] data, byte key)
		{
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = (byte)(data[i] + key);
				key += key;
			}
			return data;
		}

		static Dictionary<string, List<string>> ParseLastGameModesFile(string filename)
		{
			var map = new Dictionary<string, List<string>>();

			try
			{
				using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
				{
					using (BinaryReader r = new BinaryReader(fs))
					{
						int count = r.ReadInt32();
						for (int i = 0; i < count; i++)
						{
							int len = r.ReadInt32();
							string key = Encoding.Unicode.GetString(Scramble(r.ReadBytes(len), (byte)-Program.key));

							int gamemodeCount = r.ReadInt32();
							List<string> gamemodes = new List<string>();

							for (int j = 0; j < gamemodeCount; j++)
							{
								len = r.ReadInt32();
								string value = Encoding.Unicode.GetString(Scramble(r.ReadBytes(len), (byte)-Program.key));
								gamemodes.Add(value);
							}

							map.Add(key, gamemodes);
						}
					}
				}
			}
			catch
			{
				Console.WriteLine("Error reading 'lastGameMode.bin'");
			}

			return map;
		}

		static Dictionary<string, string> ParseMappingsFile(string filename)
		{
			var mappings = new Dictionary<string, string>();
			string[] lines = File.ReadAllLines(filename);
			foreach (var lineUntrimmed in lines)
			{
				string line = lineUntrimmed.Trim();
				if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
					continue;

				var matches = Regex.Matches(line, @"[^\s=]+");
				int matchIndex = 0;
				string prefix = null;
				foreach (Match match in matches)
				{
					if (matchIndex == 0)
						prefix = match.Value;
					else
					{
						try
						{
							mappings.Add(match.Value.ToLower(), prefix);
						}
						catch (ArgumentException ex)
						{
							Console.WriteLine($"mappings.txt has multiple mappings of '{match.Value}'");
						}
					}
					matchIndex++;
				}
			}
			return mappings;
		}
	}
}
