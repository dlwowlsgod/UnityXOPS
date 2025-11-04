// Custom Skybox/6 Sided shader with texture transform support
Shader "UnityXOPS/Skybox" {
Properties {
    [HDR] _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    _FrontTex ("Front (+Z)", 2D) = "white" {}
    _BackTex ("Back (-Z)", 2D) = "white" {}
    _LeftTex ("Left (+X)", 2D) = "white" {}
    _RightTex ("Right (-X)", 2D) = "white" {}
    _UpTex ("Up (+Y)", 2D) = "white" {}
    _DownTex ("Down (-Y)", 2D) = "white" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {
        
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        sampler2D _FrontTex;
        sampler2D _BackTex;
        sampler2D _LeftTex;
        sampler2D _RightTex;
        sampler2D _UpTex;
        sampler2D _DownTex;
        
        float4 _FrontTex_ST;
        float4 _BackTex_ST;
        float4 _LeftTex_ST;
        float4 _RightTex_ST;
        float4 _UpTex_ST;
        float4 _DownTex_ST;

        half4 _Tint;

        struct appdata_t {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 pos : SV_POSITION;
            float3 viewDir : TEXCOORD0;
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.viewDir = v.vertex.xyz;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            float3 dir = i.viewDir;
            float3 absdir = abs(dir);
            float2 tc;
            float2 uv;
            fixed4 col;

            // Z-axis dominant face (Front/Back)
            if (absdir.z >= absdir.x && absdir.z >= absdir.y) {
                if (dir.z > 0) {
                    tc = float2(dir.x, dir.y) / absdir.z;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_FrontTex, TRANSFORM_TEX(uv, _FrontTex));
                } else {
                    tc = float2(-dir.x, dir.y) / absdir.z;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_BackTex, TRANSFORM_TEX(uv, _BackTex));
                }
            }
            // X-axis dominant face (Left/Right)
            else if (absdir.x > absdir.y) {
                 if (dir.x > 0) { // +X face is 'Left'
                    tc = float2(-dir.z, dir.y) / absdir.x;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_LeftTex, TRANSFORM_TEX(uv, _LeftTex));
                } else { // -X face is 'Right'
                    tc = float2(dir.z, dir.y) / absdir.x;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_RightTex, TRANSFORM_TEX(uv, _RightTex));
                }
            }
            // Y-axis dominant face (Up/Down)
            else {
                if (dir.y > 0) {
                    tc = float2(dir.x, -dir.z) / absdir.y;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_UpTex, TRANSFORM_TEX(uv, _UpTex));
                } else {
                    tc = float2(dir.x, dir.z) / absdir.y;
                    uv = tc * 0.5 + 0.5;
                    col = tex2D(_DownTex, TRANSFORM_TEX(uv, _DownTex));
                }
            }
            
            return col * _Tint;
        }
        ENDCG
    }
}

Fallback Off

}