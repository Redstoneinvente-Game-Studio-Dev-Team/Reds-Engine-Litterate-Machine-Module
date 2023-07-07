using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New word type", menuName = "Words/WordTypes")]
public class WordsScriptableObjects : ScriptableObject
{
    public LitterateAI.WordTypesEnum wordTypesEnum;

    public LitterateAI.WordTypesEnum[] beforeAfterFormulaType;
}
