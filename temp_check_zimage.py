import json
path = r'D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows\02.ZIMAGE-人物档案.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

subgraph = data['definitions']['subgraphs'][0]
nodes = subgraph['nodes']
for n in nodes:
    if n.get('type') == 'KSampler':
        print('KSampler inputs:')
        for inp in n.get('inputs', []):
            print('  %s: link=%s, widget=%s' % (inp['name'], inp.get('link'), inp.get('widget')))
        print('widgets_values:', n.get('widgets_values'))
        print()
    elif n.get('type') == 'SaveImage':
        print('SaveImage inputs:')
        for inp in n.get('inputs', []):
            print('  %s: link=%s, widget=%s' % (inp['name'], inp.get('link'), inp.get('widget')))
        print('widgets_values:', n.get('widgets_values'))
        print()
