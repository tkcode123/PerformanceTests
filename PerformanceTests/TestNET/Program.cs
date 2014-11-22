using System;

namespace TestNET
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Performance");
			var start = DateTime.UtcNow;
			int h = 0;
			for (int i = 0; i < 10000000; i++)
				h += i.ToString ("x8").Length;

			var end = DateTime.UtcNow;
			Console.WriteLine ("{1} Took {0}", (end-start), h);
		}
	}
}
