var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

Task("Build")
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
    });
  });

Task("Default")
  .IsDependentOn("Test");

RunTarget(target);