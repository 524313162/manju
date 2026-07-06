import os, json

path = r'D:\Program Files\ComfyUI-Installs\ComfyUI\ComfyUI\user\default\workflows'
files = os.listdir(path)
for f in sorted(files):
    if 'ACE-MUSIC' in f:
        fp = os.path.join(path, f)
        with open(fp, 'r', encoding='utf-8') as fh:
            data = json.load(fh)
        
        print('=== %s ===' % f)
        print('Has definitions:', 'definitions' in data)
        
        # Check links
        links = data.get('links', [])
        print('Top-level links:')
        for link in links:
            print(' ', link)
        
        # Check KSampler
        nodes = data.get('nodes', [])
        for n in nodes:
            if n.get('type') == 'KSampler':
                print('\nKSampler inputs:')
                for inp in n.get('inputs', []):
                    print('  %s: link=%s' % (inp['name'], inp.get('link')))
                print('widgets_values:', n.get('widgets_values'))
                break
        break
