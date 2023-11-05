using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionProviderH 
{
    string GetProviderName();

    string GetProviderId();

    string GetProviderType();

    Hololens.ActionMetadataH GetActionMetadata(string action_id);


    bool IsRobot();

}
