// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Project/TransparentGlobalAlpha" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Alpha ("Texture alpha", Float) = 1.0
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	Lighting Off
	CGPROGRAM
	#pragma surface surf BlinnPhong
	sampler2D _MainTex;
	float     _Alpha;
	
	struct Input {
		float2 uv_MainTex;
	};
	void surf( Input IN, inout SurfaceOutput o )
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = c.rgb;
		o.Alpha = c.a*_Alpha*1.5;
	}
	ENDCG
	
//	Pass {
//		Lighting Off
//		SetTexture [_MainTex] { combine texture } 
//	}

}
FallBack "Diffuse"
}
