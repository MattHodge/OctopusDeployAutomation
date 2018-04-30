#addin "nuget:?package=Cake.Incubator"

#load "local:?path=scripts/OctopusHelper.cake"
#load "local:?path=scripts/YamlProcessing.cake"

using Cake.Core.IO;
using Cake.Common.IO;

const string OctopusLifecycleResourceName = "Default";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS / ENVIRONMENT VARIABLES
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Create-Projects-From-Template");
var projectYamlFile = Argument("projects", "projects.yaml");
var templateYamlFile = Argument("templates", "templates.yaml");

var octopusServer = EnvironmentVariable<string>("OCTOPUS_URL");
var octopusApiKey = EnvironmentVariable<string>("OCTOPUS_API_KEY");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Setup(context =>
{
    CreateOctopusHelper(octopusServer, octopusApiKey);
    CreateOctopusRepository();

    if (FileExists(templateYamlFile))
    {
        LoadTemplatesFromYaml(new FilePath(templateYamlFile));
    }
    else
    {
        throw new Exception($"Provided template file '{templateYamlFile}' could not be found");
    }
});

Task("Create-Projects-From-Template")
    .WithCriteria(() => FileExists(projectYamlFile))
    .DoesForEach(LoadProjectsFromYaml(new FilePath(projectYamlFile)), (project) =>
    {
        Information($"Processing project '{project.Name}'");

        var projectTemplate = GetOctopusTemplate(project);

        var newProjectSkeleton = new ProjectResource
        {
            Name = project.Name,
            Description = project.Description,
            ProjectGroupId = GetProjectGroupResourceByName(project.Group).Id,
            LifecycleId = GetLifecycleResourceByName(OctopusLifecycleResourceName).Id
        };

        var newProject = CreateProjectByClone(GetProjectResourceByName(projectTemplate.Name), newProjectSkeleton);

        Information("Fetching, updating and saving deployment process");
        var deploymentProcess = GetDeploymentProcesses(newProject);
        var updatedDeploymentProcess = UpdateDeploymentProcessTargetRoles(deploymentProcess, project.Settings.AppGroup);
        ModifyDeploymentProcess(updatedDeploymentProcess);

        Information("Fetching, updating and saving variables");
        var variableSet = GetVariableSets(newProject);
        var updatedVariableSet = UpdateVariableStepAppGroup(variableSet, project.Settings.AppGroup);
        ModifyVariableSet(updatedVariableSet);

        Information("Fetching, updating and saving project triggers");
        var projectTriggers = GetProjectTriggers(newProject);
        var updatedProjectTriggers = UpdateProjectTriggerTargetRoles(projectTriggers, project.Settings.AppGroup);
        ModifyProjectTriggers(updatedProjectTriggers);

        Information($"Project '{project.Name}' (based on the template '{projectTemplate.Name}') has been created");
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget (target);
