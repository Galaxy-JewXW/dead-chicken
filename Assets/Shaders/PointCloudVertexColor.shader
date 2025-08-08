// Unity点云顶点颜色着色器
// 基于Unity-Point-Cloud-Free-Viewer项目的VertexColor.shader
Shader "PowerlineSystem/PointCloudVertexColor" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _PointSize ("Point Size", Range(1, 10)) = 2
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass {
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            
            struct VertexInput {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
             
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
                float psize : PSIZE;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            float _PointSize;
             
            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color * _Color;
                o.psize = _PointSize;
                return o;
            }
             
            float4 frag(VertexOutput i) : SV_Target {
                return i.col;
            }
            
            ENDCG
        }
        
        // 阴影投射Pass
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            
            #include "UnityCG.cginc"
            
            struct v2f {
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata_base v) {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : SV_Target {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    
    // 简化版本（不支持阴影）
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct VertexInput {
                float4 vertex : POSITION;
                float4 color: COLOR;
            };
             
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };
            
            float4 _Color;
             
            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color;
                return o;
            }
             
            float4 frag(VertexOutput o) : COLOR {
                return o.col;
            }
            
            ENDCG
        } 
    }
    
    FallBack "Diffuse"
} 