using System;
using System.Collections.Generic;
using UnityEngine;
using Base;
using IO.Swagger.Model;
using Hololens;
using TMPro;
using RosSharp.Urdf;
using MixedReality.Toolkit;


public class ListScenes : Singleton<ListScenes>
{
    public GameObject SceneList;

    public GameObject SpherePrefab;
    public GameObject CubePrefab;
    public GameObject CylinderPrefab;

    public GameObject StartPrefab;
    public GameObject EndPrefab;

    public GameObject ActionPointPrefab;
    public GameObject ActionPrefab;

    public GameObject scenePrefab;
    public GameObject connectionPrefab;

    public Material lineMaterial;

    private Dictionary<string, GameObject> scenes = new Dictionary<string, GameObject>();
    private Dictionary<string, Dictionary<string, GameObject>> actions_scenes = new Dictionary<string, Dictionary<string, GameObject>>();

    private Dictionary<string, List<IO.Swagger.Model.LogicItem>> project_logicItems = new Dictionary<string, List<IO.Swagger.Model.LogicItem>>();
    private string waitingSceneProject = null;
    private List<GameObject> previewConnections = new();

    public void setActiveMenu(bool active)
    {
        if (!active)
        {
            foreach (var item in previewConnections)
            {
                Destroy(item);
            }
        }
        SceneList.SetActive(active);
    }
    void Start()
    {
        setActiveMenu(false);
        GameManagerH.Instance.OnScenesListChanged += UpdateScenes;
        GameManagerH.Instance.OnProjectsListChanged += UpdateProjects;
        GameManagerH.Instance.OnOpenSceneEditor += OnShowEditorScreen;
        GameManagerH.Instance.OnOpenProjectEditor += OnShowEditorScreen;
    }

    public void OnShowEditorScreen(object sender, EventArgs args)
    {
        setActiveMenu(false);
    }

    public async void UpdateProjects(object sender, EventArgs eventArgs)
    {
        Debug.Log("Update Projects");
        foreach (KeyValuePair<string, GameObject> kvp in scenes)
        {
            Destroy(kvp.Value);
        }
        actions_scenes.Clear();
        project_logicItems.Clear();
        scenes.Clear();

        CreateNewProjectObject();

        foreach (IO.Swagger.Model.ListProjectsResponseData project in GameManagerH.Instance.Projects)
        {
            createMenuProject(await WebSocketManagerH.Instance.GetScene(project.SceneId), await WebSocketManagerH.Instance.GetProject(project.Id));
        }

        UpdateLogicItem();
        setActiveMenu(true);
        SceneList.GetComponent<HorizontalObjectScrollMenu>().UpdateCollection();
        SceneList.transform.parent.localPosition = new Vector3(0, 0.25f, 0);
        SceneList.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
        SceneList.transform.localPosition = Vector3.zero;
        SceneList.transform.localRotation = Quaternion.identity;

    }

    private void CreateNewProjectObject()
    {
        GameObject newScene = Instantiate(scenePrefab, SceneList.transform);
        newScene.name = "Create new";
        newScene.GetComponentInChildren<TextMeshPro>().text = "Create new";

        scenes.Add("Create new", newScene);
        newScene.GetComponent<StatefulInteractable>().RegisterOnShortClick(CreateProject);
    }

    public async void UpdateScenes(object sender, EventArgs eventArgs)
    {
        foreach (KeyValuePair<string, GameObject> kvp in scenes)
        {
            Destroy(kvp.Value);
        }

        actions_scenes.Clear();
        project_logicItems.Clear();

        scenes.Clear();

        CreateNewSceneObject();

        foreach (ListScenesResponseData scene in GameManagerH.Instance.Scenes)
        {
            createMenuScene(await WebSocketManagerH.Instance.GetScene(scene.Id));
        }

        setActiveMenu(true);
        SceneList.GetComponent<HorizontalObjectScrollMenu>().UpdateCollection();
        SceneList.transform.parent.localPosition = new Vector3(0, 0.25f, 0);
        SceneList.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
        SceneList.transform.localPosition = Vector3.zero;
        SceneList.transform.localRotation = Quaternion.identity;
    }

    private void CreateNewSceneObject()
    {
        GameObject newScene = Instantiate(scenePrefab, SceneList.transform);
        newScene.name = "Create new";
        newScene.GetComponentInChildren<TextMeshPro>().text = "Create new";

        scenes.Add("Create new", newScene);
        newScene.GetComponent<StatefulInteractable>().RegisterOnShortClick(CreateScene);
    }

    internal void createMenuScene(Scene scene)
    {
        GameObject newScene = Instantiate(scenePrefab, SceneList.transform);
        newScene.name = scene.Name;
        newScene.GetComponentInChildren<TextMeshPro>().text = scene.Name;
        GameObject ActionObjectsSpawn = new GameObject(scene.Name);

        Dictionary<string, GameObject> ActionObjects = new Dictionary<string, GameObject>();

        foreach (IO.Swagger.Model.SceneObject sceneObject in scene.Objects)
        {
            if (!ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(sceneObject.Type, out ActionObjectMetadataH actionObject))
            {
                continue;
            }
            if (HActionObjectPickerMenu.Instance.objectsModels.TryGetValue(actionObject.Type, out GameObject objectModel))
            {
                Transform obj = objectModel.transform.Find("ImportedMeshObject");
                if (obj == null)
                {
                    obj = objectModel.GetComponentInChildren<UrdfRobot>().transform;
                }
                GameObject menuObject = Instantiate(obj.gameObject, ActionObjectsSpawn.transform);
                menuObject.transform.localScale = new Vector3(1f, 1f, 1f);
                menuObject.transform.parent = ActionObjectsSpawn.transform;
                menuObject.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                menuObject.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
            }
            else if (actionObject.CollisionObject)
            {

                switch (actionObject.ObjectModel.Type)
                {
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                        GameObject cube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        cube.transform.parent = ActionObjectsSpawn.transform;
                        cube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cube.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)actionObject.ObjectModel.Box.SizeX, (float)actionObject.ObjectModel.Box.SizeY, (float)actionObject.ObjectModel.Box.SizeZ));
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                        GameObject cylinder = Instantiate(CylinderPrefab, ActionObjectsSpawn.transform);
                        cylinder.transform.parent = ActionObjectsSpawn.transform;
                        cylinder.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cylinder.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cylinder.transform.localScale = new Vector3((float)actionObject.ObjectModel.Cylinder.Radius, (float)actionObject.ObjectModel.Cylinder.Height / 2, (float)actionObject.ObjectModel.Cylinder.Radius);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                        GameObject sphere = Instantiate(SpherePrefab, ActionObjectsSpawn.transform);
                        sphere.transform.parent = ActionObjectsSpawn.transform;
                        sphere.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        sphere.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        sphere.transform.localScale = new Vector3((float)actionObject.ObjectModel.Sphere.Radius, (float)actionObject.ObjectModel.Sphere.Radius, (float)actionObject.ObjectModel.Sphere.Radius);
                        break;

                    default:
                        GameObject defaultCube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        defaultCube.transform.parent = ActionObjectsSpawn.transform;
                        defaultCube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        defaultCube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        defaultCube.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                        break;
                }
            }

        }
        ActionObjectsSpawn.transform.parent = newScene.transform;

        if (scene.Objects.Count > 0)
        {
            Bounds bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
            foreach (Renderer mesh in ActionObjectsSpawn.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(mesh.bounds);
            }

            // Scale
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            var scaleFactor = 0.25f / maxDimension;
            ActionObjectsSpawn.transform.localScale *= scaleFactor;

            // Position
            bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
            foreach (Renderer mesh in ActionObjectsSpawn.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(mesh.bounds);
            }
            Vector3 offset = newScene.transform.position - bounds.center;
            ActionObjectsSpawn.transform.position += offset;

            // Fix model going black when making it small
            Shader unlitColorShader = Shader.Find("Mixed Reality Toolkit/Standard");
            var renderers = ActionObjectsSpawn.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.shader = unlitColorShader;
                }
            }
        }

        scenes.Add(scene.Name, newScene);
        newScene.GetComponent<StatefulInteractable>().RegisterOnShortClick(() => openScene(scene.Id));
    }

    internal void createMenuProject(Scene scene, Project project)
    {
        GameObject newProject = Instantiate(scenePrefab, SceneList.transform);
        newProject.name = project.Name;
        newProject.GetComponentInChildren<TextMeshPro>().text = project.Name;

        GameObject ActionObjectsSpawn = new GameObject(project.Name);

        Dictionary<string, GameObject> ActionObjects = new Dictionary<string, GameObject>();

        foreach (IO.Swagger.Model.SceneObject sceneObject in scene.Objects)
        {
            if (!ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(sceneObject.Type, out ActionObjectMetadataH actionObject))
            {
                continue;
            }
            if (HActionObjectPickerMenu.Instance.objectsModels.TryGetValue(actionObject.Type, out GameObject objectModel))
            {
                Transform obj = objectModel.transform.Find("ImportedMeshObject");
                if (obj == null)
                {
                    obj = objectModel.GetComponentInChildren<UrdfRobot>().transform;
                }
                GameObject menuObject = Instantiate(obj.gameObject, ActionObjectsSpawn.transform);
                menuObject.transform.localScale = new Vector3(1f, 1f, 1f);
                menuObject.transform.parent = ActionObjectsSpawn.transform;
                menuObject.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                menuObject.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                ActionObjects.Add(sceneObject.Id, menuObject);
            }
            else if (actionObject.CollisionObject)
            {
                switch (actionObject.ObjectModel.Type)
                {
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                        GameObject cube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        cube.transform.parent = ActionObjectsSpawn.transform;
                        cube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cube.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)actionObject.ObjectModel.Box.SizeX, (float)actionObject.ObjectModel.Box.SizeY, (float)actionObject.ObjectModel.Box.SizeZ));
                        ActionObjects.Add(sceneObject.Id, cube);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                        GameObject cylinder = Instantiate(CylinderPrefab, ActionObjectsSpawn.transform);
                        cylinder.transform.parent = ActionObjectsSpawn.transform;
                        cylinder.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        cylinder.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        cylinder.transform.localScale = new Vector3((float)actionObject.ObjectModel.Cylinder.Radius, (float)actionObject.ObjectModel.Cylinder.Height / 2, (float)actionObject.ObjectModel.Cylinder.Radius);
                        ActionObjects.Add(sceneObject.Id, cylinder);
                        break;
                    case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                        GameObject sphere = Instantiate(SpherePrefab, ActionObjectsSpawn.transform);
                        sphere.transform.parent = ActionObjectsSpawn.transform;
                        sphere.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        sphere.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        sphere.transform.localScale = new Vector3((float)actionObject.ObjectModel.Sphere.Radius, (float)actionObject.ObjectModel.Sphere.Radius, (float)actionObject.ObjectModel.Sphere.Radius);
                        ActionObjects.Add(sceneObject.Id, sphere);
                        break;

                    default:
                        GameObject defaultCube = Instantiate(CubePrefab, ActionObjectsSpawn.transform);
                        defaultCube.transform.parent = ActionObjectsSpawn.transform;
                        defaultCube.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(sceneObject.Pose.Position));
                        defaultCube.transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(sceneObject.Pose.Orientation));
                        defaultCube.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                        ActionObjects.Add(sceneObject.Id, defaultCube);
                        break;
                }
            }

        }

        Dictionary<string, GameObject> actions_ID = new Dictionary<string, GameObject>();
        GameObject start = Instantiate(StartPrefab, ActionObjectsSpawn.transform);
        start.transform.localPosition = PlayerPrefsHelper.LoadVector3("project/" + project.Id + "/" + "START", new Vector3(0, 0.15f, 0));
        actions_ID.Add("START", start);

        GameObject end = Instantiate(EndPrefab, ActionObjectsSpawn.transform);
        end.transform.localPosition = PlayerPrefsHelper.LoadVector3("project/" + project.Id + "/" + "END", new Vector3(0, 0.1f, 0));
        actions_ID.Add("END", end);


        Dictionary<string, GameObject> ActionPoints = new Dictionary<string, GameObject>();

        foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints)
        {
            GameObject AP = Instantiate(ActionPointPrefab, ActionObjectsSpawn.transform);
            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            if (projectActionPoint.Parent != null)
            {
                if (ActionObjects.TryGetValue(projectActionPoint.Parent, out GameObject parent))
                {
                    AP.transform.parent = parent.transform;
                }
                else
                {
                    AP.transform.parent = ActionObjectsSpawn.transform;
                }
            }
            else
            {
                AP.transform.parent = ActionObjectsSpawn.transform;
            }


            AP.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(projectActionPoint.Position));
            int i = 1;
            ActionPoints.Add(projectActionPoint.Id, AP);
            foreach (IO.Swagger.Model.Action action in projectActionPoint.Actions)
            {
                GameObject nAction = Instantiate(ActionPrefab, AP.transform);
                nAction.transform.localPosition = new Vector3(0, i * 0.015f + 0.015f, 0);
                ++i;
                nAction.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                actions_ID.Add(action.Id, nAction);
            }
        }
        foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints)
        {
            if (projectActionPoint.Parent != null)
            {
                if (ActionPoints.TryGetValue(projectActionPoint.Parent, out GameObject parent))
                {
                    if (ActionPoints.TryGetValue(projectActionPoint.Id, out GameObject child))
                    {
                        child.transform.parent = parent.transform;
                        child.transform.localPosition = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(projectActionPoint.Position));
                    }
                }
            }
        }

        project_logicItems.Add(project.Name, project.Logic);

        ActionObjectsSpawn.transform.parent = newProject.transform;
        if (scene.Objects.Count > 0)
        {
            // Scale
            Bounds bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
            foreach (Renderer mesh in ActionObjectsSpawn.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(mesh.bounds);
            }

            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            var scaleFactor = 0.25f / maxDimension;
            ActionObjectsSpawn.transform.localScale *= scaleFactor;

            // Position
            bounds = new Bounds(ActionObjectsSpawn.transform.position, Vector3.zero);
            foreach (Renderer mesh in ActionObjectsSpawn.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(mesh.bounds);
            }
            Vector3 offset = newProject.transform.position - bounds.center;
            ActionObjectsSpawn.transform.position += offset;

            // Fix model going black when making it small
            Shader unlitColorShader = Shader.Find("Mixed Reality Toolkit/Standard");
            var renderers = ActionObjectsSpawn.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.shader = unlitColorShader;
                }
            }
        }

        scenes.Add(project.Name, newProject);
        actions_scenes.Add(project.Name, actions_ID);

        newProject.GetComponent<StatefulInteractable>().RegisterOnShortClick(() => openProject(project.Id));
    }

    public void waitOpenProject(object sender, EventArgs args)
    {
        if (waitingSceneProject != null)
        {
            GameManagerH.Instance.OpenProject(waitingSceneProject);
            waitingSceneProject = null;
            GameManagerH.Instance.OnCloseProject -= waitOpenProject;
            GameManagerH.Instance.OnCloseScene -= waitOpenProject;
        }

    }
    public void openProject(String projectID)
    {
        if (GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.ProjectEditor) || GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.SceneEditor))
        {
            waitingSceneProject = projectID;
            GameManagerH.Instance.OnCloseProject += waitOpenProject;
            GameManagerH.Instance.OnCloseScene += waitOpenProject;

            HEditorMenuScreen.Instance.CloseScene();
        }
        else
        {
            GameManagerH.Instance.OpenProject(projectID);
        }
    }

    public async void waitOpenScene(object sender, EventArgs args)
    {
        if (waitingSceneProject != null)
        {
            await GameManagerH.Instance.OpenScene(waitingSceneProject);
            waitingSceneProject = null;
            GameManagerH.Instance.OnCloseScene -= waitOpenScene;
            GameManagerH.Instance.OnCloseProject -= waitOpenScene;
        }

    }

    public async void openScene(String sceneID)
    {
        if (GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.ProjectEditor) || GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.SceneEditor))
        {
            waitingSceneProject = sceneID;
            GameManagerH.Instance.OnCloseProject += waitOpenScene;
            GameManagerH.Instance.OnCloseScene += waitOpenScene;

            HEditorMenuScreen.Instance.CloseScene();
        }
        else
        {
            await GameManagerH.Instance.OpenScene(sceneID);
        }
    }

    private async void CreateScene()
    {
        string nameOfNewScene = "scene_" + Guid.NewGuid().ToString().Substring(0, 4);

        try
        {
            HEditorMenuScreen.Instance.CloseScene();
            await WebSocketManagerH.Instance.CreateScene(nameOfNewScene, "");
        }
        catch (RequestFailedException e)
        {
            // TODO notification
        }
    }

    private async void CreateProject()
    {
        string nameOfNewProject = "project_" + Guid.NewGuid().ToString().Substring(0, 4);
        try
        {
            await WebSocketManagerH.Instance.CreateProject(nameOfNewProject,
            SceneManagerH.Instance.SceneMeta.Id,
            "",
            true,
            false);
        }
        catch (RequestFailedException ex)
        {
            // TODO notification
        }
    }


    public void UpdateLogicItem()
    {
        foreach (KeyValuePair<string, List<IO.Swagger.Model.LogicItem>> kvp in project_logicItems)
        {
            if (actions_scenes.TryGetValue(kvp.Key, out Dictionary<string, GameObject> action_project))
            {
                foreach (IO.Swagger.Model.LogicItem items in kvp.Value)
                {
                    if (action_project.TryGetValue(items.Start, out GameObject start))
                    {
                        if (action_project.TryGetValue(items.End, out GameObject end))
                        {
                            GameObject lineG = Instantiate(connectionPrefab, scenes[kvp.Key].transform.GetChild(0));
                            previewConnections.Add(lineG);
                            Connection c = lineG.GetComponent<Connection>();

                            c.target[0] = start.transform.Find("Output").GetComponent<RectTransform>();
                            c.target[1] = end.transform.Find("Input").GetComponent<RectTransform>();
                        }
                    }
                }
            }
        }
    }

}
