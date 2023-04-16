using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(FogMaskController))]
[CanEditMultipleObjects]
public class MinMapInspector : Editor
{

    FogMaskController myScript { get { return (FogMaskController)target; } }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        serializedObject.Update();

        if (GUILayout.Button("Save Fog"))
        {
            myScript.SaveFog(myScript.save_path);
        }

        if (GUILayout.Button("Load Fog"))
        {
            myScript.LoadFog(myScript.save_path);
        }

        

        serializedObject.ApplyModifiedProperties();

    }



}
