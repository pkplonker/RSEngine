#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vTangent;
layout (location = 3) in vec2 vUv;
layout (location = 4) in vec3 vBiTangent;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;


out vec2 oUV;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
	oUV = vUv;
}

#####

#version 330 core

uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform sampler2D Texture2;
uniform sampler2D Texture3;
uniform sampler2D Texture4;

uniform int UseTexture;
uniform int uAlbedo;
uniform int uNormal;
uniform int uMetallic;
uniform int uRoughness;
uniform int uAO;

in vec2 oUV;

out vec4 FragColor;

void main()
{
	
	float val=0;
	if(uAlbedo +uNormal+uMetallic+uRoughness+uAO > 10)
	{
		val=1;
	}
	
    FragColor = vec4(1,0,1, 1.0f);
	
}