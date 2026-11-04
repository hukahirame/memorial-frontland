// 塊を並べた地形の塗り。上を向いた面は地面の絵、横の面は崖の絵を、世界の座標から貼る（三方向の投影）。
// 塊ごとの継ぎ目が出ず、まとめて描いても（static batching）崩れない。下を向いた面と水の面より下は、上の面も崖の絵にする
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
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _TopTex;
        sampler2D _SideTex;
        fixed4 _TopTint;
        fixed4 _SideTint;
        float _TopScale;
        float _SideScale;
        float _Sharpness;
        float _Underwater;
        half _Glossiness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.worldNormal);
            float3 w = pow(abs(n), _Sharpness);
            w /= (w.x + w.y + w.z);

            fixed4 sideX = tex2D(_SideTex, IN.worldPos.zy / _SideScale) * _SideTint;
            fixed4 sideZ = tex2D(_SideTex, IN.worldPos.xy / _SideScale) * _SideTint;
            fixed4 below = tex2D(_SideTex, IN.worldPos.xz / _SideScale) * _SideTint;
            fixed4 ground = tex2D(_TopTex, IN.worldPos.xz / _TopScale) * _TopTint;

            float up = step(0, n.y) * step(_Underwater, IN.worldPos.y);
            fixed4 top = lerp(below, ground, up);

            o.Albedo = (sideX * w.x + top * w.y + sideZ * w.z).rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
