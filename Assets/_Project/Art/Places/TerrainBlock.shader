// 塊を並べた地形の塗り。上を向いた面は地面の絵、横の面は崖の絵を、世界の座標から貼る（三方向の投影）。
// 塊ごとの継ぎ目が出ず、まとめて描いても（static batching）崩れない。下を向いた面と水の面より下は、上の面も崖の絵にする。
// _SEAM を点けた素材は、場所の境目の前後で隣の場所の塗り（Next の絵）へまだらに移る。境目の手前の端では自分の塗り、向こうの端では隣の塗りと同じになる
Shader "MemorialFloor/TerrainBlock"
{
    Properties
    {
        _TopTex ("Top", 2D) = "white" {}
        _SideTex ("Side", 2D) = "white" {}
        _TopTint ("Top Tint", Color) = (1, 1, 1, 1)
        _SideTint ("Side Tint", Color) = (1, 1, 1, 1)
        _TopScale ("Top Tile (world units)", Float) = 3
        _SideScale ("Side Tile (world units)", Float) = 3
        _Sharpness ("Blend Sharpness", Float) = 6
        _Underwater ("Underwater Height", Float) = -1000
        _Glossiness ("Smoothness", Range(0, 1)) = 0.1

        [Toggle(_SEAM)] _Seam ("Seam", Float) = 0
        _NextTopTex ("Next Top", 2D) = "white" {}
        _NextSideTex ("Next Side", 2D) = "white" {}
        _NextTopTint ("Next Top Tint", Color) = (1, 1, 1, 1)
        _NextSideTint ("Next Side Tint", Color) = (1, 1, 1, 1)
        _NextTopScale ("Next Top Tile (world units)", Float) = 3
        _NextSideScale ("Next Side Tile (world units)", Float) = 3
        _NextUnderwater ("Next Underwater Height", Float) = -1000
        _NextGlossiness ("Next Smoothness", Range(0, 1)) = 0.1
        _SeamX ("Seam X (world)", Float) = 0
        _SeamWidth ("Seam Half Width", Float) = 3
        _SeamGrain ("Seam Patch Size", Float) = 1.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma shader_feature_local _SEAM

        sampler2D _TopTex;
        sampler2D _SideTex;
        fixed4 _TopTint;
        fixed4 _SideTint;
        float _TopScale;
        float _SideScale;
        float _Sharpness;
        float _Underwater;
        half _Glossiness;

        #if defined(_SEAM)
        sampler2D _NextTopTex;
        sampler2D _NextSideTex;
        fixed4 _NextTopTint;
        fixed4 _NextSideTint;
        float _NextTopScale;
        float _NextSideScale;
        float _NextUnderwater;
        half _NextGlossiness;
        float _SeamX;
        float _SeamWidth;
        float _SeamGrain;
        #endif

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        // 面の向きの重み（w）で、横・上・奥の 3 方向から貼った絵を混ぜる
        #define PAINT(topTex, sideTex, topTint, sideTint, topScale, sideScale, underwater) \
            (( tex2D(sideTex, p.zy / sideScale) * sideTint * w.x \
             + lerp(tex2D(sideTex, p.xz / sideScale) * sideTint, tex2D(topTex, p.xz / topScale) * topTint, step(0, n.y) * step(underwater, p.y)) * w.y \
             + tex2D(sideTex, p.xy / sideScale) * sideTint * w.z ).rgb)

        #if defined(_SEAM)
        float Hash(float2 cell)
        {
            return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
        }

        // 格子の角に乱数を置いて滑らかにつないだ、0 から 1 の値
        float ValueNoise(float2 at)
        {
            float2 cell = floor(at);
            float2 f = frac(at);
            float2 u = f * f * (3 - 2 * f);

            return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), u.x),
                        lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), u.x), u.y);
        }

        // 隣の塗りの割合。境目の手前の端（_SeamX - _SeamWidth）で 0、向こうの端で 1。間はまだらに増える。
        // 境目の両側の素材が同じ式で決まるので、塊の継ぎ目でも場所の継ぎ目でも途切れない
        float NextShare(float3 p)
        {
            const float soft = 0.12;
            float t = saturate((p.x - _SeamX) / (2 * _SeamWidth) + 0.5);
            float grain = ValueNoise(p.xz / _SeamGrain) * 0.65 + ValueNoise(p.xz / _SeamGrain * 2.7 + 17.3) * 0.35;
            float reach = t * (1 + 2 * soft) - soft;

            return saturate((reach - grain) / soft + 0.5);
        }
        #endif

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 p = IN.worldPos;
            float3 n = normalize(IN.worldNormal);
            float3 w = pow(abs(n), _Sharpness);
            w /= (w.x + w.y + w.z);

            fixed3 albedo = PAINT(_TopTex, _SideTex, _TopTint, _SideTint, _TopScale, _SideScale, _Underwater);
            half smoothness = _Glossiness;

            #if defined(_SEAM)
            float next = NextShare(p);
            albedo = lerp(albedo, PAINT(_NextTopTex, _NextSideTex, _NextTopTint, _NextSideTint, _NextTopScale, _NextSideScale, _NextUnderwater), next);
            smoothness = lerp(smoothness, _NextGlossiness, next);
            #endif

            o.Albedo = albedo;
            o.Metallic = 0;
            o.Smoothness = smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
