using System;
using System.Net;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using TKCode123;

namespace NetduinoPlus2Application1
{
    public class Program
    {
        static readonly OutputPort led = new OutputPort(Pins.GPIO_PIN_D5, false);

        public static void Main()
        {
            MainGlow();
        }
       
        private static void BlinkStart()
        {
            using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
            {
                for (int i = 0; i < 10; i++)
                {
                    onboardLED.Write(!onboardLED.Read());
                    Thread.Sleep(80);
                }
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
            using(InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh))
            {
                button.OnInterrupt += new NativeEventHandler(button_OnInterrupt);
                Thread.Sleep(Int32.MaxValue);
            }
        }

        static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            led.Write(!led.Read());
        }                      

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
            using (InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh))
            {
                button.OnInterrupt += delegate
                {
                    using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
                    {
                        using (var pwm = new TKCode123.Sound.PWMMusic(PWMChannels.PWM_PIN_D6))
                        {
                            if (onboardLED.Read())
                                pwm.Play(TKCode123.Sound.PWMMusic.HappyBirthday);
                            else
                                pwm.Play("c2d2e2f2g2a2h2C1x1D2E2F2G2A2H2");
                        }
                        onboardLED.Write(!onboardLED.Read());
                    }
                };
                Thread.Sleep(Timeout.Infinite);
            }
        }

        public static void MainPoti()
        {
            var port = new AnalogInput(AnalogChannels.ANALOG_PIN_A1, 3.3, 0.0, -1);
            var plus = new OutputPort(Pins.GPIO_PIN_A0, true);
            var grnd = new OutputPort(Pins.GPIO_PIN_A2, false);
            using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
            {
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
        }

        internal static void BlinkError()
        {
            using (OutputPort onboardLED = new OutputPort(Pins.ONBOARD_LED, false))
            {
                for (int i = 0; i < 10; i++)
                {
                    onboardLED.Write(true);
                    Thread.Sleep(100);
                    onboardLED.Write(false);
                    Thread.Sleep(400);
                }
            }
        }
    }
}
