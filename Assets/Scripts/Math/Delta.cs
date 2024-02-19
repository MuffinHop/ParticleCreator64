using System.Collections;
using System.Collections.Generic;
using CTRFramework.Shared;
using UnityEngine;

public class Delta
{
    //base position difference value
    public Vector3b Position = new Vector3b(0, 0, 0);

    //additional bits to take from compressed vertex stream
    public Vector3b Bits = new Vector3b(0, 0, 0);

    private uint packedValue => packValue();

    public Delta(uint value) => unpackValue(value);

    public Delta(BinaryReaderEx br) => unpackValue(br.ReadUInt32());

    public static Delta FromReader(BinaryReaderEx br) => new Delta(br);

    /// <summary>
    /// Retrieves delta struct as a packed uint.
    /// </summary>
    /// <returns></returns>
    private uint packValue()
    {
        return
            (uint)((Bits.X & 7) << (3 * 2)) |
            (uint)((Bits.Y & 7) << (3 * 1)) |
            (uint)((Bits.Z & 7) << (3 * 0)) |
            (uint)((Position.X & 0x7F) << (9 + 8 * 2)) |
            (uint)((Position.Y & 0xFF) << (9 + 8 * 1)) |
            (uint)((Position.Z & 0xFF) << (9 + 8 * 0));
    }

    /// <summary>
    /// Unpacks uint value to a delta struct.
    /// </summary>
    /// <returns></returns>
    private void unpackValue(uint value)
    {
        Bits.X = (byte)((value >> (3 * 2)) & 7);
        Bits.Y = (byte)((value >> (3 * 1)) & 7);
        Bits.Z = (byte)((value >> (3 * 0)) & 7);

        Position.X = (byte)((value >> (9 + 8 * 2)) & 0xFF);
        Position.Y = (byte)((value >> (9 + 8 * 1)) & 0xFF);
        Position.Z = (byte)((value >> (9 + 8 * 0)) & 0xFF);

    }

}
