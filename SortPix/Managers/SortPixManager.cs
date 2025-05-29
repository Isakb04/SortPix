using System.Diagnostics;

namespace SortPix.Managers;

public class SortPixManager
{
    private string ExecutePythonScript(string scriptPath, string arguments = "")
    {
        try
        {
            Console.WriteLine($"Starting Python script: {scriptPath} with arguments: {arguments}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python", // Ensure Python is in your PATH
                Arguments = $"\"{scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Python Error: {error}");
                throw new Exception($"Python Error: {error}");
            }

            return output;
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            Console.WriteLine($"Error executing Python script: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }

    public string RunSortPixTagger(string imageDir, string outputDir)
    {
        // Get the directory the app is running from
        string binPath = AppContext.BaseDirectory;
        Console.WriteLine("binPath: " + binPath);

        // Go up 3 levels to get to project root
        string projectRoot = Directory.GetParent(binPath)?.Parent?.Parent?.Parent?.Parent?.FullName;
        Console.WriteLine("projectRoot: " + projectRoot);

        if (projectRoot == null)
        {
            throw new Exception("Failed to find project root.");
        }

        // Combine with relative path to the Python file
        string scriptPath = Path.Combine(projectRoot, "SortPixPyFiles", "SortPixTagger.py");
        Console.WriteLine("scriptPath: " + scriptPath);

        // Check if the file exists
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Python script not found at: {scriptPath}");
        }

        string arguments = $"\"{imageDir}\" \"{outputDir}\"";

        Console.WriteLine("Executing Python script...");
        string result = ExecutePythonScript(scriptPath, arguments);

        Console.WriteLine("Python script executed. Output:");
        Console.WriteLine(result);

        return result;
    }

}