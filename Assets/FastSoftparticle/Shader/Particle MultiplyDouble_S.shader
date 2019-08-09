// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Particles/Fast/Multiply (Double)" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	[Toggle(FASTSOFTPARTICLE_ON)] FASTSOFTPARTICLE_ON("Fast Softparticle Enable", Float) = 0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend DstColor SrcColor
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			//#pragma multi_compile_particles
			#pragma multi_compile __ SOFTPARTICLES_ON FASTSOFTPARTICLE_ON
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#ifndef UNITY_DECLARE_DEPTH_TEXTURE
			#define UNITY_DECLARE_DEPTH_TEXTURE(A) sampler2D_float A
			#endif

			sampler2D _MainTex;
			fixed4 _TintColor;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				#ifdef FASTSOFTPARTICLE_ON
				float4 plane : TEXCOORD1;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD2;
				#endif
				#ifdef FASTSOFTPARTICLE_ON
				float depth : TEXCOORD2;
				#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				#endif
				#ifdef FASTSOFTPARTICLE_ON
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.depth = dot(v.plane, float4(worldPos, 1));
				#endif
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float _InvFade;
			
			fixed4 frag (v2f i) : SV_Target
			{
				#ifdef SOFTPARTICLES_ON
				float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				float partZ = i.projPos.z;
				float fade = saturate (_InvFade * (sceneZ-partZ));
				i.color.a *= fade;
				#endif
				#ifdef FASTSOFTPARTICLE_ON
				i.color.a *= saturate(i.depth);
				#endif

				fixed4 col;
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				col.rgb = tex.rgb * i.color.rgb * 2;
				col.a = i.color.a * tex.a;
				col = lerp(fixed4(0.5f,0.5f,0.5f,0.5f), col, col.a);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0.5,0.5,0.5,0.5)); // fog towards gray due to our blend mode
				return col;
			}
			ENDCG 
		}
	}
}
}
