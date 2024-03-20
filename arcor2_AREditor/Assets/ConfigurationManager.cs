using Base;
using MixedReality.Toolkit.UX;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConfigurationManager : MonoBehaviour
{
    public GameObject ConfigurationWindow;
    public GameObject ConfigurationWindowGeneratedContentCollection;

    public GameObject InputFieldPrefab;

    public void ShowConfigurationWindow(HInteractiveObject interactiveObject)
    {
        if (interactiveObject is HAction action) 
        {
            GenerateWindowContent(action.Parameters);
            
        }
    }

    public void GenerateWindowContent(Dictionary<string, Base.Parameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            switch(parameter.Value.ParameterMetadata.Type)
            {
                case "string":
                    
                    var gameObject = Instantiate(InputFieldPrefab, ConfigurationWindow.transform);
                    var b = parameter.Value.Name;
                    var a = gameObject.GetComponent<MRTKUGUIInputField>();
                    
                    break;
                case "integer":
                    break;
                case "double":
                    break;
                case "boolean":
                    break;
                case "pose":
                    break;
                case "joints":
                    break;
                case "string_enum":
                    //value = ((ARServer.Models.StringEnumParameterExtra)parameter.Value.ParameterMetadata.ParameterExtra).AllowedValues.First();
                    break;
                case "integer_enum":
                    //value = ((ARServer.Models.IntegerEnumParameterExtra)parameter.Value.ParameterMetadata.ParameterExtra).AllowedValues.First().ToString();
                    break;
            }
        }
    }
  
}
