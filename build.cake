//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var buildNumber=9999;
var baseDir=System.IO.Directory.GetCurrentDirectory();
var buildDir=System.IO.Path.Combine(baseDir, "build");
var distDir=System.IO.Path.Combine(baseDir, "dist");
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
if(isRunningOnAppVeyor)
    buildNumber = AppVeyor.Environment.Build.Number;
System.Environment.SetEnvironmentVariable("DNX_BUILD_VERSION", buildNumber.ToString(), System.EnvironmentVariableTarget.Process);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("EnsureDependencies")
    .Does(() =>
{
    // Some tools are required to be installed by default.
    // Eventually, this tools will be automatically installed,
    // but dnx is currently up in the air with the build system.
    // When things are stablized, we will work on installing all
    // dependencies dynamically.
    EnsureTool("node", "--version");
    EnsureTool("dnx", "--version");
    EnsureTool("dnu", "--version");
    EnsureTool("bower", "--version");
    EnsureTool("gulp", "--version");
});

Task("Prepare")
    .Does(() =>
{
    // TODO: Install dependencies locally if not installed by default
});

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory(distDir);
});

Task("Build")
    .Does(() =>
{
    ExecuteCommand("dnu restore");
    ExecuteCommand(string.Format("dnu publish \"src/Skimur.Web/project.json\" --configuration \"{0}\" -o \"{1}\"", configuration, System.IO.Path.Combine(buildDir, "Web")));
    ExecuteCommand(string.Format("dnu publish \"src/Utilities/MirroredContentSync/project.json\" --configuration \"{0}\" -o \"{1}\"", configuration, System.IO.Path.Combine(buildDir, "MirroredContentSync")));
});

Task("Test")
    .WithCriteria(() => !isRunningOnAppVeyor)
    .Does(() =>
{
    ExecuteCommand("dnx -p \"test/Skimur.Tests/project.json\" test");
    ExecuteCommand("dnx -p \"test/Skimur.App.Tests/project.json\" test");
});

Task("Deploy")
    .Does(() =>
{
    if(!DirectoryExists(distDir))
        CreateDirectory(distDir);

    var destination =  System.IO.Path.Combine(distDir, "Web");
    if(!DirectoryExists(destination))
        CreateDirectory(destination);
    CopyDirectory(System.IO.Path.Combine(buildDir, "Web"), destination);

    destination = System.IO.Path.Combine(distDir, "MirroredContentSync");
    if(!DirectoryExists(destination))
        CreateDirectory(destination);
    CopyDirectory(System.IO.Path.Combine(buildDir, "MirroredContentSync"), destination);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Prepare")
    .IsDependentOn("EnsureDependencies")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("CI")
    .IsDependentOn("Prepare")
    .IsDependentOn("EnsureDependencies")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Deploy");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

//////////////////////////////////////////////////////////////////////
// HELPERS
//////////////////////////////////////////////////////////////////////

void EnsureTool(string tool, string arguements)
{
    try
    {
        ExecuteCommand(tool + (!string.IsNullOrEmpty(arguements) ? " " + arguements : null));
        Information("The tool \"" + tool + "\" is present...");
    }
    catch(Exception ex)
    {
        Error("The tool \"" + tool + "\" is not present...");
        throw;
    }
}

void ExecuteCommand(string command, string workingDir = null)
{
    if (string.IsNullOrEmpty(workingDir))
        workingDir = System.IO.Directory.GetCurrentDirectory();

    System.Diagnostics.ProcessStartInfo processStartInfo;

    if (IsRunningOnWindows())
    {
        processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir,
            FileName = "cmd",
            Arguments = "/C " + command,
        };
    }
    else
    {
        processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = workingDir,
            Arguments = command,
        };
    }

    using (var process = System.Diagnostics.Process.Start(processStartInfo))
    {
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(string.Format("Exit code {0} from {1}", process.ExitCode, command));
    }
}
