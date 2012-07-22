//Input variables
float4x4 WorldViewProjection;

struct VS_INPUT
{
	float4 ObjectPos: POSITION;
	float4 VertexColor: COLOR0;
};

struct VS_OUTPUT 
{
	float4 ScreenPos:   POSITION;
	float4 VertexColor: COLOR0;
};


VS_OUTPUT SimpleVS(VS_INPUT In)
{
	VS_OUTPUT Out;

	// Move to screen space
	Out.ScreenPos = mul(In.ObjectPos, WorldViewProjection);
	Out.VertexColor = In.VertexColor;

	return Out;
}

float4 SimplePS(float4 color : COLOR0) : COLOR0
{
	return color;
}

//--------------------------------------------------------------//
// Technique Section for Simple screen transform
//--------------------------------------------------------------//
technique Simple
{
	pass SinglePass
	{
		VertexShader = compile vs_2_0 SimpleVS();
		PixelShader = compile ps_2_0 SimplePS();
	}
}
