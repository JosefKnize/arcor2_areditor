using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnumParameterEditor : ParameterEditorBase
{
    public ComboBox ComboBox;
    public override void MoveValueFromEditorToParameter()
    {
        Parameter.Value = ComboBox.SelectedItem.ToString();
    }

    public override void MoveValueFromParameterToEditor()
    {
        ComboBox.SelectedItem = Parameter.Value;
        ComboBox.Items = ((ARServer.Models.StringEnumParameterExtra)Parameter.ParameterMetadata.ParameterExtra).AllowedValues.Cast<object>();
    }
}
