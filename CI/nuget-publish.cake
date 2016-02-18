#addin "Cake.Json"

var TARGET = Argument ("target", Argument ("t", Argument ("Target", "build")));

var BUILD_INFO = DeserializeJsonFromFile<BuildInfo> ("./output/buildinfo.json");

var BUILD_GROUPS = BUILD_INFO.BuiltGroups;

var BUILD_NAMES = Argument ("names", Argument ("name", Argument ("n", "")))
	.Split (new [] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
var BUILD_TARGETS = Argument ("targets", Argument ("build-targets", Argument ("build-targets", Argument ("build", ""))))
	.Split (new [] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);

// Print out environment variables to console
var ENV_VARS = EnvironmentVariables ();
Information ("Environment Variables: {0}", "");
foreach (var ev in ENV_VARS)
	Information ("\t{0} = {1}", ev.Key, ev.Value);


public class BuildInfo
{
	public List<BuildGroup> ManifestGroups { get; set; }
	public List<BuildGroup> BuiltGroups { get; set; }
}

public class BuildGroup 
{
	public BuildGroup () 
	{
		Name = string.Empty;
		TriggerPaths = new List<string> ();
		TriggerFiles = new List<string> ();
		BuildTargets = new List<string> { "libs" };
	}

	public string Name { get; set; }
	public FilePath BuildScript { get; set; }
	public List<string> TriggerPaths { get; set; }
	public List<string> TriggerFiles { get; set; }
	public List<string> BuildTargets { get; set; }
}


var NUGET_API_KEY = Argument ("nuget-api-key", "");
var NUGET_SOURCE = Argument ("nuget-publish-source", "https://www.nuget.com/api/v2/");

Task ("publish-nuget").Does (() =>
{
	var packages = new List<FilePath> ();
	
	// If a filter was specified use it
	if (BUILD_NAMES != null && BUILD_NAMES.Any ()) {
		var groupsToPublish = BUILD_GROUPS.Where (bg => BUILD_NAMES.Any (n => n.ToLower () == bg.Name.ToLower ())).ToList ();

		foreach (var buildGroup in groupsToPublish) {
			var buildScriptFilePath = new FilePath (buildGroup.BuildScript);
			packages.AddRange (GetFiles (MakeAbsolute (buildScriptFilePath.GetDirectory ()).FullPath.TrimEnd ('/') + "/**/*.nupkg"));
		}

	} else {
		// No name filters specified, so add all the nupkgs we can find
		packages.AddRange (GetFiles ("./**/*.nupkg"));
	}
	
	foreach (var pkg in packages)
		StartProcess ("nuget", string.Format ("push {0} -Source {1} -ApiKey {2}", MakeAbsolute (pkg).FullPath, NUGET_SOURCE, NUGET_API_KEY));	
});

RunTarget (TARGET);