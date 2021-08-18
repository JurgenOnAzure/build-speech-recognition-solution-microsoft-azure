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

namespace speech_to_text_m1
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

    static async Task Main(string[] args)
    {
      WaitForNext();

      var speechConfig = SpeechConfig.FromSubscription(speechSubscriptionKey, speechRegion);
      speechConfig.SpeechRecognitionLanguage = "en-US";

      // sample audio from Microsoft (English)

      var inputAudioFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\msft-en-US.wav"));
      await RecognizeSpeechFromFile(speechConfig, inputAudioFile);
      WaitForNext();
      
      // sample audio from Microsoft (French)

      inputAudioFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, @"input\msft-fr-FR.wav"));
      await RecognizeSpeechFromFile(speechConfig, inputAudioFile);
      WaitForNext();

      speechConfig.SpeechRecognitionLanguage = "fr-FR";
      await RecognizeSpeechFromFile(speechConfig, inputAudioFile);
      WaitForNext();

      // invalid credentials
      speechConfig = SpeechConfig.FromSubscription("wrong key", speechRegion);
      await RecognizeSpeechFromFile(speechConfig, inputAudioFile);

      ConsoleWriteLine();
      ConsoleWriteLine("*** Press ENTER to quit ***");
      Console.ReadLine();
    }

    private static async Task RecognizeSpeechFromFile(SpeechConfig speechConfig, FileInfo inputAudioFile)
    {
      try
      {
        ConsoleWriteLine($"Recognizing speech from file {inputAudioFile.Name}...", infoConsoleColor);
        ConsoleWriteLine($"Using language {speechConfig.SpeechRecognitionLanguage}", infoConsoleColor);

        using var fileInputAudioConfig = AudioConfig.FromWavFileInput(inputAudioFile.FullName);
        using var speechRecognizer = new SpeechRecognizer(speechConfig, fileInputAudioConfig);

        var result = await speechRecognizer.RecognizeOnceAsync();
        
        ConsoleColor resultConsoleColor;
        var reasonDetail = string.Empty;

        switch (result.Reason)
        {
          case ResultReason.RecognizedSpeech:
            reasonDetail = "Recognition was successful";
            resultConsoleColor = successConsoleColor;

            break;

          case ResultReason.NoMatch:
            reasonDetail = "Failed to recognize speech";
            resultConsoleColor = errorConsoleColor;
            
            break;

          case ResultReason.Canceled:
            var cancellationDetails = CancellationDetails.FromResult(result);

            reasonDetail = $"Canceled: {cancellationDetails.Reason}";

            if (cancellationDetails.Reason == CancellationReason.Error)
            {
              reasonDetail += Environment.NewLine;
              reasonDetail += $"Error: [{cancellationDetails.ErrorCode}] {cancellationDetails.ErrorDetails}";

              resultConsoleColor = errorConsoleColor;
            }
            else
            {
              resultConsoleColor = infoConsoleColor;
            }
            
            break;

          default:
            reasonDetail = result.Reason.ToString();
            resultConsoleColor = infoConsoleColor;
            
            break;
        }

        ConsoleWriteLine(reasonDetail, resultConsoleColor);
        ConsoleWriteLine("Results:", resultConsoleColor);
        ConsoleWriteLine(result.Text, resultConsoleColor);
      }
      catch (Exception ex)
      {
        ConsoleWriteLine($"* ERROR * {ex.Message}", errorConsoleColor);
      }
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
