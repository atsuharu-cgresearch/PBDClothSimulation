Shader "MyShader/PBDCloth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct Particle
            {
                float3 x;
                float3 p;
                float3 v;
                float m;
            };

            StructuredBuffer<Particle> _Particles;

            struct appdata
            {
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Mesh頂点番号 == Particle番号
                float3 positionOS = _Particles[v.vertexID].x;

                o.positionCS = UnityObjectToClipPos(
                    float4(positionOS, 1.0)
                );

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

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
