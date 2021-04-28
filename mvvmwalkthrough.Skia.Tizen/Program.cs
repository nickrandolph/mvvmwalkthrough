using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace mvvmwalkthrough.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new mvvmwalkthrough.App(), args);
		host.Run();
	}
}
}
