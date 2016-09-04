using System.Drawing;

namespace LiveSplit.SaltSanctuary.Data
{
	public class CharacterInfo
	{
		public CharacterInfo(int index, int monsterIndex, string name, PointF position, float hp)
		{
			Index = index;
			MonsterIndex = monsterIndex;
			Name = name;
			Position = position;
			HP = hp;
		}

		public int Index { get; set; }
		public int MonsterIndex { get; set; }
		public string Name { get; set; }
		public float HP { get; set; }

		public PointF Position { get; set; }
	}
}