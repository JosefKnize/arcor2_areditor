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
    public StatefulInteractable OpenSceneButton;
    public StatefulInteractable OpenProjectButton;
    public StatefulInteractable ExperimentToggle;
    public GameObject HandMenu;

    private bool scenesLoaded, projectsLoaded, scenesUpdating, projectsUpdating;

    private void Awake()
    {
        scenesLoaded = projectsLoaded = scenesUpdating = projectsUpdating = false;
    }

    void Start()
    {
        GameManagerH.Instance.OnConnectedToServer += (sender, args) => HandMenu.SetActive(true); 
        OpenSceneButton.OnClicked.AddListener(OpenScenesClicked);
        OpenProjectButton.OnClicked.AddListener(OpenProjectsClicked);
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

    public async void OpenProjectsClicked()
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
}
