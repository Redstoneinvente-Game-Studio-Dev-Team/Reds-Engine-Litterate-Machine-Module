using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AssetImporters;
#endif
using System.IO;

[ScriptedImporter(1, "brain")]
public class BrainFileImporter : ScriptedImporter
{
#if UNITY_EDITOR
    public override void OnImportAsset(AssetImportContext ctx)
    {
        TextAsset subAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
        ctx.AddObjectToAsset("text", subAsset);
        ctx.SetMainObject(subAsset);
    }
#endif
}
