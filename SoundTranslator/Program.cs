using System;
using Vosk;
using DeepL;
using System.Reflection;
using NAudio.Wave;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    class SoundTranslator
    {


        // https://alphacephei.com/vosk/models 에서 본인이 사용할 모델 설치
        const string spkName = "vosk-model-spk-0.4";
        const string ModelName = "vosk-model-en-us-0.22";
        const string smallName = "vosk-model-small-en-us-0.15";
     
        // 종료 플래그
        static bool _isRunning = true;
        static Translator translator = new Translator("DeepL API Key");


        static int[] samperates = new int[] { 16000, 32000, 64000, 128000, 256000, 384000 };

        static void Main(string[] args)
        {
       
            Console.WriteLine();
            var outputFilePath = Path.Combine($"{Environment.CurrentDirectory}", "recognized_text.txt");
            var TransoutputFilePath = Path.Combine($"{Environment.CurrentDirectory}", "recognized_Trans_text.txt");

            using (var capture = new WasapiLoopbackCapture())
            {

                var rec = InitModels($"{Environment.CurrentDirectory}", samperates[5]);
                capture.WaveFormat = new WaveFormat(samperates[5], 16, 1);  //설정최대값,VoskRecognizer값과 매칭

                /*
                capture.DataAvailable += async (sender, e) =>
                {
                    if (_rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        string result = _rec.Result();
                        var resultObj = System.Text.Json.JsonSerializer.Deserialize<ResultObject>(result);
                        string recognizedText = resultObj.text;
                        File.AppendAllText(outputFilePath, recognizedText + Environment.NewLine);
                        Console.WriteLine($"인식된 텍스트: {recognizedText}");
                        // DeepL을 사용한 번역
                        string translatedText = await TranslateText(translator, recognizedText, "KO");

                        Console.WriteLine($"번역된 텍스트: {translatedText}");
                        File.AppendAllText(TransoutputFilePath, $"{recognizedText} -> {translatedText}{Environment.NewLine}");
                    }
                };
                */

                
                capture.DataAvailable += async (sender, e) =>
                {
                   
                    if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        string result = rec.Result();
                        var resultObj = System.Text.Json.JsonSerializer.Deserialize<ResultObject>(result);
                        string recognizedText = resultObj.text;

                        if (!string.IsNullOrWhiteSpace(recognizedText))
                        {
                            currentSentence.Append(recognizedText + " ");
                            lastSpeechTime = DateTime.Now;

                            // 영어 문장 구분을 위한 조건
                            if (currentSentence.Length > 150)
                            {
                                await ProcessAndTranslateSentence(outputFilePath, TransoutputFilePath, translator);
                            }
                        }
                        else if ((DateTime.Now - lastSpeechTime).TotalMilliseconds > SILENCE_MS) //사운드가 인식 안될 때
                        {
                            await ProcessAndTranslateSentence(outputFilePath, TransoutputFilePath, translator);
                        }
                    }
                };



                capture.StartRecording();
                Console.WriteLine("시스템 오디오 인식 시작. 종료하려면 'q'를 입력하세요...");

                // 별도의 스레드에서 사용자 입력 처리
                Thread inputThread = new Thread(() =>
                {
                    while (_isRunning)
                    {
                        if (Console.ReadKey(true).KeyChar == 'q')
                        {
                            _isRunning = false;
                            Console.WriteLine("프로그램을 종료합니다...");
                        }
                    }
                });
                inputThread.Start();

                // 메인 루프
                while (_isRunning)
                {
                    Thread.Sleep(1); // CPU 사용량 감소를 위한 짧은 대기
                }

                capture.StopRecording();
            }
        }

        static VoskRecognizer InitModels(string path,int sampleate)
        {
            string spkpath = Path.Combine(path, spkName);
            string modelpath = Path.Combine(path, smallName);
            var spkModel = new SpkModel(spkpath);
            var model = new Model(modelpath);
            var rec = new VoskRecognizer(model, (float)sampleate);
            rec.SetSpkModel(spkModel);

            return rec;
        }
        



        private static StringBuilder currentSentence = new StringBuilder();
        private static DateTime lastSpeechTime = DateTime.Now;
        private const int SILENCE_MS = 1500; // 1.5초의 침묵을 문장의 끝으로 간주
        private static async Task ProcessAndTranslateSentence(string outputFilePath, string TransoutputFilePath, Translator translator)
        {
            if (currentSentence.Length > 0)
            {
                string sentence = currentSentence.ToString().Trim();
               
                Console.WriteLine($"Recognized sentence: {sentence}");
                File.AppendAllText(outputFilePath, sentence + Environment.NewLine);


                string translatedText = await TranslateText(translator, sentence, "KO");
                Console.WriteLine($"Translated sentence: {translatedText}");
                File.AppendAllText(TransoutputFilePath, $"{sentence} -> {translatedText}{Environment.NewLine}");

                currentSentence.Clear();
            }
        }

        static async Task<string> TranslateText(Translator translator, string text, string targetLang)
        {
            try
            {
                var result = await translator.TranslateTextAsync(text, null, targetLang);
                return result.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"번역 오류: {ex.Message}");
                return string.Empty;
            }
        }


    }

    public class ResultObject
    {
        public string text
        {
            get; set;
        }
    }


}