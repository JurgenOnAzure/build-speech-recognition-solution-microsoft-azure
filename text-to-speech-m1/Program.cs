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
using System;
using System.IO;
using System.Threading.Tasks;

namespace text_to_speech_m1
{
  class Program
  {
    // TODO: set your Speech Recognition settings here
    private const string speechSubscriptionKey = "TODO";
    private const string speechRegion = "TODO";

    private const ConsoleColor infoConsoleColor = ConsoleColor.Yellow;
    private const ConsoleColor successConsoleColor = ConsoleColor.Green;
    private const ConsoleColor errorConsoleColor = ConsoleColor.Red;

    private static readonly ConsoleColor defaultConsoleForegroundColor = Console.ForegroundColor;
    private static readonly object lockObject = new object();

    private static readonly Random random = new Random();

    static async Task Main(string[] args)
    {
      WaitForNext();

      var speechConfig = SpeechConfig.FromSubscription(speechSubscriptionKey, speechRegion);

      // sample text from Microsoft (English)

      speechConfig.SpeechSynthesisLanguage = "en-US";
      var inputFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\msft-en-US.txt"));
      await RecognizeSpeechFromFile(speechConfig, inputFile);
      WaitForNext();

      // sample text from Microsoft (French)

      speechConfig.SpeechSynthesisLanguage = "fr-FR";
      inputFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\msft-fr-FR.txt"));
      await RecognizeSpeechFromFile(speechConfig, inputFile);
      WaitForNext();

      // sample SSML(English)

      inputFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\ssml-en-US.xml"));
      await RecognizeSpeechFromFile(speechConfig, inputFile);

      ConsoleWriteLine();
      ConsoleWriteLine("*** Press ENTER to quit ***");
      Console.ReadLine();
    }

    private static async Task RecognizeSpeechFromFile(SpeechConfig speechConfig, FileInfo inputFile)
    {
      try
      {
        ConsoleWriteLine($"Recognizing speech from file {inputFile.Name}...", infoConsoleColor);

        var inputIsSSML = inputFile.Name.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase);

        if (!inputIsSSML)
        {
          await SetRandomVoice(speechConfig);
        }

        using var speechSynthesizer = new SpeechSynthesizer(speechConfig);

        var textToSpeak = File.ReadAllText(inputFile.FullName);

        ConsoleWriteLine("Input:", infoConsoleColor);
        ConsoleWriteLine(textToSpeak, infoConsoleColor);
        ConsoleWriteLine();
        ConsoleWriteLine($"Synthesizing to active PC speaker...", infoConsoleColor);

        using var result = inputIsSSML
          ? await speechSynthesizer.SpeakSsmlAsync(textToSpeak)
          : await speechSynthesizer.SpeakTextAsync(textToSpeak);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
          ConsoleWriteLine($"Synthesis was successful", successConsoleColor);

          await SaveToWaveFile(result, outputId: Path.GetFileNameWithoutExtension(inputFile.Name));
        }
        else
        {
          ConsoleWriteLine($"Unexpected result: {result.Reason}", errorConsoleColor);
        }
      }
      catch (Exception ex)
      {
        ConsoleWriteLine($"* ERROR * {ex.Message}", errorConsoleColor);
      }
    }

    private static async Task SetRandomVoice(SpeechConfig speechConfig)
    {
      ConsoleWriteLine($"Using language {speechConfig.SpeechSynthesisLanguage}", infoConsoleColor);
      ConsoleWriteLine($"Getting voices...", infoConsoleColor);

      using var speechSynthesizerForVoice = new SpeechSynthesizer(speechConfig);
      using var voices = await speechSynthesizerForVoice.GetVoicesAsync(speechConfig.SpeechSynthesisLanguage);

      var voiceIndex = random.Next(0, voices.Voices.Count);
      var voice = voices.Voices[voiceIndex];

      speechConfig.SpeechSynthesisVoiceName = voice.Name;

      ConsoleWriteLine($"Using voice {speechConfig.SpeechSynthesisVoiceName}", infoConsoleColor);
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
