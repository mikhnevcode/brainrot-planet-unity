"""Pause/background/settings checks through real Android UI, preserving save data."""
import re,time
import qa_release_064 as q

q.menu(1,1);q.tap(530,2050);time.sleep(5)
q.tap(965,190);time.sleep(.5);q.shot('ui_pause_initial')
time.sleep(5);q.tap(530,960);time.sleep(.5)
first=q.logs()
paused=re.findall(r'MATCH_PAUSED remaining=([\d.]+)',first)
resumed=re.findall(r'MATCH_RESUMED remaining=([\d.]+)',first)
timer_frozen=bool(paused and resumed and paused[-1]==resumed[-1])
q.tap(965,190);time.sleep(.3)
q.tap(530,1140);q.tap(530,1290);q.shot('ui_settings_toggled')
# Relaunch verifies persistence, without clearing the application data.
q.menu(1,1);q.tap(530,2050);time.sleep(4);q.tap(965,190);time.sleep(.4)
q.shot('ui_settings_after_restart')
q.tap(530,1140);q.tap(530,1290);q.shot('ui_settings_restored')
q.tap(530,960);time.sleep(.3)
q.call('shell','input','keyevent','KEYCODE_HOME');time.sleep(5)
q.call('shell','am','start','-n',q.ACTIVITY);time.sleep(2)
q.shot('ui_background_return');q.tap(530,960);time.sleep(.5)
q.call('shell','input','keyevent','KEYCODE_BACK');time.sleep(.5);q.shot('ui_back_pause')
q.call('shell','input','keyevent','KEYCODE_BACK');time.sleep(.5)
last=q.logs();pauses=re.findall(r'MATCH_PAUSED remaining=([\d.]+)',last);resumes=re.findall(r'MATCH_RESUMED remaining=([\d.]+)',last)
q.tap(965,190);time.sleep(.3);q.tap(530,1460);time.sleep(.8);q.shot('ui_return_menu')
data=q.result(first+'\n'+last)
data.update(pause_timer_frozen=timer_frozen,paused_remaining=pauses,resumed_remaining=resumes,
    background_timer_frozen=len(pauses)>=3 and len(resumes)>=3 and pauses[-2:]==resumes[-2:],
    settings_persistence='Requires visual comparison of toggled and after_restart screenshots',
    settings_restored='Requires visual check: sound on, haptics off')
q.save('ui_report',first+'\nRESTART_PHASE\n'+last,data)
