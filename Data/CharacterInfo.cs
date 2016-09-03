using System.Drawing;

namespace LiveSplit.SaltSanctuary.Data
{
	public class CharacterInfo
	{
		public CharacterInfo(int index, int monsterIndex, string name, PointF position, float hp, float stamina)
		{
			Index = index;
			MonsterIndex = monsterIndex;
			Name = name;
			Position = position;
			HP = hp;
			Stamina = stamina;
		}

		public int Index { get; set; }
		public int MonsterIndex { get; set; }
		public string Name { get; set; }
		public PointF Position { get; set; }
		public float HP { get; set; }
		public float Stamina { get; set; }
	}
}