"""v0.8.4 device profile: same non-destructive fixture, separate evidence."""
import run_rollo_sculpt_device_qa as Q
Q.BASE=Q.ROOT/'Builds/RolloFace'
Q.VERSION='0.8.4-rollo-face-test';Q.VERSION_CODE=74;Q.QA_MARKER='rollo07';Q.LABEL='ROLLO_FACE'
if __name__=='__main__':Q.main()
