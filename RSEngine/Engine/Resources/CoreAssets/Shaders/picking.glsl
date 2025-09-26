#version 330 core
layout (location = 0) in vec3 aPosition;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
#####

#version 330 core
out vec4 FragColor;

uniform vec3 uObjectColor;

void main()
{
    FragColor = vec4(uObjectColor, 1.0);
}