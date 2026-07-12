// 화면 전체 밝기/감마 조정용 포스트 블릿 셰이더(Built-in RP OnRenderImage에서 사용).
// Linear 컬러스페이스: tex2D는 선형값을 반환하고 dst 기록 시 자동 sRGB 인코딩되므로 여기선 선형에서 곱/pow만 한다.
Shader "UnityXOPS/ScreenColorAdjust"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Float) = 1
        _Gamma ("Gamma", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Brightness;
            float _Gamma;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb *= _Brightness;                  // 밝기 = 곱(노출)
                c.rgb = pow(max(c.rgb, 0.0), _Gamma);  // 감마(모니터 관례): 1.0=중립, 클수록 어둡게, 작을수록 밝게
                return c;
            }
            ENDCG
        }
    }
}
