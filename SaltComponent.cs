using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;
using LiveSplit.SaltSanctuary.Data;
using LiveSplit.SaltSanctuary.Memory;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace LiveSplit.SaltSanctuary
{
	using ContextMenuControls = IDictionary<string, Action>;

	public class SaltComponent : IComponent
	{
		private const int TotalSplits = 23;
		private const string LogFilename = "_SaltAutosplitter.log";

		private static string[] logKeys =
		{
			"Gamestate",
			"Menu",
            "Split"
		};

		private TimerModel model;
		private SaltMemory memory;
		private StreamWriter logWriter;

		private int currentSplit;

		private Dictionary<string, string> previousValues;
		private List<Bosses>[] splitBosses;
		private List<bool>[] bossesLogged;

		public SaltComponent()
		{
			memory = new SaltMemory();
			logWriter = new StreamWriter(LogFilename, false);
			currentSplit = -1;
			previousValues = new Dictionary<string, string>();

			foreach (string key in logKeys)
			{
				previousValues.Add(key, "[None]");
			}

			splitBosses = new List<Bosses>[TotalSplits];
			bossesLogged = new List<bool>[TotalSplits];

			for (int i = 0; i < TotalSplits; i++)
			{
				splitBosses[i] = new List<Bosses>();
			}

			splitBosses[0].Add(Bosses.Leviathon);
			splitBosses[1].Add(Bosses.Dread);
			splitBosses[2].Add(Bosses.Alchemist);
			splitBosses[3].Add(Bosses.FauxJester);
			splitBosses[4].Add(Bosses.Bull);
			splitBosses[5].Add(Bosses.CutQueen);
			splitBosses[6].Add(Bosses.GasBag);
			splitBosses[7].Add(Bosses.Clay);
			splitBosses[8].Add(Bosses.Cloak);
			splitBosses[9].Add(Bosses.Broken);
			splitBosses[10].Add(Bosses.TortureTree);
			splitBosses[11].Add(Bosses.Pirate);
			splitBosses[12].Add(Bosses.Ruinaxe);
			splitBosses[13].Add(Bosses.Mummy);
			splitBosses[14].Add(Bosses.Inquisitor);
			splitBosses[15].Add(Bosses.Hippogriff);
			splitBosses[16].Add(Bosses.Dragon);
			splitBosses[17].Add(Bosses.Butterfly);
			splitBosses[18].Add(Bosses.LakeWitch);
			splitBosses[19].Add(Bosses.Monster);
			splitBosses[19].Add(Bosses.MonsterWitch);
			splitBosses[20].Add(Bosses.DeadKnight);
			splitBosses[20].Add(Bosses.DeadKing);
			splitBosses[20].Add(Bosses.DeadJudge);
			splitBosses[21].Add(Bosses.SquidDragon);

			for (int i = 0; i < TotalSplits; i++)
			{
				bossesLogged[i] = new List<bool>();

				for (int j = 0; j < splitBosses[i].Count; j++)
				{
					bossesLogged[i].Add(false);
				}
			}
		}

		public string ComponentName => "Salt and Sanctuary Autosplitter";

		public float HorizontalWidth => 0;
		public float MinimumHeight => 0;
		public float MinimumWidth => 0;
		public float PaddingBottom => 0;
		public float PaddingLeft => 0;
		public float PaddingRight => 0;
		public float PaddingTop => 0;
		public float VerticalHeight => 0;

		public ContextMenuControls ContextMenuControls => null;

		public void Update(IInvalidator invalidator, LiveSplitState liveSplitState, float width, float height, LayoutMode mode)
		{
			if (model == null)
			{
				InitializeModel(liveSplitState);
			}

			GetValues();
		}

		private void InitializeModel(LiveSplitState liveSplitState)
		{
			model = new TimerModel
			{
				CurrentState = liveSplitState
			};

			model.InitializeGameTime();
			model.CurrentState.IsGameTimePaused = true;

			liveSplitState.OnStart += HandleStart;
			liveSplitState.OnSplit += HandleSplit;
			liveSplitState.OnPause += HandlePause;
			liveSplitState.OnResume += HandleResume;
			liveSplitState.OnUndoSplit += HandleUndoSplit;
			liveSplitState.OnSkipSplit += HandleSkipSplit;
			liveSplitState.OnReset += OnReset;
		}

		public void GetValues()
		{
			if (!memory.HookProcess())
			{
				return;
			}

			CheckAutosplit();
			LogValues();
		}

		private void CheckAutosplit()
		{
			bool shouldSplit = false;

			if (currentSplit == -1)
			{
				Menus menuType = memory.GetCurrentMenuType();
				TransitionTypes transitionType = memory.GetCurrentTransitionType();

				shouldSplit = menuType == Menus.VentureForth && transitionType == TransitionTypes.AllOut;
			}
			else if (model.CurrentState.CurrentPhase == TimerPhase.Running)
			{
				if (currentSplit < TotalSplits - 1)
				{
					List<Bosses> bosses = splitBosses[currentSplit];
					List<bool> bossesLogged = this.bossesLogged[currentSplit];
					float[] bossHealth = memory.GetBossHealth(splitBosses[currentSplit]);

					if (bossHealth != null)
					{
						for (int i = 0; i < bosses.Count; i++)
						{
							if (bossHealth[i] <= 0 && !bossesLogged[i])
							{
								Log("[Boss] " + bosses[i] + " killed", true);
								bossesLogged[i] = true;
							}
						}

						shouldSplit = bossesLogged.Count(bossKilled => bossKilled) == bosses.Count;
					}
				}
				else if (currentSplit == TotalSplits - 1)
				{
					PointF playerPosition = memory.GetPlayerPosition();
					shouldSplit = playerPosition.X > 66000 && playerPosition.X < 66400 && playerPosition.Y > 47500;

					if (shouldSplit)
					{
						Log("[End] Run complete", true);
					}
				}
			}

			if (shouldSplit)
			{
				Autosplit();
			}
		}

		private void Autosplit()
		{
			if (currentSplit == -1)
			{
				model.Start();
			}
			else
			{
				model.Split();
			}
		}

		private void HandleStart(object sender, EventArgs e)
		{
			ResetValues();
			currentSplit++;
			Log("[Timer] Timer started.");
		}

		private void HandleSplit(object sender, EventArgs e)
		{
			currentSplit++;
			model.CurrentState.IsGameTimePaused = true;
			Log("[Split] Split.");
		}

		private void HandlePause(object sender, EventArgs e)
		{
			Log("[Timer] Timer paused");
		}

		private void HandleResume(object sender, EventArgs e)
		{
			Log("[Timer] Timer resumed.");
		}

		private void HandleUndoSplit(object sender, EventArgs e)
		{
			currentSplit--;
			Log("[Split] Split undone.");
		}

		private void HandleSkipSplit(object sender, EventArgs e)
		{
			currentSplit++;
			Log("[Split] Split skipped.");
		}

		private void OnReset(object sender, TimerPhase e)
		{
			ResetValues();
			Log("[Timer] Timer reset.");
		}

		private void ResetValues()
		{
			currentSplit = -1;
			model.CurrentState.IsGameTimePaused = true;

			foreach (List<bool> logList in bossesLogged)
			{
				for (int i = 0; i < logList.Count; i++)
				{
					logList[i] = false;
				}
			}
		}

		public void LogValues()
		{
			foreach (string key in logKeys)
			{
				string previousValue = previousValues[key];
				string currentValue = GetCurrentValue(key);

				if (currentValue != previousValue)
				{
					Log("[Change] " + key + " changed (" + previousValue + " => " + currentValue + ")");
					previousValues[key] = currentValue;
				}
			}
		}

		private string GetCurrentValue(string key)
		{
			switch (key)
			{
				case "Gamestate":
					return memory.GetCurrentGamestate().ToString();

				case "Menu":
					return memory.GetCurrentMenuType().ToString();

				case "Split":
					return currentSplit.ToString();
			}

			return null;
		}

		private void Log(string value, bool logTime = false)
		{
			if (logTime)
			{
				value += " [" + model.CurrentState.Run[currentSplit].SplitTime.RealTime + "]";
			}

            if (Console.IsOutputRedirected)
			{
				logWriter.WriteLine(value);
			}
			else
			{
				Console.WriteLine(value);
			}
		}

		public Control GetSettingsControl(LayoutMode mode)
		{
			return null;
		}

		public void SetSettings(XmlNode settings)
		{
		}

		public XmlNode GetSettings(XmlDocument document)
		{
			return document.CreateElement("Settings");
		}

		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
		{
		}

		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
		{
		}

		public void Dispose()
		{
		}
	}
}