using System;
using System.Diagnostics;
using System.Drawing;
using LiveSplit.SaltSanctuary.Data;

namespace LiveSplit.SaltSanctuary.Memory
{
	public class SaltMemory
	{
		private ProgramPointer characterManagerPointer;
		private ProgramPointer monsterCatalogPointer;
		private ProgramPointer gamestatePointer;
		private ProgramPointer playerManagerPointer;
		private DateTime lastHooked;

		public SaltMemory()
		{
			characterManagerPointer = new ProgramPointer(this, PointerTypes.CharacterManager, true);
			monsterCatalogPointer = new ProgramPointer(this, PointerTypes.MonsterCatalogue, true);
			gamestatePointer = new ProgramPointer(this, PointerTypes.Gamestate, false);
			playerManagerPointer = new ProgramPointer(this, PointerTypes.PlayerManager, false);
			lastHooked = DateTime.MinValue;
		}

		public Process Process { get; set; }

		public bool IsHooked { get; set; }

		public Gamestates GetCurrentGamestate()
		{
			return (Gamestates)gamestatePointer.Read<int>();
		}

		public Menus GetCurrentMenuType()
		{
			return (Menus)playerManagerPointer.Read<int>(0x0, 0x17, 0x0, 0x8, 0x3C, 0x14);
		}

		public TransitionTypes GetCurrentTransitionType()
		{
			return (TransitionTypes)playerManagerPointer.Read<int>(0x0, 0x17, 0x0, 0x8, 0x3C, 0x18);
		}

		public CharacterInfo GetPlayerInfo()
		{
			if (characterManagerPointer.Value == IntPtr.Zero)
			{
				return null;
			}

			int length = characterManagerPointer.Read<int>(0x4);

			for (int i = 0; i < length; i++)
			{
				bool exists = characterManagerPointer.Read<bool>(0x8 + (0x4 * i), 0xC8);

				if (exists)
				{
					int monsterIndex = characterManagerPointer.Read<int>(0x8 + (0x4 * i), 0x5C);
					string name = monsterCatalogPointer.ReadString(0x4, 0x8 + (0x4 * monsterIndex), 0x4);

					if (name == "hero")
					{
						float px = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0xD4);
						float py = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0xD8);
						float hp = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0x60);

						return new CharacterInfo(i, monsterIndex, name, new PointF(px, py), hp);
					}
				}
			}

			return null;
		}

		public CharacterInfo GetBossInfo(int index = -1)
		{
			if (characterManagerPointer.Value == IntPtr.Zero)
			{
				return null;
			}

			int length = characterManagerPointer.Read<int>(0x4);

			for (int i = index < 0 ? 0 : index; i < length; i++)
			{
				bool exists = characterManagerPointer.Read<bool>(0x8 + (0x4 * i), 0xC8);
				bool isBoss = characterManagerPointer.Read<bool>(0x8 + (0x4 * i), 0xD2);

				if (isBoss && (exists || index >= 0))
				{
					int monsterIndex = characterManagerPointer.Read<int>(0x8 + (0x4 * i), 0x5C);
					string name = monsterCatalogPointer.ReadString(0x4, 0x8 + (0x4 * monsterIndex), 0x4);
					float px = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0xD4);
					float py = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0xD8);
					float hp = characterManagerPointer.Read<float>(0x8 + (0x4 * i), 0x60);

					return new CharacterInfo(index, monsterIndex, name, new PointF(px, py), hp);
				}
			}

			return null;
		}

		public bool HookProcess()
		{
			if (Process == null || Process.HasExited)
			{
				if (DateTime.Now > lastHooked.AddSeconds(1))
				{
					Process[] processes = Process.GetProcessesByName("Salt");
					Process = processes.Length == 0 ? null : processes[0];
					lastHooked = DateTime.Now;
					IsHooked = true;
				}
				else
				{
					IsHooked = Process != null && !Process.HasExited;
				}
			}

			return IsHooked;
		}

		public void Dispose()
		{
			Process?.Dispose();
		}
	}
}