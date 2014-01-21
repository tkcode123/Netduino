using System;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace TKCode123.Sound
{
    public class PWMMusic : IDisposable
    {
        public const string HappyBirthday = "g8g8 a4 g4 C4 h2 g8g8 a4 g4 D4 C2 g8g8 G4 E4 C4 h4 a2 F8F8 E4 C4 D4 C1";
        private PWM _pwm;

        public PWMMusic(Cpu.PWMChannel channel)
        {
            _pwm = new PWM(channel, 10000.0, 0.5, false);
        }

        public void Dispose()
        {
            if (_pwm != null)
            {
                _pwm.Stop();
                _pwm.Dispose();
                _pwm = null;
            }
        }

        // c#1,d-2,E4,x8
        public void Play(string notes)
        {
            for (int i = 0, n = notes.Length; i < n; i+=2)
            {
                char note = notes[i];
                if (note != ' ')
                {
                    bool sharp = false;
                    if (notes[i + 1] == '#')
                    {
                        i++; sharp = true;
                    }
                    char len = notes[i + 1];
                    PlayNote(note, sharp, len);
                }
                else
                    i--;
            }
        }

        private void PlayNote(char note, bool sharp, char len)
        {
            int ms = 1000;
            switch(len)
            {
                case '0': ms *= 2;  break;
                case '1': ms = 1000; break;
                case '2': ms /= 2; break;
                case '3': ms /= 3; break;
                case '4': ms /= 4; break;
                case '8': ms /= 8; break;
                case '6': ms /= 16; break;
                case '9': ms /= 32; break;
                default: 
                    throw new ArgumentOutOfRangeException("len", "Invalid length, use 01234669.");
            }

            int idx = pitches.IndexOf(note);
            if (idx >= 0)
            {
                double freq = freqs[idx];
                if (sharp)
                {
                    double n = freqs[idx + 1];
                    freq = (freq + n) / 2;
                }
                _pwm.Frequency = freq;
                _pwm.Start();
                Thread.Sleep(ms);
                _pwm.Stop();
            }
            else if (note == 'x')
            {
                Thread.Sleep(ms);
            }
            else
            {
                throw new ArgumentOutOfRangeException("note", "Invalid pitch, use cdefgahbCDEFGAHB.");
            }
        }

        private const string pitches = "cdefgabhCDEFGABH";
        private static readonly double[] freqs;

        static PWMMusic()
        {
            string freqsn = "261.63,293.66,329.63,349.23,392.00,440.00,466.16,493.88,523.25,587.332,659.26,698.46,783.99,880.00,923.33,987.77,1046.50";
            string[] x = freqsn.Split(',');
            freqs = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
                freqs[i] = double.Parse(x[i]);
        }
    }
}
