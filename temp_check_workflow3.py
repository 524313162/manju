import os, json

path = r'D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows'
files = os.listdir(path)

# Check all workflow files for KSampler inputs
for f in sorted(files):
    if not f.endswith('.json') or f == 'workflows.zip':
        continue
    fp = os.path.join(path, f)
    with open(fp, 'r', encoding='utf-8') as fh:
        data = json.load(fh)
    
    # Check top-level nodes first
    nodes = data.get('nodes', [])
    for n in nodes:
        if n.get('type') == 'KSampler':
            nid = n.get('id')
            print('=== %s (top-level) KSampler id=%s ===' % (f, nid))
            for inp in n.get('inputs', []):
                print('  %s: link=%s, widget=%s' % (inp['name'], inp.get('link'), 'widget' in inp))
            print('  widgets_values:', n.get('widgets_values'))
            print()
    
    # Check subgraph nodes
    subgraphs = data.get('definitions', {}).get('subgraphs', [])
    for sg in subgraphs:
        sg_nodes = sg.get('nodes', [])
        for n in sg_nodes:
            if n.get('type') == 'KSampler':
                nid = n.get('id')
                print('=== %s (subgraph) KSampler id=%s ===' % (f, nid))
                for inp in n.get('inputs', []):
                    print('  %s: link=%s, widget=%s' % (inp['name'], inp.get('link'), 'widget' in inp))
                print('  widgets_values:', n.get('widgets_values'))
                print()
