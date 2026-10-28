using AvaloniaNativePrintDemo.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace AvaloniaNativePrintDemo.Services;

public static class PrintingService
{
    public static IReadOnlyList<PrinterInfo> GetPrinters()
    {
        if (OperatingSystem.IsWindows())
            return GetPrintersWindows();

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return GetPrintersCups();

        throw new PlatformNotSupportedException("Unsupported OS");
    }

    public static void PrintFile(string filePath, string? printerName = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        if (OperatingSystem.IsWindows())
        {
            PrintWindows(filePath, printerName);
            return;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            PrintCups(filePath, printerName);
            return;
        }

        throw new PlatformNotSupportedException("Unsupported OS");
    }

    private static IReadOnlyList<PrinterInfo> GetPrintersWindows()
    {
        var list = new List<PrinterInfo>();
        using var searcher = new ManagementObjectSearcher("SELECT Name, Default FROM Win32_Printer");

        foreach (var obj in searcher.Get().Cast<ManagementObject>())
        {
            var name = obj["Name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var isDefault = (obj["Default"] as bool?) ?? false;
            list.Add(new PrinterInfo(name, isDefault));
        }

        return list
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private static void PrintWindows(string filePath, string? printerName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true,
            Verb = string.IsNullOrWhiteSpace(printerName) ? "print" : "printto",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (!string.IsNullOrWhiteSpace(printerName))
            psi.Arguments = $"\"{printerName}\"";

        Process.Start(psi);
    }

    private static IReadOnlyList<PrinterInfo> GetPrintersCups()
    {
        var printersOutput = RunProcess("lpstat", "-p");
        var defaultOutput = RunProcess("lpstat", "-d", ignoreExitCode: true);

        string? defaultName = null;
        foreach (var line in defaultOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            const string prefix = "system default destination:";
            if (line.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                defaultName = line[(line.IndexOf(':') + 1)..].Trim();
                break;
            }
        }

        var list = new List<PrinterInfo>();
        foreach (var line in printersOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (!t.StartsWith("printer ")) continue;

            var rest = t["printer ".Length..];
            var name = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(name)) continue;

            list.Add(new PrinterInfo(name, string.Equals(name, defaultName, StringComparison.Ordinal)));
        }

        return list
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private static void PrintCups(string filePath, string? printerName)
    {
        var args = string.IsNullOrWhiteSpace(printerName)
            ? $"\"{filePath}\""
            : $"-d \"{printerName}\" \"{filePath}\"";

        RunProcess("lp", args);
    }

    private static string RunProcess(string fileName, string args, bool ignoreExitCode = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Cannot start {fileName}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!ignoreExitCode && p.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} failed ({p.ExitCode}): {stderr}");

        return stdout;
    }
}
