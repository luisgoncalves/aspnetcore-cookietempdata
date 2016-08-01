var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var versionSuffix = Argument("versionSuffix", (string)null);

Task("Clean")
  .Does(() => 
  {
    CleanDirectory("./artifacts");
  });

Task("Build")
  .IsDependentOn("Clean")
  .Does(() =>
  {
    DotNetCoreRestore();
    DotNetCoreBuild("./*/*/project.json", new DotNetCoreBuildSettings
    {
      Configuration = configuration,
    });
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => 
  {
    var testSettings = new DotNetCoreTestSettings { NoBuild = true, Configuration = configuration };

    // Couldn't get 'dotnet-test-xunit' to run on .NET 4.5.1 on Ubuntu 
    if(IsRunningOnUnix())
    {
      testSettings.Framework = "netcoreapp1.0";
    }

    var testProjects = GetFiles("./test/*.Tests/project.json"); 
    foreach(var p in testProjects){
      DotNetCoreTest(p.ToString(), testSettings);
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => 
  {
    var p = GetFiles("./src/*/project.json").First();
    DotNetCorePack(p.ToString(), new DotNetCorePackSettings
    {
      NoBuild = true,
      Configuration = configuration,
      VersionSuffix = versionSuffix,
      OutputDirectory = "./artifacts",
    });
  });

Task("Default")
  .IsDependentOn("Test");

RunTarget(target);