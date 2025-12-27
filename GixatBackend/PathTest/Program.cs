// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics.CodeAnalysis;
using IOPath = System.IO.Path;

namespace PathTest;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
class PathTestProgram
{
    static void TestMain(string[] args)
    {
        Console.WriteLine("Testing IOPath.GetFileName behavior:");

        string[] testCases = {
            "C:\\Windows\\file.jpg",
            "/etc/passwd.jpg",
            "folder/subfolder/file.jpg",
            "simple.jpg"
        };

        foreach (var test in testCases)
        {
            Console.WriteLine($"Input: '{test}'");
            Console.WriteLine($"  GetFileName: '{IOPath.GetFileName(test)}'");
            Console.WriteLine($"  Sanitized: '{SanitizeFileName(test)}'");
            Console.WriteLine();
        }
    }

    public static string SanitizeFileName(string fileName)
    {
        // Remove any path characters
        fileName = IOPath.GetFileName(fileName);

        // Remove any potentially dangerous characters
        var invalidChars = IOPath.GetInvalidFileNameChars();
        fileName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Add timestamp to prevent collisions
        var extension = IOPath.GetExtension(fileName);
        var nameWithoutExtension = IOPath.GetFileNameWithoutExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

        return $"{nameWithoutExtension}_{timestamp}{extension}";
    }
}
