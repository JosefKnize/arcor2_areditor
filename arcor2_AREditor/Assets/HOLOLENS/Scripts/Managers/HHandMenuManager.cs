using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using Base;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using IO.Swagger.Model;
using TMPro;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;

public class HHandMenuManager : Singleton<HHandMenuManager>
{
    public StatefulInteractable ShowScenesButton;
    public StatefulInteractable AddObjectMenu;
    public StatefulInteractable ExperimentToggle;

    public GameObject models;
    public GameObject collisionObjects;   

    private bool scenesLoaded, projectsLoaded, packagesLoaded, scenesUpdating, projectsUpdating, packagesUpdating;
    private bool wasLastUpdate = false;


    private void Awake()
    {
        scenesLoaded = projectsLoaded = scenesUpdating = projectsUpdating = packagesLoaded = packagesUpdating = false;
    }

    void Start()
    {
        ShowScenesButton.OnClicked.AddListener(OpenScenesClicked);
        AddObjectMenu.OnClicked.AddListener(AddObjectClicked);
    }

    private void ExperimentButtonPressed()
    {
        if (ExperimentToggle.IsToggled)
        {
            ExperimentManager.Instance.StartExperiment();
        }
        else
        {
            ExperimentManager.Instance.StopExperiment();
        }
    }

    private void AddObjectClicked()
    {
        
    }

    public async void OpenScenesClicked()
    {
        try
        {
            if (!scenesUpdating)
            {
                scenesUpdating = true;
                scenesLoaded = false;
                WebSocketManagerH.Instance.LoadScenes(LoadScenesCb);
            }
            await WaitUntilScenesLoaded();
        }
        catch (TimeoutException ex)
        {
            HNotificationManager.Instance.ShowNotification("Failed to open scenes");
        }
    }
    public async void OpenProjects()
    {
        try
        {
            if (!projectsUpdating)
            {
                projectsUpdating = true;
                projectsLoaded = false;
                WebSocketManagerH.Instance.LoadProjects(LoadProjectsCb);
            }
            await WaitUntilProjectsLoaded();
        }
        catch (TimeoutException ex)
        {
            HNotificationManager.Instance.ShowNotification("Failed to switch to projects");
        }
    }

    public void LoadScenesCb(string id, string responseData)
    {
        IO.Swagger.Model.ListScenesResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.ListScenesResponse>(responseData);

        if (response == null || !response.Result)
        {
            HNotificationManager.Instance.ShowNotification("Failed to load scenes");
            scenesUpdating = false;
            return;
        }
        GameManagerH.Instance.Scenes = response.Data;
        GameManagerH.Instance.Scenes.Sort(delegate (ListScenesResponseData x, ListScenesResponseData y)
        {
            return y.Modified.CompareTo(x.Modified);
        });
        scenesUpdating = false;
        scenesLoaded = true;
        GameManagerH.Instance.InvokeScenesListChanged();
    }

    public void LoadProjectsCb(string id, string responseData)
    {
        IO.Swagger.Model.ListProjectsResponse response = JsonConvert.DeserializeObject<IO.Swagger.Model.ListProjectsResponse>(responseData);
        if (response == null)
        {
            HNotificationManager.Instance.ShowNotification("Failed to load projects");
            return;
        }
        GameManagerH.Instance.Projects = response.Data;
        GameManagerH.Instance.Projects.Sort(delegate (ListProjectsResponseData x, ListProjectsResponseData y)
        {
            return y.Modified.CompareTo(x.Modified);
        });
        projectsUpdating = false;
        projectsLoaded = true;
        GameManagerH.Instance.InvokeProjectsListChanged();
    }

    private async Task WaitUntilScenesLoaded()
    {
        await Task.Run(() =>
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (true)
            {
                if (sw.ElapsedMilliseconds > 5000)
                    throw new TimeoutException("Failed to load scenes");
                if (scenesLoaded)
                {
                    return true;
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        });
    }

    private async Task WaitUntilProjectsLoaded()
    {
        await Task.Run(() =>
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (true)
            {
                if (sw.ElapsedMilliseconds > 5000)
                    throw new TimeoutException("Failed to load projects");
                if (projectsLoaded)
                {
                    return true;
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        });

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
            Debug.LogError("Failed to create new project" + ex.Message);
        }
    }

    private async void CreateScene()
    {
        string nameOfNewScene = "scene_" + Guid.NewGuid().ToString().Substring(0, 4);
        try
        {
            await WebSocketManagerH.Instance.CreateScene(nameOfNewScene, "");
        }
        catch (RequestFailedException e)
        {
            Notifications.Instance.ShowNotification("Failed to create new scene", e.Message);
        }
    }
}
