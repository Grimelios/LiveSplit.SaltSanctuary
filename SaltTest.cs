using System;
using System.Threading;
using System.Windows.Forms;

namespace LiveSplit.SaltSanctuary
{
	public class SaltTest
	{
		public static void Main(string[] args)
		{
			Thread t = new Thread(TestComponent)
			{
				IsBackground = true
			};

			t.Start();

			Application.Run();
		}

		private static void TestComponent()
		{
			SaltComponent component = new SaltComponent();

			while (true) {
				try {
					component.GetValues();

					Thread.Sleep(5);
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}