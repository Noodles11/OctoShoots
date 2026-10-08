// A pufferling's needle: a pale bone spike with a corrupted lime-and-red glow along it, bright at the point. The real
// ones (the 8 in the swim plane) stay solid; the decorative ones flying up, down and diagonally (`decor`) are dimmer and
// fade out over their flight.
shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_opaque;

instance uniform float fade = 1.0;
instance uniform float decor = 0.0;

void fragment() {
	// UV.y runs along the cylinder: 0 at the point (its top), 1 at the base.
	float tip = 1.0 - UV.y;
	vec3 bone = vec3(1.0, 0.96, 0.86);
	vec3 leak = mix(vec3(1.0, 0.3, 0.36), vec3(0.45, 1.0, 0.25), smoothstep(0.3, 0.9, tip));
	vec3 col = mix(bone, leak, 0.45) * (1.4 + 1.2 * tip);
	ALBEDO = col;
	ALPHA = clamp(fade * mix(1.0, 0.6, decor) * (0.7 + 0.3 * tip), 0.0, 1.0);
}
