using System.Diagnostics;

var logPath = Path.Combine(Path.GetTempPath(), "Castle.Updater.log");
File.WriteAllText(logPath, $"Updater started at {DateTime.Now}\nArgs: {string.Join(", ", args)}\n");

if (args.Length == 0)
{
    File.AppendAllText(logPath, "No args, exiting.\n");
    return;
}

var installerPath = args[0];
var isStage2 = args.Contains("--stage2");
File.AppendAllText(logPath, $"Installer path: {installerPath}\nStage2: {isStage2}\n");

if (!isStage2)
{
    var tempUpdater = Path.Combine(Path.GetTempPath(), "Castle.Updater.exe");
    File.AppendAllText(logPath, $"Stage 1: Copying to {tempUpdater}\n");
    try
    {
        File.Copy(Environment.ProcessPath!, tempUpdater, true);
        File.AppendAllText(logPath, "Copy succeeded, launching stage 2.\n");

        Process.Start(new ProcessStartInfo
        {
            FileName = tempUpdater,
            Arguments = $"\"{installerPath}\" --stage2",
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
        File.AppendAllText(logPath, "Stage 2 launched, exiting stage 1.\n");
        return;
    }
    catch (Exception ex)
    {
        File.AppendAllText(logPath, $"Copy failed: {ex.Message}, falling through to inline.\n");
    }
}

// Stage 2
File.AppendAllText(logPath, "Stage 2: Waiting 2 seconds...\n");
await Task.Delay(2000);

try
{
    File.AppendAllText(logPath, "Killing Castle processes...\n");
    var castleProcesses = Process.GetProcessesByName("Castle");
    File.AppendAllText(logPath, $"Found {castleProcesses.Length} Castle process(es).\n");
    foreach (var p in castleProcesses)
    {
        try { p.Kill(); p.WaitForExit(3000); File.AppendAllText(logPath, $"Killed Castle PID {p.Id}.\n"); }
        catch (Exception ex) { File.AppendAllText(logPath, $"Failed to kill Castle: {ex.Message}\n"); }
    }

    File.AppendAllText(logPath, $"Running installer: {installerPath} /VERYSILENT\n");
    if (!File.Exists(installerPath))
    {
        File.AppendAllText(logPath, "INSTALLER FILE NOT FOUND!\n");
        return;
    }

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
        File.AppendAllText(logPath, "Waiting for installer to finish...\n");
        await installer.WaitForExitAsync();
        File.AppendAllText(logPath, $"Installer exited with code {installer.ExitCode}.\n");
    }
    else
    {
        File.AppendAllText(logPath, "Failed to start installer process.\n");
    }

    try { File.Delete(installerPath); File.AppendAllText(logPath, "Deleted downloaded installer.\n"); }
    catch { }
}
catch (Exception ex)
{
    File.AppendAllText(logPath, $"Stage 2 error: {ex.Message}\n{ex.StackTrace}\n");
}

try
{
    await Task.Delay(1000);
    var tempUpdater = Path.Combine(Path.GetTempPath(), "Castle.Updater.exe");
    if (File.Exists(tempUpdater))
    {
        File.Delete(tempUpdater);
        File.AppendAllText(logPath, "Deleted temp updater.\n");
    }
}
catch { }

File.AppendAllText(logPath, "Updater finished.\n");