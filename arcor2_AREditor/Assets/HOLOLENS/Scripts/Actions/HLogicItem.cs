using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;


public class HLogicItem 
{
      public IO.Swagger.Model.LogicItem Data;

    private Connection connection;

    private HInputOutput input;
    private HPuckOutput output;

    public HInputOutput Input {
        get => input;
        set => input = value;
    }
    public HPuckOutput Output {
        get => output;
        set => output = value;
    }

    public HLogicItem(IO.Swagger.Model.LogicItem logicItem) {
        Data = logicItem;
        UpdateConnection(logicItem);
    }

    public void Remove() {
        input.RemoveLogicItem(Data.Id);
        output.RemoveLogicItem(Data.Id);
        HConnectionManagerArcoro.Instance.DestroyConnection(connection);
        connection = null;
    }

    public void UpdateConnection(IO.Swagger.Model.LogicItem logicItem) {
        if (connection != null) {
            Remove();
        }
        input = HProjectManager.Instance.GetAction(logicItem.End).Input;
        output = HProjectManager.Instance.GetAction(logicItem.Start).Output;
        input.AddLogicItem(Data.Id);
        output.AddLogicItem(Data.Id);        
        connection = HConnectionManagerArcoro.Instance.CreateConnection(input.gameObject, output.gameObject);
        //output.Action.UpdateRotation(input.Action);
    }

    public Connection GetConnection() {
        return connection;
    }
}
