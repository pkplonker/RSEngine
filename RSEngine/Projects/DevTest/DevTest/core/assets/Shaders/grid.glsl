#version 330 core
layout (location = 0) in vec3 aPosition;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uModel;

out vec3 worldPos;

void main()
{
    worldPos = aPosition;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}

#####

#version 330 core
in vec3 worldPos;
out vec4 FragColor;

uniform vec3 uGridColor;
uniform float uGridScale;
uniform float uFadeDistance;

void main()
{
    vec2 grid = abs(fract(worldPos.xz * uGridScale) - 0.5) / fwidth(worldPos.xz * uGridScale);
    float line = min(grid.x, grid.y);
    float alpha = 1.0 - min(line, 1.0);
    
    // Fade out grid at distance
    float distance = length(worldPos);
    alpha *= exp(-distance * uFadeDistance);
    
    // Make grid more opaque near origin
    float centerBoost = exp(-length(worldPos.xz) * 0.1);
    alpha = mix(alpha, alpha * 2.0, centerBoost);
    
    FragColor = vec4(uGridColor, alpha * 0.5);
}