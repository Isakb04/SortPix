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
        // Adjust the path to go back to the SortPix directory and then down to SortPixPyFiles
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SortPixPyFiles", "SortPixTagger.py");
        string arguments = $"\"{imageDir}\" \"{outputDir}\"";
        
        Console.WriteLine("Executing Python script...");
        string result = ExecutePythonScript(scriptPath, arguments);

        // Check if the file exists
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Python script not found at: {scriptPath}");
        }

        Console.WriteLine("Python script executed. Output:");
        Console.WriteLine(result); // Log the output

        return result;
    }
}