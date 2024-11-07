using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.XPath;

namespace AbakConfigurator
{
    /**
     * Клас шифрования по алгоритму RC6
     */
    public class CrypterClass
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Block
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt32[] blocks;
        }

        private const int Rounds = 20;
        private const int KeyLength = 2 * (Rounds + 2);
        private const int BlockSize = 16;
        private const int KeySize = 16 * 4;
        private const uint P32 = 0xb7e15163;
        private const uint Q32 = 0x9e3779b9;
        private const int lgw = 5;

        private String key;
        private byte[] KeyPtr = new byte[KeyLength];
        private UInt32[] S = new UInt32[KeyLength];

        /**
         * key - ключ шифрования
         */
        public CrypterClass(String key)
        {
            this.key = key;
        }

        /**
         * Циклический сдвиг влево
         */
        private UInt32 rol(UInt32 data, UInt32 shift)
        {
            byte s = (byte)(shift % 32);
            UInt32 l = data << s;
            UInt32 r = data >> (32 - s);

            return l | r;
        }

        /**
         * Циклический сдвиг вправо
         */
        private UInt32 ror(UInt32 data, UInt32 shift)
        {
            byte s = (byte)(shift % 32);
            UInt32 l = data >> s;
            UInt32 r = data << (32 - s);

            return l | r;
        }

        private void calculateSubKeys()
        {
            UInt32[] L = new UInt32[BlockSize];

            // Инициализация подключа S
            S[0] = P32;
            for (int l = 1; l < KeyLength; l++)
            {
                UInt64 temp = S[l - 1] + Q32;
                S[l] = (UInt32)temp;
            }
            // Смешивание S с ключом
            int i = 0;
            int j = 0;
            UInt32 A = 0;
            UInt32 B = 0;
            for (int k = 0; k < (3 * KeyLength); k++)
            {
                A = rol((S[i] + A + B), 3);
                S[i] = A;
                B = rol(L[j] + A + B, (A + B));
                L[j] = B;
                i = (i + 1) % KeyLength;
                j = (j + 1) % 16;
            }
        }

        private Block copyBufferToBlock(byte[] buff)
        {
            Block block;

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Block)));
            Marshal.Copy(buff, 0, buffer, Marshal.SizeOf(typeof(Block)));
            block = (Block)Marshal.PtrToStructure(buffer, typeof(Block));
            Marshal.FreeHGlobal(buffer);
            return block;
        }

        private byte[] copyBlockToBuffer(Block block)
        {
            byte[] buff = new byte[16];

            IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Block)));
            Marshal.StructureToPtr(block, buffer, false);
            Marshal.Copy(buffer, buff, 0, Marshal.SizeOf(typeof(Block)));
            Marshal.FreeHGlobal(buffer);

            return buff;
        }

        private void copyBufferToBuffer(byte[] source, int sofsset, byte[] dest, int doffset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                dest[i + doffset] = source[i + sofsset];
            }
        }

        /**
         * Шифрация отдельного блока
         */
        private void EncipherBlock(Block block)
        {
            //Инициализация блока
            block.blocks[1] += this.S[0];
            block.blocks[3] += this.S[1];

            for (int i = 1; i <= Rounds; i++)
            {
                UInt32 t = rol(block.blocks[1] * (2 * block.blocks[1] + 1), lgw);
                UInt32 u = rol(block.blocks[3] * (2 * block.blocks[3] + 1), lgw);
                block.blocks[0] = rol(block.blocks[0] ^ t, u) + this.S[2 * i];
                block.blocks[2] = rol(block.blocks[2] ^ u, t) + this.S[2 * i + 1];

                UInt32 temp = block.blocks[0];
                block.blocks[0] = block.blocks[1];
                block.blocks[1] = block.blocks[2];
                block.blocks[2] = block.blocks[3];
                block.blocks[3] = temp;
            }
            block.blocks[0] += this.S[2 * Rounds + 2];
            block.blocks[2] += this.S[2 * Rounds + 3];
        }

        /**
         * Расшифровка отдельного блока
         */
        private void DecipherBlock(Block block)
        {
            //Инициализация блока
            block.blocks[2] -= this.S[2 * Rounds + 3];
            block.blocks[0] -= this.S[2 * Rounds + 2];

            for (int i = Rounds; i >= 1; i--)
            {
                UInt32 temp = block.blocks[3];
                block.blocks[3] = block.blocks[2];
                block.blocks[2] = block.blocks[1];
                block.blocks[1] = block.blocks[0];
                block.blocks[0] = temp;

                UInt32 u = rol(block.blocks[3] * (2 * block.blocks[3] + 1), lgw);
                UInt32 t = rol(block.blocks[1] * (2 * block.blocks[1] + 1), lgw);
                block.blocks[2] = ror(block.blocks[2] - this.S[2 * i + 1], t) ^ u;
                block.blocks[0] = ror(block.blocks[0] - this.S[2 * i], u) ^ t;
            }

            block.blocks[3] -= this.S[1];
            block.blocks[1] -= this.S[0];
        }

        /**
         * Функция шифрования данных
         */
        public MemoryStream Encrypt(MemoryStream source)
        {
            //Расчет md5 суммы источника
            MD5 md5 = MD5.Create();
            byte[] temp = source.ToArray();
            byte[] md5Hash = md5.ComputeHash(temp);

            //Формирование массива в которойм присутствуют md5 сумма и данные которые необходимо зашифровать
            byte[] dest = new byte[md5Hash.Length + source.Length];
            md5Hash.CopyTo(dest, 0);
            source.Position = 0;
            source.Read(dest, (int)md5Hash.Length, (int)source.Length);

            //Расчет подключей
            this.calculateSubKeys();

            //Шифрование
            UInt32 offset = 0;
            while ((md5Hash.Length + source.Length - offset) >= BlockSize)
            {
                byte[] buff = new byte[16];
                this.copyBufferToBuffer(dest, (int)offset, buff, 0, buff.Length);
                Block block = this.copyBufferToBlock(buff);
                this.EncipherBlock(block);
                buff = this.copyBlockToBuffer(block);
                this.copyBufferToBuffer(buff, 0, dest, (int)offset, buff.Length);
                offset += (UInt32)buff.Length;
            }

            return new MemoryStream(dest, 0, dest.Length);
        }

        /**
         * Функция расшифровки данных
         */
        public MemoryStream Decrypt(MemoryStream source)
        {
            //Зашифрованный файл
            byte[] dest = new byte[source.Length];
            source.Position = 0;
            source.Read(dest, 0, (int)source.Length);

            //Расчет подключей
            this.calculateSubKeys();

            //Расшифровка
            UInt32 offset = 0;
            while ((source.Length - offset) >= BlockSize)
            {
                byte[] buff = new byte[16];
                this.copyBufferToBuffer(dest, (int)offset, buff, 0, buff.Length);
                Block block = this.copyBufferToBlock(buff);
                this.DecipherBlock(block);
                buff = this.copyBlockToBuffer(block);
                this.copyBufferToBuffer(buff, 0, dest, (int)offset, buff.Length);
                offset += (UInt32)buff.Length;
            }

            //первые 128 бит это md5 сумма
            byte[] md5Hash = new byte[16];
            this.copyBufferToBuffer(dest, 0, md5Hash, 0, 16);

            MemoryStream outStream = new MemoryStream(dest, 16, dest.Length - 16);

            byte[] temp = outStream.ToArray();
            MD5 md5 = MD5.Create();
            byte[] checkHash = md5.ComputeHash(temp);

            for (int i = 0; i < 16; i++)
            {
                if (md5Hash[i] != checkHash[i])
                    return null;
            }

            return outStream;
        }


        public void EncryptXmlToFile(XmlDocument doc, FileStream file)
        {
            String s = doc.InnerXml;
            MemoryStream source = new MemoryStream();
            doc.Save(source);

            MemoryStream dest = this.Encrypt(source);
            dest.Position = 0;
            byte[] buff = dest.ToArray();
            file.Position = 0;
            file.Write(buff, 0, buff.Length);
        }

        public void DecryptFileToXml(FileStream file, XmlDocument doc)
        {
            file.Position = 0;
            byte[] buff = new byte[file.Length];
            file.Read(buff, 0, (int)file.Length);

            MemoryStream source = new MemoryStream(buff);
            MemoryStream dest = this.Decrypt(source);

            if (dest != null)
            {
                dest.Position = 0;
                doc.Load(dest);
            }
        }

        public XPathDocument DecriptFile(String path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);

            fs.Position = 0;
            byte[] buff = new byte[fs.Length];
            fs.Read(buff, 0, (int)fs.Length);

            MemoryStream source = new MemoryStream(buff);
            MemoryStream dest = this.Decrypt(source);
            
            if (dest != null)
            {
                dest.Position = 0;
                XPathDocument doc = new XPathDocument(dest);
                return doc;
            }
            return null;
        }
    }
}
