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
using UnityEngine.XR.ARSubsystems;

public class ExperimentManager : Base.Singleton<ExperimentManager>
{
    private Vector3 lastPosition;
    private float totalDistance;
    private DateTime startTime;

    private GameObject refDobotM1;
    private GameObject refDobotMagician;

    public GameObject TrackedCamera;
    public GameObject SceneOrigin;

    public GameObject RobotPrefab;

    public bool Running { get; set; } = false;
    public bool DisplayModels { get; private set; } = false;

    private bool ghostRobotsCreated = false;
    private StreamWriter cameraPositionLogger;
    private float lastLogTime;

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

            if (Time.time - lastLogTime >= 0.1f)
            {
                LogCameraData();
                lastLogTime = Time.time; 
            }
        }
    }

    private void LogCameraData()
    {
        Vector3 cameraPositionRelativeToReference = SceneOrigin.transform.InverseTransformPoint(TrackedCamera.transform.position);
        Quaternion cameraRotationRelativeToReference = Quaternion.Inverse(SceneOrigin.transform.rotation) * TrackedCamera.transform.rotation;

        string data = string.Format("{0:F3};{1:F3};{2:F3};{3:F3};{4:F3};{5:F3};{6:F3}",
                                     cameraPositionRelativeToReference.x,
                                     cameraPositionRelativeToReference.y,
                                     cameraPositionRelativeToReference.z,
                                     cameraRotationRelativeToReference.eulerAngles.x,
                                     cameraRotationRelativeToReference.eulerAngles.y,
                                     cameraRotationRelativeToReference.eulerAngles.z,
                                     Time.time);
        Debug.Log(data);
        cameraPositionLogger.WriteLine(data);
    }

    private void StartLoggingCameraPosition()
    {
        var filePath = Path.Combine(Application.persistentDataPath, $"Experiment_CameraPath_{DateTime.Now.ToString("dd.MM_HH.mm")}.txt");
        Debug.Log(filePath);
        cameraPositionLogger = new StreamWriter(filePath, true);
    }

    private void StopLoggingCameraPosition()
    {
        cameraPositionLogger.Close();
        cameraPositionLogger = null;
    }

    public void StartExperiment()
    {
        Running = true;
        lastPosition = TrackedCamera.transform.position;
        startTime = DateTime.Now;

        if (!ghostRobotsCreated)
        {
            // Add invisible robots in scene Vector3(Dopøedu/Dozadu, Nahoru/Dolu, Doleva/Doprava)
            refDobotM1 = CreateGhostRobot("DobotM1", new Vector3(-0.5445f, 0, 0.6665f), new Vector3(0, 90f, 0));
            refDobotMagician = CreateGhostRobot("DobotMagician", new Vector3(-0.38f, 0.141f, 0.03f), new Vector3(0, 90, 0));
 
            ghostRobotsCreated = true;
        }

        StartLoggingCameraPosition();
    }

    private void OnModelLoaded(object sender, ImportedMeshEventArgsH args)
    {
        args.RootGameObject.gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        args.RootGameObject.gameObject.transform.localPosition = new Vector3(0, 0, 0);
        MeshImporterH.Instance.OnMeshImported -= OnModelLoaded;
    }

    GameObject CreateGhostRobot(string type, Vector3 scenePosition, Vector3 rotation)
    {
        var gameObject = new GameObject($"GhostRobot_{type}");
        gameObject.transform.parent = SceneOrigin.transform;
        gameObject.transform.localEulerAngles = rotation;
        gameObject.transform.localPosition = scenePosition;

        if (DisplayModels)
        {
            if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(type, out RobotMeta robotMeta))
            {
                RobotModelH robotModel = UrdfManagerH.Instance.GetRobotModelInstance(robotMeta.Type, robotMeta.UrdfPackageFilename);
                robotModel.RobotModelGameObject.gameObject.transform.parent = gameObject.transform;
                robotModel.RobotModelGameObject.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                robotModel.RobotModelGameObject.gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                robotModel.RobotModelGameObject.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                robotModel.SetActiveAllVisuals(true);
            }
            else
            {
                throw new Exception("Experiment manager couldn't create ghost robot");
            }
        }

        return gameObject;
    }

    public void StopExperiment()
    {
        Running = false;
        StopLoggingCameraPosition();
        var time = DateTime.Now - startTime;

        // Measure distance in robots

        var dobot_magician = GameObject.Find("dobot_magician");
        var dobot_m1 = GameObject.Find("dobot_m1");

        float DobotM1_distance = Vector3.Distance(refDobotM1.transform.position, dobot_m1.transform.position);
        float DobotM1_angleDifference = Mathf.Abs(dobot_m1.transform.localEulerAngles.y - refDobotM1.transform.localEulerAngles.y);
        DobotM1_angleDifference = (DobotM1_angleDifference > 180f) ? 360f - DobotM1_angleDifference : DobotM1_angleDifference;

        float DobotMagician_distance = Vector3.Distance(refDobotMagician.transform.position, dobot_magician.transform.position);
        float DobotMagician_angleDifference = Mathf.Abs(dobot_magician.transform.localEulerAngles.y - refDobotMagician.transform.localEulerAngles.y);
        DobotMagician_angleDifference = (DobotMagician_angleDifference > 180f) ? 360f - DobotMagician_angleDifference : DobotMagician_angleDifference;



        float averageDistance = (DobotM1_distance + DobotMagician_distance) / 3;
        float averageAngleDifference = (DobotM1_angleDifference + DobotMagician_angleDifference) / 3;

        string filePath = Path.Combine(Application.persistentDataPath, $"Experiment_{DateTime.Now.ToString("dd.MM_HH.mm")}.txt");

        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }

        File.WriteAllText(filePath, $"Time: {time}\nDistance: {totalDistance}\nRobot position error: {averageDistance}\nRobot rotation error: {averageAngleDifference}\nDobotM1: {dobot_m1.transform.localPosition}{dobot_m1.transform.localEulerAngles}\nDobotMagician: {dobot_magician.transform.localPosition}{dobot_magician.transform.localEulerAngles}");
    }
}
