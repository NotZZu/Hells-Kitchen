using HellsKitchen.Runtime.Stations;
using UnityEngine;

namespace HellsKitchen.Runtime.Items
{
    /// <summary>
    /// 던져진 물건의 비행을 처리한다.
    ///
    /// 물리 엔진에 맡기지 않고 직접 굴리는 이유는, 결과가 예측 가능해야 하기 때문이다.
    /// 던진 재료가 굴러다니다 엉뚱한 데로 사라지면 플레이어는 던지기를 쓰지 않는다.
    /// 여기서는 포물선으로 날아가다 설비에 닿으면 그 위에 얹히고,
    /// 아무것도 못 만나면 바닥에 떨어져 다시 주울 수 있는 상태가 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ThrownItem : MonoBehaviour
    {
        private const float Gravity = -22f;
        private const float GroundY = 0f;

        /// <summary>설비에 얹힐 수 있는지 검사하는 기본 반경.</summary>
        private const float CatchRadius = 0.7f;

        /// <summary>
        /// 유도 대상을 찾는 반경.
        /// 반경 안이라고 곧바로 끌려가지는 않는다. 세기가 근접도의 제곱에 비례하므로
        /// 가장자리에서는 거의 영향이 없고 설비 앞에서만 확 당겨진다.
        /// 반경은 "후보로 볼 범위", 근접도 곡선은 "붙는 느낌"을 각각 맡는다.
        /// </summary>
        private const float HomingRadius = 2.2f;

        /// <summary>유도 세기. 붙는 순간에는 확실하게 당긴다.</summary>
        private const float HomingStrength = 11f;

        /// <summary>
        /// 거리 계산에서 높이 차이에 곱하는 가중치.
        /// 던진 물건은 포물선을 그리므로 조리대 위를 지날 때 높이가 얼마간 어긋난다.
        /// 수직 오차에 관대해야 실제로 얹힌다.
        /// </summary>
        private const float VerticalTolerance = 0.45f;

        /// <summary>이 시간이 지나면 어디에 있든 떨어뜨린다. 영원히 날아가는 것을 막는다.</summary>
        private const float MaxFlightSeconds = 4f;

        private static readonly Collider[] Hits = new Collider[16];

        private Carryable _item;
        private Vector3 _velocity;
        private float _elapsed;
        private Vector3 _spin;

        /// <summary>물건을 던진다. 이미 손에서 떨어져 있어야 한다.</summary>
        public static void Launch(Carryable item, Vector3 origin, Vector3 velocity)
        {
            if (item == null) return;

            item.Detach();
            item.transform.position = origin;

            var thrown = item.gameObject.GetComponent<ThrownItem>();
            if (thrown == null) thrown = item.gameObject.AddComponent<ThrownItem>();

            thrown._item = item;
            thrown._velocity = velocity;
            thrown._elapsed = 0f;
            item.IsFlying = true;

            // 날아가는 동안에도 얇은 재료가 보여야 한다.
            if (item is IngredientItem ingredient) ingredient.SetGroundMode(true);
            thrown._spin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 320f;
        }

        private void Update()
        {
            if (_item == null)
            {
                Destroy(this);
                return;
            }

            var dt = Time.deltaTime;
            _elapsed += dt;

            ApplyHoming(transform.position, dt);

            _velocity.y += Gravity * dt;
            var next = transform.position + _velocity * dt;

            // 날아가는 동안 빙글빙글 돌려 준다. 던졌다는 느낌이 확실해진다.
            transform.Rotate(_spin * dt, Space.World);

            // 설비에 닿았는지 먼저 본다. 바닥보다 우선이다.
            if (TryLandOnStation(next)) return;

            if (next.y <= GroundY)
            {
                next.y = GroundY;
                LandOnGround(next);
                return;
            }

            transform.position = next;

            if (_elapsed >= MaxFlightSeconds)
            {
                var resting = transform.position;
                resting.y = GroundY;
                LandOnGround(resting);
            }
        }

        /// <summary>
        /// 지금 재료가 가야 할 곳으로 궤도를 살짝 휘게 한다.
        ///
        /// 어디로 가야 하는지는 설비가 스스로 답한다(GetAttraction).
        /// 그래서 원물은 도마로, 구울 것은 화로로, 손질을 마친 재료는
        /// 접시가 놓인 자리로 자연스럽게 빨려 든다.
        /// 조준을 정확히 하지 않아도 손이 바쁜 와중에 던지기가 쓸모 있어진다.
        /// </summary>
        private void ApplyHoming(Vector3 position, float dt)
        {
            var target = FindStation(position, HomingRadius, out var attraction);
            if (target == null || attraction <= 1f) return;

            var toTarget = target.PlacePoint.position - position;
            var distance = WeightedDistance(target.PlacePoint.position, position);
            if (distance < 0.02f) return;

            // 가까울수록 급격히 강해진다.
            // 반경 가장자리에서는 거의 영향이 없다가 코앞에서 확 당겨지므로,
            // 멀리서 빨려 드는 느낌 대신 "착 붙는" 느낌이 난다.
            var closeness = 1f - Mathf.Clamp01(distance / HomingRadius);
            var pull = closeness * closeness;

            var desired = toTarget.normalized * _velocity.magnitude;
            var t = Mathf.Clamp01(HomingStrength * (attraction - 1f) * pull * dt);
            _velocity = Vector3.Lerp(_velocity, desired, t);
        }

        /// <summary>
        /// 근처 설비에 얹어 본다. 설비가 받아 주면 비행이 끝난다.
        ///
        /// 받아 주는 판단은 전부 설비 쪽 규칙에 맡긴다.
        /// 그래서 접시가 놓인 조리대에 재료를 던지면 접시에 담기고,
        /// 도마에 못 쓰는 재료를 던지면 튕겨 나가 계속 날아간다.
        /// </summary>
        private bool TryLandOnStation(Vector3 position)
        {
            // 끌리는 정도가 큰 설비는 조금 멀어도 받아 준다.
            var best = FindStation(position, CatchRadius * 1.8f, out var attraction);
            if (best == null) return false;

            var reach = CatchRadius * Mathf.Clamp(0.95f + attraction * 0.2f, 0.95f, 1.65f);
            if (WeightedDistance(best.PlacePoint.position, position) > reach) return false;

            // 설비가 받아 줄지 판단하기 전에 비행 상태를 내려 둔다.
            // 설비 쪽에서 이 물건을 다시 조회할 때 '날아가는 중'으로 보이면 안 된다.
            _item.IsFlying = false;

            if (!best.TryPlace(_item))
            {
                _item.IsFlying = true;
                return false;
            }

            Finish();
            return true;
        }

        /// <summary>반경 안에서 가장 끌리는 설비를 찾는다. 같은 값이면 가까운 쪽.</summary>
        private Station FindStation(Vector3 position, float radius, out float attraction)
        {
            attraction = 0f;

            var count = Physics.OverlapSphereNonAlloc(
                position, radius, Hits, ~0, QueryTriggerInteraction.Collide);

            Station best = null;
            var bestScore = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                if (Hits[i] == null) continue;

                var station = Hits[i].GetComponentInParent<Station>();
                if (station == null) continue;

                var pull = station.GetAttraction(_item);
                if (pull <= 0f) continue;

                var distance = WeightedDistance(station.PlacePoint.position, position);
                var score = pull * 4f - distance;

                if (score <= bestScore) continue;

                bestScore = score;
                best = station;
                attraction = pull;
            }

            return best;
        }

        /// <summary>높이 차이를 덜 세는 거리. 포물선 궤도의 수직 오차를 감안한다.</summary>
        private static float WeightedDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y *= VerticalTolerance;
            return delta.magnitude;
        }

        private void LandOnGround(Vector3 position)
        {
            _item.PlaceOnGround(position);
            Finish();
        }

        private void Finish()
        {
            if (_item != null) _item.IsFlying = false;
            _item = null;
            Destroy(this);
        }
    }
}
