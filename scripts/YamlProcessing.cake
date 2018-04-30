#addin nuget:?package=Cake.Yaml&version=2.1.0
#addin nuget:?package=YamlDotNet&version=4.3.1

using YamlDotNet.Serialization;

public enum ProjectTypeEnum 
{
    WindowsService,
    WebApplicationIIS
}

public class OctopusDeployProject 
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [YamlMember(Alias = "group")]
    public string Group { get; set; }

    [YamlMember(Alias = "type")]
    public ProjectTypeEnum ProjectType { get; set; }

    [YamlMember(Alias = "settings")]
    public OctopusDeployProjectSettings Settings { get; set; }

    public string GetProjectTemplateName()
    {
        switch (ProjectType)
        {
            case ProjectTypeEnum.WebApplicationIIS:
                return "Canary .NET Core 2.0";
            case ProjectTypeEnum.WindowsService:
                return "Canary .NET 4.6.2 - Core SDK";
            default:
                throw new Exception($"Provided 'Type' value '{ProjectType}' could not be mapped to an appropriate TemplateName");
        }
    }
}

public class OctopusDeployProjectSettings 
{
    [YamlMember(Alias = "appgroup")]
    public string AppGroup { get; set; }
}

public List<OctopusDeployProject> LoadProjectsFromYaml(FilePath yamlFile) 
{
    List<OctopusDeployProject> result;

    try
    {
        result = DeserializeYamlFromFile<List<OctopusDeployProject>>(yamlFile);
    }
    catch (Exception ex) 
    {
        throw new Exception($"Failed to deserialise the YAML file '{yamlFile.FullPath}'. Check your formatting: {ex.Message}");
    }

    return result;
}
