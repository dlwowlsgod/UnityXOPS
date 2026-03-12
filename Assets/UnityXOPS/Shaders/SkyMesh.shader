Shader "UnityXOPS/SkyMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" }

        Cull Back
        ZWrite Off
        // Background 큐라 씬 오브젝트보다 먼저 렌더링되므로 ZTest Always 사용
        // ZTest LEqual은 Unity 6 Reversed-Z(far=0.0) 환경에서 항상 실패함
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                // 카메라 위치에 메쉬를 고정하여 스카이박스처럼 따라다니게 함
                // 원본 XOPS 기준으로 180도 회전 보정 (Y축 기준: X, Z 반전)
                float3 skyVertex = float3(-v.vertex.x, v.vertex.y, -v.vertex.z);
                float3 worldPos = _WorldSpaceCameraPos + skyVertex;
                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
