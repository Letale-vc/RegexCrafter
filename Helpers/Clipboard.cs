using System.Threading;

namespace RegexCrafter.Helpers;

/// <summary>
/// Provides methods to interact with the Windows clipboard.
/// Supports retrieving, setting, and clearing clipboard content.
/// </summary>
public static class Clipboard
{
    /// <summary>
    /// Retrieves the text content from the clipboard.
    /// </summary>
    /// <returns>
    /// A string containing the text from the clipboard.
    /// Returns an empty string if the clipboard is empty or does not contain text.
    /// </returns>
    public static string GetClipboardText()
    {
        var result = string.Empty;
        Thread staThread = new(() => result = System.Windows.Forms.Clipboard.GetText());
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        return result;
    }

    /// <summary>
    /// Clears the clipboard by removing its current content.
    /// </summary>
    public static void CleanClipboard()
    {
        Thread staThread = new(System.Windows.Forms.Clipboard.Clear);
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
    }

    /// <summary>
    /// Sets the specified text to the clipboard.
    /// </summary>
    /// <param name="text">The text to set in the clipboard.</param>
    public static void SetClipboardText(string text)
    {
        Thread staThread = new(() => System.Windows.Forms.Clipboard.SetText(text));
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
    }
}
