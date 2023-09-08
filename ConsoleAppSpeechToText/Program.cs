// See https://aka.ms/new-console-template for more information
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;

Console.WriteLine("Hello, World!");

string speechKey = "<- Speech Services' key ->";
string speechRegion = "<- Speech Services' region ->";

var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
// https://learn.microsoft.com/ja-jp/azure/ai-services/speech-service/language-support?tabs=stt
speechConfig.SpeechRecognitionLanguage = "ja-jP";

try
{
    string deviceName = "plughw:CARD=ArrayUAC10,DEV=0";
    using AudioConfig audioConfig = AudioConfig.FromMicrophoneInput(deviceName);
    //    using AudioConfig audioConfig2 = AudioConfig.FromDefaultMicrophoneInput();

    using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
    {
        var result = await recognizer.RecognizeOnceAsync();
        Console.WriteLine($"Recognized: {result.Reason}");
        Console.WriteLine(result.Text);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
