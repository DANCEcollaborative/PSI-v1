namespace Smartlab_Demo_v2_1
{
    using CMU.Smartlab.Communication;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Kinect;
    using Apache.NMS;

    class Program
    {
        private const string AppName = "SmartLab Project - Demo v2.2 (for SigDial Demo)";

        private const string TopicToBazaar = "PSI_Bazaar_Text";
        private const string TopicToPython = "PSI_Python_Image";
        private const string TopicToNVBG = "PSI_NVBG_Location";
        private const string TopicToVHText = "PSI_VHT_Text";
        private const string TopicFromPython = "Python_PSI_Location";
        private const string TopicFromBazaar = "Bazaar_PSI_Text";
        private const string TopicFromPython_QueryKinect = "Python_PSI_QueryKinect";
        private const string TopicToPython_AnswerKinect = "PSI_Python_AnswerKinect";

        private const int SendingImageWidth = 360;
        private const int KinectImageWidth = 1920;

        private static string AzureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
        private static string AzureRegion = "eastus";

        private static CommunicationManager manager;

        public static readonly object SendToBazaarLock = new object();
        public static readonly object SendToPythonLock = new object();

        public static DateTime LastLocSendTime = new DateTime();

        public static SortedList<DateTime, CameraSpacePoint[]> KinectMappingBuffer;


        static void Main(string[] args)
        {
            SetConsole();
            if (Initialize())
            {
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("############################################################################");
                    Console.WriteLine("1) Multimodal streaming. Press any key to finish streaming.");
                    Console.WriteLine("2) Audio only. Press any key to finish streaming.");
                    Console.WriteLine("Q) Quit.");
                    ConsoleKey key = Console.ReadKey().Key;
                    Console.WriteLine();
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            RunDemo();
                            break;
                        case ConsoleKey.D2:
                            RunDemo(true);
                            break;
                        case ConsoleKey.Q:
                            exit = true;
                            break;
                    }
                }
            }
            else
            {

                Console.ReadLine();
            }
        }

        private static void SetConsole()
        {
            Console.Title = AppName;
            Console.Write(@"                                                                                                    
                                                                                                                   
                                                                                                   ,]`             
                                                                                                 ,@@@              
            ]@@\                                                           ,/\]                ,@/=@*              
         ,@@[@@/                                           ,@@           ,@@[@@/.                 =\               
      .//`   [      ,`                 ,]]]]`             .@@^           @@`            ]]]]]     @^               
    .@@@@@\]]`    .@@`  /]   ]]      ,@/,@@^    /@@@,@@@@@@@@@@[`        @@           /@`\@@     ,@@@@@@@^         
             \@@` =@^ ,@@@`//@@^    .@^ =@@^     ,@@`     /@*           ,@^          =@*.@@@*    =@   ,@/          
             ,@@* =@,@` =@@` =@^  ` @@ //\@@  ,\ @@^     ,@^            /@          =@^,@[@@^ ./`=@. /@`           
    ,@^    ,/@[   =@@. ,@@`  ,@^//.=@\@` ,@@@@` .@@     .@@^  /@    ,@\]@`     ,@@/ @@//  \@@@/  @@]@`             
    ,\/@[[`      =@@`  \/`    [[`  =@/    ,@`   ,[`      @@@@/      [[@@@@@@@@@[`  .@@`    \/*  /@/`               
                  ,`                                                                           ,`                  
                                                                                                                   
                                                                                                                   
                                                                                                                 
");
            Console.WriteLine("############################################################################");
        }

        static bool Initialize()
        {
            if (!GetSubscriptionKey())
            {
                Console.WriteLine("Missing Subscription Key!");
                return false;
            }
            manager = new CommunicationManager();
            manager.subscribe(TopicFromPython, ProcessLocation);
            manager.subscribe(TopicFromBazaar, ProcessText);
            manager.subscribe(TopicFromPython_QueryKinect, HandleKinectQuery);
            KinectMappingBuffer = new SortedList<DateTime, CameraSpacePoint[]>();
            return true;
        }

        private static void HandleKinectQuery(byte[] b)
        {
            string text = Encoding.ASCII.GetString(b);
            string[] infos = text.Split(';');
            int ticks = int.Parse(infos[0]);
            // x should from left to right and y should from up to down
            double x = double.Parse(infos[1]);
            double y = double.Parse(infos[2]);
            if (KinectMappingBuffer.Count == 0)
            {
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};null");
                return;
            }

            // Binary search for the nearest Mapper
            int left = 0;
            int right = KinectMappingBuffer.Count;
            while (right - left > 1)
            {
                int mid = (right + left) / 2;
                if (KinectMappingBuffer.ElementAt(mid).Key.Ticks <= ticks)
                {
                    left = mid;
                }
                else
                {
                    right = mid;
                }
            }

            long diff1 = Math.Abs(KinectMappingBuffer.ElementAt(left).Key.Ticks - ticks);
            long diff2;
            if (left + 1 < KinectMappingBuffer.Count)
            {
                diff2 = Math.Abs(KinectMappingBuffer.ElementAt(left).Key.Ticks - ticks);
            }
            else
            {
                diff2 = long.MaxValue;
            }

            CameraSpacePoint[] mapper;
            if (diff1 < diff2)
            {
                mapper = KinectMappingBuffer.ElementAt(left).Value;
            }
            else
            {
                mapper = KinectMappingBuffer.ElementAt(left + 1).Value;
            }

            // Convert to original image size:
            float scale = ((float)KinectImageWidth) / SendingImageWidth;
            int real_x = (int)(x * scale);
            int real_y = (int)(y * scale);
            CameraSpacePoint p = mapper[real_y * KinectImageWidth + real_x];
            manager.SendText(TopicToPython_AnswerKinect, $"{ticks};{p.X};{p.Y};{p.Z}");
        }

        private static void ProcessLocation(byte[] b)
        {
            DateTime time = DateTime.Now;
            /*
            if (time.Subtract(LastLocSendTime).TotalSeconds < 0.5)
            {
                return;
            }
            */
            LastLocSendTime = time;
            string text = Encoding.ASCII.GetString(b);
            string[] infos = text.Split(';');
            int num = int.Parse(infos[0]);
            if (num >= 1)
            {
                Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:someone;%;location:{infos[1]}");
                manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:someone;%;location:{infos[1]}");
            }
        }

        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($"Send location message to VHT: multimodal:false;%;identity:someone;%;text:{s}");
                manager.SendText(TopicToVHText, $"multimodal:false;%;identity:someone;%;text:{s}");
            }
        }

        public static void RunDemo(bool AudioOnly = false)
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // var store = Store.Open(pipeline, Program.LogName, Program.LogPath);
                // Send video part to Python

                // var video = store.OpenStream<Shared<EncodedImage>>("Image");
                if (!AudioOnly)
                {
                    var kinectSensorConfig = new KinectSensorConfiguration
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputRGBD = true,
                        OutputColorToCameraMapping = true,
                        OutputBodies = false,
                    };
                    var kinectSensor = new Microsoft.Psi.Kinect.KinectSensor(pipeline, kinectSensorConfig);
                    // MediaCapture webcam = new MediaCapture(pipeline, 1280, 720, 30);
                    // var kinectRGBD = kinectSensor.RGBDImage;
                    var kinectColor = kinectSensor.ColorImage;
                    var kinectMapping = kinectSensor.ColorToCameraMapper;
                    kinectMapping.Do(addNewMapper);

                    // var kinectDepth = kinectSensor.DepthImage;
                    // var decoded = video.Out.Decode().Out;
                    ImageSendHelper helper = new ImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendingImageWidth, Program.SendToPythonLock);
                    kinectColor.Do(helper.SendImage);
                    // var encoded = webcam.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                }

                // Send audio part to Bazaar

                // var audio = store.OpenStream<AudioBuffer>("Audio");
                var audioConfig = new AudioCaptureConfiguration()
                {
                    OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                    DropOutOfOrderPackets = true
                };
                IProducer<AudioBuffer> audio = new AudioCapture(pipeline, audioConfig);

                var vad = new SystemVoiceActivityDetector(pipeline);
                audio.PipeTo(vad);

                var recognizer = new AzureSpeechRecognizer(pipeline, new AzureSpeechRecognizerConfiguration()
                {
                    SubscriptionKey = Program.AzureSubscriptionKey,
                    Region = Program.AzureRegion
                });
                var annotatedAudio = audio.Join(vad);
                annotatedAudio.PipeTo(recognizer);

                var finalResults = recognizer.Out.Where(result => result.IsFinal);
                finalResults.Do(SendDialogToBazaar);

                // Todo: Add some data storage here
                // var dataStore = Store.Create(pipeline, Program.AppName, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

                pipeline.RunAsync();
                if (AudioOnly)
                {
                    Console.WriteLine("Running Smart Lab Project Demo v2.2 - Audio Only.");
                }
                else
                {
                    Console.WriteLine("Running Smart Lab Project Demo v2.2");
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        private static void addNewMapper(CameraSpacePoint[] mapper, Envelope envelope)
        {
            var time = envelope.OriginatingTime;
            KinectMappingBuffer.Add(time, mapper);
            while (DateTime.Now.Subtract(KinectMappingBuffer.First().Key).TotalSeconds > 10)
            {
                KinectMappingBuffer.RemoveAt(0);
            }
        }

        private static void SendDialogToBazaar(IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            Console.WriteLine($"Send text message to Bazaar: {result.Text}");
            manager.SendText(TopicToBazaar, result.Text);
            manager.SendText(TopicToVHText, $"multimodal:false;%;identity:someone;%;text:{result.Text}");
        }

        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private static bool GetSubscriptionKey()
        {
            Console.WriteLine("A cognitive services Azure Speech subscription key is required to use this. For more info, see 'https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account'");
            Console.Write("Enter subscription key");
            Console.Write(string.IsNullOrWhiteSpace(Program.AzureSubscriptionKey) ? ": " : string.Format(" (current = {0}): ", Program.AzureSubscriptionKey));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.AzureSubscriptionKey = response;
            }

            Console.Write("Enter region");
            Console.Write(string.IsNullOrWhiteSpace(Program.AzureRegion) ? ": " : string.Format(" (current = {0}): ", Program.AzureRegion));

            // Read a new key or hit enter to keep using the current one (if any)
            response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.AzureRegion = response;
            }

            return !string.IsNullOrWhiteSpace(Program.AzureSubscriptionKey) && !string.IsNullOrWhiteSpace(Program.AzureRegion);
        }
    }
}
