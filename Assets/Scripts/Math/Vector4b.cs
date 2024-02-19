﻿using System;
using System.Drawing;

namespace CTRFramework.Shared
{
    public class Vector4b : IEquatable<Vector4b>
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte W;

        public void Scale(float x)
        {
            X = (byte)(X * x);
            Y = (byte)(Y * x);
            Z = (byte)(Z * x);
            W = (byte)(W * x);
        }


        public Vector4b(byte x, byte y, byte z, byte w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void Scale(float x, float y, float z, float w)
        {
            X = (byte)(X * x);
            Y = (byte)(Y * y);
            Z = (byte)(Z * z);
            W = (byte)(W * w);
        }

        public Vector4b(Color c)
        {
            X = c.R;
            Y = c.G;
            Z = c.B;
            W = 0;
        }

        public Vector4b(uint a)
        {
            X = (byte)(a >> 24 & 0xFF);
            Y = (byte)(a >> 16 & 0xFF);
            Z = (byte)(a >> 8 & 0xFF);
            W = (byte)(a & 0xFF);
        }

        public Vector4b(BinaryReaderEx br)
        {
            Read(br);
        }

        public void Read(BinaryReaderEx br)
        {
            uint val = br.ReadUInt32();

            W = (byte)(val >> 24 & 0xFF);
            Z = (byte)(val >> 16 & 0xFF);
            Y = (byte)(val >> 8 & 0xFF);
            X = (byte)(val & 0xFF);

            //X = br.ReadByte();
            //Y = br.ReadByte();
            //Z = br.ReadByte();
            //W = br.ReadByte();
        }

        public void Write(BinaryWriterEx bw)
        {
            bw.Write(X);
            bw.Write(Y);
            bw.Write(Z);
            bw.Write(W);
        }

        public bool Equals(Vector4b v)
        {
            if (v.X != X)
                return false;

            if (v.Y != Y)
                return false;

            if (v.Z != Z)
                return false;

            if (v.W != W)
                return false;

            return true;
        }
    }
}