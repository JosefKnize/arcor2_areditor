using Base;
using Hololens;
using IO.Swagger.Model;
using QRTracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExperimentManager : Base.Singleton<ExperimentManager>
{
    private Vector3 lastPosition;
    private float totalDistance;
    private DateTime startTime;

    private GameObject refDobotM1;
    private GameObject refDobotMagician;
    private GameObject refConveyorBelt;

    public GameObject TrackedCamera;
    public GameObject SceneOrigin;

    public GameObject RobotPrefab;

    public bool Running { get; set; } = false;

    private bool ghostRobotsCreated = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Running)
        {
            totalDistance += Vector3.Distance(TrackedCamera.transform.position, lastPosition);
            lastPosition = TrackedCamera.transform.position;
        }
    }

    public void StartExperiment()
    {
        Running = true;
        lastPosition = TrackedCamera.transform.position;
        startTime = DateTime.Now;

        if (!ghostRobotsCreated)
        {
            // Add invisible robots in scene
            refDobotM1 = CreateGhostRobot("DobotM1", new Vector3(0, 0, 0), new Vector3(0, 90, 0));             // TODO
            refDobotMagician = CreateGhostRobot("DobotMagician", new Vector3(0, 0, 0), new Vector3(0, 90, 0)); // TODO
            refConveyorBelt = CreateGhostConveyorBelt("ConveyorBelt");
            ghostRobotsCreated = true;
        }
    }

    GameObject CreateGhostConveyorBelt(string type)
    {
        MeshImporterH.Instance.OnMeshImported += OnModelLoaded;

        var gameObject = new GameObject($"GhostRobot_ConveyorBelt");
        gameObject.transform.parent = SceneOrigin.transform;

        var actionObject = ActionsManagerH.Instance.ActionObjectsMetadata.Values.First(x => x.Type == type);
        MeshImporterH.Instance.LoadModel(actionObject.ObjectModel.Mesh, actionObject.Type);

        return gameObject;
    }

    private void OnModelLoaded(object sender, ImportedMeshEventArgsH args)
    {
        args.RootGameObject.gameObject.transform.parent = refConveyorBelt.transform;

        args.RootGameObject.gameObject.transform.localEulerAngles = new Vector3(0, 0, 0); // TODO
        args.RootGameObject.gameObject.transform.localPosition = new Vector3(0, 0, 0); // TODO
        MeshImporterH.Instance.OnMeshImported -= OnModelLoaded;
    }

    GameObject CreateGhostRobot(string type, Vector3 scenePosition, Vector3 rotation)
    {
        if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(type, out RobotMeta robotMeta))
        {
            var gameObject = new GameObject($"GhostRobot_{type}");
            gameObject.transform.parent = SceneOrigin.transform;

            RobotModelH robotModel = UrdfManagerH.Instance.GetRobotModelInstance(robotMeta.Type, robotMeta.UrdfPackageFilename);
            robotModel.RobotModelGameObject.gameObject.transform.parent = gameObject.transform;
            robotModel.RobotModelGameObject.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            robotModel.RobotModelGameObject.gameObject.transform.localEulerAngles = rotation;
            robotModel.RobotModelGameObject.gameObject.transform.localPosition = scenePosition;
            robotModel.SetActiveAllVisuals(true);

            return gameObject;
        }
        else
        {
            throw new Exception("Experiment manager couldn't create ghost robot");
        }
    }

    public void StopExperiment()
    {
        Running = false;
        var time = DateTime.Now - startTime;

        // Measure distance in robots

        var conveyor_belt = GameObject.Find("conveyor_belt");
        var dobot_magician = GameObject.Find("dobot_magician");
        var dobot_m1 = GameObject.Find("dobot_m1");

        float DobotM1_distance = Vector3.Distance(refDobotM1.transform.position, dobot_m1.transform.position);
        float DobotM1_angleDifference = Mathf.Abs(dobot_m1.transform.rotation.eulerAngles.y - 90 - refDobotM1.transform.rotation.eulerAngles.y);
        DobotM1_angleDifference = (DobotM1_angleDifference > 180f) ? 360f - DobotM1_angleDifference : DobotM1_angleDifference;

        float DobotMagician_distance = Vector3.Distance(refDobotMagician.transform.position, dobot_magician.transform.position);
        float DobotMagician_angleDifference = Mathf.Abs(dobot_magician.transform.rotation.eulerAngles.y - 90 - refDobotMagician.transform.rotation.eulerAngles.y);
        DobotMagician_angleDifference = (DobotMagician_angleDifference > 180f) ? 360f - DobotMagician_angleDifference : DobotMagician_angleDifference;

        float ConveyorBelt_distance = Vector3.Distance(refConveyorBelt.transform.position, conveyor_belt.transform.position);
        float ConveyorBelt_angleDifference = Mathf.Abs(conveyor_belt.transform.rotation.eulerAngles.y - refConveyorBelt.transform.rotation.eulerAngles.y);
        ConveyorBelt_angleDifference = (ConveyorBelt_angleDifference > 180f) ? 360f - ConveyorBelt_angleDifference : ConveyorBelt_angleDifference;

        float averageDistance = (DobotM1_distance + DobotMagician_distance + ConveyorBelt_distance) / 3;
        float averageAngleDifference = (DobotM1_angleDifference + DobotMagician_angleDifference + ConveyorBelt_angleDifference) / 3;

        string filePath = Path.Combine(Application.persistentDataPath, $"Experiment_{DateTime.Now.ToString("dd.MM_HH.mm")}.txt");

        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }
        File.WriteAllText(filePath, $"Time: {time}\nDistance: {totalDistance}\nRobot position error: {averageDistance}\nRobot rotation error: {averageAngleDifference}");
    }
}
