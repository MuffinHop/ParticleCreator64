using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitStreams
{
    [Serializable]
    public class BitExtractor
    {
        private readonly byte _byteValue;

        public BitExtractor(byte byteValue)
        {
            _byteValue = byteValue;
        }

        public bool GetBit(int position)
        {
            if (position < 0 || position > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 0 and 7.");
            }

            // Shifting 1 to the right position and performing bitwise AND with the byte value
            return (_byteValue & (1 << position)) != 0;
        }

    }
}