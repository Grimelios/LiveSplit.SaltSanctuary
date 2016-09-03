using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
			"Boss",
			"Gamestate",
			"Menu",
            "Split",
			"Transition"
		};

		private TimerModel model;
		private SaltMemory memory;
		private HashSet<int> killedBosses;
		private StreamWriter logWriter;

		private int currentSplit;
		private int currentBossIndex;

		private Dictionary<string, string> previousValues;

		public SaltComponent()
		{
			memory = new SaltMemory();
			killedBosses = new HashSet<int>();
			logWriter = new StreamWriter(LogFilename, false);
			currentSplit = -1;
			previousValues = new Dictionary<string, string>();

			foreach (string key in logKeys)
			{
				previousValues.Add(key, "");
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

			CharacterInfo bossInfo = memory.GetBossInfo();

			if (bossInfo != null && bossInfo.Index != currentBossIndex)
			{
				currentBossIndex = bossInfo.Index;
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
					CharacterInfo bossInfo = memory.GetBossInfo(currentBossIndex);

					if (bossInfo != null && bossInfo.HP <= 0 && !killedBosses.Contains(bossInfo.MonsterIndex))
					{
						killedBosses.Add(bossInfo.MonsterIndex);
						shouldSplit = true;
					}
				}
				else if (currentSplit == TotalSplits - 1)
				{
					CharacterInfo playerInfo = memory.GetPlayerInfo();
					shouldSplit = playerInfo.Position.X > 99999 && playerInfo.Position.Y > 99999;
				}
			}

			if (shouldSplit)
			{
				Autosplit();
			}
		}

		private void Autosplit(bool shouldReset = false)
		{
			if (currentSplit > 0 && shouldReset)
			{
				model.Reset();
			}
			else if (currentSplit == -1)
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
			killedBosses.Clear();
			model.CurrentState.IsGameTimePaused = true;
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
				case "Boss":
					return ((Bosses)currentBossIndex).ToString();

				case "Gamestate":
					return memory.GetCurrentGamestate().ToString();

				case "Menu":
					return memory.GetCurrentMenuType().ToString();

				case "Split":
					return currentSplit.ToString();

				case "Transition":
					return memory.GetCurrentTransitionType().ToString();
			}

			return null;
		}

		private void Log(string value)
		{
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