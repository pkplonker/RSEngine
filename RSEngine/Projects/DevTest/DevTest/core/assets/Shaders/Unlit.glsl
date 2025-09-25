#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vUv;

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
uniform int UseTexture;

in vec2 oUV;

out vec4 FragColor;

void main()
{
	if(UseTexture == 1)
	{
		float val = 88.0f/255.0f;
		FragColor = vec4(val,val,val, 1.0f);
	}
	else
	{
	    FragColor = texture(Texture0, oUV);
	}
}