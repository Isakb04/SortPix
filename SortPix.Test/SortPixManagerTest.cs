using Xunit;
using SortPix.Managers;
using System;
using System.IO;
using System.Reflection;

namespace SortPix.Test;

public class SortPixManagerTest
{
    [Fact(DisplayName = "TC001: RunSortPixTagger executes Python script for valid directories (integration)")]
    public void TC001_RunSortPixTagger_ExecutesPythonScript()
    {
        // This is an integration test and requires Python and the script to exist.
        // We'll skip if the script does not exist.
        var manager = new SortPixManager();

        // Try to find the script path as the production code does
        string binPath = AppContext.BaseDirectory;
        string projectRoot = Directory.GetParent(binPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
        string scriptPath = Path.Combine(projectRoot ?? "", "SortPixPyFiles", "SortPixTagger.py");

        if (!File.Exists(scriptPath))
        {
            // Skip test if script does not exist
            return;
        }

        // Use dummy directories (must exist or be created for real integration)
        string imageDir = Path.Combine(projectRoot, "SortPixPyFiles", "Images1");
        string outputDir = Path.Combine(projectRoot, "SortPixPyFiles", "Processed_Images");
        Directory.CreateDirectory(imageDir);
        Directory.CreateDirectory(outputDir);

        var result = manager.RunSortPixTagger(imageDir, outputDir);

        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact(DisplayName = "TC002: RunSortPixTagger throws for missing Python script")]
    public void TC002_RunSortPixTagger_ThrowsForMissingScript()
    {
        var manager = new SortPixManager();

        // Temporarily rename the script if it exists
        string binPath = AppContext.BaseDirectory;
        string projectRoot = Directory.GetParent(binPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
        string scriptPath = Path.Combine(projectRoot ?? "", "SortPixPyFiles", "SortPixTagger.py");
        string backupPath = scriptPath + ".bak";

        bool renamed = false;
        if (File.Exists(scriptPath))
        {
            File.Move(scriptPath, backupPath);
            renamed = true;
        }

        try
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                manager.RunSortPixTagger("dummy", "dummy");
            });
        }
        finally
        {
            // Restore the script if it was renamed
            if (renamed && File.Exists(backupPath))
                File.Move(backupPath, scriptPath);
        }
    }

    [Fact(DisplayName = "TC003: ExecutePythonScript captures output of a Python script")]
    public void TC003_ExecutePythonScript_CapturesOutput()
    {
        // Create a simple Python script that prints something
        string tempDir = Path.GetTempPath();
        string scriptPath = Path.Combine(tempDir, "test_script.py");
        File.WriteAllText(scriptPath, "print('Hello from Python!')");

        var manager = new SortPixManager();
        // Use reflection to call the private method
        var method = typeof(SortPixManager).GetMethod("ExecutePythonScript", BindingFlags.NonPublic | BindingFlags.Instance);

        string output = (string)method.Invoke(manager, new object[] { scriptPath, "" });

        Assert.Contains("Hello from Python!", output);

        File.Delete(scriptPath);
    }

    [Fact(DisplayName = "TC004: ExecutePythonScript handles errors during script execution")]
    public void TC004_ExecutePythonScript_HandlesErrors()
    {
        var manager = new SortPixManager();
        // Use reflection to call the private method
        var method = typeof(SortPixManager).GetMethod("ExecutePythonScript", BindingFlags.NonPublic | BindingFlags.Instance);

        // Provide an invalid script path
        string invalidPath = Path.Combine(Path.GetTempPath(), "does_not_exist.py");
        string output = (string)method.Invoke(manager, new object[] { invalidPath, "" });

        Assert.Contains("Error", output, StringComparison.OrdinalIgnoreCase);
    }
}
