// Unlit, vertex-colored, alpha or additive — telegraphs, rings, slashes, beams, debris.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_mix;

uniform vec4 color : source_color = vec4(1.0);
uniform sampler2D tex : source_color, hint_default_white;

void fragment() {
	vec4 t = texture(tex, UV);
	ALBEDO = COLOR.rgb * color.rgb * t.rgb;
	ALPHA = COLOR.a * color.a * t.a;
}
