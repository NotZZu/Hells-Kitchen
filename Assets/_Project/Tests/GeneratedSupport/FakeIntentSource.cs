using HellsKitchen.Runtime.Inputs;
using UnityEngine;

namespace HellsKitchen.Tests.Generated
{
    /// <summary>
    /// 생성 시험이 쓰는 가짜 입력. 필드를 setField 로 바꾸면 Current 에 그대로 반영된다.
    /// 캐릭터는 입력 장치를 직접 읽지 않고 IIntentSource 만 읽으므로, 이것을 끼워 넣으면 키 입력을 흉내 낼 수 있다.
    /// </summary>
    public class FakeIntentSource : MonoBehaviour, IIntentSource
    {
        public bool DropHeld;
        public bool DropReleased;
        public bool CancelPressed;
        public bool InteractPressed;
        public bool WorkHeld;
        public bool WorkPressed;
        public bool SprintHeld;
        public float MoveX;
        public float MoveY;

        public PlayerIntent Current => new PlayerIntent
        {
            Move = new Vector2(MoveX, MoveY),
            InteractPressed = InteractPressed,
            WorkHeld = WorkHeld,
            WorkPressed = WorkPressed,
            SprintHeld = SprintHeld,
            DropHeld = DropHeld,
            DropReleased = DropReleased,
            CancelPressed = CancelPressed,
        };

        public void ConsumePresses()
        {
            InteractPressed = false;
            WorkPressed = false;
            DropReleased = false;
            CancelPressed = false;
        }
    }
}
