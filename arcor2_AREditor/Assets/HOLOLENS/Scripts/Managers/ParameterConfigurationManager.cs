/*
 Author: Josef Kníže
*/

using Base;
using Hololens;
using MixedReality.Toolkit;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ParameterConfigurationManager : Singleton<ParameterConfigurationManager>
{
    public GameObject ConfigurationWindow;
    public GameObject ConfigurationWindowGeneratedContentCollection;
    public TextMeshProUGUI HeaderText;

    public GameObject StringParameterEditor;
    public GameObject IntParameterEditor;
    public GameObject DoubleParameterEditor;
    public GameObject EnumParameterEditor;
    public GameObject BoolParameterEditor;

    public StatefulInteractable SaveButton;
    public StatefulInteractable DiscardButton;

    private List<ParameterEditorBase> parameterEditors = new();
    private HInteractiveObject configuredObject;
    private List<Parameter> editedParameters;

    private void Start()
    {
        SaveButton.OnClicked.AddListener(SaveButtonClicked);
        DiscardButton.OnClicked.AddListener(DiscardButtonClicked);
    }

    public void ShowConfigurationWindow(HInteractiveObject interactiveObject)
    {
        ClearParameterEditors();
        if (interactiveObject is HAction action)
        {
            GenerateWindowContent(action.Parameters);
        }
        else if (interactiveObject is ActionObjectH actionObject)
        {
            GenerateWindowContent(actionObject.ObjectParameters);
        }
        else
        {
            return;
        }

        configuredObject = interactiveObject;
        ConfigurationWindow.SetActive(true);
        HeaderText.text = $"Configuration of {interactiveObject.GetName()}";
        ConfigurationWindow.transform.position = NearObjectMenuManager.Instance.NearObjectMenuGameObject.transform.position;
        ConfigurationWindow.transform.position += ConfigurationWindow.transform.forward * -0.20f;
    }

    private void ClearParameterEditors()
    {
        parameterEditors.Clear();
        foreach (Transform child in ConfigurationWindowGeneratedContentCollection.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void GenerateWindowContent(Dictionary<string, Base.Parameter> parameters)
    {
        editedParameters = parameters.Values.ToList();

        GameObject editorGameObject = null;
        ParameterEditorBase editor;
        foreach (var parameter in editedParameters)
        {
            switch (parameter.ParameterMetadata.Type)
            {
                case ParameterMetadata.STR:
                    editorGameObject = Instantiate(StringParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.STR_ENUM:
                    editorGameObject = Instantiate(EnumParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.INT:
                    editorGameObject = Instantiate(IntParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.INT_ENUM:
                    editorGameObject = Instantiate(EnumParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.DOUBLE:
                    editorGameObject = Instantiate(DoubleParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.BOOL:
                    editorGameObject = Instantiate(BoolParameterEditor, ConfigurationWindowGeneratedContentCollection.transform);
                    break;
                case ParameterMetadata.POSE: // Ignore
                    break;
                case ParameterMetadata.POSITION: // Ignore
                    break;
                case ParameterMetadata.JOINTS: // Ignore
                    break;
            }

            if (editorGameObject is not null)
            {
                editor = editorGameObject.GetComponent<ParameterEditorBase>();
                editor.Parameter = parameter;
                parameterEditors.Add(editor);
            }
        }
    }

    private void DiscardButtonClicked()
    {
        ConfigurationWindow.SetActive(false);
    }

    private void SaveButtonClicked()
    {
        foreach (var editor in parameterEditors)
        {
            editor.MoveValueFromEditorToParameter();
        }

        SendUpdateToServer();

        ConfigurationWindow.SetActive(false);
    }

    private void SendUpdateToServer()
    {
        switch (configuredObject)
        {
            case HAction action:
                action.UploadNewParametersAsync(editedParameters);
                break;
            case ActionObjectH robot:
                robot.UploadNewParametersAsync(editedParameters);
                break;
        }
    }
}
