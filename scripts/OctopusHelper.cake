#addin Octopus.Client&version=4.33.1

using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.Triggers;

private string _serverUrl;
private string _apiKey;

private OctopusClient _client = null;
private OctopusRepository _repo = null;

public void CreateOctopusHelper(string OctopusServerURL, string OctopusAPIKey)
{
    _serverUrl = OctopusServerURL;
    _apiKey = OctopusAPIKey;
}

public OctopusRepository CreateOctopusRepository()
{
    if (_client == null)
    {
        Information("OctopusDeploy 'client' not yet created. Creating now");
        CreateOctopusClient();
    }

    if (_repo == null)
    {
        Information("Creating repository for access to OctopusDeploy");
        _repo = new OctopusRepository (_client);
    }

    return _repo;
}

private OctopusClient CreateOctopusClient()
{
    if (_client == null)
    {
        Information($"Connecting to OctopusServer '{_serverUrl}'...");
        _client = new OctopusClient(new OctopusServerEndpoint(_serverUrl, _apiKey));
    }
    return _client;
}

public void ModifyDeploymentProcess(DeploymentProcessResource deploymentProcess)
{
    _repo.DeploymentProcesses.Modify(deploymentProcess);
    Information($"DeploymentProcess '{deploymentProcess.Id}' has been modified");
}

public void ModifyVariableSet(VariableSetResource variableSet)
{
    _repo.VariableSets.Modify(variableSet);
    Information($"VariableSet '{variableSet.Id}' has been modified");
}

public void ModifyProjectTriggers(IEnumerable<ProjectTriggerResource> projectTriggers)
{
    foreach (var projectTrigger in projectTriggers)
    {
        _repo.ProjectTriggers.Modify(projectTrigger);
        Information ($"ProjectTrigger '{projectTrigger.Name}' ({projectTrigger.Id}) has been modified");
    }
}

public void ModifyProject(ProjectResource projectResource)
{
    _repo.Projects.Modify (projectResource);
    Information ($"Project '{projectResource.Name}' ({projectResource.Id}) has been modified");
}

public ProjectResource CreateProjectByClone(ProjectResource sourceProject, ProjectResource destinationProject)
{
    var newProject = GetProjectResourceByName(destinationProject.Name);

    if (newProject == null)
    {
        Information($"Project '{destinationProject.Name}' doesn't exist, creating by cloning from '{sourceProject.Name}'");

        _client.Post("~/api/projects?clone=" + sourceProject.Id, destinationProject);

        newProject = GetProjectResourceByName(destinationProject.Name);

        Information($"Project '{destinationProject.Name}' has been created with ID '{newProject.Id}'");
    }
    else
    {
        Information ($"Project {destinationProject.Name} already exists with ID '{newProject.Id}'. Skipping clone action");
    }

    return newProject;
}

private ProjectResource GetProjectResourceByName(string projectName)
{
    return _repo.Projects.FindByName(projectName);
}

private ProjectGroupResource GetProjectGroupResourceByName(string projectGroupName)
{
    return _repo.ProjectGroups.FindByName(projectGroupName);
}

public LifecycleResource GetLifecycleResourceByName(string lifecycleName)
{
    return _repo.Lifecycles.FindByName(lifecycleName);
}

public DeploymentProcessResource UpdateDeploymentProcessTargetRoles(DeploymentProcessResource deploymentProcess, string targetRole)
{
    foreach (var step in deploymentProcess.Steps)
    {
        foreach (var stepProperty in step.Properties.ToList())
        {
            if (stepProperty.Key == "Octopus.Action.TargetRoles") // Find steps that are pinned to a role
            {
                if (stepProperty.Value.Value != "octopus-deploy") // Skip the octopus-deploy server role, we leave this the same
                {
                    Verbose($"    > Updating step '{step.Name}' ({step.Id}) setting TargetingRole to '{targetRole}'");
                    step.TargetingRoles(targetRole);
                    Verbose($"    > Step '{step.Name}' ({step.Id}) updated");
                }
            }
        }
    }

    return deploymentProcess;
}

public IEnumerable<ProjectTriggerResource> UpdateProjectTriggerTargetRoles(IEnumerable<ProjectTriggerResource> projectTriggers, string targetRole)
{
    var machineFilter = new MachineFilterResource();
    machineFilter.Roles.Add(targetRole);
    machineFilter.EventGroups.Add("MachineAvailableForDeployment");

    foreach (var trigger in projectTriggers)
    {
        Verbose($"    > Adding TriggerFilter to trigger '{trigger.Name}' ({trigger.Id})");
        trigger.Filter = machineFilter;
        Verbose($"    > Trigger '{trigger.Name}' ({trigger.Id}) updated");
        yield return trigger;
    }
}

public DeploymentProcessResource GetDeploymentProcesses(ProjectResource projectResource)
{
    return _repo.DeploymentProcesses.Get(projectResource.DeploymentProcessId);
}

public VariableSetResource GetVariableSets(ProjectResource projectResource)
{
    return _repo.VariableSets.Get(projectResource.VariableSetId);
}

public IEnumerable<ProjectTriggerResource> GetProjectTriggers(ProjectResource projectResource)
{
    var projectTriggers = _repo.Projects.GetTriggers(projectResource);

    foreach (var projectTrigger in projectTriggers.Items)
    {
        yield return _repo.ProjectTriggers.Get(projectTrigger.Id);
    }
}

public VariableSetResource UpdateVariableStepAppGroup (VariableSetResource variableSet, string appgroup)
{
    if (variableSet.Variables.Any(i => i.Name == "appgroup"))
    {
        Verbose($"    > Updating VariableSet 'appgroup'");
        variableSet.AddOrUpdateVariableValue("appgroup", appgroup);
        Verbose($"    > VariableSet 'appgroup' updated.");
    }
    return variableSet;
}
