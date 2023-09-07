using Azure;
using Azure.AI.Translation.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfAppSpeechToText
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cbMicDevices.ItemsSource = microphoneDevices;
        }

        private void buttonSelect_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "wave file|*.wav";
            if (dialog.ShowDialog() == true)
            {
                tbFileName.Text = dialog.FileName;
            }
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            var player = new MediaPlayer();
            player.Open(new Uri(tbFileName.Text));
            player.Play();
        }

        private static readonly string speechKey = "<- your key ->";
        private static readonly string speechRegion = "<- region of your resource ->";

        private async void ButtonRecognize_Click(object sender, RoutedEventArgs e)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            // https://learn.microsoft.com/ja-jp/azure/ai-services/speech-service/language-support?tabs=stt
            speechConfig.SpeechRecognitionLanguage = "ja-jP";

            try
            {
                AudioConfig audioConfig = null;
                if (cbUseMic.IsChecked == true)
                {
                    string deviceName = cbMicDevices.SelectedItem.ToString();
                    // audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                    audioConfig=AudioConfig.FromMicrophoneInput(deviceName);
                }
                else
                {
                    audioConfig = AudioConfig.FromWavFileInput(tbFileName.Text);
                }
                using (audioConfig = AudioConfig.FromWavFileInput(tbFileName.Text))
                {
                    using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                    {
                        var result = await recognizer.RecognizeOnceAsync();
                        tbText.Text = result.Text;
                        
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Recognizer_SpeechStartDetected(object? sender, RecognitionEventArgs e)
        {
            Debug.WriteLine($"SpeechStartDetected - SessionId:{e.SessionId}, Offset:{e.Offset}");
        }

        private void Recognizer_SpeechEndDetected(object? sender, RecognitionEventArgs e)
        {
            Debug.WriteLine($"SpeechEndDetected - SessionId:{e.SessionId}, Offset:{e.Offset}");
        }

        private void Recognizer_SessionStopped(object? sender, SessionEventArgs e)
        {
            Debug.WriteLine($"SessionStopped - SessionId:{e.SessionId}");
            stopCRecognition.TrySetResult(0);
        }

        private void Recognizer_SessionStarted(object? sender, SessionEventArgs e)
        {
            Debug.WriteLine($"SessionStarted - SessionId:{e.SessionId}");
        }

        private async void Recognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            Debug.WriteLine($"Recognized - SessionId:{e.SessionId}, Offset:{e.Offset}, Result:{e.Result}");
            await ShowResult(e);
        }

        private void Recognizer_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            Debug.WriteLine($"Cancele - SessionId:{e.SessionId}, Offset:{e.Offset}");
            stopCRecognition.TrySetResult(0);
        }

        private async void Recognizer_Recognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            Debug.WriteLine($"Recognizing - SessionId:{e.SessionId}, Offset:{e.Offset}, Result:{e.Result}");

            await ShowResult(e);
        }

        private async Task ShowResult(SpeechRecognitionEventArgs e)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {

                var sb = new StringBuilder(tbText.Text);
            //    sb.AppendLine($"{e.Result.OffsetInTicks}:{e.Result.Reason.ToString()}:{e.Result.Text}");
                sb.AppendLine(String.Format("{0:D12}:{1}:{2}", e.Result.OffsetInTicks, e.Result.Reason, e.Result.Text));
                tbText.Text = sb.ToString();
            }));
        }

        ObservableCollection<String> microphoneDevices = new ObservableCollection<string>();
        private void cbUseMic_Checked(object sender, RoutedEventArgs e)
        {
            if (cbUseMic.IsChecked == true)
            {
                microphoneDevices.Clear();
                var mmDevices = new MMDeviceEnumerator();
                int numOfMMDevices = 0;
                foreach (var endpoint in mmDevices.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    microphoneDevices.Add(endpoint.DeviceFriendlyName);
                    numOfMMDevices++;
                }

                if (numOfMMDevices > 0)
                {
                    cbMicDevices.SelectedIndex = 0;
                }
            }
        }

        TaskCompletionSource<int> stopCRecognition;

        private async void ButtonContRecognize_Click(object sender, RoutedEventArgs e)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            // https://learn.microsoft.com/ja-jp/azure/ai-services/speech-service/language-support?tabs=stt
            speechConfig.SpeechRecognitionLanguage = "ja-jP";

            stopCRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                AudioConfig audioConfig = null;
                if (cbUseMic.IsChecked == true)
                {
                    audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                }
                else
                {
                    audioConfig = AudioConfig.FromWavFileInput(tbFileName.Text);
                }
                using (audioConfig = AudioConfig.FromWavFileInput(tbFileName.Text))
                {
                    using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                    {
                        recognizer.Recognizing += Recognizer_Recognizing;
                        recognizer.Canceled += Recognizer_Canceled;
                        recognizer.Recognized += Recognizer_Recognized;
                        recognizer.SessionStarted += Recognizer_SessionStarted;
                        recognizer.SessionStopped += Recognizer_SessionStopped;
                        recognizer.SpeechEndDetected += Recognizer_SpeechEndDetected;
                        recognizer.SpeechStartDetected += Recognizer_SpeechStartDetected;

                        tbText.Text = "";
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                        Task.WaitAny(new[] { stopCRecognition.Task });

                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                        MessageBox.Show("Recognition Completed");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private async void buttonTranslate_Click(object sender, RoutedEventArgs e)
        {
            var speechTranslationConfig = SpeechTranslationConfig.FromSubscription(speechKey, speechRegion);
            var fromLanguage = "ja-JP";
            var toLanguages = new List<string>() { "es" };
            speechTranslationConfig.SpeechRecognitionLanguage = fromLanguage;
            toLanguages.ForEach(speechTranslationConfig.AddTargetLanguage);

            using var audioConfig = AudioConfig.FromWavFileInput(tbFileName.Text);
            using var translationRecognizer = new TranslationRecognizer(speechTranslationConfig, audioConfig);
            var result = await translationRecognizer.RecognizeOnceAsync();
            if (result.Reason == ResultReason.TranslatedSpeech)
            {
                foreach (var element in result.Translations)
                {
                    tbTranslated.Text = element.Value;
                }
            }

        }

        private async void buttonSpeak_Click(object sender, RoutedEventArgs e)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechSynthesisVoiceName = "es-ES-ElviraNeural";

            using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();

            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
            {
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(tbTranslated.Text);
                
            }
        }

        Uri translatorEndpoint = new Uri("https://api.cognitive.microsofttranslator.com/");
        string translatorKey = "<- your key ->";
        string translatorRegion = "<- region of your resource ->";
        private async void buttonTranslateText_Click(object sender, RoutedEventArgs e)
        {
            AzureKeyCredential credential = new(translatorKey);
            TextTranslationClient client = new(credential, translatorRegion);

            try
            {
                string targetLanguage = "es";
                var response = await client.TranslateAsync(targetLanguage, tbText.Text).ConfigureAwait(false);
                var translations = response.Value;
                var translation = translations.FirstOrDefault();
                await Dispatcher.BeginInvoke(new Action(() =>
                {

                    var sb = new StringBuilder();
                    sb.AppendLine($"Detected:{translation.DetectedLanguage?.Language}, Score:{translation?.DetectedLanguage?.Score}");
                    sb.AppendLine($"To:{translation?.Translations?.FirstOrDefault().To}");
                    sb.AppendLine("Translation Result : ");
                    sb.AppendLine($"{translation?.Translations?.FirstOrDefault().Text}");
                    tbTranslated.Text = sb.ToString();
                }));
            }
            catch (RequestFailedException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
