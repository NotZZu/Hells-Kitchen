using UnityEngine;

namespace HellsKitchen.Runtime.Inputs
{
    /// <summary>
    /// "플레이어가 이번 프레임에 무엇을 하려 했는가"를 담는 값.
    ///
    /// 키보드/게임패드/터치 어느 쪽에서 왔는지는 여기서 사라진다.
    /// 나중에 온라인 협동으로 갈 때 서버로 보내는 것도 결국 이 구조체이므로,
    /// 캐릭터 코드는 절대 입력 장치를 직접 읽지 않는다.
    /// </summary>
    public struct PlayerIntent
    {
        /// <summary>이동 방향. 길이는 0~1.</summary>
        public Vector2 Move;

        /// <summary>집기/놓기 버튼을 이번 프레임에 눌렀다.</summary>
        public bool InteractPressed;

        /// <summary>작업(썰기) 버튼을 누르고 있다.</summary>
        public bool WorkHeld;

        /// <summary>작업 버튼을 이번 프레임에 눌렀다.</summary>
        public bool WorkPressed;

        /// <summary>달리기 버튼을 누르고 있다.</summary>
        public bool SprintHeld;

        /// <summary>내려놓기/던지기 버튼을 누르고 있다.</summary>
        public bool DropHeld;

        /// <summary>내려놓기/던지기 버튼을 이번 프레임에 뗐다.</summary>
        public bool DropReleased;

        /// <summary>취소 버튼을 이번 프레임에 눌렀다.</summary>
        public bool CancelPressed;

        public static readonly PlayerIntent None = new PlayerIntent();

        /// <summary>버튼의 '이번 프레임에 눌림/뗌' 플래그만 지운다. 소비 후 호출한다.</summary>
        public void ConsumePresses()
        {
            InteractPressed = false;
            WorkPressed = false;
            DropReleased = false;
            CancelPressed = false;
        }
    }

    /// <summary>
    /// 인텐트 공급자. 로컬 입력, AI, 네트워크 수신, 테스트용 스크립트가
    /// 모두 이 인터페이스를 구현해서 같은 캐릭터를 굴릴 수 있다.
    /// </summary>
    public interface IIntentSource
    {
        PlayerIntent Current { get; }

        /// <summary>매 프레임 캐릭터가 인텐트를 읽은 뒤 호출한다.</summary>
        void ConsumePresses();
    }
}
