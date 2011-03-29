using UnityEngine;
using System.Collections;

/// <summary>
/// The TranslatorClient MonoBehaviour acts as an intermediate in the case that no
/// root / GameMaster script is being used that could provide the TextAsset with
/// the translated strings.
/// </summary>
public class TranslatorClient : MonoBehaviour {

	public TextAsset stringFile;

	private void Awake () {
		new Translator(stringFile);
	}
}

