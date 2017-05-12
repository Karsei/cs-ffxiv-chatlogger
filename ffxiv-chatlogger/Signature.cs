using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffxiv_chatlogger
{
    internal class Signature
    {
        /****
         * {"Value":"47616D654D61696E000000","ASMSignature":false,"Offset":1344,"Notes":[],"PointerPath":[],"Key":"GAMEMAIN"},{"Value":"750e85d2750ab9","ASMSignature":false,"Offset":7,"Notes":[],"PointerPath":[0,108],"Key":"TARGET"},{"Value":"8b55fc83e2f983ca098b4d08a1********515250E8********a1","ASMSignature":false,"Offset":26,"Notes":[],"PointerPath":[0,0,24,696],"Key":"CHATLOG"},{"Value":"81feffff0000743581fe58010000732d8b3cb5","ASMSignature":false,"Offset":19,"Notes":[],"PointerPath":[0,0],"Key":"CHARMAP"},
{"Value":"85c074178b407450b9","ASMSignature":false,"Offset":9,"Notes":[],"PointerPath":[0,16],"Key":"PARTYMAP"},{"Value":"8b0d********85c975068b0d","ASMSignature":false,"Offset":12,"Notes":[],"PointerPath":[0,0],"Key":"MAPINFO"},
{"Value":"83f8ff740f8b04875056b9","ASMSignature":false,"Offset":11,"Notes":[],"PointerPath":[0,0],"Key":"PLAYERINFO"},
{"ASMSignature":false,"Offset":0,"Notes":[],"PointerPath":[17860644],"Key":"AGRO"},
{"ASMSignature":false,"Offset":0,"Notes":[],"PointerPath"[17862948],"Key":"AGRO_COUNT"},
{"ASMSignature":false,"Offset":0,"Notes":[],"PointerPath":[17858332],"Key":"ENMITYMAP"},
{"ASMSignature":false,"Offset":0,"Notes":[],"PointerPath":[18281740],"Key":"PARTYCOUNT"},
{"ASMSignature":false,"Offset":0,"Key":"ZONEINFO","Notes":[],"PointerPath":[18261784]}
        ****/

        /*******************************************
         * 메모리 패턴
         * 
         * @param X64               64비트 운영체제 여부
         * @param pattern           메모리 패턴
         * @param offsetStart       시작 포인터 지점(배열 3개)
         * @param offsetEnd         종료 포인터 지점(배열 3개)
         * @param offsetLenStart    길이 시작(배열 3개)
         * @param offsetLenEnd      길이 종료(배열 3개)
        ********************************************/
        public Signature(bool X64, string pattern, long[] offsetStart, long[] offsetEnd, long[] offsetLenStart, long[] offsetLenEnd)
        {
            this.IsX64          = X64;
            this.Pattern        = pattern;
            this.OffsetStart    = offsetStart;
            this.OffsetEnd      = offsetEnd;
            this.OffsetLenStart = offsetLenStart;
            this.OffsetLenEnd   = offsetLenEnd;
        }

        public bool     IsX64           { get; private set; }
        public string   Pattern         { get; private set; }
        public long[]   OffsetStart     { get; private set; }
        public long[]   OffsetEnd       { get; private set; }
        public long[]   OffsetLenStart  { get; private set; }
        public long[]   OffsetLenEnd    { get; private set; }

        public IntPtr Scan(Process targetProcess, IntPtr targetProcessHnd)
        {
            // 메모리 패턴을 16진수 바이트로 표현
            var patArr = GetPatternArray(this.Pattern);

            // 메모리 범위 파악
            IntPtr curPtr = targetProcess.MainModule.BaseAddress;
            IntPtr maxPtr = IntPtr.Add(curPtr, targetProcess.MainModule.ModuleMemorySize);

            // 메모리 사이즈 관련
            int lenSize = 0x1000;
            IntPtr nSize = new IntPtr(lenSize);
            byte[] buffer = new byte[lenSize];

            // 인덱스 관련
            int index;
            IntPtr read = IntPtr.Zero;

            // 주소를 증가시키면서 패턴 찾음
            while (curPtr.ToInt64() < maxPtr.ToInt64())
            {
                try
                {
                    // 사이즈 재정의
                    if ((curPtr + lenSize).ToInt64() > maxPtr.ToInt64())
                        nSize = new IntPtr(maxPtr.ToInt64() - curPtr.ToInt64());

                    // 프로세스 핸들에서 현 주소로부터 정해진 사이즈만큼 메모리 패턴을 가져옴
                    if (BaseMethod.ReadProcessMemory(targetProcessHnd, curPtr, buffer, nSize, ref read))
                    {
                        // 현재 가져온 메모리 패턴을 가지고 메모리 패턴을 찾음
                        index = FindPatternArray(buffer, patArr, 0, read.ToInt32() - 3);

                        // 제대로 패턴을 찾은 경우
                        if (index != -1)
                        {
                            IntPtr ptr;
                            if (this.IsX64)  // 64비트
                            {
                                ptr = new IntPtr(BitConverter.ToInt32(buffer, index + patArr.Length));
                                ptr = new IntPtr(curPtr.ToInt64() + (index + patArr.Length) + 4 + ptr.ToInt64());
                            }
                            else        // 32비트
                            {
                                ptr = new IntPtr(BitConverter.ToInt32(buffer, index + patArr.Length));
                            }

                            return ptr;
                        }
                    }
                    // 주소 증가
                    curPtr += lenSize;
                }
                catch (Exception e)
                {
                    LogWriter.Error("포인터를 찾는 도중에 오류가 발생했습니다.", e);
                }
            }

            return IntPtr.Zero;
        }

        private byte?[] GetPatternArray(string pattern)
        {
            byte?[] arr = new byte?[pattern.Length / 2];

            for (int i = 0; i < (pattern.Length / 2); i++)
            {
                // 2개씩 끊어서 찾음
                string str = pattern.Substring(i * 2, 2);
                if (str == "**")
                    // 가변 부분은 null로 처리
                    arr[i] = null;
                else
                    // 한 바이트를 16진수로 표현
                    arr[i] = new byte?(Convert.ToByte(str, 16));
            }
            return arr;
        }

        private int FindPatternArray(byte[] buffer, byte?[] pattern, int startIndex, int length)
        {
            // 길이는 작은 쪽에 맞춤
            length = Math.Min(buffer.Length, length);

            int i, j;
            for (i = startIndex; i < (length - pattern.Length); i++)    // 전체 길이에서
            {
                for (j = 0; j < pattern.Length; j++)    // 주어진 패턴의 길이를 찾는데
                    if (pattern[j].HasValue && buffer[i + j] != pattern[j].Value)   // 패턴 값이 null이 아니고 주어진 패턴과 다르면
                        break;  // 중지

                if (j == pattern.Length)    // 이전 for문에서 끝까지 도달했다면 찾은 것!
                    return i;
            }

            return -1;
        }
    }
}
