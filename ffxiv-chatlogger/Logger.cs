﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace ffxiv_chatlogger
{
    internal class Logger
    {
        // 시그네쳐
        private static readonly Signature SigX64 = new Signature(
            true,
            "e8********85c0740e488b0d********33D2E8********488b0d",
            new long[] { 0, 0x30, 0x438 },
            new long[] { 0, 0x30, 0x440 },
            new long[] { 0, 0x30, 0x418 },
            new long[] { 0, 0x30, 0x420 });
        private static readonly Signature SigX86 = new Signature(
            false,
            "**088b**********505152e8********a1",
            new long[] { 0, 0x18, 0x2F0 },
            new long[] { 0, 0x18, 0x2F4 },
            new long[] { 0, 0x18, 0x2E0 },
            new long[] { 0, 0x18, 0x2E4 });

        // 프로세스 목록
        public static readonly ObservableCollection<string> ProcessList         = new ObservableCollection<string>();
        // 채팅 로그
        public static readonly ObservableCollection<Chat> ChatLog               = new ObservableCollection<Chat>();
        public static ObservableCollection<ChatType> ChatFilterLog              = new ObservableCollection<ChatType>();
        public static ObservableCollection<ChatType> ChatFilterTransLog         = new ObservableCollection<ChatType>();
        // 스레드 작업
        private static readonly ManualResetEvent Work                           = new ManualResetEvent(false);

        private static readonly DateTime BaseTimeStamp                          = new DateTime(1970, 1, 1, 0, 0, 0);

        // 메모리 관련
        private static Signature    m_memPattern;
        // 메인 프로세스
        private static Process      m_mainProcess;
        // 메인 프로세스 포인터
        private static IntPtr       m_mainProcessPtr;
        // 프로세스 기본 주소
        private static IntPtr       m_basePtr;
        // 프로세스 채팅로그 주소
        private static IntPtr       m_chatLogPtr;
        // 운영체제 비트 여부
        private static bool         m_isX64;

        public static WinForms.NotifyIcon notify;

        [STAThread]
        public static void Init()
        {
            // 트레이 아이콘
            notify = new WinForms.NotifyIcon();
            notify.Icon = ffxiv_chatlogger.Properties.Resources.hp_notepad2_pencil;
            notify.Visible = true;
            notify.BalloonTipTitle = "FFXIV 채팅 수집기";
            notify.Text = "FFXIV 채팅 수집기";

            // FFXIV 프로세스 찾기
            FindFFXIVProcess();

            // 스레드 만들고 작업 시작
            Task.Factory.StartNew(() =>
            {
                // 프로세스가 발견되지 않은 경우 30초 재설정 작업
                while (true)
                {
                    // 30초 동안 대기
                    Thread.Sleep(30 * 1000);

                    // 찾은 프로세스가 없거나 종료되었다면
                    if (m_mainProcess == null || m_mainProcess.HasExited)
                    {
                        // FFXIV 프로세스 초기화
                        ResetFFXIVProcess();

                        // 다시 프로세스 찾기
                        App.Current.Dispatcher.Invoke(new Action(FindFFXIVProcess));
                    }
                }
            });

            // 파싱 작업 시작
            Task.Factory.StartNew(ParsingLog);
        }

        public static void FindFFXIVProcess()
        {
            // 프로세스 목록 초기화
            ProcessList.Clear();
            LogWriter.Info("프로세스를 찾는 중...");

            // 프로세스 범위 추가
            var ffxivPrc = new List<Process>();
            ffxivPrc.AddRange(Process.GetProcessesByName("ffxiv"));
            ffxivPrc.AddRange(Process.GetProcessesByName("ffxiv_dx11"));

            if (ffxivPrc.Count == 0)
            {
                //notify.BalloonTipText = "FFXIV 프로세스를 찾을 수 없어 30초 후에 다시 시도합니다.";
                //notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스를 찾을 수 없습니다!");
            }
            else if (ffxivPrc.Count > 1)
            {
                notify.BalloonTipText = "FFXIV 프로세스가 2개 이상이므로 직접 프로세스를 설정하시기 바랍니다.";
                notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스가 2개 이상이므로 직접 프로세스를 설정하시기 바랍니다.");
                foreach (var process in ffxivPrc)
                    using (process)
                        ProcessList.Add(string.Format("{0}:{1}", process.ProcessName, process.Id));
            }
            else
            {
                notify.BalloonTipText = "FFXIV 프로세스를 발견했습니다!";
                notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스를 찾았습니다! 설정을 시작합니다...");
                SetFFXIVProcess(ffxivPrc[0]);
            }
        }

        private static void SetFFXIVProcess(Process process)
        {
            if (m_mainProcess != null)
                m_mainProcess.Dispose();

            try
            {
                // 주 프로세스 설정
                m_mainProcess = process;
                // 프로세스 핸들 로드
                m_mainProcessPtr = BaseMethod.OpenProcess(0x00000010, false, m_mainProcess.Id);
                
                // 운영체제 비트 부분
                m_isX64 = !BaseMethod.IsX64Process(m_mainProcess.Handle);
                // 운영체제 비트에 따라 메모리 패턴 구분
                m_memPattern = m_isX64 ? SigX64 : SigX86;
                
                // 주 프로세스의 메모리 주소 가져옴
                m_basePtr = m_mainProcess.MainModule.BaseAddress;
                m_chatLogPtr = m_memPattern.Scan(m_mainProcess, m_mainProcessPtr);
                
                if (m_chatLogPtr != IntPtr.Zero)
                {
                    Work.Set();

                    // 프로세스 이름 설정
                    string procName = string.Format("{0}:{1}", m_mainProcess.ProcessName, m_mainProcess.Id);
                    LogWriter.Info("FFXIV 프로세스가 선택되었습니다 - {0}", procName);

                    // 프로세스 목록 초기화
                    ProcessList.Clear();
                    ProcessList.Add(procName);
                }
                else
                {
                    notify.BalloonTipText = "메모리 패턴을 찾는데 실패했습니다.";
                    notify.ShowBalloonTip(1000);
                    LogWriter.Info("메모리 패턴을 찾는데 실패했습니다.");
                }
            }
            catch (Exception e)
            {
                LogWriter.Error("프로세스를 설정하는 과정에서 오류가 발생했습니다.", e);
            }
        }

        private static void ResetFFXIVProcess()
        {
            try
            {
                // 주 프로세스 초기화
                m_mainProcess = null;
                Work.Reset();
            }
            catch (Exception e)
            {
                LogWriter.Error("프로세스를 다시 설정하는 과정에서 오류가 발생했습니다.", e);
            }
        }

        private static void ParsingLog()
        {
            LogWriter.Info("로그 파싱을 시작합니다...");

            IntPtr  offsetStart,
                    offsetEnd,
                    offsetLenStart,
                    offsetLenEnd;

            int[] buffer = new int[0xfa0];  // 4000

            int len, num = 0;
            int i, j;
            bool flag = false;

            IntPtr zero = IntPtr.Zero;
            IntPtr ptr = IntPtr.Zero;

            var data = new List<byte[]>();

            try
            {
                while (Work.WaitOne())
                {
                    // 채팅 오프셋 위치 설정
                    offsetStart = GetPointer(m_chatLogPtr, m_memPattern.OffsetStart);
                    offsetEnd = GetPointer(m_chatLogPtr, m_memPattern.OffsetEnd);
                    offsetLenStart = GetPointer(m_chatLogPtr, m_memPattern.OffsetLenStart);
                    offsetLenEnd = GetPointer(m_chatLogPtr, m_memPattern.OffsetLenEnd);
                    
                    if ((offsetStart == IntPtr.Zero || offsetEnd == IntPtr.Zero) || (offsetLenStart == IntPtr.Zero || offsetLenEnd == IntPtr.Zero))
                        // 완전하게 주어지지 않으면 잠시 0.1초 기다림
                        Thread.Sleep(100);

                    else if ((offsetLenStart.ToInt64() + (num * 4)) == offsetLenEnd.ToInt64())
                    {
                        flag = false;
                        Thread.Sleep(100);
                    }
                    else
                    {
                        if (offsetLenEnd.ToInt64() < offsetLenStart.ToInt64())
                            throw new ApplicationException("[오류] 오프셋의 끝 포인터가 시작 포인터보다 앞에 있습니다.");

                        if (offsetLenEnd.ToInt64() < offsetLenStart.ToInt64() + (num * 4))
                        {
                            if ((zero != IntPtr.Zero) && (zero != IntPtr.Zero))
                            {
                                for (j = num; j < 0x3e8; j++) // 1000
                                {
                                    // 4바이트씩 주소를 올려서 정수값 반환
                                    buffer[j] = ReadInt32(ptr + (j * 4));

                                    // 길이가 너무 길면 안됨
                                    if (buffer[j] > 0x100000)
                                    {
                                        zero = IntPtr.Zero;
                                        ptr = IntPtr.Zero;
                                        throw new ApplicationException("[오류] 메세지 길이가 너무 깁니다.");
                                    }

                                    // 각 메시지 타입 요소와의 길이
                                    int length = buffer[j] - ((j == 0) ? 0 : buffer[j - 1]);
                                    if (length != 0)
                                    {
                                        byte[] message = ReadBytes(
                                            IntPtr.Add(offsetStart, j == 0 ? 0 : buffer[j - 1]),
                                            length
                                        );
                                        if (CheckMessage(message))
                                            data.Add(message);
                                    }
                                }
                            }
                            buffer = new int[0xfa0];
                            num = 0;
                        }

                        // 시작 지점 설정
                        zero = offsetStart;
                        ptr = offsetLenStart;

                        // 올바르지 않은 형식 걸러냄
                        if ((offsetLenEnd.ToInt64() - offsetLenStart.ToInt64()) > 0x100000L)
                            throw new ApplicationException("[오류] 읽지 않은 데이터 길이가 너무 큽니다.");

                        if (((offsetLenEnd.ToInt64() - offsetLenStart.ToInt64()) % 4L) != 0L)
                            throw new ApplicationException("[오류] 올바르지 않은 길이 배열입니다.");

                        if ((offsetLenEnd.ToInt64() - offsetLenStart.ToInt64()) > 0xfa0L)
                            throw new ApplicationException("[오류] 길이 배열이 너무 작습니다.");

                        // 길이 포인터를 정수로 반환 후 4바이트로 나눔
                        len = (int)(offsetLenEnd.ToInt64() - offsetLenStart.ToInt64()) / 4;
                        for (i = num; i < len; i++)
                        {
                            // 4바이트씩 주소를 올려서 정수값 반환
                            buffer[i] = ReadInt32(offsetLenStart + (i * 4));
                            byte[] message = ReadBytes(
                                offsetStart + (i == 0 ? 0 : buffer[i - 1]),
                                buffer[i] - (i == 0 ? 0 : buffer[i - 1])
                            );
                            num++;
                            if (!flag && CheckMessage(message))
                                data.Add(message);
                        }
                        flag = false;

                        // 데이터가 하나라도 있다면
                        if (data.Count > 0)
                        {
                            // 채팅 메세지 작업 시작!
                            Task.Factory.StartNew(RawToChatMessage, data.ToArray());

                            // 그리고 기존 데이터는 비움
                            data.Clear();
                        }
                        
                        // 간격 0.2초 기다림
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.Error("파싱하는 과정에서 오류가 발생했습니다.", e);
            }
        }

        /*******************************************
         * 채팅 메세지 종류 체크
         * 
         * @param rawData               데이터
         * @return  채팅 메세지 종류에 해당 메세지 타입이 있다면 true, 아니면 false
        ********************************************/
        private static bool CheckMessage(byte[] rawData)
        {
            //Console.WriteLine("발견 타입 - {0}", BitConverter.ToInt16(rawData, 4));
            // 메세지에서 4번째의 항목(채팅 메세지 구분)을 정수로 반환
            return ChatType.TypeList.ContainsKey(BitConverter.ToInt16(rawData, 4));
        }

        /*******************************************
         * 채팅 메세지 종류 체크 (번역)
         * 
         * @param type                  채팅 메세지 종류
         * @return  채팅 메세지 종류 번역 목록에 해당 메세지 타입이 있다면 true, 아니면 false
        ********************************************/
        private static bool CheckTransMessage(ChatType type)
        {
            bool enable = Settings.globalSetting.enableTransService.HasValue ? Settings.globalSetting.enableTransService.Value : false;
            if (enable)
                return ChatFilterTransLog.Contains(type);
            else
                return false;
        }

        private static void RawToChatMessage(object arg)
        {
            var rawData = (byte[][])arg;

            if (rawData.Length == 0)
                return;

            var arr = new Chat[rawData.Length];
            for (int i = 0; i < rawData.Length; ++i)
                arr[i] = ParseChat(rawData[i]);

            App.Current.Dispatcher.Invoke(new Action<Chat[]>(AddChatMessage), (object)arr);
        }

        private static Chat ParseChat(byte[] rawData)
        {
            // 채팅 메세지 종류
            var type = BitConverter.ToInt16(rawData, 4);
            // 해당 메세지 종류가 채팅 타입에 없으면 생략
            if (!ChatType.TypeList.ContainsKey(type))
                return null;

            // 시간
            var dateTime = BaseTimeStamp.AddSeconds(BitConverter.ToInt32(rawData, 0)).ToLocalTime();

            int pos;
            // 캐릭터 이름
            var charName = GetCharacterName(rawData, 9, 0x3A, out pos);
            // 캐릭터 이름 뒤에는 채팅 메세지
            var text = GetChatString(rawData, pos, rawData.Length);
            // 채팅 메세지 종류
            var typeObj = ChatType.TypeList[type];

            // 번역 항목에 포함되어있다면?
            if (CheckTransMessage(typeObj))
            {
                var src = Settings.globalSetting.sourceLang;
                var dest = Settings.globalSetting.destLang;
                
                try
                {
                    Func<Task<string>> transFunc = new Func<Task<string>>(() => Translate.Run(
                        TranslateServiceList.TypeList[Settings.globalSetting.selectAPIService],
                        text,
                        src,
                        dest
                    ));
                    var s = Task<Task<string>>.Factory.StartNew(transFunc);
                    text = s.Result.Result;
                }
                catch (Exception e)
                {
                    LogWriter.Error("번역 메세지 전달하는 과정에서 오류가 발생했습니다.", e);
                }
            }

            return new Chat(typeObj, string.Format(typeObj.GetFormat, dateTime.Hour, dateTime.Minute, charName, text, type - 0xf));
        }

        private static void AddChatMessage(Chat[] list)
        {
            for (int i = 0; i < list.Length; ++i)
            {
                if (list[i] != null)
                    ChatLog.Add(list[i]);
            }

            // 스크롤 내림
            MainWindow.Instance.ScrollToBottom();
        }

        private static string GetCharacterName(byte[] rawData, int index, int endByte, out int pos)
        {
            int len = 0;
            // 캐릭터 이름 종료 지점까지 길이 상승
            while (rawData[index + len] != endByte)
                len++;

            // +1을 하여 끝 지점 설정
            pos = index + len + 1;

            return len > 0 ? GetChatString(rawData, index, index + len) : null;
        }

        private static string GetChatString(byte[] rawData, int index, int endIndex)
        {
            var buffer = new byte[rawData.Length];
            var bindex = 0;

            byte v;
            while (index < endIndex)
            {
                // 원본 데이터의 위치에서 문자(바이트) 읽음
                v = rawData[index++];
                if (v == 2)
                    index += rawData[index + 1] + 2;
                else
                    // 새 버퍼에 문자를 넣음
                    buffer[bindex++] = v;
            }

            return bindex > 0 ? Encoding.UTF8.GetString(buffer, 0, bindex) : null;
        }

        private static IntPtr ReadPointer(IntPtr offset)
        {
            // 64비트면 8비트, 32비트면 4비트 (long)
            int num = m_isX64 ? 8 : 4;
            // long 포인터 버퍼 저장할 변수
            byte[] lpBuffer = new byte[num];
            // 제로 포인터
            IntPtr zero = IntPtr.Zero;

            // 오프셋에서 주소를 찾고 그 값을 버퍼에 저장하지 못하면 그냥 제로 반환
            if (!BaseMethod.ReadProcessMemory(m_mainProcessPtr, offset, lpBuffer, new IntPtr(num), ref zero))
                return IntPtr.Zero;

            if (m_isX64)    // 64비트
                return new IntPtr(BitConverter.ToInt64(lpBuffer, 0));
            else            // 32비트
                return new IntPtr(BitConverter.ToInt32(lpBuffer, 0));
        }

        private static IntPtr GetPointer(IntPtr sigPointer, long[] pointerTree)
        {
            // 포인터 배열이 없으면 제로 반환
            if (pointerTree == null)
                return IntPtr.Zero;

            // 포인터 배열의 길이가 없으면 시그네쳐 포인터의 길이로 반환
            if (pointerTree.Length == 0)
                return new IntPtr(sigPointer.ToInt64());

            // 기본적으로 시그네쳐 포인터의 길이로 생성
            IntPtr ptr = new IntPtr(sigPointer.ToInt64());
            for (int i = 0; i < pointerTree.Length; i++)    // 해당 길이만큼
            {
                ptr = ReadPointer(new IntPtr(ptr.ToInt64() + pointerTree[i]));
                if (ptr == IntPtr.Zero)
                    return IntPtr.Zero;
            }

            return ptr;
        }

        /*******************************************
         * 32비트 정수로 변환
         * 
         * @param offset                주소
         * @return  32비트 정수
        ********************************************/
        private static int ReadInt32(IntPtr offset)
        {
            byte[] lpBuffer = new byte[4];
            IntPtr zero = IntPtr.Zero;

            if (!BaseMethod.ReadProcessMemory(m_mainProcessPtr, offset, lpBuffer, new IntPtr(4), ref zero))
                return 0;

            return BitConverter.ToInt32(lpBuffer, 0);
        }

        /*******************************************
         * 주소로부터 바이트 읽기
         * 
         * @param offset                주소
         * @param length                길이
         * @return  바이트 배열
        ********************************************/
        private static byte[] ReadBytes(IntPtr offset, int length) {
            IntPtr zero = IntPtr.Zero;

            // 길이가 없거나 너무 길면 반환
            if ((length <= 0) || (length > 0x186a0))
                return null;

            // 주소가 없으면 반환
            if (offset == IntPtr.Zero)
                return null;

            // 읽자
            byte[] lpBuffer = new byte[length];
            BaseMethod.ReadProcessMemory(m_mainProcessPtr, offset, lpBuffer, new IntPtr(length), ref zero);

            return lpBuffer;
        }
    }
}
