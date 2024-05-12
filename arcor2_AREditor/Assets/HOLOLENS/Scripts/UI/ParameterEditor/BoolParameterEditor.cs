/*
 Author: Josef kníže
*/

using MixedReality.Toolkit;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolParameterEditor : ParameterEditorBase
{
    public StatefulInteractable Toggle;
    public override void MoveValueFromEditorToParameter()
    {
        Parameter.Value = JsonConvert.SerializeObject(Toggle.IsToggled.Active);
    }

    public override void MoveValueFromParameterToEditor()
    {
        Toggle.ForceSetToggled(JsonConvert.DeserializeObject<bool>(Parameter.Value));
    }
}
