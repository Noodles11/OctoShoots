// Grey-box rock: triplanar 1 m / 4 m grid so speed and distance read clearly while tuning feel.
shader_type spatial;

uniform vec3 rock_color : source_color = vec3(0.30, 0.40, 0.46);
uniform vec3 floor_color : source_color = vec3(0.52, 0.48, 0.38);
uniform vec3 line_color : source_color = vec3(0.10, 0.15, 0.19);

varying vec3 world_pos;
varying vec3 world_normal;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

float grid(vec2 p, float size, float width) {
	vec2 q = p / size;
	vec2 g = abs(fract(q - 0.5) - 0.5) / max(fwidth(q), vec2(1e-5));
	return 1.0 - clamp(min(g.x, g.y) - width, 0.0, 1.0);
}

float triplanar_grid(vec3 p, vec3 w, float size, float width) {
	return grid(p.yz, size, width) * w.x + grid(p.xz, size, width) * w.y + grid(p.xy, size, width) * w.z;
}

void fragment() {
	vec3 w = pow(abs(world_normal), vec3(4.0));
	w /= (w.x + w.y + w.z);

	float fine = triplanar_grid(world_pos, w, 1.0, 0.4);
	float coarse = triplanar_grid(world_pos, w, 4.0, 0.9);

	float up = smoothstep(0.45, 0.9, world_normal.y);
	vec3 base = mix(rock_color, floor_color, up);
	base *= 0.88 + 0.12 * sin(world_pos.y * 0.45 + world_pos.x * 0.07);
	base = mix(base, line_color, max(fine * 0.3, coarse * 0.65));

	ALBEDO = base;
	ROUGHNESS = 0.92;
	SPECULAR = 0.2;
}
