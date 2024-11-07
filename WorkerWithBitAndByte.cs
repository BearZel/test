namespace AbakConfigurator
{
    /// <summary>
    /// Работа с битами и байтами
    /// </summary>
    public static class WorkerWithBitAndByte
    {
        /// <summary> Размер байта </summary>
        public const int ByteSize = 8;

        /// <summary> Размер ushort </summary>
        public const int UshortSize = 16;

        /// <summary> Размер uint </summary>
        public const int UintSize = 32;


        /// <summary>
        /// Возвращаем значение указанного бита (в массиве типа Byte).
        /// </summary>
        /// <param name="arrByte">Массив байт, содержащий искомый бит.</param>
        /// <param name="curIntNumberBit">Номер искомого бита (начинается с 1)</param>
        /// <returns></returns>
        public static bool ArrByte2Bite(byte[] arrByte, int curIntNumberBit)
        {
            var intNumberBit = curIntNumberBit - 1;
            if (arrByte.Length * ByteSize < curIntNumberBit)
                return false; //Вернём false если номер бита вышел за пределы массива

            int intNumByte = intNumberBit / ByteSize; // Уточним номер байта
            int intNumBit = intNumberBit % ByteSize; // Уточним номер бита в байте
            return ((arrByte[intNumByte] >> intNumBit) & 1) == 1;
        }

        /// <summary>
        /// Возвращаем значение указанного бита (в массиве типа ushort).
        /// </summary>
        /// <param name="arrUshort">Массив USHORT, содержащий искомый бит.</param>
        /// <param name="curIntNumberBit">Номер искомого бита (начинается с 1)</param>
        /// <returns></returns>
        public static bool ArrUshort2Bite(ushort[] arrUshort, int curIntNumberBit)
        {
            var intNumberBit = curIntNumberBit - 1;
            if (arrUshort.Length * UshortSize < curIntNumberBit)
                return false; //Вернём false если номер бита вышел за пределы массива

            int intNumUshort = intNumberBit / UshortSize;   // Уточним номер ushort
            int intNumBit = intNumberBit % UshortSize;     // Уточним номер бита в ushort
            return ((arrUshort[intNumUshort] >> intNumBit) & 1) == 1;
        }

        /// <summary>
        /// Возвращаем значение указанного бита (в массиве типа uint).
        /// </summary>
        /// <param name="arrUint">Массив UINT, содержащий искомый бит.</param>
        /// <param name="curIntNumberBit">Номер искомого бита (начинается с 1)</param>
        /// <returns></returns>
        public static bool ArrUint2Bite(uint[] arrUint, int curIntNumberBit)
        {
            var intNumberBit = curIntNumberBit - 1;
            if (arrUint.Length * UintSize < curIntNumberBit)
                return false; //Вернём false если номер бита вышел за пределы массива

            int intNumUshort = intNumberBit / UintSize;   // Уточним номер uint
            int intNumBit = intNumberBit % UintSize;     // Уточним номер бита в uint
            return ((arrUint[intNumUshort] >> intNumBit) & 1) == 1;
        }
    }
}
