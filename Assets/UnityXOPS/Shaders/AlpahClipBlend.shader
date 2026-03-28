 Shader "UnityXOPS/AlphaClipBlend"
  {
      Properties
      {
          _MainTex ("Texture", 2D)                     = "white" {}
          _Color   ("Tint", Color)                     = (1,1,1,1)
          _Cutoff  ("Alpha Cutoff", Range(0.001, 0.1)) = 0.004
          [Toggle] _ZWrite ("ZWrite", Float)           = 1
      }

      SubShader
      {
          Tags
          {
              "Queue"           = "AlphaTest"       // 2450 — Opaque 이후, Transparent 이전
              "RenderType"      = "TransparentCutout"
              "IgnoreProjector" = "True"
          }

          Blend SrcAlpha OneMinusSrcAlpha
          ZWrite [_ZWrite]   // 기본값 On
          Cull Back

          Pass
          {
              CGPROGRAM
              #pragma vertex   vert
              #pragma fragment frag
              #pragma multi_compile_fog

              #include "UnityCG.cginc"

              struct appdata
              {
                  float4 vertex : POSITION;
                  float2 uv     : TEXCOORD0;
                  fixed4 color  : COLOR;
              };

              struct v2f
              {
                  float4 pos   : SV_POSITION;
                  float2 uv    : TEXCOORD0;
                  fixed4 color : COLOR;
                  UNITY_FOG_COORDS(1)
              };

              sampler2D _MainTex;
              float4    _MainTex_ST;
              fixed4    _Color;
              float     _Cutoff;

              v2f vert(appdata v)
              {
                  v2f o;
                  o.pos   = UnityObjectToClipPos(v.vertex);
                  o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                  o.color = v.color * _Color;
                  UNITY_TRANSFER_FOG(o, o.pos);
                  return o;
              }

              fixed4 frag(v2f i) : SV_Target
              {
                  fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                  clip(col.a - _Cutoff);
                  UNITY_APPLY_FOG(i.fogCoord, col);
                  return col;
              }
              ENDCG
          }
      }

      FallBack "Transparent/Cutout/Diffuse"
  }