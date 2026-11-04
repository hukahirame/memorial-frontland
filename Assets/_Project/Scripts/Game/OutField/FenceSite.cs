using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 柵を立てる所を、場の物と突き合わせて決める。格子のどの線に立てるかは FenceRule が決め、
    /// ここは地面の高さと、動かない物や立てる人の体との重なりを見る
    /// </summary>
    public static class FenceSite
    {
        /// <summary>体と柵のあいだに残すすき間</summary>
        private const float Margin = 0.05f;

        /// <summary>足元からこれより高い所と低い所には立てない。段の上や崖の下に立たないように</summary>
        private const float MaxStep = 0.5f;

        /// <summary>重なりを見るとき、柵の当たりの下をこれだけ持ち上げる。立っている地面は重なりに数えない</summary>
        private const float Lift = 0.05f;

        /// <param name="fence">立てる柵の prefab。当たりの大きさを使う</param>
        /// <param name="body">立てる人の体の当たり</param>
        /// <param name="side">立てる人が向いている横の側。左なら負、ほかは右</param>
        /// <returns>立てられる所が無ければ false</returns>
        public static bool TryFind(Fence fence, Collider body, float side, out Vector3 position, out float yaw)
        {
            Bounds bounds = body.bounds;
            Vector3 size = fence.Size;

            //向きで体の幅が違うので、大きい方の半分に、柵の厚みの半分とすき間を足して離す
            float clearance = Mathf.Max(bounds.extents.x, bounds.extents.z) + size.z / 2f + Margin;
            FenceSpot spot = FenceRule.Spot(bounds.center.x, bounds.center.z, side, clearance);

            yaw = spot.Yaw;
            position = default;
            if (!TryFindGround(spot.X, spot.Z, bounds.min.y, out float ground)) return false;

            position = new Vector3(spot.X, ground, spot.Z);
            return !IsTaken(fence, position, Quaternion.Euler(0f, yaw, 0f), body);
        }

        /// <summary>
        /// x, z の真下の地面の高さ。動く物（敵・プレイヤー・落とし物）は地面とみなさない。
        /// 柵は Ignore Raycast のレイヤーにあり、柵の上にも立てない
        /// </summary>
        private static bool TryFindGround(float x, float z, float feet, out float ground)
        {
            ground = feet;
            bool found = false;
            float nearest = float.MaxValue;

            Vector3 from = new Vector3(x, feet + MaxStep, z);
            foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, MaxStep * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody != null || hit.distance >= nearest) continue;

                nearest = hit.distance;
                ground = hit.point.y;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// 動かない物（ほかの柵・倒れた柵・壁・岩・見えない壁など）か、立てる人の体に重なるか。動く物（敵・落とし物）は柵が押しのける。
        /// 端と角で触れ合うのは重なりとみなさないよう、長さの向きを両端から厚みの半分とすき間ぶん縮めて見る
        /// </summary>
        private static bool IsTaken(Fence fence, Vector3 position, Quaternion rotation, Collider body)
        {
            Vector3 size = fence.Size;
            Vector3 half = new Vector3(size.x / 2f - size.z / 2f - Margin, (size.y - Lift) / 2f, size.z / 2f);
            Vector3 center = position + rotation * (fence.Center + Vector3.up * (Lift / 2f));

            foreach (Collider hit in Physics.OverlapBox(center, half, rotation, Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                //倒れた柵の当たりは素通りする当たりにしてあり、起き上がると重なるので、それも重なりに数える
                if (hit.isTrigger)
                {
                    if (hit.TryGetComponent(out Fence fallen) && fallen.IsFallen) return true;
                    continue;
                }

                if (hit == body || hit.attachedRigidbody == null) return true;
            }

            return false;
        }
    }
}
