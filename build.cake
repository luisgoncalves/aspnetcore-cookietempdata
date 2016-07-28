var target = Argument("target", "Default");

Task("Build")
  .Does(() => 
  {
    DotNetCoreRestore();
    DotNetCoreBuild("./*/*/project.json");
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => 
  {
    var testSettings = new DotNetCoreTestSettings { NoBuild = true }; 
    foreach(var p in GetFiles("./test/*.Tests/project.json")){
      DotNetCoreTest(p.ToString(), testSettings);
    }
  });

Task("Default")
  .IsDependentOn("Test");

RunTarget(target);