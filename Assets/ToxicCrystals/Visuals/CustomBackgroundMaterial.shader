// Combination of Mobile/Diffuse and LightingLambert, but added in support
// for blending with a second unlit texture.

// Mobile/Diffuse:
// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color

Shader "KT/Custom Toxic Crystals Background" {
Properties {
    _UvOffset ("UV Vertical Offset", Range (0,1)) = 0
	_HighlightStrength ("Highlight Strength", Range (0,1)) = 0
	_BlueLinesTex ("Blue Lines (RGB)", 2D) = "white" {}
    _BackgroundColorsTex ("Background Colors (RGB)", 2D) = "white" {}
	_CasingHighlight ("Casing Highlight (R)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 150

CGPROGRAM
// Mobile improvement: noforwardadd
// http://answers.unity3d.com/questions/1200437/how-to-make-a-conditional-pragma-surface-noforward.html
// http://gamedev.stackexchange.com/questions/123669/unity-surface-shader-conditinally-noforwardadd
#pragma surface surf BlendLitUnlit

half _UvOffset;
half _HighlightStrength;
sampler2D _BlueLinesTex;
sampler2D _BackgroundColorsTex;
sampler2D _CasingHighlight;

struct Input {
	float2 uv_BlueLinesTex;
};

struct SurfaceOutputCustom {
    half3 Albedo;
    half3 Normal;
    half3 Emission;
    half Specular;
    half Gloss;
    half Alpha;
    // Custom fields:
	half3 Lines;
    half3 Background;
	half highlight;
};

void surf (Input IN, inout SurfaceOutputCustom o) {
	float2 u = fixed2(0, _UvOffset);
	fixed4 c = tex2D(_BlueLinesTex, IN.uv_BlueLinesTex);
	o.Albedo = 0.18;
	o.Lines = c.rgb;
	o.Alpha = c.a;
	o.Background = tex2D(_BackgroundColorsTex, IN.uv_BlueLinesTex + u);
	o.highlight = tex2D(_CasingHighlight, IN.uv_BlueLinesTex).r;
}

half4 LightingBlendLitUnlit (SurfaceOutputCustom s, half3 lightDir, half atten)
{
	fixed diff = max (0, dot (s.Normal, lightDir));
	
	fixed4 c;
	c.rgb = ((s.highlight * _HighlightStrength) + (s.Lines * s.Alpha) + (s.Background * (1 - s.Alpha))) * _LightColor0.rgb * (diff * atten);
	c.a = 1;
	
	return c;
}


ENDCG
}

Fallback "Mobile/Diffuse"
}
