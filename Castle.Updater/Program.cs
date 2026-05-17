using System.Diagnostics;

if (args.Length == 0)
{
    return;
}

var installerPath = args[0];

// Copy ourselves to temp so we survive the install overwriting us
var tempUpdater = Path.Combine(Path.GetTempPath(), "Castle.Updater.exe");
try
{
    File.Copy(Environment.ProcessPath!, tempUpdater, true);

    // Launch the temp copy with the same args and exit this instance
    Process.Start(new ProcessStartInfo
    {
        FileName = tempUpdater,
        Arguments = $"\"{installerPath}\" --stage2",
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Hidden
    });
    return;
}
catch
{
    // If copy fails, just continue inline
}

// Stage 2: actual update logic
if (args.Contains("--stage2") || true)
{
    // Wait for Castle to fully close
    await Task.Delay(2000);

    try
    {
        // Kill any lingering Castle processes
        var castleProcesses = Process.GetProcessesByName("Castle");
        foreach (var p in castleProcesses)
        {
            try { p.Kill(); p.WaitForExit(3000); } catch { }
        }

        // Run installer silently
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var installer = Process.Start(startInfo);
        if (installer != null)
        {
            await installer.WaitForExitAsync();
        }

        // Clean up downloaded installer
        try { File.Delete(installerPath); } catch { }
    }
    catch { }

    // Clean up temp copy
    try
    {
        await Task.Delay(1000);
        File.Delete(tempUpdater);
    }
    catch { }
}