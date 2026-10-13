"""Exercise actual bomb impacts and the sound toggle on the release APK.
Logs prove clip dispatch/mute, not acoustic quality of the phone speaker.
"""
import os, re, time
os.environ['BRAINROT_QA_VERSION']='0.6.6'
import qa_release_064 as q

def fire(label, muted):
    q.call('logcat','-c')
    q.tap(1030,2045)
    time.sleep(5)
    log=q.logs()
    rows=[s for s in log.splitlines() if 'BOMB_EXPLOSION_AUDIO ' in s]
    clips=re.findall(r'BOMB_EXPLOSION_AUDIO clip=(\w+)',log)
    passed=(len(rows)>=3 and (not clips if muted else len(set(clips))==3))
    if muted:passed=passed and all('muted=true' in s for s in rows)
    data=q.result(log)
    data.update(phase=label,expected_muted=muted,impacts=len(rows),clips=clips,
                pass_check=passed and not data['errors'])
    q.save('audio_'+label,log,data)
    if not data['pass_check']:raise RuntimeError('Bomb sound check failed: '+label)
    return data

q.menu(1,0);q.tap(530,2050)
deadline=time.monotonic()+20
while 'PLAYER_DROP_LANDED' not in q.logs():
    if time.monotonic()>deadline:raise RuntimeError('No landing')
    time.sleep(.5)
phases=[fire('enabled',False)]
q.tap(965,190);q.shot('audio_pause_enabled');q.tap(530,1140)
q.shot('audio_pause_muted');q.tap(530,960);time.sleep(8)
phases.append(fire('muted',True))
q.tap(965,190);q.tap(530,1140);q.shot('audio_pause_restored');q.tap(530,960);time.sleep(8)
phases.append(fire('restored',False))
q.tap(965,190);q.tap(530,1460)
q.save('audio_report','',dict(phases=phases,passed=all(p['pass_check'] for p in phases),
    acoustic_listening='Not established by log checks; user should evaluate the sound on phone speakers.'))
