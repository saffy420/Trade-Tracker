using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Tracker.Avalonia.Services.Input;

public static class MacWindowActivator
{
    // Attempts to find and activate a browser tab whose title contains `titleContains`.
    // Tries common Chromium-based browsers + Safari. Returns true if activated.
    public static async Task<bool> ActivateBrowserTabAsync(string titleContains, int delayAfterMs = 300)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return false;

        var browsers = new[] { "Google Chrome", "Safari", "Microsoft Edge", "Brave Browser", "Chromium" };

        foreach (var browser in browsers)
        {
            var script = $@"
tell application ""{browser}""
  try
    repeat with w in windows
      try
        set tabList to (tabs of w)
      on error
        set tabList to {{}}
      end try
      repeat with t in tabList
        try
          if (name of t as string) contains ""{titleContains}"" or (title of t as string) contains ""{titleContains}"" then
            set active tab index of w to (index of t)
            set index of w to 1
            activate
            return ""OK""
          end if
        end try
      end repeat
    end repeat
  end try
end tell
return ""NOTFOUND""
";

            var tmp = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(tmp, script).ConfigureAwait(false);

                var psi = new ProcessStartInfo("osascript", tmp)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) continue;

                var outp = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await proc.WaitForExitAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(outp) && outp.Trim() == "OK")
                {
                    await Task.Delay(delayAfterMs).ConfigureAwait(false);
                    return true;
                }
            }
            catch
            {
                // ignore and try next browser
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        return false;
    }
}
