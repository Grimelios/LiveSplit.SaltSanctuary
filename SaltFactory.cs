using System;
using LiveSplit.Model;
using LiveSplit.UI.Components;

namespace LiveSplit.SaltSanctuary
{
    public class SaltFactory : IComponentFactory
	{
		public ComponentCategory Category => ComponentCategory.Control;

		public IComponent Create(LiveSplitState state)
		{
			return new SaltComponent();
		}

		public string ComponentName => "Salt and Sanctuary Autosplitter v" + Version;
		public string Description => "Autosplitter for Salt and Sanctuary";
		public string UpdateName => ComponentName;
		public string UpdateURL => "https://raw.githubusercontent.com/ShootMe/LiveSplit.SaltSanctuary/master/";
		public string XMLURL => UpdateURL + "Components/LiveSplit.SaltSanctuary.Updates.xml";

		public Version Version => new Version("1.0");
	}
}