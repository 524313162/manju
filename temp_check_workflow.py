import os, json

path = r'D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows'
files = os.listdir(path)
for f in sorted(files):
    if f.startswith('01'):
        fp = os.path.join(path, f)
        with open(fp, 'r', encoding='utf-8') as fh:
            data = json.load(fh)
        nodes = data.get('nodes', [])
        for n in nodes:
            if n.get('type') == 'KSampler':
                print('=== Standard workflow KSampler ===')
                print('inputs:')
                for inp in n.get('inputs', []):
                    print(f'  {inp["name"]}: link={inp.get("link")}, widget={"widget" in inp}')
                print('widgets_values:', n.get('widgets_values'))
                break
        break

# Also check the subgraph workflow
for f in sorted(files):
    if f.startswith('02'):
        fp = os.path.join(path, f)
        with open(fp, 'r', encoding='utf-8') as fh:
            data = json.load(fh)
        subgraph = data.get('definitions', {}).get('subgraphs', [{}])[0]
        nodes = subgraph.get('nodes', [])
        for n in nodes:
            if n.get('type') == 'KSampler':
                print('\n=== Subgraph workflow KSampler ===')
                print('inputs:')
                for inp in n.get('inputs', []):
                    print(f'  {inp["name"]}: link={inp.get("link")}, widget={"widget" in inp}')
                print('widgets_values:', n.get('widgets_values'))
                break
        break
