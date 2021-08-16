/*
  This demo application accompanies Pluralsight course 'Build a Speech Recognition Solution with Microsoft Azure', 
  by Jurgen Kevelaers. See https://pluralsight.pxf.io/speech-recognition.

  MIT License

  Copyright (c) 2021 Jurgen Kevelaers

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace translation_m1
{
  class Program
  {
    // TODO: set your Speech Recognition settings here
    private const string speechSubscriptionKey = "TODO";
    private const string speechRegion = "eastus";

    private const ConsoleColor infoConsoleColor = ConsoleColor.Yellow;
    private const ConsoleColor successConsoleColor = ConsoleColor.Green;
    private const ConsoleColor errorConsoleColor = ConsoleColor.Red;

    private static readonly ConsoleColor defaultConsoleForegroundColor = Console.ForegroundColor;
    private static readonly object lockObject = new object();

    static async Task Main(string[] args)
    {
      WaitForNext();

      // sample audio from Jurgen (English)

      var speechConfigForTranslation = SpeechTranslationConfig.FromSubscription(
        speechSubscriptionKey, speechRegion);
      
      speechConfigForTranslation.SpeechRecognitionLanguage = "en-US";
      speechConfigForTranslation.AddTargetLanguage("da"); // Danish
      speechConfigForTranslation.AddTargetLanguage("it"); // Italian
      speechConfigForTranslation.AddTargetLanguage("tlh-Latn"); // Klingon?!

      var inputAudioFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\jurgen-en-US.wav"));
      await TranslateSpeechFromFile(speechConfigForTranslation, inputAudioFile);
      WaitForNext();

      // audio from microphone (Dutch)

      speechConfigForTranslation = SpeechTranslationConfig.FromSubscription(
        speechSubscriptionKey, speechRegion);
      
      speechConfigForTranslation.SpeechRecognitionLanguage = "nl-NL";
      speechConfigForTranslation.AddTargetLanguage("en"); // English
      speechConfigForTranslation.AddTargetLanguage("es"); // Spanish

      await TranslateSpeechFromMicrophone(speechConfigForTranslation);

      ConsoleWriteLine();
      ConsoleWriteLine("*** Press ENTER to quit ***");
      Console.ReadLine();
    }

    private static async Task TranslateSpeechFromFile(
      SpeechTranslationConfig speechConfigForTranslation, FileInfo inputAudioFile)
    {
      try
      {
        ConsoleWriteLine($"Translating speech from file {inputAudioFile.Name}...", infoConsoleColor);

        using var audioConfig = AudioConfig.FromWavFileInput(inputAudioFile.FullName);

        await TranslateSpeech(speechConfigForTranslation, audioConfig, 
          outputId: Path.GetFileNameWithoutExtension(inputAudioFile.Name));
      }
      catch (Exception ex)
      {
        ConsoleWriteLine($"* ERROR * {ex.Message}", errorConsoleColor);
      }
    }

    private static async Task TranslateSpeechFromMicrophone(SpeechTranslationConfig speechConfigForTranslation)
    {
      try
      {
        ConsoleWriteLine(
          $"Speak into the microphone in language {speechConfigForTranslation.SpeechRecognitionLanguage}...", 
          infoConsoleColor);

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();

        await TranslateSpeech(speechConfigForTranslation, audioConfig,
          outputId: "microphone");
      }
      catch (Exception ex)
      {
        ConsoleWriteLine($"* ERROR * {ex.Message}", errorConsoleColor);
      }
    }

    private static async Task TranslateSpeech(SpeechTranslationConfig speechConfigForTranslation, 
      AudioConfig audioConfigForTranslation, string outputId)
    {
      ConsoleWriteLine(
        $"Using language {speechConfigForTranslation.SpeechRecognitionLanguage}", 
        infoConsoleColor);

      using var translationRecognizer = new TranslationRecognizer(
        speechConfigForTranslation, audioConfigForTranslation);

      var translationResult = await translationRecognizer.RecognizeOnceAsync();

      if (translationResult.Reason != ResultReason.TranslatedSpeech)
      {
        ConsoleWriteLine($"Unexpected result: {translationResult.Reason}", errorConsoleColor);
        return;
      }

      ConsoleWriteLine($"Successfully processed text:", successConsoleColor);
      ConsoleWriteLine(translationResult.Text, successConsoleColor);

      var speechConfigForSynthesizer = SpeechConfig.FromSubscription(
        speechConfigForTranslation.SubscriptionKey, speechConfigForTranslation.Region);

      using var speechSynthesizerForVoice = new SpeechSynthesizer(speechConfigForSynthesizer);
      using var voices = await speechSynthesizerForVoice.GetVoicesAsync();

      foreach (var targetLanguage in translationResult.Translations.Keys)
      {
        // show translation result

        var translatedText = translationResult.Translations[targetLanguage];

        ConsoleWriteLine();
        ConsoleWriteLine($"[{targetLanguage}]", successConsoleColor);
        ConsoleWriteLine(translatedText, successConsoleColor);

        await SpeakIfPossible(
          speechConfigForSynthesizer, voices, targetLanguage, translatedText, outputId);
      }
    }

    private static async Task SpeakIfPossible(SpeechConfig speechConfig, SynthesisVoicesResult voices, 
      string targetLanguage, string textToSpeak, string outputId)
    {
      var voice = voices.Voices.FirstOrDefault(x => x.Locale.StartsWith(targetLanguage + "-"));

      if (voice == null)
      {
        ConsoleWriteLine($"Found no voice for language {targetLanguage}", infoConsoleColor);

        return;
      }

      speechConfig.SpeechSynthesisVoiceName = voice.Name;

      ConsoleWriteLine($"Speaking with voice {speechConfig.SpeechSynthesisVoiceName}...", infoConsoleColor);

      using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
      using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

      var result = await speechSynthesizer.SpeakTextAsync(textToSpeak);

      if (result.Reason == ResultReason.SynthesizingAudioCompleted)
      {
        ConsoleWriteLine($"Speaking was successful", successConsoleColor);

        await SaveToWaveFile(result, outputId + "-to-" + targetLanguage);
      }
      else
      {
        ConsoleWriteLine($"Speaking failed: {result.Reason}", errorConsoleColor);
      }
    }

    private static async Task SaveToWaveFile(SpeechSynthesisResult result, string outputId)
    {
      var outputDirectory = new DirectoryInfo(
        Path.Combine(Environment.CurrentDirectory, "output"));

      if (!outputDirectory.Exists)
      {
        outputDirectory.Create();
        outputDirectory.Refresh();
      }

      using var resultStream = AudioDataStream.FromResult(result);

      var outputFile = new FileInfo(Path.Combine(outputDirectory.FullName, outputId + ".wav"));

      ConsoleWriteLine($"Writing to output file {outputFile.Name}...", infoConsoleColor);

      await resultStream.SaveToWaveFileAsync(outputFile.FullName);

      ConsoleWriteLine($"Successfully written to output file", successConsoleColor);
    }

    private static void WaitForNext()
    {
      ConsoleWriteLine();
      ConsoleWriteLine("*** Press ENTER to move on ***");
      Console.ReadLine();
      Console.Clear();
    }

    private static void ConsoleWriteLine(string message = null, ConsoleColor? foregroundColor = null)
    {
      lock (lockObject)
      {
        Console.ForegroundColor = foregroundColor ?? defaultConsoleForegroundColor;
        Console.WriteLine(message);
        Console.ForegroundColor = defaultConsoleForegroundColor;
      }
    }
  }
}
