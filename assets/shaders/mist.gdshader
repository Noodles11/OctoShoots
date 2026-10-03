// Limited vision: past mist_start the view blurs and fades into the water colour, gone by mist_end
// (a soft fog of war). Full-screen quad; drawn before other transparent objects.
shader_type spatial;
render_mode unshaded, depth_test_disabled, depth_draw_never, cull_disabled, fog_disabled;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D depth_tex : hint_depth_texture, filter_nearest;
uniform float mist_start = 10.0;
uniform float mist_end = 32.0;
uniform float max_blur = 5.0;
uniform vec3 mist_color : source_color = vec3(0.05, 0.16, 0.22);

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
	float depth = texture(depth_tex, SCREEN_UV).r;
	vec4 view = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
	float dist = length(view.xyz / view.w);
	float f = smoothstep(mist_start, mist_end, dist);
	vec3 blurred = textureLod(screen_tex, SCREEN_UV, f * max_blur).rgb;
	ALBEDO = mix(blurred, mist_color, f * 0.9);
}
