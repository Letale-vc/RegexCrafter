using System.Threading;

namespace RegexCrafter.Utils;
public class Clipboard
{
	public static string GetClipboardText()
	{
		string result = string.Empty;
		Thread staThread = new(() =>
		{
			result = System.Windows.Forms.Clipboard.GetText();
		});
		staThread.SetApartmentState(ApartmentState.STA);
		staThread.Start();
		staThread.Join();
		return result;
	}
	public static void CleanClipboard()
	{
		Thread staThread = new(System.Windows.Forms.Clipboard.Clear);
		staThread.SetApartmentState(ApartmentState.STA);
		staThread.Start();
		staThread.Join();
	}


}