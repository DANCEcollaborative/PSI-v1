namespace Smartlab_Demo_v2_1
{
    using CMU.Smartlab.Communication;
    using CMU.Smartlab.Identity;
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
        private const int KinectImageHeight = 1080;

        private static Dictionary<string, string[]> idInfo = new Dictionary<string, string[]>();
        private static Dictionary<string, string[]> idTemp = new Dictionary<string, string[]>();

        private static string AzureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
        private static string AzureRegion = "eastus";

        private static CommunicationManager manager;
        private static IdentityInfoProcess idProcess;

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
            //Console.WriteLine($"Queried for the depth information. Query: {text}");
            string[] infos = text.Split(';');
            long ticks = long.Parse(infos[0]);
            // x should from left to right and y should from up to down
            double x = double.Parse(infos[1]);
            double y = double.Parse(infos[2]);
            //Console.WriteLine($"Parsed: {ticks}, {x}, {y}");
            if (KinectMappingBuffer.Count == 0)
            {
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};null");
               // Console.WriteLine($"Answering Query: {ticks};null");
                return;
            }

            // Binary search for the nearest Mapper
            int left = 0;
            int right = KinectMappingBuffer.Count;
            while (right - left > 1)
            {
                // Console.WriteLine($"left: {left}, right: {right}");
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
            int real_x = (int)(x * KinectImageWidth);
            int real_y = (int)(y * KinectImageHeight);
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = 0;
            result.Y = 0;
            result.Z = 0;
            int valid = 0;
            for (int i = real_x - 5; i < real_x + 6; ++i)
            {
                for (int j = real_y - 5; j < real_y + 6; ++j)
                {
                    if ((i < 0) || (j < 0) || (i > KinectImageWidth) || (j > KinectImageHeight))
                    {
                        continue;
                    }
                    CameraSpacePoint p = mapper[j * KinectImageWidth + i];
                   // Console.WriteLine($"({p.X}, {p.Y}, {p.Z})");
                    if (p.X + p.Y + p.Z < -1000000 || p.X + p.Y + p.Z > 1000000)
                    {
                        continue;
                    }
                    valid++;
                    result.X += p.X;
                    result.Y += p.Y;
                    result.Z += p.Z;
                }
            }
            if (valid > 0)
            {
                // CameraSpacePoint p = mapper[real_y * KinectImageWidth + real_x];
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};{result.X / valid};{result.Y / valid};{result.Z / valid}");
                //Console.WriteLine($"Answering Query: {ticks};{result.X / valid};{result.Y / valid};{result.Z / valid}");
            }
            else
            {
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};null");
               // Console.WriteLine($"Answering Query: {ticks};null");
            }
        }

        private static void ProcessLocation(byte[] b)
        {
            /*
            DateTime time = DateTime.Now;
            if (time.Subtract(LastLocSendTime).TotalSeconds < 0.5)
            {
                return;
            }
            LastLocSendTime = time;
            */
            string text = Encoding.ASCII.GetString(b);
            string[] infos = text.Split(';');
            int num = int.Parse(infos[0]);
            if (num >= 1)
            {
                for (int i = 1; i < infos.Length; ++i)
                {
                    ProcessID(text);
                    Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:someone;%;location:{infos[1]}");
                    manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:someone;%;location:{infos[1]}");
/*                    string info = infos[i];
                    string id = info.Split('&')[0];
                    string pos = info.Split('&')[1];
                    Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:{id};%;location:{pos}");
                    manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:{id};%;location:{pos}");*/
                }
            }
        }

        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($"Send location message to VHT: multimodal:false;%;identity:someone;%;text:{s}");
                manager.SendText(TopicToVHText, s);
            }
        }
        private static void ProcessID(string s)
        {
            idTemp = idProcess.MsgParse(s);
            idProcess.IdCompare(idInfo, idTemp);
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
                    kinectMapping.Do(AddNewMapper);

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

        private static void AddNewMapper(CameraSpacePoint[] mapper, Envelope envelope)
        {
            var time = envelope.OriginatingTime;
            KinectMappingBuffer.Add(time, mapper);
            while (KinectMappingBuffer.Last().Key.Subtract(KinectMappingBuffer.First().Key).TotalSeconds > 10)
            {
                var rem_time = KinectMappingBuffer.First().Key;
                KinectMappingBuffer.RemoveAt(0);
            }
        }

        private static void SendDialogToBazaar(IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            String speech = result.Text;
            if (speech != "")
            {
                String name = getRandomName();
                String location = getRandomLocation();
                String messageToBazaar = "multimodal:true;%;speech:" + result.Text + ";%;identity:" + name + ";%;location:" + location;
                Console.WriteLine($"Send text message to Bazaar: {messageToBazaar}");
                manager.SendText(TopicToBazaar, messageToBazaar);
            }
        }

        private static String getRandomName()
        {
            Random randomFunc = new Random();
            int randomNum = randomFunc.Next(0, 3);
            if (randomNum == 1)
                return "Haogang";
            else
                return "Yansen";
        }

        private static String getRandomLocation()
        {
            Random randomFunc = new Random();
            int randomNum = randomFunc.Next(0, 4);
            switch (randomNum)
            {
                case 0:
                    return "0:0:0";
                case 1:
                    return "75:100:0";
                case 2:
                    return "150:200:0";
                case 3:
                    return "225:300:0";
                default:
                    return "0:0:0";
            }
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
