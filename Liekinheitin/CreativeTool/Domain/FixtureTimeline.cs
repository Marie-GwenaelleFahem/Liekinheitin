using System;
using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class FixtureKeyframe
    {
        public double TimeSeconds { get; init; }
        public byte Pan { get; init; }
        public byte Tilt { get; init; }
        public byte Speed { get; init; }
        public byte Dimming { get; init; }
        public byte Strobe { get; init; }
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }
        public byte W { get; init; }
    }

    public sealed class FixtureTrack
    {
        public int EntityId { get; init; }
        public List<FixtureKeyframe> Keyframes { get; } = new();

        public void SetKeyframe(FixtureKeyframe keyframe)
        {
            Keyframes.RemoveAll(k => Math.Abs(k.TimeSeconds - keyframe.TimeSeconds) < 0.001);
            Keyframes.Add(keyframe);
            Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        }

        public FixtureKeyframe? Evaluate(double timeSeconds)
        {
            if (Keyframes.Count == 0) return null;
            if (timeSeconds <= Keyframes[0].TimeSeconds) return Keyframes[0];
            if (timeSeconds >= Keyframes[^1].TimeSeconds) return Keyframes[^1];

            for (int i = 0; i < Keyframes.Count - 1; i++)
            {
                var a = Keyframes[i];
                var b = Keyframes[i + 1];
                if (timeSeconds < a.TimeSeconds || timeSeconds > b.TimeSeconds) continue;

                double span = b.TimeSeconds - a.TimeSeconds;
                double t = span <= 0 ? 0 : (timeSeconds - a.TimeSeconds) / span;

                return new FixtureKeyframe
                {
                    TimeSeconds = timeSeconds,
                    Pan = LerpByte(a.Pan, b.Pan, t),
                    Tilt = LerpByte(a.Tilt, b.Tilt, t),
                    Speed = LerpByte(a.Speed, b.Speed, t),
                    Dimming = LerpByte(a.Dimming, b.Dimming, t),
                    Strobe = LerpByte(a.Strobe, b.Strobe, t),
                    R = LerpByte(a.R, b.R, t),
                    G = LerpByte(a.G, b.G, t),
                    B = LerpByte(a.B, b.B, t),
                    W = LerpByte(a.W, b.W, t),
                };
            }

            return Keyframes[^1];
        }

        private static byte LerpByte(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
    }
}