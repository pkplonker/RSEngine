#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 vColor;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uModel;

out vec3 worldPos;
out vec3 color;

void main()
{
    worldPos = aPosition;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    color = vColor;
}

#####

#version 330 core
in vec3 worldPos;
out vec4 FragColor;
in vec3 color;

void main()
{
    FragColor = vec4(color, 1.0f);
}