using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
	public class PostProcess
	{
        private bool _init;
		internal ComputeWork BloomWork;

		bool _bloomEnabled;
		public bool BloomEnabled
		{
			get { return _bloomEnabled; }
			set
			{
				_bloomEnabled = value;

                if(_init)
                {
                    if(value)
                    {
                        CreateBloom();
                        CreateFullScreenBind();
                    }
                    else
                    {
                        DestroyBloom();
                    }
                }
			}
		}
		public float BloomThreshold { get; set; }
		public float BloomKnee { get; set; }
        public float BloomIntensity { get; set; }

        int BloomMipCount = 7;
        bool bloomTurn;
        int uniformStep;

        private Texture downTex0;
        private Texture downTex1;
        private Texture upTex;

        private TextureView downTex0View;
        private TextureView downTex1View;
        private TextureView upTexView;

        private Buffer bloomBuffer;
        private Buffer modeBuffer;

        private BloomData bloomData;
        private ModeData modeData;

        BindGroupLayout bloomLayout;
        private List<BindGroup> bloomGroups;

        private ComputePipeline bloomPipeline;

        // Full screen
        private static BindGroupLayout fsLayout;
        internal static RenderPipeline fsPipeline;

        internal BindGroup fsBindGroup;


        public PostProcess()
		{
			BloomThreshold = 1f;
			BloomKnee = 0.2f;
		}

        internal void InitPostProcess()
        {
            _init = true;

            if (fsLayout == null)
                CreateFSPipeline();

            if (BloomEnabled)
            {
                CreateBloom();
                CreateFullScreenBind();
            }
        }

        internal void RemovePostProcess()
        {
            if(BloomEnabled)
                DestroyBloom();
        }

        internal void RecreateBloom()
        {
            if (BloomEnabled && BloomWork != null)
            {
                DestroyBloom();
                CreateBloom();
                CreateFullScreenBind();
            }
        }

        private void CreateBloom()
		{
			var wgil = Game.wgil;

            const uint MODE_PREFILTER = 0;
            const uint MODE_DOWNSAMPLE = 1;
            const uint MODE_UPSAMPLE_FIRST = 2;
            const uint MODE_UPSAMPLE = 3;

            FindMaxBloomMips((uint)Game.pixelSize.X, (uint)Game.pixelSize.Y);
            uniformStep = (int)wgil.GetMinUniformOffset();
            bloomGroups = new List<BindGroup>();

            // Compute work
            BloomWork = wgil.CreateComputeWork(true);
            BloomWork.JoinComputeQueue(BloomWork_OnCompute);

            // Bind Groups
            // Layout

            BindGroupLayoutDescriptor bloomLayoutDesc = new()
            {
                Entries = new BindGroupLayoutEntry[]
                {
                new()
                {
                    BindingType = BindingType.StorageTexture,
                    ShaderStages = ShaderStages.COMPUTE,
                },
                new()
                {
                    BindingType = BindingType.Texture,
                    ShaderStages = ShaderStages.COMPUTE,
                },
                new()
                {
                    BindingType = BindingType.Texture,
                    ShaderStages = ShaderStages.COMPUTE
                },
                new()
                {
                    BindingType = BindingType.Sampler,
                    ShaderStages = ShaderStages.COMPUTE,
                },
                new()
                {
                    BindingType = BindingType.Buffer,
                    ShaderStages = ShaderStages.COMPUTE,
                },
                new()
                {
                    BindingType = BindingType.DynamicBuffer,
                    ShaderStages = ShaderStages.COMPUTE
                }
                }
            };
            bloomLayout = wgil.CreateBindGroupLayout(ref bloomLayoutDesc, true);

            Vector2 halfSize = Game.pixelSize / 2f;
            uint bloomW = (uint)halfSize.X;
            uint bloomH = (uint)halfSize.Y;

            // Textures
            downTex0 = wgil.CreateTexture(bloomW, bloomH, (uint)BloomMipCount, TextureFormat.Rgba16Float, TextureUsages.STORAGE_BINDING | TextureUsages.TEXTURE_BINDING, true);
            downTex1 = wgil.CreateTexture(bloomW, bloomH, (uint)BloomMipCount, TextureFormat.Rgba16Float, TextureUsages.STORAGE_BINDING | TextureUsages.TEXTURE_BINDING, true);
            upTex = wgil.CreateTexture(bloomW, bloomH, (uint)BloomMipCount, TextureFormat.Rgba16Float, TextureUsages.STORAGE_BINDING | TextureUsages.TEXTURE_BINDING, true);

            // Views
            downTex0View = downTex0.CreateView();
            downTex1View = downTex1.CreateView();
            upTexView = upTex.CreateView();

            // Bloom and Mode Buffer
            bloomData = new BloomData()
            {
                parameters = new Vector4(BloomThreshold, BloomThreshold - BloomKnee, BloomKnee * 2.0f, 0.25f / BloomKnee),
                combine = 0.68f,
            };

            bloomBuffer = wgil.CreateBuffer(BloomData.size, BufferUsages.UNIFORM | BufferUsages.COPY_DST, true);
            wgil.WriteBuffer(bloomBuffer, bloomData);

            int runC = (BloomMipCount - 1) * 2 + 1 + BloomMipCount;
            modeBuffer = wgil.CreateBuffer(uniformStep * runC, BufferUsages.UNIFORM | BufferUsages.COPY_DST, true);
            modeBuffer.DynamicEntrySize = 4;

            modeData = new ModeData();

            // Bind Groups
            TextureView mainRenderView = Game.resourceContext.lightRenderView;
            int bInd = 0;
            // Prefilter bind group
            bloomGroups.Add(CreateBloomGroup(downTex0.MipViews[0], mainRenderView, mainRenderView));
            modeData.modeLod = MODE_PREFILTER << 16 | 0;
            wgil.WriteBuffer(modeBuffer, modeData, uniformStep * bInd++, ModeData.size);

            // Downsample bind groups
            for (int i = 1; i < BloomMipCount; i++)
            {
                // Ping
                modeData.modeLod = MODE_DOWNSAMPLE << 16 | ((uint)i - 1);
                wgil.WriteBuffer(modeBuffer, modeData, uniformStep * bInd++, ModeData.size);
                bloomGroups.Add(CreateBloomGroup(downTex1.MipViews[i], downTex0View, mainRenderView));

                // Pong
                modeData.modeLod = MODE_DOWNSAMPLE << 16 | ((uint)i);
                wgil.WriteBuffer(modeBuffer, modeData, uniformStep * bInd++, ModeData.size);
                bloomGroups.Add(CreateBloomGroup(downTex0.MipViews[i], downTex1View, mainRenderView));
            }

            // First Upsample
            bloomGroups.Add(CreateBloomGroup(upTex.MipViews[BloomMipCount - 1], downTex0View, mainRenderView));
            modeData.modeLod = MODE_UPSAMPLE_FIRST << 16 | ((uint)BloomMipCount - 2);
            wgil.WriteBuffer(modeBuffer, modeData, uniformStep * bInd++, ModeData.size);

            //Upsample
            bloomTurn = true;
            for (int i = BloomMipCount - 2; i >= 0; i--)
            {
                if (bloomTurn)
                    bloomGroups.Add(CreateBloomGroup(downTex1.MipViews[i], downTex0View, upTexView));
                else
                    bloomGroups.Add(CreateBloomGroup(upTex.MipViews[i], downTex0View, downTex1View));

                bloomTurn = !bloomTurn;

                modeData.modeLod = MODE_UPSAMPLE << 16 | (uint)i;
                wgil.WriteBuffer(modeBuffer, modeData, uniformStep * bInd++, ModeData.size);
            }

            // Pipeline
            ComputeDescriptor bloomPipeDesc = new ComputeDescriptor()
            {
                pushRange = 0,
                BindGroupLayouts = new BindGroupLayout[] { bloomLayout }
            };
            bloomPipeline = wgil.CreateComputePipeline(BloomShader, ref bloomPipeDesc, true, true);
        }

        BindGroup CreateBloomGroup(TextureView output, TextureView input, TextureView bloom)
        {
            BindGroupDescriptor bloomGroupDesc = new()
            {
               BindGroupLayout = bloomLayout,
               Entries = new BindResource[]
               {
                    output,
                    input,
                    bloom,
                    GraphicsManager.linearSampleClamp,
                    bloomBuffer,
                    modeBuffer
               }
            };
            return Game.wgil.CreateBindGroup(ref bloomGroupDesc, true);
        }

        private void BloomWork_OnCompute(ComputeWork work)
        {
            work.SetPipeline(bloomPipeline);

            int bgIndex = 0;

            // * PreFilter
            work.SetBindGroup(0, (uint)(bgIndex * uniformStep), bloomGroups[bgIndex++]);
            var mipSize = downTex0.MipSizes[0];
            work.DispatchCompute(mipSize.Item1 / 8 + 1, mipSize.Item2 / 4 + 1, 1);

            // * Downsample
            for (int i = 1; i < BloomMipCount; i++)
            {
                mipSize = downTex0.MipSizes[i];

                // * Ping
                work.SetBindGroup(0, (uint)(bgIndex * uniformStep), bloomGroups[bgIndex++]);
                work.DispatchCompute(mipSize.Item1 / 8 + 1, mipSize.Item2 / 4 + 1, 1);

                // * Pong
                work.SetBindGroup(0, (uint)(bgIndex * uniformStep), bloomGroups[bgIndex++]);
                work.DispatchCompute(mipSize.Item1 / 8 + 1, mipSize.Item2 / 4 + 1, 1);
            }

            // * First Upsample
            work.SetBindGroup(0, (uint)(bgIndex * uniformStep), bloomGroups[bgIndex++]);
            mipSize = upTex.MipSizes[BloomMipCount - 1];
            work.DispatchCompute(mipSize.Item1 / 8 + 1, mipSize.Item2 / 4 + 1, 1);

            // * Upsample
            for (int i = BloomMipCount - 2; i >= 0; i--)
            {
                mipSize = upTex.MipSizes[i];
                work.SetBindGroup(0, (uint)(bgIndex * uniformStep), bloomGroups[bgIndex++]);
                work.DispatchCompute(mipSize.Item1 / 8 + 1, mipSize.Item2 / 4 + 1, 1);
            }
        }

        private void FindMaxBloomMips(uint width, uint height)
        {
            BloomMipCount = 0;

            while (width > 16 && height > 16)
            {
                width /= 2;
                height /= 2;

                BloomMipCount++;
            }
        }

        private void DestroyBloom()
        {
            // Textures and Views
            downTex0.DestroyMipViews();
            downTex1.DestroyMipViews();
            upTex.DestroyMipViews();

            downTex0View.Dispose();
            downTex1View.Dispose();
            upTexView.Dispose();

            downTex0.Dispose();
            downTex1.Dispose();
            upTex.Dispose();

            bloomBuffer.Dispose();
            modeBuffer.Dispose();
            bloomLayout.Dispose();
            foreach (var bg in bloomGroups)
                bg.Dispose();
            bloomPipeline.Dispose();

            BloomWork.Dispose();
            fsBindGroup.Dispose();
        }

        private void CreateFSPipeline()
        {
            var wgil = Game.wgil;

            BindGroupLayoutDescriptor texLayoutDesc = new()
            {
                Entries = new BindGroupLayoutEntry[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                     new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };

            fsLayout = Game.wgil.CreateBindGroupLayout(ref texLayoutDesc, true);

            PipelineDescriptor fsPipeDesc = new()
            {
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.TriangleList,
                    FrontFace = FrontFace.Cw,
                    CullFace = CullFace.None
                },
                BlendStates = new[]
                {
                    BlendState.OverrideBlend
                },
                VertexLayouts = new[] { GraphicsManager.fullScreenVertexLayout },
                BindGroupLayouts = new[]
                {
                    fsLayout
                },
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new TextureFormat[] { GraphicsManager.surfaceFormat }
                }
            };
            fsPipeline = wgil.CreateRenderPipeline(FullScreenQuadVertex, FullScreenQuadFragment, ref fsPipeDesc, true);
        }

        private void CreateFullScreenBind()
        {
            // FS Bind

            TextureView bloomView = bloomTurn ? upTexView : downTex1View;

            BindGroupDescriptor fsGroupDesc = new()
            {
                BindGroupLayout = fsLayout,
                Entries = new BindResource[]
                {
                    Game.resourceContext.lightRenderView,
                    bloomView,
                    GraphicsManager.linearSampleClamp
                }
            };
            fsBindGroup = Game.wgil.CreateBindGroup(ref fsGroupDesc, true);
        }

        private const string FullScreenQuadVertex = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;

layout(location = 0) out vec2 fsin_0;

void main()
{
    fsin_0 = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}
";

        private const string FullScreenQuadFragment = @"
#version 450

layout(set = 0, binding = 0) uniform texture2D SceneTex;
layout(set = 0, binding = 1) uniform texture2D BloomTex;
layout(set = 0, binding = 2) uniform sampler SceneSampler;

layout(location = 0) in vec2 fsTexCoord;
layout(location = 0) out vec4 OutputColor;

void main()
{
    vec4 hdr_color = texture(sampler2D(SceneTex, SceneSampler), fsTexCoord);
    vec4 bloom_color = texture(sampler2D(BloomTex, SceneSampler), fsTexCoord);

    vec4 color = ((bloom_color * 10) * 0.68) + hdr_color;

    float luminance = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
    float tonemappedLuminance = luminance / (luminance + 1.0);

    // reinhard tone mapping
    vec3 mapped = color.rgb * tonemappedLuminance / luminance;
    OutputColor = vec4(mapped, color.a);

    //OutputColor = vec4(color.rgb, color.a);
}
";

        private const string BloomShader = @"
// Compute Shader

const BLOOM_MIP_COUNT: i32 = 2;

const MODE_PREFILTER: u32 = 0u;
const MODE_DOWNSAMPLE: u32 = 1u;
const MODE_UPSAMPLE_FIRST: u32 = 2u;
const MODE_UPSAMPLE: u32 = 3u;

const EPSILON: f32 = 1.0e-4;

struct bloom_param {
	parameters: vec4<f32>, // (x) threshold, (y) threshold - knee, (z) knee * 2, (w) 0.25 / knee
	combine_constant: f32,
    padding1: f32,
    padding2: f32,
    padding3: f32,
};

struct mode_param {
    mode_lod: u32
}

@group(0) @binding(0) var output_texture: texture_storage_2d<rgba16float, write>;
@group(0) @binding(1) var input_texture: texture_2d<f32>;
@group(0) @binding(2) var bloom_texture: texture_2d<f32>;
@group(0) @binding(3) var samp: sampler;
@group(0) @binding(4) var<uniform> param: bloom_param;
@group(0) @binding(5) var<uniform> mp: mode_param;

// Quadratic color thresholding
// curve = (threshold - knee, knee * 2, 0.25 / knee)
fn QuadraticThreshold(color: vec4<f32>, threshold: f32, curve: vec3<f32>) -> vec4<f32>
{
	// Maximum pixel brightness
	let brightness = max(max(color.r, color.g), color.b);
	// Quadratic curve
	var rq: f32 = clamp(brightness - curve.x, 0.0, curve.y);
	rq = curve.z * (rq * rq);
	let ret_color = color * max(rq, brightness - threshold) / max(brightness, EPSILON);
	return ret_color;
}

fn Prefilter(colorp: vec4<f32>, uv: vec2<f32>) -> vec4<f32>
{
	let clamp_value = 20.0;
	var color = min(vec4<f32>(clamp_value), colorp);
	color = QuadraticThreshold(colorp, param.parameters.x, param.parameters.yzw);
	return color;
}

fn DownsampleBox13(tex: texture_2d<f32>, lod: f32, uv: vec2<f32>, texel_sizep: vec2<f32>) -> vec3<f32>
{
	// Center
	let A = textureSampleLevel(tex, samp, uv, lod).rgb;

	let texel_size = texel_sizep * 0.5; // Sample from center of texels

	// Inner box
	let B = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(-1.0, -1.0), lod).rgb;
	let C = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(-1.0, 1.0), lod).rgb;
	let D = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(1.0, 1.0), lod).rgb;
	let E = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(1.0, -1.0), lod).rgb;

	// Outer box
	let F = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(-2.0, -2.0), lod).rgb;
	let G = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(-2.0, 0.0), lod).rgb;
	let H = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(0.0, 2.0), lod).rgb;
	let I = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(2.0, 2.0), lod).rgb;
	let J = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(2.0, 2.0), lod).rgb;
	let K = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(2.0, 0.0), lod).rgb;
	let L = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(-2.0, -2.0), lod).rgb;
	let M = textureSampleLevel(tex, samp, uv + texel_size * vec2<f32>(0.0, -2.0), lod).rgb;

	// Weights
	var result: vec3<f32> = vec3<f32>(0.0);
	// Inner box
	result = result + (B + C + D + E) * 0.5;
	// Bottom-left box
	result = result + (F + G + A + M) * 0.125;
	// Top-left box
	result = result + (G + H + I + A) * 0.125;
	// Top-right box
	result = result + (A + I + J + K) * 0.125;
	// Bottom-right box
	result = result + (M + A + K + L) * 0.125;

	// 4 samples each
	result = result * 0.25;

	return result;
}

fn UpsampleTent9(tex: texture_2d<f32>, lod: f32, uv: vec2<f32>, texel_size: vec2<f32>, radius: f32) -> vec3<f32>
{
	let offset = texel_size.xyxy * vec4<f32>(1.0, 1.0, -1.0, 0.0) * radius;

	// Center
	var result: vec3<f32> = textureSampleLevel(tex, samp, uv, lod).rgb * 4.0;

	result = result + textureSampleLevel(tex, samp, uv - offset.xy, lod).rgb;
	result = result + textureSampleLevel(tex, samp, uv - offset.wy, lod).rgb * 2.0;
	result = result + textureSampleLevel(tex, samp, uv - offset.zy, lod).rgb;

	result = result + textureSampleLevel(tex, samp, uv + offset.zw, lod).rgb * 2.0;
	result = result + textureSampleLevel(tex, samp, uv + offset.xw, lod).rgb * 2.0;

	result = result + textureSampleLevel(tex, samp, uv + offset.zy, lod).rgb;
	result = result + textureSampleLevel(tex, samp, uv + offset.wy, lod).rgb * 2.0;
	result = result + textureSampleLevel(tex, samp, uv + offset.xy, lod).rgb;

	return result * (1.0 / 16.0);
}

fn combine(existing_colorp: vec3<f32>, color_to_add: vec3<f32>, combine_constant: f32) -> vec3<f32>
{
	let existing_color = existing_colorp + (-color_to_add);
	let blended_color = (combine_constant * existing_color) + color_to_add;
	return blended_color;
}

@compute @workgroup_size(8, 4, 1)
fn cs_main(@builtin(global_invocation_id) global_invocation_id: vec3<u32>)
{
	let mode_lod = mp.mode_lod;
	let mode = mp.mode_lod >> 16u;
	let lod = mp.mode_lod & 65535u;

	let out_text = output_texture;
	let in_text = input_texture;
	let bl_text = bloom_texture;

	let imgSize = textureDimensions(out_text);

	if (global_invocation_id.x <= u32(imgSize.x) && global_invocation_id.y <= u32(imgSize.y)) {

		// float combine_constant = 0.68;

		var texCoords: vec2<f32> = vec2<f32>(f32(global_invocation_id.x) / f32(imgSize.x), f32(global_invocation_id.y) / f32(imgSize.y));
		texCoords = texCoords + (1.0 / vec2<f32>(imgSize)) * 0.5;

		let texSize = vec2<f32>(textureDimensions(in_text, i32(lod)));
		var color: vec4<f32> = vec4<f32>(1.0);

		if (mode == MODE_PREFILTER)
		{
			color = vec4<f32>(DownsampleBox13(in_text, f32(lod), texCoords, 1.0 / texSize), 1.0);
			color = Prefilter(color, texCoords);
		}
		else if (mode == MODE_DOWNSAMPLE)
		{
			color = vec4<f32>(DownsampleBox13(in_text, f32(lod), texCoords, 1.0 / texSize), 1.0);
		}
		else if (mode == MODE_UPSAMPLE_FIRST)
		{
			let bloomTexSize = textureDimensions(in_text, i32(lod) + 1);
			let sampleScale = 1.0;
			let upsampledTexture = UpsampleTent9(in_text, f32(lod) + 1.0, texCoords, 1.0 / vec2<f32>(bloomTexSize), sampleScale);

			let existing = textureSampleLevel(in_text, samp, texCoords, f32(lod)).rgb;
			color = vec4<f32>(combine(existing, upsampledTexture, param.combine_constant), 1.0);
		}
		else if (mode == MODE_UPSAMPLE)
		{
			let bloomTexSize = textureDimensions(bl_text, i32(lod) + 1);
			let sampleScale = 1.0;
			let upsampledTexture = UpsampleTent9(bl_text, f32(lod) + 1.0, texCoords, 1.0 / vec2<f32>(bloomTexSize), sampleScale);

			let existing = textureSampleLevel(in_text, samp, texCoords, f32(lod)).rgb;
			color = vec4<f32>(combine(existing, upsampledTexture, param.combine_constant), 1.0);
		}
		textureStore(out_text, vec2<i32>(global_invocation_id.xy), color);
	}
}
";
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BloomData
    {
        public const int size = 32;

        public Vector4 parameters;
        public float combine;
        public float padding1;
        public float padding2;
        public float padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ModeData
    {
        public const int size = 4;

        public uint modeLod;
    }
}

