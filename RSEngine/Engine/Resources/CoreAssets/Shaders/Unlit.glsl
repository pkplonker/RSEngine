#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vUv;
layout (location = 2) in vec4 vColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 oUV;
out vec4 oColor; 
void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
	oUV = vUv;
    oColor = vColor;
}

#####

#version 330 core

uniform sampler2D Texture0;
uniform int UseTexture;

in vec2 oUV;

out vec4 FragColor;

void main()
{
	if (UseTexture == 0)
    {
        FragColor = oColor;
    }
    else
    {
        FragColor = texture(Texture0, oUV);
    }
}