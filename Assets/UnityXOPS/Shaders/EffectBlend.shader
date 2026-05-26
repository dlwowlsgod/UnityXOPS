Shader "UnityXOPS/EffectBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D)     = "white" {}
        _Color   ("Tint", Color)     = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"   // 3000 — 거리 정렬 반투명
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        // 원본 이펙트: 표준 알파 블렌딩 + Z-write off (가산 아님). 깊이 테스트는 유지(벽 뒤 가려짐).
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull  Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 인스턴스별 페이드 알파는 MaterialPropertyBlock 으로 주입되는 _Color.a 가 담당.
            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
