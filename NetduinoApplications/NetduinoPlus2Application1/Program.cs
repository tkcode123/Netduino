using System;
using System.Net;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace NetduinoPlus2Application1
{
    public class Program
    {
        static readonly OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false);
        static readonly OutputPort led = new OutputPort(Pins.GPIO_PIN_D5, false);
        static readonly InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);

        public static void Main()
        {
            MainStart();
            //MainHttp();
            MainBlink();
        }

        private static void MainStart()
        {
            for (int i = 0; i < 10; i++)
            {
                onboardLED.Write(!onboardLED.Read());
                Thread.Sleep(100);
            }
        }

        public static void MainBlink()
        {
            while (true)
            {
                Thread.Sleep(250);
                led.Write(! led.Read());
            }
        }

        public static void MainButton()
        {
            button.OnInterrupt += new NativeEventHandler(button_OnInterrupt);
            Thread.Sleep(Int32.MaxValue);
        }

        static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            onboardLED.Write(!onboardLED.Read());
        }               

        //public static void MainP()
        //{
        //    const int NOTE_C = 261;
        //    const int NOTE_D = 294;
        //    const int NOTE_E = 330;
        //    const int NOTE_F = 349;
        //    const int NOTE_G = 392;

        //    const int WHOLE_DURATION = 1000;
        //    const int EIGHTH = WHOLE_DURATION / 8;
        //    const int QUARTER = WHOLE_DURATION / 4;
        //    const int QUARTERDOT = WHOLE_DURATION / 3;
        //    const int HALF = WHOLE_DURATION / 2;
        //    const int WHOLE = WHOLE_DURATION;

        //    //make sure the two below arrays match in length. each duration element corresponds to
        //    //one note element.
        //     int[] note = { NOTE_E, NOTE_E, NOTE_F, NOTE_G, NOTE_G, NOTE_F, NOTE_E, NOTE_D,
        //        NOTE_C, NOTE_C, NOTE_D, NOTE_E, NOTE_E, NOTE_D, NOTE_D, NOTE_E, NOTE_E, NOTE_F, NOTE_G,
        //        NOTE_G, NOTE_F, NOTE_E, NOTE_D, NOTE_C, NOTE_C, NOTE_D, NOTE_E, NOTE_D, NOTE_C, NOTE_C};

        //    int[] duration = { QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER,
        //    QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTERDOT, EIGHTH, HALF, QUARTER, QUARTER,
        //    QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER, QUARTER,
        //    QUARTERDOT, EIGHTH, WHOLE};

        //    PWM MyPWM = new PWM(PWMChannels.PWM_PIN_D6, 261.0, 0.5, false);
        //    while (true)
        //    {
        //        for (int i = 0; i < note.Length; i++)
        //        {
        //            MyPWM.Stop();
        //            MyPWM.Frequency = note[i];
        //            MyPWM.Start();
        //            Thread.Sleep(duration[i]);
        //        }
        //        Thread.Sleep(100);
        //    }
        //}

        public static void MainGlow()
        {
            PWM MyFader = new PWM(PWMChannels.PWM_PIN_D3, 100, 0.1, false);
            double i = 0.1;
            double dirr = 0.1;
            MyFader.Start();
            while (true)
            {
                MyFader.DutyCycle = i;

                i = (double)(i + dirr);

                if (i >= 0.9)
                    dirr = -0.1;
                if (i <= 0.1)
                    dirr = 0.1;

                //Debug.Print(i.ToString());

                Thread.Sleep(10);
            }
        }

        public static void MainMusic()
        {
            button.OnInterrupt += delegate
            {
                using (var pwm = new PWMMusic(PWMChannels.PWM_PIN_D6))
                {
                    if (onboardLED.Read())
                        pwm.Play("g8g8 a4 g4 C4 h2 g8g8 a4 g4 D4 C2 g8g8 G4 E4 C4 h4 a2 F8F8 E4 C4 D4 C1");
                    else
                        pwm.Play("c2d2e2f2g2a2h2C1x1D2E2F2G2A2H2");
                }
                onboardLED.Write(!onboardLED.Read());
            };
            Thread.Sleep(Timeout.Infinite);
        }

        public static void MainPoti()
        {
            var port = new AnalogInput(AnalogChannels.ANALOG_PIN_A1, 3.3, 0.0, -1);
            var plus = new OutputPort(Pins.GPIO_PIN_A0, true);
            var grnd = new OutputPort(Pins.GPIO_PIN_A2, false);
            for (int i = 0; i < 5; i++)
            {
                onboardLED.Write(!onboardLED.Read());
                Thread.Sleep(100);
            }

            while (true)
            {
                var val = port.Read();
                Debug.Print("Voltage " + val + "V");
                Thread.Sleep(3000);
            }
        }

        public static void MainHttp()
        {
            //// Create a listener.
            //HttpListener listener = new HttpListener("http://localhost",8888);
           
            //listener.Start();
            //Debug.Print("Listening...");
            //// Note: The GetContext method blocks while waiting for a request. 
            //HttpListenerContext context = listener.GetContext();
            //HttpListenerRequest request = context.Request;
            //// Obtain a response object.
            //HttpListenerResponse response = context.Response;
            //// Construct a response. 
            //string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            //// Get a response stream and write the response to it.
            //response.ContentLength64 = buffer.Length;
            //System.IO.Stream output = response.OutputStream;
            //output.Write(buffer, 0, buffer.Length);
            //// You must close the output stream.
            //output.Close();
            //listener.Stop();
        }
    }
}
