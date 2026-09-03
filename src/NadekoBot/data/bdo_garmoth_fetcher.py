import sys
import json
import re
from curl_cffi import requests

def unflatten(data):
    if not isinstance(data, list):
        return data
    def hydrate(index, visited=None):
        if visited is None:
            visited = set()
        if not isinstance(index, int):
            return index
        if index < 0 or index >= len(data):
            return index
        if index in visited:
            return '[Circular]'
        val = data[index]
        if val is None or not isinstance(val, (dict, list)):
            return val
        visited.add(index)
        if isinstance(val, list):
            if len(val) == 2 and isinstance(val[0], str) and val[0] in ('ShallowReactive', 'Reactive', 'Ref', 'Set'):
                return hydrate(val[1], set(visited))
            return [hydrate(item, set(visited)) for item in val]
        res = {}
        for k, v in val.items():
            res[k] = hydrate(v, set(visited))
        return res
    return hydrate(0)

def fetch_character(slug_or_url):
    slug = slug_or_url.strip()
    if 'garmoth.com/character/' in slug:
        slug = slug.split('garmoth.com/character/')[-1].split('/')[0].split('?')[0]
    elif '/' in slug:
        slug = slug.split('/')[-1]
    
    url = f'https://garmoth.com/character/{slug}'
    headers = {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        'Accept-Language': 'en-US,en;q=0.5'
    }
    
    try:
        r = requests.get(url, impersonate='chrome120', headers=headers, timeout=15)
        if r.status_code != 200:
            return {'success': False, 'error': f'Garmoth returned HTTP {r.status_code}'}
        
        m = re.search(r'<script[^>]*>\s*(\[\[.*?\]\])\s*</script>', r.text, re.DOTALL)
        if not m:
            return {'success': False, 'error': 'Could not find character payload in Garmoth page'}
        
        raw = json.loads(m.group(1))
        un = unflatten(raw)
        
        if 'error' in un and un['error']:
            return {'success': False, 'error': 'Character not found on Garmoth'}
        
        gb = un.get('pinia', {}).get('GearBuilder', {})
        char_info = gb.get('character', {})
        builds = gb.get('builds', [])
        if not builds:
            return {'success': False, 'error': 'No gear builds found on this character'}
        
        b0 = builds[0]
        score = b0.get('score', {})
        gear = b0.get('gear', {})
        
        result = {
            'success': True,
            'slug': slug,
            'url': url,
            'name': char_info.get('name', 'Unknown'),
            'level': int(char_info.get('level', 0) or 0),
            'class_id': int(char_info.get('class', 0) or 0),
            'spec': char_info.get('spec', 'succ'),
            'build_name': b0.get('name', 'Current'),
            'ap': int(score.get('ap', 0) or 0),
            'aap': int(score.get('aap', 0) or 0),
            'dp': int(score.get('dp', 0) or 0),
            'score': int(score.get('score', 0) or 0),
            'totalap': float(score.get('totalap', 0) or 0),
            'adventureap': float(score.get('adventureap', 0) or 0),
            'monsterap': float(score.get('monsterap', 0) or 0),
            'humanap': float(score.get('humanap', 0) or 0),
            'kamaap': float(score.get('kamaap', 0) or 0),
            'demiap': float(score.get('demiap', 0) or 0),
            'edaniaap': float(score.get('edaniaap', 0) or 0),
            'normalap': float(score.get('normalap', 0) or 0),
            'hiddenap': float(score.get('hiddenap', 0) or 0),
            'totalaap': float(score.get('totalaap', 0) or 0),
            'acc': int(score.get('acc', 0) or 0),
            'evasion_melee': int(score.get('meev', 0) or 0),
            'evasion_ranged': int(score.get('raev', 0) or 0),
            'evasion_magic': int(score.get('maev', 0) or 0),
            'dr_melee': int(score.get('mldr', 0) or 0),
            'dr_ranged': int(score.get('radr', 0) or 0),
            'dr_magic': int(score.get('madr', 0) or 0),
            'dr_rate': float(score.get('drr', 30.0) or 30.0),
            'gear': gear
        }
        return result
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == '__main__':
    target = sys.argv[1] if len(sys.argv) > 1 else 'pravv'
    res = fetch_character(target)
    print(json.dumps(res))