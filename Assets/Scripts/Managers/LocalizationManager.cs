using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationManager : MonoBehaviour
{

	private const string DEFAULT_LANGUAGE_CODE = "en_US";

	// 16.5 KB per language serialization(Unicode chars takes 2 bytes per symbol, so the Code will have 256 * 2 + the Sentence 8192 * 2 = 16.5KB in total allocated in the stack)
	private const int MAX_CODE_LENGTH = 256;
	private const int MAX_SENTENCE_LENGTH = 8192;
	private const char CODE_SENTENCE_SPLITTER = '|';
	private const char CONTROL_START_SEQUENCE_CHAR = '|';
	private const int CONTROL_SEQUENCE_LENGTH = 2;
	private const int NEW_LINE_SEQUENCE = CONTROL_START_SEQUENCE_CHAR | ('n' << 16);
	private const int SPLITTER_SEQUENCE = CONTROL_START_SEQUENCE_CHAR | (CODE_SENTENCE_SPLITTER << 16);
	private static readonly char Line_Splitter = Environment.NewLine[0];
	private static readonly int Line_Splitter_Length = Environment.NewLine.Length;

	public static LocalizationManager Instance;

	/// <summary>
	/// Deserialize all languages and keep in memory.
	/// </summary>
	[SerializeField] private bool deserializeAllLanguages = false;
	[SerializeField] private TextAsset[] languagesData;
	[SerializeField] private string[] languagesCodes;
	[SerializeField] private Text[] localizationTextElements;
	[SerializeField] private string[] localizationKeys;

	private Dictionary<string, Dictionary<string, string>> languages;
	private Dictionary<string, string> defaultLanguage;
	private Dictionary<string, string> currentLanguage;
	private string currentLanguageCode;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		if (deserializeAllLanguages) {
			languages = new Dictionary<string, Dictionary<string, string>>();
			for (int i = 0; i < languagesCodes.Length; i++) {
				Dictionary<string, string> tempDictionary;
				if (TryDeserializeLanguage(languagesCodes[i], out tempDictionary)) {
					languages.Add(languagesCodes[i], tempDictionary);
				}
			}
			defaultLanguage = languages[DEFAULT_LANGUAGE_CODE];
			currentLanguage = defaultLanguage;
		} else {
			TryDeserializeLanguage(DEFAULT_LANGUAGE_CODE, out defaultLanguage);
			currentLanguage = defaultLanguage;
		}

		currentLanguageCode = DEFAULT_LANGUAGE_CODE;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void ChangeLanguage() => LocalizeAll(languagesCodes[(Array.IndexOf(languagesCodes, currentLanguageCode) + 1) % languagesCodes.Length]);

	/// <summary>
	/// Returns localized sentence.
	/// </summary>
	/// <param name="languageCode">What language to look for. Example: "en_US".</param>
	/// <param name="sentenceKey">What sentence to look for. Example: "game_name".</param>
	/// <exception cref="ArgumentException"></exception>
	public string Localize(string sentenceKey) {
		string tempSentence;
		if (currentLanguage.TryGetValue(sentenceKey, out tempSentence) || defaultLanguage.TryGetValue(sentenceKey, out tempSentence))
			return tempSentence;

		throw new ArgumentException("Sentence not found.", "sentenceKey");
	}

	/// <summary>
	/// Returns localized sentence.
	/// </summary>
	/// <param name="languageCode">What language to look for. Example: "en_US".</param>
	/// <param name="sentenceKey">What sentence to look for. Example: "game_name".</param>
	/// <exception cref="ArgumentException"></exception>
	public string Localize(string languageCode, string sentenceKey) {
		string tempSentence;
		if (TrySetCurrentLanguage(languageCode)) {
			if (currentLanguage.TryGetValue(sentenceKey, out tempSentence) || defaultLanguage.TryGetValue(sentenceKey, out tempSentence))
				return tempSentence;

			throw new ArgumentException("Sentence not found.", "sentenceKey");
		} else {
			if (defaultLanguage.TryGetValue(sentenceKey, out tempSentence))
				return tempSentence;

			throw new ArgumentException("Sentence not found.", "sentenceKey");
		}
	}

	/// <summary>
	/// Tries to localize all known localization elements.
	/// </summary>
	/// <param name="languageCode">What language to look for. Example: "en_US".</param>
	/// <exception cref="ArgumentException"></exception>
	public void LocalizeAll(string languageCode) {
		string tempSentence;
		if (TrySetCurrentLanguage(languageCode)) {
			for (int i = 0; i < localizationKeys.Length; i++) {
				if (currentLanguage.TryGetValue(localizationKeys[i], out tempSentence) || defaultLanguage.TryGetValue(localizationKeys[i], out tempSentence)) {
					localizationTextElements[i].text = tempSentence;
					continue;
				}

				throw new Exception("Sentence not found at position " + i + '.');
			}
		} else {
			for (int i = 0; i < localizationKeys.Length; i++) {
				if (defaultLanguage.TryGetValue(localizationKeys[i], out tempSentence)) {
					localizationTextElements[i].text = tempSentence;
					continue;
				}

				throw new Exception("Sentence not found at position " + i + '.');
			}
		}
	}

	private bool TrySetCurrentLanguage(string languageCode) {
		if (!deserializeAllLanguages) {
			if (currentLanguageCode == languageCode || TryDeserializeLanguage(languageCode, out currentLanguage)) {
				currentLanguageCode = languageCode;
			} else
				return false;
				//throw new Exception("Can't deserialize language by given language code.");
		} else {
			if (currentLanguageCode == languageCode || languages.TryGetValue(languageCode, out currentLanguage)) {
				currentLanguageCode = languageCode;
			} else
				return false;
				//throw new ArgumentException("Language not found.", "languageCode");
		}

		return true;
	}

	/// <summary>
	/// Tries to serialize a given language and create a new Dictionary.
	/// </summary>
	/// <param name="languageCode">What language to look for. Example: "en_US".</param>
	/// <exception cref="ArgumentException"></exception>
	private bool TryDeserializeLanguage(string languageCode, out Dictionary<string, string> language) {
		language = null;
		int index = Array.IndexOf(languagesCodes, languageCode);
		if (index != -1) {
			language = new Dictionary<string, string>();
			unsafe {
				char* codeBuffer = stackalloc char[MAX_CODE_LENGTH];
				char* sentenceBuffer = stackalloc char[MAX_SENTENCE_LENGTH];
				fixed (char* ptr = languagesData[index].text) {
					int i = 0, len = languagesData[index].text.Length;
					while (i < len) {
						int codeBufferPos = 0, sentenceBufferPos = 0;
						while (i < len && ptr[i] != CODE_SENTENCE_SPLITTER) { // Collect the code
							codeBuffer[codeBufferPos++] = ptr[i++];
						}
						i++; // Skip CODE_SENTENCE_SPLITTER
						while (i < len && ptr[i] != Line_Splitter) { // Collect the sentence
							if (ptr[i] == CONTROL_START_SEQUENCE_CHAR && len - i >= CONTROL_SEQUENCE_LENGTH) {
								DecryptSequence(ptr, sentenceBuffer, ref i, ref sentenceBufferPos);
							} else {
								sentenceBuffer[sentenceBufferPos++] = ptr[i++];
							}
						}
						i += Line_Splitter_Length; // Skip Environment.NewLine

						// Turn collected buffers into strings and add to the Language Dictionary
						language.Add(Marshal.PtrToStringUni((IntPtr)codeBuffer, codeBufferPos), Marshal.PtrToStringUni((IntPtr)sentenceBuffer, sentenceBufferPos));
					}
				}
			}

			return true;
		} else
			return false;
			//throw new ArgumentException("Language not found.", "languageCode");
	}

	/// <summary>
	/// Tries to decrypt a control sequence(sequence entry char + control char).
	/// </summary>
	/// <param name="languageData">Pointer to a language data file(string).</param>
	/// <param name="sentenceBuffer">Pointer to the sentence buffer.</param>
	/// <param name="ptrPos">Current language data pointer position.</param>
	/// <param name="sentencePtrPos">Current sentence buffer pointer position.</param>
	private unsafe void DecryptSequence(char* languageData, char* sentenceBuffer, ref int ptrPos, ref int sentencePtrPos) {
		int tempSequence = languageData[ptrPos] | (languageData[ptrPos + 1] << 16);
		int tempLen;
		string tempString;
		switch (tempSequence) {
			case NEW_LINE_SEQUENCE:
				tempString = Environment.NewLine;
				tempLen = tempString.Length;
				break;
			case SPLITTER_SEQUENCE:
				tempString = CODE_SENTENCE_SPLITTER.ToString();
				tempLen = 1;
				break;
			default:
				tempString = string.Empty;
				tempLen = 0;
				break;
		}

		for (int i = 0; i < tempLen; i++) {
			sentenceBuffer[sentencePtrPos++] = tempString[i];
		}

		ptrPos += tempLen;
	}

}
