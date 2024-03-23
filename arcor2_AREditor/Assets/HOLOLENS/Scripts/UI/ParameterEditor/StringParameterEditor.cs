using Base;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityEngine;

public class StringParameterEditor : ParameterEditorBase
{
    public MRTKUGUIInputField InputField;

    public override void MoveValueFromEditorToParameter()
    {
        switch(Parameter.Type)
        {
            case ParameterMetadata.STR:
                Parameter.Value = JsonConvert.SerializeObject(InputField.text);
                break;
            case ParameterMetadata.INT:
                Parameter.Value = JsonConvert.SerializeObject(int.Parse(InputField.text));
                break;
            case ParameterMetadata.DOUBLE:
                Parameter.Value = JsonConvert.SerializeObject(double.Parse(InputField.text));
                break;
        }
    }

    public override void MoveValueFromParameterToEditor()
    {
        switch (Parameter.Type)
        {
            case ParameterMetadata.STR:
                InputField.text = JsonConvert.DeserializeObject<string>(Parameter.Value);
                break;
            case ParameterMetadata.INT:
                InputField.text = JsonConvert.DeserializeObject<int>(Parameter.Value).ToString();
                break;
            case ParameterMetadata.DOUBLE:
                InputField.text = JsonConvert.DeserializeObject<double>(Parameter.Value).ToString();
                break;
        }
        
    }
}
