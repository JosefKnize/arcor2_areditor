using ARServer.Models;
using Base;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityEngine;

public class StringParameterEditor : ParameterEditorBase
{
    public MRTKUGUIInputField InputField;

    private void Start()
    {
        Debug.Log(InputField);
        InputField.onEndEdit.AddListener(OnEdit);
    }

    private void OnEdit(string newValue)
    {

        switch (Parameter.Type)
        {
            case ParameterMetadata.STR:
                break;
            case ParameterMetadata.INT:
                var intExtra = (IntParameterExtra)Parameter.ParameterMetadata.ParameterExtra;
                var parsedInt = int.Parse(newValue);
                
                if (parsedInt < intExtra.Minimum)
                {
                    InputField.text = intExtra.Minimum.ToString();
                }
                else if(parsedInt > intExtra.Maximum)
                {
                    InputField.text = intExtra.Maximum.ToString();
                }
                break;
            case ParameterMetadata.DOUBLE:
                var doubleExtra = (DoubleParameterExtra)Parameter.ParameterMetadata.ParameterExtra;
                var parsedDouble = double.Parse(newValue);
                if (parsedDouble < doubleExtra.Minimum)
                {
                    InputField.text = doubleExtra.Minimum.ToString();
                }
                else if (parsedDouble > doubleExtra.Maximum)
                {
                    InputField.text = doubleExtra.Maximum.ToString();
                }
                break;
        }
    }

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
