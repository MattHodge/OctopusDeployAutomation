#addin nuget:?package=Cake.Yaml&version=2.1.0
#addin nuget:?package=YamlDotNet&version=4.3.1

using YamlDotNet.Serialization;

private IEnumerable<OctopusTemplate> _templates = null;

public class OctopusDeployProject
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [YamlMember(Alias = "group")]
    public string Group { get; set; }

    [YamlMember(Alias = "type")]
    public string ProjectType { get; set; }

    [YamlMember(Alias = "settings")]
    public OctopusDeployProjectSettings Settings { get; set; }
}

public class OctopusDeployProjectSettings
{
    [YamlMember(Alias = "appgroup")]
    public string AppGroup { get; set; }
}

public IEnumerable<OctopusDeployProject> LoadProjectsFromYaml(FilePath yamlFile)
{
    IEnumerable<OctopusDeployProject> result;

    try
    {
        result = DeserializeYamlFromFile<IEnumerable<OctopusDeployProject>>(yamlFile);
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to deserialise the YAML file '{yamlFile.FullPath}'. Check your formatting: {ex.Message}");
    }

    return result;
}

public class OctopusTemplate
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "type")]
    public string Type { get; set; }
}

public IEnumerable<OctopusTemplate> LoadTemplatesFromYaml(FilePath yamlFile)
{
    try
    {
        _templates = DeserializeYamlFromFile<IEnumerable<OctopusTemplate>>(yamlFile);
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to deserialise the YAML file '{yamlFile.FullPath}'. Check your formatting: {ex.Message}");
    }

    return _templates;
}

public OctopusTemplate GetOctopusTemplate(OctopusDeployProject project)
{
    var projectTemplate = _templates.First(i => i.Type == project.ProjectType);

    if (projectTemplate == null)
    {
        throw new Exception($"Requested template '{project.ProjectType}' for '{project.Name}' cannot be found. Check if exists in the template yaml configuration file.");
    }

    Information($"Project '{project.Name}' will be based on the template '{projectTemplate.Name}'");

    return projectTemplate;
}
