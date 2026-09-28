// Pixel sprite on a quad: nearest filtering, hard alpha cut (opaque pipeline = clean depth sorting
// between layers and scenery), flash to a solid color, and dithered fade for blink/death.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque;

uniform sampler2D tex : source_color, filter_nearest;
uniform vec4 tint : source_color = vec4(1.0);
uniform vec4 flash_color : source_color = vec4(1.0);
uniform float flash = 0.0;
uniform float fade = 1.0;
uniform float flip = 0.0;

float bayer4(vec2 p) {
	int x = int(mod(p.x, 4.0));
	int y = int(mod(p.y, 4.0));
	int i = x + y * 4;
	float m[16] = float[](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
	return (m[i] + 0.5) / 16.0;
}

void fragment() {
	vec2 uv = vec2(mix(UV.x, 1.0 - UV.x, flip), UV.y);
	vec4 c = texture(tex, uv);
	if (c.a < 0.5 || fade < bayer4(FRAGCOORD.xy)) {
		discard;
	}
	ALBEDO = mix(c.rgb * tint.rgb, flash_color.rgb, flash);
}
