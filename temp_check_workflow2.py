import os, json

path = r'D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows'
files = os.listdir(path)
for f in sorted(files):
    if f.startswith('01'):
        fp = os.path.join(path, f)
        with open(fp, 'r', encoding='utf-8') as fh:
            data = json.load(fh)
        
        # Check if it's subgraph format
        if 'definitions' in data:
            print('=== 01 file is subgraph format ===')
            subgraph = data.get('definitions', {}).get('subgraphs', [{}])[0]
            nodes = subgraph.get('nodes', [])
        else:
            print('=== 01 file is standard format ===')
            nodes = data.get('nodes', [])
        
        for n in nodes:
            if n.get('type') == 'KSampler':
                print('KSampler inputs:')
                for inp in n.get('inputs', []):
                    print(f'  {inp["name"]}: link={inp.get("link")}, widget={"widget" in inp}')
                print('widgets_values:', n.get('widgets_values'))
                break
        break
