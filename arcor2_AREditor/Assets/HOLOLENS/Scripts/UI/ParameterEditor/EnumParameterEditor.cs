/*
 Author: Josef kníže
*/
using Base;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnumParameterEditor : ParameterEditorBase
{
    public ComboBox ComboBox;
    public override void MoveValueFromEditorToParameter()
    {
        switch (Parameter.Type)
        {
            case ParameterMetadata.INT_ENUM:
                Parameter.Value = JsonConvert.SerializeObject((int)ComboBox.SelectedItem);
                break;
            case ParameterMetadata.STR_ENUM:
                Parameter.Value = JsonConvert.SerializeObject((string)ComboBox.SelectedItem);
                break;
        }
    }

    public override void MoveValueFromParameterToEditor()
    {
        switch (Parameter.Type)
        {
            case ParameterMetadata.INT_ENUM:
                ComboBox.SelectedItem = JsonConvert.DeserializeObject<int>(Parameter.Value);
                break;
            case ParameterMetadata.STR_ENUM:
                ComboBox.SelectedItem = JsonConvert.DeserializeObject<string>(Parameter.Value);
                break;
        }

        ComboBox.Items = ((ARServer.Models.StringEnumParameterExtra)Parameter.ParameterMetadata.ParameterExtra).AllowedValues.Cast<object>();
    }
}
