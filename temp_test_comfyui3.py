import json, urllib.request

# Test with what the current code generates (steps="randomize", cfg=8, sampler_name=1, etc.)
prompt = {
    '28': {'class_type': 'UNETLoader', 'inputs': {'unet_name': 'z_image_turbo_bf16.safetensors', 'weight_dtype': 'default'}},
    '30': {'class_type': 'CLIPLoader', 'inputs': {'clip_name': 'qwen_3_4b.safetensors', 'type': 'lumina2', 'device': 'default'}},
    '29': {'class_type': 'VAELoader', 'inputs': {'vae_name': 'ae.safetensors'}},
    '200': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['30', 0], 'text': 'test'}},
    '201': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['30', 0], 'text': 'test'}},
    '72': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': 1024, 'height': 1024, 'batch_size': 1}},
    '73': {'class_type': 'ModelSamplingAuraFlow', 'inputs': {'model': ['28', 0], 'shift': 3}},
    '74': {'class_type': 'KSampler', 'inputs': {'model': ['73', 0], 'positive': ['203', 0], 'negative': ['201', 0], 'latent_image': ['72', 0], 'seed': 867212146274031, 'steps': 'randomize', 'cfg': 8, 'sampler_name': 1, 'scheduler': 'res_multistep', 'denoise': 'simple'}},
    '75': {'class_type': 'VAEDecode', 'inputs': {'samples': ['74', 0], 'vae': ['29', 0]}},
    '76': {'class_type': 'SaveImage', 'inputs': {'images': ['75', 0], 'filename_prefix': 'test'}},
    '202': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['30', 0], 'text': 'test'}},
    '203': {'class_type': 'ConditioningConcat', 'inputs': {'conditioning_to': ['202', 0], 'conditioning_from': ['200', 0]}}
}

body = json.dumps({'prompt': prompt}).encode('utf-8')
req = urllib.request.Request('http://localhost:8188/prompt', data=body, headers={'Content-Type': 'application/json'}, method='POST')
try:
    resp = urllib.request.urlopen(req)
    print('Success:', resp.read().decode())
except urllib.error.HTTPError as e:
    print('Error:', e.code, e.read().decode())
