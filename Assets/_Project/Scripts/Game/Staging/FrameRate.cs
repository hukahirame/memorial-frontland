using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 描画の回数。モバイルは指定しないと 30fps に抑えられる。
    /// ブラウザ（WebGL）では指定しない。指定すると画面の書き換えに合わせて描かなくなり、スマホのブラウザでもかくつく
    /// </summary>
    public static class FrameRate
    {
        /// <summary>モバイルで狙う 1 秒あたりの描画回数</summary>
        public const int Mobile = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            if (Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer) Application.targetFrameRate = Mobile;
        }
    }
}
