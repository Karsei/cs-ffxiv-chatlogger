using System;
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
        /********************************************************
         * 
         * [SIGNATURE]
         * 
         ********************************************************/
        // 64비트
        private static readonly Signature SigX64 = new Signature(
            true,
            "e8********85c0740e488b0d********33D2E8********488b0d",
            new long[] { 0, 0x30, 0x3D8 },  // 오프셋 시작 주소
            new long[] { 0, 0x30, 0x3E0 },  // 오프셋 끝 주소
            new long[] { 0, 0x30, 0x3C0 },  // 오프셋 시작 길이
            new long[] { 0, 0x30, 0x3C8 }); // 오프셋 끝 길이
        // 32비트
        private static readonly Signature SigX86 = new Signature(
            false,
            "8b55fc83e2f983ca098b4d08a1********515250E8********a1",
            new long[] { 0, 0x18, 0x2EC },  // 오프셋 시작 주소
            new long[] { 0, 0x18, 0x2F0 },  // 오프셋 끝 주소
            new long[] { 0, 0x18, 0x2DC },  // 오프셋 시작 길이
            new long[] { 0, 0x18, 0x2E0 }); // 오프셋 끝 길이

        /********************************************************
         * 
         * [PROCESS RESOURCE]
         * 
         ********************************************************/
        // 프로세스 목록
        public static readonly ObservableCollection<string> ProcessList         = new ObservableCollection<string>();

        // 메모리 패턴 구분
        private static Signature m_memPattern;
        // 메인 프로세스
        private static Process m_mainProcess;
        // 메인 프로세스 포인터
        private static IntPtr m_mainProcessPtr;
        // 프로세스 기본 주소
        private static IntPtr m_basePtr;
        // 패턴으로 찾아진 채팅 로그 주소
        private static IntPtr m_chatLogPtr;
        // 운영체제 비트 구분
        private static bool m_isX64;

        /********************************************************
         * 
         * [THREADS]
         * 
         ********************************************************/
        // 스레드 작업
        private static readonly ManualResetEvent Work = new ManualResetEvent(false);

        /********************************************************
         * 
         * [LIST DATA]
         * 
         ********************************************************/
        // 채팅 로그
        public static readonly ObservableCollection<Chat> ChatLog               = new ObservableCollection<Chat>();
        public static ObservableCollection<ChatType> ChatFilterLog              = new ObservableCollection<ChatType>();
        public static ObservableCollection<ChatType> ChatFilterTransLog         = new ObservableCollection<ChatType>();
        
        // 타임스탬프
        private static readonly DateTime BaseTimeStamp                          = new DateTime(1970, 1, 1, 0, 0, 0);
        // 윈도우 알림
        public static WinForms.NotifyIcon notify;

        /// <summary>
        /// 채팅 수집 프로세스를 초기화합니다.
        /// </summary>
        [STAThread]
        public static void Init()
        {
            // 트레이 아이콘
            notify = new WinForms.NotifyIcon();
            notify.Icon = ffxiv_chatlogger.Properties.Resources.hp_notepad2_pencil;
            notify.Visible = true;
            notify.BalloonTipTitle = "FFXIV 채팅 수집기";
            notify.Text = "FFXIV 채팅 수집기";

            //** 등록된 채팅 종류를 제공 채팅 목록 필터 목록에 추가 **//
            if (Logger.ChatFilterLog.Count <= 0)
            {
                foreach (var item in ChatType.TypeList.Values)
                {
                    // 필터링
                    if (item.GetId == 0x0839) continue;
                    else if (item.GetId == 0x0840) continue;

                    // 목록 추가
                    Logger.ChatFilterLog.Add(item);
                }
            }

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

        /// <summary>
        /// FFXIV 프로세스의 이름을 구분하고 프로세스 목록에 넣어 설정합니다.
        /// </summary>
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
                // 프로세스를 찾을 수 없음
                //notify.BalloonTipText = "FFXIV 프로세스를 찾을 수 없어 30초 후에 다시 시도합니다.";
                //notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스를 찾을 수 없습니다!");
            }
            else if (ffxivPrc.Count > 1)
            {
                // 프로세스가 2개 이상으로 직접 선택해서 설정해야 함
                notify.BalloonTipText = "FFXIV 프로세스가 2개 이상이므로 직접 프로세스를 설정하시기 바랍니다.";
                notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스가 2개 이상이므로 직접 프로세스를 설정하시기 바랍니다.");

                // 옵션의 프로세스 목록에 추가
                foreach (var process in ffxivPrc)
                    using (process)
                        ProcessList.Add(string.Format("{0}:{1}", process.ProcessName, process.Id));
            }
            else
            {
                // 프로세스가 1개이므로 바로 설정
                notify.BalloonTipText = "FFXIV 프로세스를 발견했습니다!";
                notify.ShowBalloonTip(1000);
                LogWriter.Info("FFXIV 프로세스를 찾았습니다! 설정을 시작합니다...");

                // 메모리 패턴 분석 시작
                SetFFXIVProcess(ffxivPrc[0]);
            }
        }

        /// <summary>
        /// 선택된 FFXIV 프로세스의 주소를 찾아가서 메모리 패턴을 분석합니다.
        /// <para>process - FFXIV 프로세스</para>
        /// </summary>
        /// <param name="process">FFXIV 프로세스</param>
        private static void SetFFXIVProcess(Process process)
        {
            // 이미 주 프로세스가 설정되어 있는 경우 프로세스와 연관된 리소스를 해제시킴
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

                // 메모리 패턴 분석하여 포인터 주소 가져옴
                m_chatLogPtr = m_memPattern.Scan(m_mainProcess, m_mainProcessPtr);
                
                if (m_chatLogPtr != IntPtr.Zero)
                {
                    // 스레드 시작
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

        /// <summary>
        /// FFXIV 프로세스를 다시 찾는 과정을 실행합니다.
        /// </summary>
        public static void ResetFFXIVProcess()
        {
            try
            {
                // 주 프로세스 초기화
                m_mainProcess = null;
                Work.Reset();

                // 프로세스 다시 찾음
                FindFFXIVProcess();
            }
            catch (Exception e)
            {
                LogWriter.Error("프로세스를 다시 설정하기 위한 작업에서 오류가 발생했습니다.", e);
            }
        }

        /// <summary>
        /// FFXIV 프로세스를 직접 설정합니다.
        /// </summary>
        /// <param name="pid"></param>
        public static void SelectFFXIVProcess(int pid)
        {
            try
            {
                // 프로세스 선택
                SetFFXIVProcess(Process.GetProcessById(pid));
            }
            catch
            {
                LogWriter.Error("프로세스를 설정하는 과정에서 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 시그네쳐 패턴을 가지고 채팅 로그 부분의 메모리를 분석합니다.
        /// </summary>
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
                            throw new ApplicationException("[오류] 오프셋의 끝 포인터가 시작 포인터보다 앞에 있습니다. 이 오류가 나타난 경우 메모리 오프셋 주소가 맞지 않아 나타날 확률이 높습니다.");

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
                            throw new ApplicationException("[오류] 올바르지 않은 배열의 길이입니다.");

                        if ((offsetLenEnd.ToInt64() - offsetLenStart.ToInt64()) > 0xfa0L)
                            throw new ApplicationException("[오류] 배열의 길이가 너무 작습니다.");

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

        /// <summary>
        /// 오버레이 출력 목록에 헤딩 메세지 종류 번호가 있는지 검사합니다.
        /// </summary>
        /// <param name="rawData">채팅 문자열</param>
        /// <returns>채팅 메세지 종류에 해당 메세지 타입이 있다면 true, 아니면 false</returns>
        private static bool CheckMessage(byte[] rawData)
        {
            // 임시
            /*int pos;
            var charName = GetCharacterName(rawData, 9, 0x3A, out pos);
            var text = GetChatString(rawData, pos, rawData.Length);
            Console.WriteLine("채팅 종류: {0} / 내용: {1}", BitConverter.ToInt16(rawData, 4), text);*/

            //return ChatType.TypeList.ContainsKey(BitConverter.ToInt16(rawData, 4));

            // 문자열로 구성된 채팅 메세지 종류를 정수로 변환하여 출력 목록에 있는지 검사
            foreach (var item in ChatFilterLog)
                if (item.GetId == BitConverter.ToInt16(rawData, 4))
                    return true;

            return false;
        }

        /// <summary>
        /// 채팅 번역 출력 목록에 헤딩 메세지 종류 번호가 있는지 검사합니다.
        /// </summary>
        /// <param name="type">채팅 종류</param>
        /// <returns>채팅 메세지 종류 번역 목록에 해당 메세지 타입이 있다면 true, 아니면 false</returns>
        private static bool CheckTransMessage(ChatType type)
        {
            bool enable = Settings.globalSetting.enableTransService != 0 ? true : false;
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
            //Console.WriteLine(BitConverter.ToString(rawData).Replace("-", " "));

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

        /// <summary>
        /// 현재 프로세스 메모리의 주어진 오프셋으로부터 32비트 정수값을 얻습니다.
        /// </summary>
        /// <param name="offset">오프셋 주소</param>
        /// <returns>32비트 부호있는 정수</returns>
        private static int ReadInt32(IntPtr offset)
        {
            byte[] lpBuffer = new byte[4];
            IntPtr zero = IntPtr.Zero;

            if (!BaseMethod.ReadProcessMemory(m_mainProcessPtr, offset, lpBuffer, new IntPtr(4), ref zero))
                return 0;

            return BitConverter.ToInt32(lpBuffer, 0);
        }

        /// <summary>
        /// 현재 프로세스 메모리의 주어진 오프셋으로부터 문자열을 얻습니다.
        /// </summary>
        /// <param name="offset">오프셋 주소</param>
        /// <param name="length">길이</param>
        /// <returns>바이트 배열</returns>
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
