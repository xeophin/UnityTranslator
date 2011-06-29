/* XML String Reader 2.0
 * 
 * Kaspar Manz
 * xeophin.net/worlds
 * 
 * kaspar@xeophin.net
 */

using System;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;

/// <summary>
/// A file reader that extracts localizable data out of an XML file and
/// provides an interface for both audio player functions as well as
/// any other class that displays localizable GUI strings.
///
/// This class is now a Singleton and does not have to be attached to
/// an existing GameObject. Call it from a <c>GameMaster</c> or
/// <see cref="TranslatorClient"/> object.
/// </summary>
/// <remarks>
/// Usually, I use this class together with two other classes: The
/// <c>GameMaster</c> contains all user preferences (like the current
/// language) and provides a connection to this class. A second class
/// connects to the <c>GameMaster</c> and provides a simplified
/// method to return a string:
/// 
/// <code>
/// internal string s (string id) { ... }
/// </code>
/// 
/// This second class then acts as an intermediate, with all other 
/// classen in the game inheriting from it (instead of
/// <c>MonoBehaviour</c>). Like this, the <c>GameMaster</c> can
/// provide the correct language, and all GameObjects inheriting
/// from the second class get easy access to localised strings,
/// without having to fuss around with language codes.
/// 
/// This version has been heavily reworked from the first version. Things 
/// might fail, as I was trying to get all the old cruft out. Most likely
/// the whole thing could still be greatly improved. Any suggestions are
/// welcome and can be posted over at https://github.com/xeophin/UnityTranslator.
/// Feel free to fork the project and build upon it.
/// \warning This code will only work with an XML String file of version 1.1 and up, since
/// it collects the list of available languages from it.
/// \warning A later version of this class might take the GetText and GetAudio methods apart,
/// in order to keep performance.
/// </remarks>
/// \version 2.1
/// \author Kaspar Manz
public class Translator {

	#region Singleton stuff

	private static Translator instance;

	/// <summary>
	/// The actual instance of the Translator. Provides access to all functions.
	/// </summary>
	/// \warning When called directly without an existing instance, this script
	/// will assume that there is a <see cref="GameMaster"/> object available that
	/// provides the string file.
	public static Translator Instance {
		get {
			if (instance == null) {
				instance = new Translator (GameMaster.Instance.stringFile);
			}

			return instance;
		}
	}


	public Translator (TextAsset ta) {
		if (instance != null) {
			return;
		}

		stringFile = ta;
		instance = this;
		Awake();
	}

	#endregion


	private TextAsset stringFile;

	private string baselanguage;
	private XmlDocument root;
	
	private Dictionary<string,string> availableLanguages = new Dictionary<string, string>();
	public Dictionary<string, string> AvailableLanguages {
		get {
			return this.availableLanguages;
		}
	}

	/// <summary>
	/// Sets up various variables.
	/// </summary>
	void Awake () {

		//Check if we have a text asset available, otherwise die.
		if (stringFile == null) {
			throw new ArgumentNullException ();
		}


		// Get the file into the document.
		root = new XmlDocument ();
		root.LoadXml (stringFile.text);
		XPathNavigator nav;
		nav = root.CreateNavigator ();
		
		// Get all available languages.
		foreach (XPathNavigator a in nav.Select ("localizableStrings/meta/enabledLanguages/language")) {
			availableLanguages.Add (a.SelectSingleNode ("@id").Value, a.SelectSingleNode ("@fullName").Value);
			try {
				if (a.SelectSingleNode ("@default").ValueAsBoolean == true) {
					baselanguage = a.SelectSingleNode ("@id").Value;
				}
			} catch (NullReferenceException) {
				// skip – this just means that the default attribute has not been set. This is not an error.
			}
		}
		
	}

	/// <summary>
	/// Retrieves the string by its ID. Does not respect any language
	/// settings and just returns the first valid <c>text</c> or
	/// <c>audio</c> element it will find, no matter what language.
	/// </summary>
	/// <param name="id">
	/// The ID of the string as a <see cref="System.String"/>.
	/// </param>
	/// <returns>
	/// An array of <see cref="System.String[]"/> containing both the
	/// actual text as well as the path to the audio file. Returns "null"
	/// when it doesn't find anything. Check your log to see what has not
	/// been found.
	/// </returns>
	/// <exception cref="NullReferenceException"></exception>
	public string[] GetText (string id) {
		string[] result = new String[2];
		try {
				string n = root.SelectSingleNode ("//string[@id='" + id + "']/text").InnerText;
				result[0] = n;
				try {
					string a = root.SelectSingleNode ("//string[@id='" + id + "']/audio").InnerText;
					result[1] = a;
				} catch (NullReferenceException) {
					string a = null;
					result[1] = a;
				}
				return result;
			
		} catch (NullReferenceException ex) {
			Debug.Log ("[" + this.GetType ().ToString () + "] " + ex.ToString ());
			result[0] = null;
			return result;
		}
	}

	/// <summary>
	/// Retrieves the string by its ID and language.
	/// </summary>
	/// <param name="id">
	/// The ID of the text as a <see cref="System.String"/>.
	/// </param>
	/// <param name="language">
	/// The group name as a <see cref="System.String"/>.
	/// </param>
	/// <returns>
	/// An array of <see cref="System.String[]"/> containing both the
	/// actual text as well as the path to the audio file. Returns "null"
	/// when it doesn't find anything. Check your log to see what has not
	/// been found.
	/// </returns>
	/// \overload
	public string[] GetText (string id, string language) {
		XmlNode node;

		node = root.SelectSingleNode ("//string[@id='" + id + "']");
		return GetText (node, language);

	}

	/// \overload
	/// \deprecated This is just a placeholder method to remain compatible to earlier
	/// versions of this class. The <c>group</c> <see cref="String"/> will
	/// silently be dropped.
	[System.Obsolete]
	public string[] GetText (string id, string group, string language) {
		return GetText(id, language);
	}

    /// <summary>
    ///
    /// </summary>
    /// <param name="element">
    /// A <see cref="XPathNavigator"/>
    /// </param>
    /// <param name="lang">
    /// A <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// A <see cref="System.String[]"/>
    /// </returns>
    public string[] GetText (XPathNavigator element, string lang)
    {
        string[] result = new string[2];

        string text = element.SelectSingleNode ("text[@lang='" + lang + "']").Value;
        if (text != null) {
            result[0] = text;
        } else {
            text = element.SelectSingleNode ("text[@lang='" + baselanguage + "']").Value;

            if (text != null) {
                result[0] = text + " (missing)";
            } else {
                result[0] = element.GetAttribute ("id", "") + " (string missing)";
            }
        }

        return result;
    }

	/// <summary>
	/// This method takes a <see cref="XmlNode"/> and tries to extract the
	/// text with the correct language.
	/// </summary>
	/// <remarks>This method could actually help to reduce the cruft of this
	/// class, since it would be easy to get out the XML elements based on their
	/// IDs and let this method handle the actual extracting based on the
	/// language.
	/// 
	/// Also, this helps splitting up the XML files – now any other class can
	/// easily parse it's own files and then hand the <see cref="XmlNode"/>
	/// to this method.
	/// </remarks>
	/// <param name="lang">
	/// A <see cref="System.String"/> defining in which language the string
	/// is supposed to be returned.
	/// </param>
	/// <param name="element">
	/// A <see cref="XmlNode"/> that contains the extracted strings.
	/// </param>
	/// <returns>
	/// A <see cref="System.String[]"/>.
	/// </returns>
 [Obsolete]
	public string[] GetText (XmlNode element, string lang) {
		string[] result = new string[2];


		// Get the text from the fragment
		try {
			string n = element.SelectSingleNode (".//text[@lang='" + lang + "']").InnerText;
			result[0] = n;
		} catch (NullReferenceException) {
			try {
				string n = element.SelectSingleNode (".//text[@lang='" + baselanguage + "']").InnerText;
				result[0] = n;
			} catch (NullReferenceException) {
				try {
					string n = element.SelectSingleNode (".//text").InnerText;
					result[0] = n;
				} catch (NullReferenceException) {
					result[0] = null;
				}

			}
		}

		// Get the audio from the fragment
		try {
			string a = element.SelectSingleNode (".//audio[@lang='" + lang + "']").InnerText;
			result[1] = a;
		} catch (NullReferenceException) {

						try {
				string a = element.SelectSingleNode (".//audio[@lang='" + baselanguage + "']").InnerText;
				result[1] = a;
			} catch (NullReferenceException) {
				result[1] = null;
			}

		}
		
		return result;
	}
	
	/// <summary>
	/// Gets the audio string.
	/// </summary>
	/// <returns>
	/// A string (usually the path to the audio file, depends on the project)
	/// </returns>
	/// <param name='element'>
	/// The <see cref="XmlNode"/> that contains the audio.
	/// </param>
	/// <param name='lang'>
	/// The language the string has to be in.
	/// </param>
	/// \version stub
	public string GetAudio (XmlNode element, string lang) {
		throw new NotImplementedException();
	}

	/// <summary>
	/// Creates the XPath to a node.
	/// </summary>
	/// <returns>
	/// A string with the complete path.
	/// </returns>
	/// <param name='what'>
	/// What element shall be selected, i.e text or audio.
	/// </param>
	/// <param name='id'>
	/// The ID of the string.
	/// </param>
	/// <param name='group'>
	/// The group in which the string resides in.
	/// </param>
	/// <param name='lang'>
	/// The language in which the string should be accessed.
	/// </param>
	/// \deprecated Should not be used anymore ... wasn't very useful in the first place.
	[System.Obsolete]
	private string CreatePath (string what, string id, string @group, string lang) {
		string xPath = "localizableStrings";
		xPath += "/group[@id='" + @group + "']";
		xPath += "/string[@id='" + id + "']";
		xPath += "/" + what + "[@lang='" + lang + "']";
		return xPath;
	}

	/// \overload
	/// \deprecated
	[System.Obsolete]
	private string CreatePath (string what, string id, string @group) {
		string xPath = "localizableStrings";
		xPath += "/group[@id='" + @group + "']";
		xPath += "/string[@id='" + id + "']";
		xPath += "/" + what;
		return xPath;
	}

	/// \overload
	/// \deprecated
	[System.Obsolete]
	private string CreatePath (string what, string id) {
		string xPath = "localizableStrings";
		xPath += "/group/string[@id='" + id + "']";
		xPath += "/" + what;
		return xPath;
	}
	
}
