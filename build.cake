#addin Octopus.Client&version=4.33.1
#addin nuget:?package=Cake.Yaml&version=2.1.0
#addin nuget:?package=YamlDotNet&version=4.3.1
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.Triggers;
using YamlDotNet.Serialization;
using Cake.Core.IO;

//////////////////////////////////////////////////////////////////////
// MOVE INTO ITS OWN FILE
//////////////////////////////////////////////////////////////////////

public enum ProjectType {
    WindowsService,
    WebApplicationIIS
}

public string GetProjectTemplateName (ProjectType projectType) {
    switch (projectType)
    {
        case ProjectType.WebApplicationIIS:
            return "Canary .NET Core 2.0";
        case ProjectType.WindowsService:
            return "Canary .NET 4.6.2 - Core SDK";
        default:
            return null;
    }
}

public class OctopusDeployProject {
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; }

    [YamlMember(Alias = "group")]
    public string Group { get; set; }

    [YamlMember(Alias = "type")]
    public ProjectType Type { get; set; }

    [YamlMember(Alias = "settings")]
    public OctopusDeployProjectSettings Settings { get; set; }
}

public class OctopusDeployProjectSettings {
    [YamlMember(Alias = "appgroup")]
    public string AppGroup { get; set; }
}

public OctopusRepository GenerateOctopusRepository (OctopusClient client) {
    return new OctopusRepository (client);
}

public OctopusClient GenerateOctopusClient (string OctopusServerURL, string OctopusAPIKey) {
    return new OctopusClient (new OctopusServerEndpoint (OctopusServerURL, OctopusAPIKey));
}

public void ModifyDeploymentProcess (OctopusRepository repository, DeploymentProcessResource deploymentProcess) {
    try {
        repository.DeploymentProcesses.Modify (deploymentProcess);
        Information ($"Deployment Process {deploymentProcess.Id} has been modified");
    } catch (Exception ex) {
        Error (ex.Message);
    }
}

public void ModifyVariableSet (OctopusRepository repository, VariableSetResource variableSet) {
    try {
        repository.VariableSets.Modify (variableSet);
        Information ($"Variable Set {variableSet.Id} has been modified");
    } catch (Exception ex) {
        Error (ex.Message);
    }
}

public void ModifyProjectTriggers (OctopusRepository repository, List<ProjectTriggerResource> projectTriggers) {
    try {
        foreach (var projectTrigger in projectTriggers) {
            repository.ProjectTriggers.Modify (projectTrigger);
            Information ($"Project Triggers {projectTrigger.Id} has been modified");
        }
    } catch (Exception ex) {
        Error (ex.Message);
    }
}

public void ModifyProject (OctopusRepository repository, ProjectResource projectResource) {
    try {
        repository.Projects.Modify (projectResource);
        Information ("Project was modified");
    } catch (Exception ex) {
        Error (ex.Message);
    }
}

public void CreateProjectGroup (OctopusRepository repository, string projectGroup, string projectDescription) {
    var newprojectgroup = new ProjectGroupResource {
        Name = projectGroup
    };

    if (projectDescription != null) {
        newprojectgroup.Description = projectDescription;
    }

    try {
        repository.ProjectGroups.Create (newprojectgroup);
        Information ($"Project Group {newprojectgroup.Name} created.");
    } catch (Exception ex) {
        Error (ex.Message);
    }
}

public ProjectResource CreateProjectByClone (OctopusClient client, ProjectResource sourceProject, ProjectResource destinationProject) {
    var repository = GenerateOctopusRepository (client);
    var newproject = GetProjectResourceByName (repository, destinationProject.Name);

    if (newproject == null) {
        Information ($"Project {destinationProject.Name} doesn't exist, creating by cloning from {sourceProject.Name}");

        client.Post ("~/api/projects?clone=" + sourceProject.Id, destinationProject);

        newproject = GetProjectResourceByName (repository: repository, projectName: destinationProject.Name);
    } else {
        Information ($"Project {destinationProject.Name} already exists. Skipping.");
    }

    return newproject;
}

public static ProjectResource GetProjectResourceByName (OctopusRepository repository, string projectName) {
    return repository.Projects.FindByName (projectName);
}

public static void UpdateDeploymentProcessTargetRoles (DeploymentProcessResource deploymentProcess, string targetRole) {
    var stepsToBeUpdated = new List<DeploymentStepResource> ();

    foreach (var step in deploymentProcess.Steps) {
        foreach (var stepProperty in step.Properties) {
            // find steps that are pinned to a role
            if (stepProperty.Key == "Octopus.Action.TargetRoles") {
                // make sure the role isn't the octopus-deploy server, we leave this the same
                if (stepProperty.Value.Value != "octopus-deploy") {
                    stepsToBeUpdated.Add (step);
                }
            }
        }
    }

    foreach (var stepToBeUpdated in stepsToBeUpdated) {
        stepToBeUpdated.TargetingRoles (targetRole);
    }
}

public static void UpdateProjectTriggerTargetRoles (List<ProjectTriggerResource> projectTriggers, string targetRole) {

    var machineFilter = new MachineFilterResource ();
    machineFilter.Roles.Add (targetRole);
    machineFilter.EventGroups.Add ("MachineAvailableForDeployment");

    foreach (var trigger in projectTriggers) {
        trigger.Filter = machineFilter;
    }
}

public static List<ProjectTriggerResource> GetProjectTriggers (OctopusRepository repository, ProjectResource projectResource) {
    var triggersToBeReturned = new List<ProjectTriggerResource> ();
    var projectTriggers = repository.Projects.GetTriggers (projectResource);

    foreach (var projectTrigger in projectTriggers.Items) {
        triggersToBeReturned.Add (repository.ProjectTriggers.Get (projectTrigger.Id));
    }

    return triggersToBeReturned;
}

public static void UpdateVariableStepAppGroup (VariableSetResource variableSet, string appgroup) {
    if (variableSet.Variables.Any (i => i.Name == "appgroup")) {
        variableSet.AddOrUpdateVariableValue ("appgroup", appgroup);
    }
}

public void TestEnvironmentVariablesExist (List<string> environmentVariables) {
    foreach (var environmentvar in environmentVariables)
    {
        if (HasEnvironmentVariable(environmentvar))
        {
            Information($"Environment variable {environmentvar} exists.");
        }
        else
        {
            Error($"Environment variable {environmentvar} is not present.");
        }
    }
}

public List<OctopusDeployProject> LoadProjectsYaml(FilePath yamlFile) {
    try {
        DeserializeYamlFromFile<List<OctopusDeployProject>>(yamlFile);
    }
    catch (Exception ex) {
        Error($@"Failed to load the Projects YAML file {yamlFile.FullPath}. Check your formatting.
        {ex.Message}");
    }

    return DeserializeYamlFromFile<List<OctopusDeployProject>>(yamlFile.FullPath);
}

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    List<string> environmentVariables = new List<string> { "OCTOPUS_API_KEY", "OCTOPUS_URL" };
    TestEnvironmentVariablesExist(environmentVariables);
});

Task ("Build")
    .Does (() => {
        var projects = LoadProjectsYaml(new FilePath("projects.yaml"));

        var client = GenerateOctopusClient(EnvironmentVariable("OCTOPUS_URL"), EnvironmentVariable("OCTOPUS_API_KEY"));
        var repo = GenerateOctopusRepository(client);

        foreach (var project in projects)
        {
            Information($"Processing Project {project.Name}");

            var projectTemplateName = GetProjectTemplateName(project.Type);
            Information($"Project will be based on the template {projectTemplateName}");

            var projectTemplate = GetProjectResourceByName(repository: repo, projectName: projectTemplateName);

            var newProjectSkeleton = new ProjectResource
            {
                Name = project.Name,
                Description = project.Description,
                ProjectGroupId = "ProjectGroups-261",
                LifecycleId = "Lifecycles-21"
            };

            var newProject = CreateProjectByClone(client, projectTemplate, newProjectSkeleton);

            var newProjectTriggers = GetProjectTriggers(repo, newProject);
            var deploymentProcess = repo.DeploymentProcesses.Get(newProject.DeploymentProcessId);
            var variableSet = repo.VariableSets.Get(newProject.VariableSetId);

            UpdateDeploymentProcessTargetRoles(deploymentProcess, project.Settings.AppGroup);
            UpdateVariableStepAppGroup(variableSet, project.Settings.AppGroup);
            UpdateProjectTriggerTargetRoles(newProjectTriggers, project.Settings.AppGroup);

            ModifyDeploymentProcess(repository: repo, deploymentProcess: deploymentProcess);
            ModifyVariableSet(repository: repo, variableSet: variableSet);
            ModifyProjectTriggers(repository: repo, projectTriggers: newProjectTriggers);
        }
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget (target);
